import json
import socket
import subprocess
import sys
import uuid
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from ipaddress import ip_address
from pathlib import Path
from typing import Any


@dataclass
class ImageConfig:
    dockerfile: str
    context: str


@dataclass
class RuntimeConfig:
    bind_host: str
    bind_port: int
    auth_token: str
    repo_root: str
    allowed_images: dict[str, ImageConfig]
    allowed_mount_roots: list[str]
    default_timeout_seconds: int
    max_output_bytes: int


@dataclass
class CommandResult:
    returncode: int
    stdout: str
    stderr: str


RUN_RECORDS: dict[str, dict[str, Any]] = {}


def choose_bind_host(candidates: list[str]) -> str:
    for candidate in candidates:
        try:
            parsed = ip_address(candidate)
        except ValueError:
            continue

        if parsed.version == 4 and parsed.is_private and not parsed.is_loopback:
            return candidate

    raise ValueError("No private LAN IPv4 address is available")


def discover_bind_host_candidates() -> list[str]:
    candidates: list[str] = []

    try:
        hostname = socket.gethostname()
        for info in socket.getaddrinfo(hostname, None, socket.AF_INET):
            address = info[4][0]
            if address not in candidates:
                candidates.append(address)
    except OSError:
        pass

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as probe:
            probe.connect(("8.8.8.8", 80))
            address = probe.getsockname()[0]
            if address not in candidates:
                candidates.append(address)
    except OSError:
        pass

    return candidates


def resolve_bind_host(bind_host: str, candidates: list[str] | None = None) -> str:
    if bind_host == "auto-private-ipv4":
        return choose_bind_host(candidates if candidates is not None else discover_bind_host_candidates())
    return bind_host


def _required_string(payload: dict[str, Any], field_name: str) -> str:
    value = payload.get(field_name)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{field_name} must be a non-blank string")
    return value


def _required_positive_int(payload: dict[str, Any], field_name: str) -> int:
    value = payload.get(field_name)
    if not isinstance(value, int) or isinstance(value, bool) or value <= 0:
        raise ValueError(f"{field_name} must be a positive integer")
    return value


def _load_allowed_images(payload: dict[str, Any]) -> dict[str, ImageConfig]:
    allowed_images = payload.get("allowed_images")
    if not isinstance(allowed_images, dict) or not allowed_images:
        raise ValueError("allowed_images must be a non-empty object")

    result: dict[str, ImageConfig] = {}
    for name, image in allowed_images.items():
        if not isinstance(name, str) or not name.strip():
            raise ValueError("allowed_images keys must be non-blank strings")
        if not isinstance(image, dict):
            raise ValueError("allowed_images entries must be objects")

        dockerfile = _required_string(image, "dockerfile")
        context = _required_string(image, "context")
        result[name] = ImageConfig(dockerfile=dockerfile, context=context)

    return result


def _load_allowed_mount_roots(payload: dict[str, Any]) -> list[str]:
    allowed_mount_roots = payload.get("allowed_mount_roots")
    if not isinstance(allowed_mount_roots, list) or not allowed_mount_roots:
        raise ValueError("allowed_mount_roots must be a non-empty list")
    if any(not isinstance(root, str) or not root.strip() for root in allowed_mount_roots):
        raise ValueError("allowed_mount_roots entries must be non-blank strings")
    return allowed_mount_roots


def load_config(path: str | Path) -> RuntimeConfig:
    with open(path, "r", encoding="utf-8") as handle:
        payload = json.load(handle)

    bind_host = _required_string(payload, "bind_host")
    auth_token = _required_string(payload, "auth_token")
    repo_root = _required_string(payload, "repo_root")
    bind_port = _required_positive_int(payload, "bind_port")
    if bind_port > 65535:
        raise ValueError("bind_port must be between 1 and 65535")

    return RuntimeConfig(
        bind_host=bind_host,
        bind_port=bind_port,
        auth_token=auth_token,
        repo_root=repo_root,
        allowed_images=_load_allowed_images(payload),
        allowed_mount_roots=_load_allowed_mount_roots(payload),
        default_timeout_seconds=_required_positive_int(
            payload,
            "default_timeout_seconds",
        ),
        max_output_bytes=_required_positive_int(payload, "max_output_bytes"),
    )


def validate_run_request(config: RuntimeConfig, request: dict[str, Any]) -> dict[str, Any]:
    image = str(request.get("image", ""))
    if image not in config.allowed_images:
        raise ValueError(f"Image {image!r} is not allowlisted")

    argv = request.get("argv")
    if not isinstance(argv, list):
        raise ValueError("argv must be a list of non-blank strings")

    for arg in argv:
        if not isinstance(arg, str) or not arg.strip():
            raise ValueError("argv must contain only non-blank strings")

    return request


def authorize(header: str | None, expected_token: str) -> None:
    if header != f"Bearer {expected_token}":
        raise PermissionError("Unauthorized")


def build_docker_run_command(
    config: RuntimeConfig,
    image: str,
    argv: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    validate_run_request(config, {"image": image, "argv": argv})

    effective_timeout = config.default_timeout_seconds if timeout_seconds is None else timeout_seconds
    if (
        not isinstance(effective_timeout, int)
        or isinstance(effective_timeout, bool)
        or effective_timeout <= 0
    ):
        raise ValueError("timeout_seconds must be positive")

    repo_root = str(Path(config.repo_root))
    return [
        "docker",
        "run",
        "--rm",
        "-v",
        f"{repo_root}:{repo_root}",
        "-v",
        "/tmp:/tmp",
        "-w",
        repo_root,
        image,
        *argv,
    ]


def _run_subprocess(command: list[str], timeout_seconds: int) -> CommandResult:
    completed = subprocess.run(
        command,
        capture_output=True,
        text=True,
        timeout=timeout_seconds,
        check=False,
    )
    return CommandResult(
        returncode=completed.returncode,
        stdout=completed.stdout,
        stderr=completed.stderr,
    )


def _trim_output(value: str, max_bytes: int) -> str:
    encoded = value.encode("utf-8")
    if len(encoded) <= max_bytes:
        return value
    return encoded[:max_bytes].decode("utf-8", errors="replace")


def runtime_health(config: RuntimeConfig) -> dict[str, Any]:
    return {
        "status": "ok",
        "bind_host": config.bind_host,
        "bind_port": config.bind_port,
        "repo_root": config.repo_root,
    }


def list_runtime_images(config: RuntimeConfig) -> dict[str, Any]:
    return {
        "images": sorted(config.allowed_images.keys()),
    }


def build_runtime_image(
    config: RuntimeConfig,
    arguments: dict[str, Any],
    runner=_run_subprocess,
) -> dict[str, Any]:
    image = _required_string(arguments, "image")
    if image not in config.allowed_images:
        raise ValueError(f"Image {image!r} is not allowlisted")

    image_config = config.allowed_images[image]
    timeout_seconds = arguments.get("timeout_seconds", config.default_timeout_seconds)
    if not isinstance(timeout_seconds, int) or isinstance(timeout_seconds, bool) or timeout_seconds <= 0:
        raise ValueError("timeout_seconds must be positive")

    command = [
        "docker",
        "build",
        "-f",
        image_config.dockerfile,
        "-t",
        image,
        image_config.context,
    ]
    result = runner(command, timeout_seconds)
    return {
        "image": image,
        "command": command,
        "returncode": result.returncode,
        "stdout": _trim_output(result.stdout, config.max_output_bytes),
        "stderr": _trim_output(result.stderr, config.max_output_bytes),
    }


def run_sut_command(
    config: RuntimeConfig,
    arguments: dict[str, Any],
    runner=_run_subprocess,
    id_factory=lambda: uuid.uuid4().hex,
) -> dict[str, Any]:
    run_id = id_factory()
    image = _required_string(arguments, "image")
    argv = arguments.get("argv")
    timeout_seconds = arguments.get("timeout_seconds", config.default_timeout_seconds)
    command = build_docker_run_command(config, image, argv, timeout_seconds)
    result = runner(command, timeout_seconds)
    status = "completed" if result.returncode == 0 else "failed"
    record = {
        "run_id": run_id,
        "status": status,
        "image": image,
        "argv": argv,
        "timeout_seconds": timeout_seconds,
        "command": command,
        "returncode": result.returncode,
        "stdout": _trim_output(result.stdout, config.max_output_bytes),
        "stderr": _trim_output(result.stderr, config.max_output_bytes),
    }
    RUN_RECORDS[run_id] = record
    return record


def get_run_result(arguments: dict[str, Any]) -> dict[str, Any]:
    run_id = _required_string(arguments, "run_id")
    if run_id not in RUN_RECORDS:
        raise KeyError(f"Unknown run_id {run_id!r}")
    return RUN_RECORDS[run_id]


def dispatch_tool(
    config: RuntimeConfig,
    authorization_header: str | None,
    request: dict[str, Any],
    runner=_run_subprocess,
    id_factory=lambda: uuid.uuid4().hex,
) -> dict[str, Any]:
    authorize(authorization_header, config.auth_token)
    tool = _required_string(request, "tool")
    arguments = request.get("arguments", {})
    if not isinstance(arguments, dict):
        raise ValueError("arguments must be an object")

    if tool == "runtime_health":
        return runtime_health(config)
    if tool == "list_runtime_images":
        return list_runtime_images(config)
    if tool == "build_runtime_image":
        return build_runtime_image(config, arguments, runner=runner)
    if tool == "run_sut_command":
        return run_sut_command(config, arguments, runner=runner, id_factory=id_factory)
    if tool == "get_run_result":
        return get_run_result(arguments)
    raise ValueError(f"Unknown tool {tool!r}")


def handle_http_tool_request(
    config: RuntimeConfig,
    authorization_header: str | None,
    content_length_header: str | None,
    read_body,
    runner=_run_subprocess,
) -> tuple[int, dict[str, Any]]:
    try:
        content_length = int(content_length_header or "0")
        payload = json.loads(read_body(content_length).decode("utf-8"))
        response = dispatch_tool(
            config,
            authorization_header,
            payload,
            runner=runner,
        )
        return 200, response
    except PermissionError as error:
        return 401, {"error": str(error)}
    except Exception as error:
        return 400, {"error": str(error)}


def create_http_handler(config: RuntimeConfig, runner=_run_subprocess):
    class Handler(BaseHTTPRequestHandler):
        def do_POST(self) -> None:
            if self.path != "/tool":
                self.send_error(404, "Not Found")
                return

            status, payload = handle_http_tool_request(
                config,
                self.headers.get("Authorization"),
                self.headers.get("Content-Length"),
                self.rfile.read,
                runner=runner,
            )
            body = json.dumps(payload).encode("utf-8")
            self.send_response(status)

            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

    return Handler


def serve_http(config: RuntimeConfig) -> None:
    config.bind_host = resolve_bind_host(config.bind_host)
    server = ThreadingHTTPServer((config.bind_host, config.bind_port), create_http_handler(config))
    server.serve_forever()


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print("usage: server.py CONFIG_PATH", file=sys.stderr)
        return 2
    serve_http(load_config(argv[1]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))

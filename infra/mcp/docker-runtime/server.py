import argparse
import json
import os
import re
import socket
import subprocess
import sys
import uuid
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from ipaddress import ip_address
from pathlib import Path
from typing import Any
from urllib.parse import quote


@dataclass
class ImageConfig:
    dockerfile: str
    context: str


@dataclass
class ToolConfig:
    executable: str


@dataclass
class RuntimeConfig:
    bind_host: str
    bind_port: int
    auth_token: str
    repo_root: str
    allowed_images: dict[str, ImageConfig]
    allowed_tools: dict[str, ToolConfig]
    allowed_mount_roots: list[str]
    default_timeout_seconds: int
    max_output_bytes: int
    backend: str = "docker"


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


VALID_BACKENDS = ("docker", "local")


def _load_backend(payload: dict[str, Any]) -> str:
    backend = payload.get("backend", "docker")
    if backend not in VALID_BACKENDS:
        raise ValueError(f"backend must be one of {VALID_BACKENDS}")
    return backend


def _load_allowed_images(payload: dict[str, Any], backend: str = "docker") -> dict[str, ImageConfig]:
    allowed_images = payload.get("allowed_images")
    if not isinstance(allowed_images, dict) or not allowed_images:
        raise ValueError("allowed_images must be a non-empty object")

    result: dict[str, ImageConfig] = {}
    for name, image in allowed_images.items():
        if not isinstance(name, str) or not name.strip():
            raise ValueError("allowed_images keys must be non-blank strings")
        if not isinstance(image, dict):
            raise ValueError("allowed_images entries must be objects")

        if backend == "local":
            dockerfile = image.get("dockerfile", "")
            context = image.get("context", "")
            if not isinstance(dockerfile, str) or not isinstance(context, str):
                raise ValueError("allowed_images dockerfile/context must be strings when present")
        else:
            dockerfile = _required_string(image, "dockerfile")
            context = _required_string(image, "context")
        result[name] = ImageConfig(dockerfile=dockerfile, context=context)

    return result


def _load_allowed_tools(payload: dict[str, Any]) -> dict[str, ToolConfig]:
    allowed_tools = payload.get("allowed_tools")
    if not isinstance(allowed_tools, dict) or not allowed_tools:
        raise ValueError("allowed_tools must be a non-empty object")

    result: dict[str, ToolConfig] = {}
    for name, tool in allowed_tools.items():
        if not isinstance(name, str) or not name.strip():
            raise ValueError("allowed_tools keys must be non-blank strings")
        if not isinstance(tool, dict):
            raise ValueError("allowed_tools entries must be objects")
        result[name] = ToolConfig(executable=_required_string(tool, "executable"))

    return result


def _load_allowed_mount_roots(payload: dict[str, Any]) -> list[str]:
    allowed_mount_roots = payload.get("allowed_mount_roots")
    if not isinstance(allowed_mount_roots, list) or not allowed_mount_roots:
        raise ValueError("allowed_mount_roots must be a non-empty list")
    if any(not isinstance(root, str) or not root.strip() for root in allowed_mount_roots):
        raise ValueError("allowed_mount_roots entries must be non-blank strings")
    return allowed_mount_roots


def load_config(path: str | Path) -> RuntimeConfig:
    with open(path, "r", encoding="utf-8-sig") as handle:
        payload = json.load(handle)

    bind_host = _required_string(payload, "bind_host")
    auth_token = _required_string(payload, "auth_token")
    repo_root = _required_string(payload, "repo_root")
    bind_port = _required_positive_int(payload, "bind_port")
    if bind_port > 65535:
        raise ValueError("bind_port must be between 1 and 65535")

    backend = _load_backend(payload)

    return RuntimeConfig(
        bind_host=bind_host,
        bind_port=bind_port,
        auth_token=auth_token,
        repo_root=repo_root,
        allowed_images=_load_allowed_images(payload, backend),
        allowed_tools=_load_allowed_tools(payload),
        allowed_mount_roots=_load_allowed_mount_roots(payload),
        default_timeout_seconds=_required_positive_int(
            payload,
            "default_timeout_seconds",
        ),
        max_output_bytes=_required_positive_int(payload, "max_output_bytes"),
        backend=backend,
    )


def validate_run_request(config: RuntimeConfig, request: dict[str, Any]) -> list[str]:
    image = str(request.get("image", ""))
    if image not in config.allowed_images:
        raise ValueError(f"Image {image!r} is not allowlisted")

    if "argv" in request:
        raise ValueError("raw argv is not accepted; use an allowlisted tool and args")

    tool = str(request.get("tool", ""))
    if tool not in config.allowed_tools:
        raise ValueError(f"Tool {tool!r} is not allowlisted")

    args = request.get("args", [])
    if not isinstance(args, list):
        raise ValueError("args must be a list of non-blank strings")

    normalized_args: list[str] = []
    for arg in args:
        if not isinstance(arg, str) or not arg.strip():
            raise ValueError("args must contain only non-blank strings")
        _validate_tool_argument(config, arg)
        normalized_args.append(_normalize_tool_argument(config, arg))

    return [config.allowed_tools[tool].executable, *normalized_args]


def authorize(header: str | None, expected_token: str) -> None:
    if header != f"Bearer {expected_token}":
        raise PermissionError("Unauthorized")


WINDOWS_PATH_PATTERN = re.compile(r"^([A-Za-z]):[\\/](.*)$")


def _validate_tool_argument(config: RuntimeConfig, arg: str) -> None:
    if arg in {"-c", "/c", "-m", "/m"}:
        raise ValueError("tool arguments must not request shell or module execution")
    if arg.lower().endswith((".py", ".pyc")):
        raise ValueError("tool arguments must not contain script path values")
    if (arg.startswith("/") or WINDOWS_PATH_PATTERN.match(arg)) and not _is_allowed_data_path(config, arg):
        raise ValueError("absolute tool arguments must be under an allowed mount root")
    if ".." in re.split(r"[\\/]", arg):
        raise ValueError("tool arguments must not contain path traversal")
    if any(operator in arg for operator in [";", "&&", "||", "|"]) or "$(" in arg or "`" in arg:
        raise ValueError("tool arguments must not contain shell operators")


def _normalize_tool_argument(config: RuntimeConfig, arg: str) -> str:
    if (arg.startswith("/") or WINDOWS_PATH_PATTERN.match(arg)) and _is_allowed_data_path(config, arg):
        return translate_mount_target(arg)
    return arg


def _is_allowed_data_path(config: RuntimeConfig, path: str) -> bool:
    translated_path = os.path.normpath(translate_mount_target(path))
    roots = [config.repo_root, *config.allowed_mount_roots]
    if not _is_windows_path(config.repo_root):
        roots.append("/tmp")

    for root in roots:
        translated_root = os.path.normpath(translate_mount_target(root))
        try:
            if os.path.commonpath([translated_path, translated_root]) == translated_root:
                return True
        except ValueError:
            continue
    return False


def translate_mount_target(path: str) -> str:
    match = WINDOWS_PATH_PATTERN.match(path)
    if match is None:
        return path
    drive = match.group(1).lower()
    rest = match.group(2).replace("\\", "/")
    return f"/mnt/{drive}/{rest}"


def _is_windows_path(path: str) -> bool:
    return WINDOWS_PATH_PATTERN.match(path) is not None


def build_docker_run_command(
    config: RuntimeConfig,
    image: str,
    tool: str,
    args: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    command_args = validate_run_request(config, {"image": image, "tool": tool, "args": args})

    effective_timeout = config.default_timeout_seconds if timeout_seconds is None else timeout_seconds
    if (
        not isinstance(effective_timeout, int)
        or isinstance(effective_timeout, bool)
        or effective_timeout <= 0
    ):
        raise ValueError("timeout_seconds must be positive")

    roots: list[str] = []
    for root in [config.repo_root, *config.allowed_mount_roots]:
        if root not in roots:
            roots.append(root)
    # Legacy compatibility: Linux hosts always mounted /tmp; Windows hosts must not.
    if not _is_windows_path(config.repo_root) and "/tmp" not in roots:
        roots.append("/tmp")

    command = ["docker", "run", "--rm"]
    for root in roots:
        command += ["-v", f"{root}:{translate_mount_target(root)}"]
    command += ["-w", translate_mount_target(config.repo_root), image, *command_args]
    return command


def build_local_run_command(
    config: RuntimeConfig,
    image: str,
    tool: str,
    args: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    command_args = validate_run_request(config, {"image": image, "tool": tool, "args": args})

    effective_timeout = config.default_timeout_seconds if timeout_seconds is None else timeout_seconds
    if (
        not isinstance(effective_timeout, int)
        or isinstance(effective_timeout, bool)
        or effective_timeout <= 0
    ):
        raise ValueError("timeout_seconds must be positive")

    return list(command_args)


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
    if config.backend == "local":
        raise ValueError("build_runtime_image is not supported when backend is 'local'")
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
    tool = _required_string(arguments, "tool")
    args = arguments.get("args", [])
    timeout_seconds = arguments.get("timeout_seconds", config.default_timeout_seconds)
    if config.backend == "local":
        command = build_local_run_command(config, image, tool, args, timeout_seconds)
    else:
        command = build_docker_run_command(config, image, tool, args, timeout_seconds)
    result = runner(command, timeout_seconds)
    status = "completed" if result.returncode == 0 else "failed"
    record = {
        "run_id": run_id,
        "status": status,
        "image": image,
        "tool": tool,
        "args": args,
        "timeout_seconds": timeout_seconds,
        "command": command,
        "returncode": result.returncode,
        "stdout": _trim_output(result.stdout, config.max_output_bytes),
        "stderr": _trim_output(result.stderr, config.max_output_bytes),
    }
    RUN_RECORDS[run_id] = record
    print(
        f"run_sut_command run_id={run_id} status={status} image={image} returncode={result.returncode}",
        flush=True,
    )
    return record


def get_run_result(arguments: dict[str, Any]) -> dict[str, Any]:
    run_id = _required_string(arguments, "run_id")
    if run_id not in RUN_RECORDS:
        raise KeyError(f"Unknown run_id {run_id!r}")
    return RUN_RECORDS[run_id]


def kill_run(arguments: dict[str, Any]) -> dict[str, Any]:
    run_id = _required_string(arguments, "run_id")
    record = RUN_RECORDS.get(run_id)
    if record is None:
        return {
            "run_id": run_id,
            "status": "not_found",
            "killed": False,
            "message": f"Unknown run_id {run_id!r}",
        }

    return {
        "run_id": run_id,
        "status": "not_running",
        "killed": False,
        "existing_status": record.get("status", ""),
        "message": "run_sut_command is synchronous; completed run records have no live backend handle to kill",
    }


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
    if tool == "kill_run":
        return kill_run(arguments)
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
    print(
        f"docker-runtime MCP server ({config.backend} backend) listening on "
        f"http://{config.bind_host}:{config.bind_port}",
        flush=True,
    )
    server.serve_forever()


def build_profile_uri(
    runtime_key: str,
    endpoint: str,
    image: str,
    python: str,
    tool: str,
    local: str,
    auth_token_env: str | None = None,
) -> str:
    normalized_key = runtime_key.strip().lower()
    query = [
        ("image", image),
        ("tool", tool),
        ("local", local),
        ("python", python),
        ("endpoint", endpoint),
    ]
    if auth_token_env and auth_token_env.strip():
        query.append(("authTokenEnv", auth_token_env.strip()))

    encoded = "&".join(f"{name}={quote(value, safe='')}" for name, value in query)
    return f"docker-mcp://{normalized_key}?{encoded}"


def main(argv: list[str], serve=serve_http, out=print) -> int:
    if len(argv) == 2 and not argv[1].startswith("-"):
        serve(load_config(argv[1]))
        return 0

    parser = argparse.ArgumentParser(prog=Path(argv[0]).name)
    subcommands = parser.add_subparsers(dest="command", required=True)

    serve_parser = subcommands.add_parser("serve", help="serve Docker runtime MCP over HTTP")
    serve_parser.add_argument("--config", required=True, help="path to Docker runtime MCP JSON config")

    profile_parser = subcommands.add_parser(
        "profile-uri",
        help="print a docker-mcp:// runtime profile URI for MetBench appsettings.local.json",
    )
    profile_parser.add_argument("--runtime-key", required=True)
    profile_parser.add_argument("--endpoint", required=True)
    profile_parser.add_argument("--image", required=True)
    profile_parser.add_argument("--tool", required=True)
    profile_parser.add_argument("--local", required=True)
    profile_parser.add_argument("--python", required=True)
    profile_parser.add_argument("--auth-token-env")

    try:
        args = parser.parse_args(argv[1:])
    except SystemExit as error:
        return int(error.code)

    if args.command == "serve":
        serve(load_config(args.config))
        return 0
    if args.command == "profile-uri":
        out(
            build_profile_uri(
                args.runtime_key,
                args.endpoint,
                args.image,
                args.python,
                args.tool,
                args.local,
                args.auth_token_env,
            )
        )
        return 0

    parser.print_usage(sys.stderr)
    return 2


def legacy_main(argv: list[str]) -> int:
    if len(argv) != 2:
        print("usage: server.py CONFIG_PATH", file=sys.stderr)
        return 2
    serve_http(load_config(argv[1]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))

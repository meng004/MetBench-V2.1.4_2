import argparse
import json
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Any
from urllib import request as urllib_request
from urllib.error import HTTPError


BUSINESS_TOOLS = (
    "business_health",
    "submit_run",
    "get_job",
    "get_result",
    "get_evidence",
    "cancel_job",
)

RESERVED_ARGUMENT_FRAGMENTS = (
    "argv",
    "command",
    "manifest",
    "artifact",
    "root",
    "path",
    "workingdirectory",
    "executable",
)


@dataclass
class BusinessMcpConfig:
    bind_host: str
    bind_port: int
    auth_token: str
    api_base_url: str
    api_token: str
    default_timeout_seconds: int


def _required_string(payload: dict[str, Any], field_name: str) -> str:
    value = payload.get(field_name)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{field_name} must be a non-blank string")
    return value.strip()


def _required_positive_int(payload: dict[str, Any], field_name: str) -> int:
    value = payload.get(field_name)
    if not isinstance(value, int) or isinstance(value, bool) or value <= 0:
        raise ValueError(f"{field_name} must be a positive integer")
    return value


def load_config(path: str) -> BusinessMcpConfig:
    with open(path, "r", encoding="utf-8") as handle:
        payload = json.load(handle)

    bind_port = _required_positive_int(payload, "bind_port")
    if bind_port > 65535:
        raise ValueError("bind_port must be between 1 and 65535")

    return BusinessMcpConfig(
        bind_host=_required_string(payload, "bind_host"),
        bind_port=bind_port,
        auth_token=_required_string(payload, "auth_token"),
        api_base_url=_required_string(payload, "api_base_url").rstrip("/"),
        api_token=_required_string(payload, "api_token"),
        default_timeout_seconds=_required_positive_int(payload, "default_timeout_seconds"),
    )


def authorize(header: str | None, expected_token: str) -> None:
    if header != f"Bearer {expected_token}":
        raise PermissionError("Unauthorized")


def business_health(config: BusinessMcpConfig) -> dict[str, Any]:
    return {
        "status": "ok",
        "api_base_url": config.api_base_url,
        "tools": list(BUSINESS_TOOLS),
    }


def dispatch_tool(
    config: BusinessMcpConfig,
    authorization_header: str | None,
    request: dict[str, Any],
    api_client=None,
) -> dict[str, Any]:
    authorize(authorization_header, config.auth_token)
    tool = _required_string(request, "tool")
    arguments = request.get("arguments", {})
    if not isinstance(arguments, dict):
        raise ValueError("arguments must be an object")
    if tool not in BUSINESS_TOOLS:
        raise ValueError(f"Unknown tool {tool!r}")

    client = api_client or (lambda method, path, payload=None: call_rest_api(config, method, path, payload))

    if tool == "business_health":
        return business_health(config)
    if tool == "submit_run":
        return submit_run(arguments, client)
    if tool == "get_job":
        return client("GET", f"/api/v1/systemmt/jobs/{_required_job_id(arguments)}")
    if tool == "get_result":
        return client("GET", f"/api/v1/systemmt/jobs/{_required_job_id(arguments)}/result")
    if tool == "get_evidence":
        return client("GET", f"/api/v1/systemmt/jobs/{_required_job_id(arguments)}/evidence")
    if tool == "cancel_job":
        return client("DELETE", f"/api/v1/systemmt/jobs/{_required_job_id(arguments)}")

    raise ValueError(f"Unknown tool {tool!r}")


def submit_run(arguments: dict[str, Any], api_client) -> dict[str, Any]:
    _reject_infrastructure_arguments(arguments)
    mr_id = _required_string(arguments, "mr_id")
    parameter_overrides = arguments.get("parameter_overrides")
    if parameter_overrides is not None:
        if not isinstance(parameter_overrides, dict):
            raise ValueError("parameter_overrides must be an object")
        _reject_infrastructure_arguments(parameter_overrides)
        for key, value in parameter_overrides.items():
            if not isinstance(key, str) or not key.strip():
                raise ValueError("parameter_overrides keys must be non-blank strings")
            if not isinstance(value, str) or not value.strip():
                raise ValueError("parameter_overrides values must be non-blank strings")

    payload: dict[str, Any] = {"mrId": mr_id}
    if parameter_overrides:
        payload["parameterOverrides"] = dict(parameter_overrides)
    return api_client("POST", "/api/v1/systemmt/jobs", payload)


def call_rest_api(
    config: BusinessMcpConfig,
    method: str,
    path: str,
    payload: dict[str, Any] | None = None,
) -> dict[str, Any]:
    body = None if payload is None else json.dumps(payload).encode("utf-8")
    req = urllib_request.Request(
        f"{config.api_base_url}{path}",
        data=body,
        method=method,
        headers={
            "Accept": "application/json",
            "Authorization": f"Bearer {config.api_token}",
        },
    )
    if body is not None:
        req.add_header("Content-Type", "application/json")

    try:
        with urllib_request.urlopen(req, timeout=config.default_timeout_seconds) as response:
            data = response.read().decode("utf-8")
    except HTTPError as error:
        data = error.read().decode("utf-8")
        if not data:
            raise ValueError(f"REST API returned HTTP {error.code}") from error

    return {} if not data else json.loads(data)


def _required_job_id(arguments: dict[str, Any]) -> str:
    job_id = _required_string(arguments, "job_id")
    _reject_infrastructure_arguments({"job_id": job_id})
    return job_id


def _reject_infrastructure_arguments(arguments: dict[str, Any]) -> None:
    for key in arguments:
        if not isinstance(key, str) or not key.strip():
            raise ValueError("argument keys must be non-blank strings")
        normalized = key.replace("_", "").replace("-", "").lower()
        if any(fragment in normalized for fragment in RESERVED_ARGUMENT_FRAGMENTS):
            raise ValueError(f"Argument {key!r} is reserved for infrastructure control")


def handle_http_tool_request(
    config: BusinessMcpConfig,
    authorization_header: str | None,
    content_length_header: str | None,
    read_body,
) -> tuple[int, dict[str, Any]]:
    try:
        content_length = int(content_length_header or "0")
        payload = json.loads(read_body(content_length).decode("utf-8"))
        response = dispatch_tool(config, authorization_header, payload)
        return 200, response
    except PermissionError as error:
        return 401, {"error": str(error)}
    except Exception as error:
        return 400, {"error": str(error)}


def create_http_handler(config: BusinessMcpConfig):
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
            )
            body = json.dumps(payload).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

    return Handler


def serve_http(config: BusinessMcpConfig) -> None:
    server = ThreadingHTTPServer((config.bind_host, config.bind_port), create_http_handler(config))
    print(
        f"metbench-business MCP server listening on http://{config.bind_host}:{config.bind_port}",
        flush=True,
    )
    server.serve_forever()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="MetBench Business MCP server")
    parser.add_argument("--config", required=True)
    args = parser.parse_args(argv)
    serve_http(load_config(args.config))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

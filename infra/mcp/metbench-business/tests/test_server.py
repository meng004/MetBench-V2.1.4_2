import importlib.util
import io
import json
import os
import tempfile
import unittest
from pathlib import Path
from urllib.error import HTTPError


SERVER_PATH = Path(__file__).resolve().parents[1] / "server.py"


def load_server_module():
    spec = importlib.util.spec_from_file_location("metbench_business_server", SERVER_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class MetBenchBusinessServerTests(unittest.TestCase):
    def setUp(self):
        self.server = load_server_module()

    def valid_config_payload(self):
        return {
            "bind_host": "127.0.0.1",
            "bind_port": 8790,
            "auth_token": "mcp-secret",
            "api_base_url": "http://127.0.0.1:5080",
            "api_token": "api-secret",
            "default_timeout_seconds": 30,
        }

    def write_config_and_load(self, payload):
        handle = tempfile.NamedTemporaryFile(
            "w", suffix=".json", delete=False, encoding="utf-8")
        try:
            with handle:
                json.dump(payload, handle)
            return self.server.load_config(handle.name)
        finally:
            os.unlink(handle.name)

    def test_load_config_accepts_utf8_bom_config_file(self):
        payload = self.valid_config_payload()
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = Path(temp_dir) / "business.json"
            config_path.write_bytes(b"\xef\xbb\xbf" + json.dumps(payload).encode("utf-8"))

            config = self.server.load_config(config_path)

        self.assertEqual("mcp-secret", config.auth_token)
        self.assertEqual("http://127.0.0.1:5080", config.api_base_url)

    def test_load_config_rejects_blank_api_base_url(self):
        payload = self.valid_config_payload()
        payload["api_base_url"] = " "

        with self.assertRaisesRegex(ValueError, "api_base_url"):
            self.write_config_and_load(payload)

    def test_business_tools_do_not_expose_runtime_plane_terms(self):
        forbidden = ["docker", "runtime", "run_sut_command", "argv", "command", "artifactPath"]
        joined = " ".join(self.server.BUSINESS_TOOLS)

        for term in forbidden:
            with self.subTest(term=term):
                self.assertNotIn(term, joined)

    def test_design_doc_lists_current_business_mcp_tools(self):
        root = Path(__file__).resolve().parents[4]
        design_path = root / "docs" / "superpowers" / "specs" / "2026-06-21-systemmt-api-mcp-control-plane-design.md"
        text = design_path.read_text(encoding="utf-8")

        expected = (
            "`business_health`, `submit_run`, `get_job`, `cancel_job`, "
            "`get_result`, and `get_evidence`"
        )
        self.assertIn(expected, text)

    def test_dispatch_submit_run_posts_business_job_request_to_rest_api(self):
        config = self.write_config_and_load(self.valid_config_payload())
        api = FakeApiClient({"jobId": "job-1", "acceptedAtUtc": "2026-06-21T00:00:00Z"})

        result = self.server.dispatch_tool(
            config,
            "Bearer mcp-secret",
            {
                "tool": "submit_run",
                "arguments": {
                    "mr_id": "mr-alpha",
                    "parameter_overrides": {"scale": "2.0"},
                },
            },
            api_client=api,
        )

        self.assertEqual({"jobId": "job-1", "acceptedAtUtc": "2026-06-21T00:00:00Z"}, result)
        self.assertEqual("POST", api.calls[0][0])
        self.assertEqual("/api/v1/systemmt/jobs", api.calls[0][1])
        self.assertEqual(
            {"mrId": "mr-alpha", "parameterOverrides": {"scale": "2.0"}},
            api.calls[0][2],
        )

    def test_dispatch_submit_run_rejects_raw_or_path_like_arguments(self):
        config = self.write_config_and_load(self.valid_config_payload())
        cases = [
            {"argv": ["python", "runner.py"]},
            {"mr_id": "mr-alpha", "artifactPath": "/tmp/out"},
            {"mr_id": "mr-alpha", "parameter_overrides": {"sourcePath": "/tmp/input"}},
        ]

        for arguments in cases:
            with self.subTest(arguments=arguments):
                with self.assertRaises(ValueError):
                    self.server.dispatch_tool(
                        config,
                        "Bearer mcp-secret",
                        {"tool": "submit_run", "arguments": arguments},
                        api_client=FakeApiClient({}),
                    )

    def test_dispatch_get_job_uses_job_oriented_url(self):
        config = self.write_config_and_load(self.valid_config_payload())
        api = FakeApiClient({"jobId": "job-1", "state": "Succeeded"})

        result = self.server.dispatch_tool(
            config,
            "Bearer mcp-secret",
            {"tool": "get_job", "arguments": {"job_id": "job-1"}},
            api_client=api,
        )

        self.assertEqual({"jobId": "job-1", "state": "Succeeded"}, result)
        self.assertEqual(("GET", "/api/v1/systemmt/jobs/job-1", None), api.calls[0])

    def test_dispatch_cancel_job_posts_cancel_action(self):
        config = self.write_config_and_load(self.valid_config_payload())
        api = FakeApiClient({})

        self.server.dispatch_tool(
            config,
            "Bearer mcp-secret",
            {"tool": "cancel_job", "arguments": {"job_id": "job-1"}},
            api_client=api,
        )

        self.assertEqual(("POST", "/api/v1/systemmt/jobs/job-1/cancel", None), api.calls[0])

    def test_call_rest_api_raises_for_rest_http_error_body(self):
        config = self.write_config_and_load(self.valid_config_payload())
        original = self.server.urllib_request.urlopen

        def raise_bad_request(_request, timeout):
            raise HTTPError(
                "http://127.0.0.1:5080/api/v1/systemmt/jobs",
                400,
                "Bad Request",
                hdrs={},
                fp=io.BytesIO(b'{"code":"bad_request","message":"MrId must be non-blank."}'),
            )

        self.server.urllib_request.urlopen = raise_bad_request
        try:
            with self.assertRaisesRegex(ValueError, "HTTP 400"):
                self.server.call_rest_api(config, "POST", "/api/v1/systemmt/jobs", {"mrId": ""})
        finally:
            self.server.urllib_request.urlopen = original


class FakeApiClient:
    def __init__(self, response):
        self.response = response
        self.calls = []

    def __call__(self, method, path, payload=None):
        self.calls.append((method, path, payload))
        return self.response


if __name__ == "__main__":
    unittest.main()

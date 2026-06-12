import importlib.util
import json
import os
import tempfile
import unittest
from dataclasses import fields
from pathlib import Path


SERVER_PATH = Path(__file__).resolve().parents[1] / "server.py"
CONFIG_EXAMPLE_PATH = Path(__file__).resolve().parents[1] / "config.example.json"
REPO_ROOT = "/Users/limeng/Codes/MetBench-V2.1.4_2/.worktrees/docker-runtime-mcp"


def load_server_module():
    spec = importlib.util.spec_from_file_location("docker_runtime_server", SERVER_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class DockerRuntimeServerTests(unittest.TestCase):
    def setUp(self):
        self.server = load_server_module()

    def valid_config_payload(self):
        return {
            "bind_host": "auto-private-ipv4",
            "bind_port": 8765,
            "auth_token": "secret",
            "repo_root": REPO_ROOT,
            "allowed_images": {
                "metbench-sut:latest": {
                    "dockerfile": "docker/sut/Dockerfile",
                    "context": ".",
                },
            },
            "allowed_mount_roots": [REPO_ROOT, "/tmp"],
            "default_timeout_seconds": 60,
            "max_output_bytes": 4096,
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

    def valid_runtime_config(self):
        return self.server.RuntimeConfig(
            bind_host="auto-private-ipv4",
            bind_port=8765,
            auth_token="secret",
            repo_root=REPO_ROOT,
            allowed_images={
                "metbench-sut:latest": self.server.ImageConfig(
                    dockerfile="docker/sut/Dockerfile",
                    context=".",
                ),
            },
            allowed_mount_roots=["/tmp"],
            default_timeout_seconds=60,
            max_output_bytes=1024,
        )

    def test_choose_bind_host_returns_first_private_non_loopback_ipv4(self):
        result = self.server.choose_bind_host(
            ["127.0.0.1", "8.8.8.8", "192.168.1.20", "10.0.0.5"]
        )

        self.assertEqual("192.168.1.20", result)

    def test_choose_bind_host_rejects_missing_private_lan_ipv4(self):
        with self.assertRaisesRegex(ValueError, "private LAN IPv4"):
            self.server.choose_bind_host(["127.0.0.1", "8.8.8.8"])

    def test_resolve_bind_host_expands_auto_private_ipv4(self):
        result = self.server.resolve_bind_host(
            "auto-private-ipv4",
            candidates=["127.0.0.1", "10.1.2.3"],
        )

        self.assertEqual("10.1.2.3", result)

    def test_resolve_bind_host_keeps_explicit_host(self):
        result = self.server.resolve_bind_host(
            "0.0.0.0",
            candidates=["10.1.2.3"],
        )

        self.assertEqual("0.0.0.0", result)

    def test_load_config_rejects_blank_auth_token(self):
        payload = self.valid_config_payload()
        payload["auth_token"] = "   "

        with self.assertRaisesRegex(ValueError, "auth_token"):
            self.write_config_and_load(payload)

    def test_load_config_rejects_non_string_or_blank_required_strings(self):
        cases = [
            ("bind_host", None),
            ("bind_host", ""),
            ("auth_token", None),
            ("repo_root", None),
            ("repo_root", "   "),
        ]

        for field_name, value in cases:
            with self.subTest(field_name=field_name, value=value):
                payload = self.valid_config_payload()
                payload[field_name] = value

                with self.assertRaisesRegex(ValueError, field_name):
                    self.write_config_and_load(payload)

    def test_load_config_rejects_invalid_allowed_images(self):
        cases = [
            (None, "allowed_images"),
            ({}, "allowed_images"),
            ({"": {"dockerfile": "Dockerfile", "context": "."}}, "allowed_images"),
            ({"image": None}, "allowed_images"),
            ({"image": {"dockerfile": None, "context": "."}}, "dockerfile"),
            ({"image": {"dockerfile": "", "context": "."}}, "dockerfile"),
            ({"image": {"dockerfile": "Dockerfile", "context": None}}, "context"),
            ({"image": {"dockerfile": "Dockerfile", "context": "  "}}, "context"),
        ]

        for allowed_images, expected_message in cases:
            with self.subTest(allowed_images=allowed_images):
                payload = self.valid_config_payload()
                payload["allowed_images"] = allowed_images

                with self.assertRaisesRegex(ValueError, expected_message):
                    self.write_config_and_load(payload)

    def test_load_config_rejects_invalid_allowed_mount_roots(self):
        cases = [
            (None, "allowed_mount_roots"),
            ([], "allowed_mount_roots"),
            ([None], "allowed_mount_roots"),
            ([""], "allowed_mount_roots"),
            (["/tmp", "   "], "allowed_mount_roots"),
        ]

        for allowed_mount_roots, expected_message in cases:
            with self.subTest(allowed_mount_roots=allowed_mount_roots):
                payload = self.valid_config_payload()
                payload["allowed_mount_roots"] = allowed_mount_roots

                with self.assertRaisesRegex(ValueError, expected_message):
                    self.write_config_and_load(payload)

    def test_load_config_rejects_invalid_numeric_ranges(self):
        cases = [
            ("bind_port", 0),
            ("bind_port", 65536),
            ("bind_port", "8765"),
            ("default_timeout_seconds", 0),
            ("default_timeout_seconds", "60"),
            ("max_output_bytes", 0),
            ("max_output_bytes", "1024"),
        ]

        for field_name, value in cases:
            with self.subTest(field_name=field_name, value=value):
                payload = self.valid_config_payload()
                payload[field_name] = value

                with self.assertRaisesRegex(ValueError, field_name):
                    self.write_config_and_load(payload)

    def test_runtime_config_and_image_config_dataclasses_exist(self):
        image = self.server.ImageConfig(
            dockerfile="docker/sut/Dockerfile",
            context=".",
        )
        config = self.server.RuntimeConfig(
            bind_host="auto-private-ipv4",
            bind_port=8765,
            auth_token="secret",
            repo_root=REPO_ROOT,
            allowed_images={"metbench-sut:latest": image},
            allowed_mount_roots=["/tmp"],
            default_timeout_seconds=60,
            max_output_bytes=1024,
        )

        self.assertEqual(["dockerfile", "context"], [field.name for field in fields(image)])
        self.assertEqual(
            [
                "bind_host",
                "bind_port",
                "auth_token",
                "repo_root",
                "allowed_images",
                "allowed_mount_roots",
                "default_timeout_seconds",
                "max_output_bytes",
                "backend",
            ],
            [field.name for field in fields(config)],
        )
        self.assertEqual("docker/sut/Dockerfile", config.allowed_images["metbench-sut:latest"].dockerfile)

    def test_validate_run_request_rejects_non_allowlisted_image(self):
        config = self.valid_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.validate_run_request(
                config,
                {"image": "untrusted:latest", "argv": ["python", "script.py"]},
            )

    def test_validate_run_request_allows_sut_flags_after_image(self):
        config = self.valid_runtime_config()

        result = self.server.validate_run_request(
            config,
            {
                "image": "metbench-sut:latest",
                "argv": ["python", "runner.py", "--input", "source.json", "--output", "out.json"],
            },
        )

        self.assertEqual("metbench-sut:latest", result["image"])

    def test_validate_run_request_requires_list_of_non_blank_string_argv(self):
        config = self.valid_runtime_config()
        cases = [
            ("python script.py", "argv"),
            ([None], "argv"),
            ([123], "argv"),
            ([""], "argv"),
            (["python", "  "], "argv"),
        ]

        for argv, expected_message in cases:
            with self.subTest(argv=argv):
                with self.assertRaisesRegex(ValueError, expected_message):
                    self.server.validate_run_request(
                        config,
                        {"image": "metbench-sut:latest", "argv": argv},
                    )

    def test_authorize_accepts_only_exact_bearer_token(self):
        self.assertIsNone(self.server.authorize("Bearer secret", "secret"))

        rejected_headers = [
            None,
            "",
            "secret",
            "Bearer",
            "Bearer secret extra",
            "bearer secret",
            "Bearer wrong",
            " Bearer secret",
            "Bearer secret ",
        ]

        for header in rejected_headers:
            with self.subTest(header=header):
                with self.assertRaisesRegex(PermissionError, "Unauthorized"):
                    self.server.authorize(header, "secret")

    def test_build_docker_run_command_uses_repo_and_tmp_mounts_without_privileged_networking(self):
        config = self.valid_runtime_config()

        command = self.server.build_docker_run_command(
            config,
            "metbench-sut:latest",
            ["python", "sut.py"],
            timeout_seconds=30,
        )

        self.assertEqual("docker", command[0])
        self.assertEqual("run", command[1])
        self.assertIn("--rm", command)
        self.assertIn("-v", command)
        self.assertIn(f"{REPO_ROOT}:{REPO_ROOT}", command)
        self.assertIn("/tmp:/tmp", command)
        self.assertIn("-w", command)
        self.assertIn(REPO_ROOT, command)
        self.assertEqual(["metbench-sut:latest", "python", "sut.py"], command[-3:])
        self.assertNotIn("--privileged", command)
        self.assertNotIn("--network", command)
        self.assertNotIn("host", command)

    def test_build_docker_run_command_rejects_non_allowlisted_image_and_non_positive_timeout(self):
        config = self.valid_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.build_docker_run_command(
                config,
                "untrusted:latest",
                ["python", "sut.py"],
            )

        for timeout_seconds in [0, -1]:
            with self.subTest(timeout_seconds=timeout_seconds):
                with self.assertRaisesRegex(ValueError, "timeout"):
                    self.server.build_docker_run_command(
                        config,
                        "metbench-sut:latest",
                        ["python", "sut.py"],
                        timeout_seconds=timeout_seconds,
                    )

    def test_build_and_run_paths_reject_string_and_bool_timeouts(self):
        config = self.valid_runtime_config()

        for timeout_seconds in ["12", True, False]:
            with self.subTest(path="docker_run", timeout_seconds=timeout_seconds):
                with self.assertRaisesRegex(ValueError, "timeout"):
                    self.server.build_docker_run_command(
                        config,
                        "metbench-sut:latest",
                        ["python", "sut.py"],
                        timeout_seconds=timeout_seconds,
                    )

            with self.subTest(path="build_image", timeout_seconds=timeout_seconds):
                with self.assertRaisesRegex(ValueError, "timeout"):
                    self.server.dispatch_tool(
                        config,
                        "Bearer secret",
                        {
                            "tool": "build_runtime_image",
                            "arguments": {
                                "image": "metbench-sut:latest",
                                "timeout_seconds": timeout_seconds,
                            },
                        },
                        runner=lambda command, timeout: self.server.CommandResult(0, "", ""),
                    )

    def test_dispatch_run_sut_command_stores_result_for_get_run_result(self):
        config = self.valid_runtime_config()
        calls = []

        def fake_runner(command, timeout_seconds):
            calls.append((command, timeout_seconds))
            return self.server.CommandResult(
                returncode=0,
                stdout="sut ok",
                stderr="",
            )

        run_response = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "run_sut_command",
                "arguments": {
                    "image": "metbench-sut:latest",
                    "argv": ["python", "sut.py"],
                    "timeout_seconds": 12,
                },
            },
            runner=fake_runner,
            id_factory=lambda: "generated-run-1",
        )

        self.assertEqual("generated-run-1", run_response["run_id"])
        self.assertEqual("completed", run_response["status"])
        self.assertEqual(0, run_response["returncode"])
        self.assertEqual(1, len(calls))
        self.assertEqual(12, calls[0][1])
        self.assertEqual(["metbench-sut:latest", "python", "sut.py"], calls[0][0][-3:])

        result_response = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "get_run_result",
                "arguments": {
                    "run_id": "generated-run-1",
                },
            },
            runner=fake_runner,
        )

        self.assertEqual(run_response, result_response)

    def test_run_sut_command_generates_id_and_ignores_client_supplied_run_id(self):
        config = self.valid_runtime_config()

        def fake_runner(command, timeout_seconds):
            return self.server.CommandResult(
                returncode=0,
                stdout="sut ok",
                stderr="",
            )

        first = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "run_sut_command",
                "arguments": {
                    "run_id": "attacker-controlled",
                    "image": "metbench-sut:latest",
                    "argv": ["python", "first.py"],
                },
            },
            runner=fake_runner,
            id_factory=lambda: "generated-a",
        )
        second = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "run_sut_command",
                "arguments": {
                    "run_id": "attacker-controlled",
                    "image": "metbench-sut:latest",
                    "argv": ["python", "second.py"],
                },
            },
            runner=fake_runner,
            id_factory=lambda: "generated-b",
        )

        self.assertEqual("generated-a", first["run_id"])
        self.assertEqual("generated-b", second["run_id"])
        self.assertNotIn("attacker-controlled", self.server.RUN_RECORDS)
        self.assertEqual(["python", "first.py"], self.server.RUN_RECORDS["generated-a"]["argv"])
        self.assertEqual(["python", "second.py"], self.server.RUN_RECORDS["generated-b"]["argv"])

    def test_dispatch_build_runtime_image_uses_fake_runner(self):
        config = self.valid_runtime_config()
        calls = []

        def fake_runner(command, timeout_seconds):
            calls.append((command, timeout_seconds))
            return self.server.CommandResult(
                returncode=0,
                stdout="built",
                stderr="",
            )

        response = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "build_runtime_image",
                "arguments": {
                    "image": "metbench-sut:latest",
                    "timeout_seconds": 33,
                },
            },
            runner=fake_runner,
        )

        self.assertEqual(0, response["returncode"])
        self.assertEqual("built", response["stdout"])
        self.assertEqual(1, len(calls))
        self.assertEqual(33, calls[0][1])
        self.assertEqual(
            ["docker", "build", "-f", "docker/sut/Dockerfile", "-t", "metbench-sut:latest", "."],
            calls[0][0],
        )

    def test_http_tool_request_returns_json_400_for_malformed_body(self):
        config = self.valid_runtime_config()
        cases = [
            b"{not-json",
            b"\xff",
        ]

        for body in cases:
            with self.subTest(body=body):
                status, payload = self.server.handle_http_tool_request(
                    config,
                    "Bearer secret",
                    str(len(body)),
                    lambda length: body[:length],
                )

                self.assertEqual(400, status)
                self.assertIn("error", payload)

    def test_http_tool_request_returns_json_400_for_invalid_content_length(self):
        config = self.valid_runtime_config()

        status, payload = self.server.handle_http_tool_request(
            config,
            "Bearer secret",
            "not-an-int",
            lambda length: b"",
        )

        self.assertEqual(400, status)
        self.assertIn("error", payload)

    def test_load_config_uses_approved_plan_shape(self):
        payload = {
            "bind_host": "auto-private-ipv4",
            "bind_port": 8765,
            "auth_token": "secret",
            "repo_root": REPO_ROOT,
            "allowed_images": {
                "metbench-sut:latest": {
                    "dockerfile": "docker/sut/Dockerfile",
                    "context": ".",
                },
                "metbench-runtime:latest": {
                    "dockerfile": "docker/runtime/Dockerfile",
                    "context": ".",
                },
            },
            "allowed_mount_roots": [REPO_ROOT, "/tmp"],
            "default_timeout_seconds": 60,
            "max_output_bytes": 4096,
        }

        config = self.write_config_and_load(payload)

        self.assertEqual(8765, config.bind_port)
        self.assertEqual(REPO_ROOT, config.repo_root)
        self.assertEqual([REPO_ROOT, "/tmp"], config.allowed_mount_roots)
        self.assertEqual(60, config.default_timeout_seconds)
        self.assertEqual(4096, config.max_output_bytes)
        self.assertEqual(
            "docker/runtime/Dockerfile",
            config.allowed_images["metbench-runtime:latest"].dockerfile,
        )

    def test_config_example_uses_approved_plan_shape(self):
        with open(CONFIG_EXAMPLE_PATH, "r", encoding="utf-8") as handle:
            payload = json.load(handle)

        self.assertEqual(REPO_ROOT, payload["repo_root"])
        self.assertEqual("auto-private-ipv4", payload["bind_host"])
        self.assertEqual(8765, payload["bind_port"])
        self.assertEqual("change-me", payload["auth_token"])
        self.assertEqual([REPO_ROOT, "/tmp"], payload["allowed_mount_roots"])
        self.assertIn("metbench-sut:latest", payload["allowed_images"])
        self.assertIn("metbench-runtime:latest", payload["allowed_images"])
        self.assertIn("dockerfile", payload["allowed_images"]["metbench-sut:latest"])
        self.assertIn("context", payload["allowed_images"]["metbench-sut:latest"])
        self.assertIn("default_timeout_seconds", payload)
        self.assertIn("max_output_bytes", payload)

    def test_load_config_defaults_backend_to_docker(self):
        config = self.write_config_and_load(self.valid_config_payload())

        self.assertEqual("docker", config.backend)

    def test_load_config_accepts_local_backend(self):
        payload = self.valid_config_payload()
        payload["backend"] = "local"

        config = self.write_config_and_load(payload)

        self.assertEqual("local", config.backend)

    def test_load_config_rejects_unknown_backend(self):
        for backend in ["kubernetes", 1, None, True]:
            with self.subTest(backend=backend):
                payload = self.valid_config_payload()
                payload["backend"] = backend

                with self.assertRaisesRegex(ValueError, "backend"):
                    self.write_config_and_load(payload)

    def test_load_config_local_backend_allows_image_without_dockerfile(self):
        payload = self.valid_config_payload()
        payload["backend"] = "local"
        payload["allowed_images"] = {"wsl-openmc": {}}

        config = self.write_config_and_load(payload)

        self.assertEqual("", config.allowed_images["wsl-openmc"].dockerfile)
        self.assertEqual("", config.allowed_images["wsl-openmc"].context)

    def test_load_config_docker_backend_still_requires_dockerfile(self):
        payload = self.valid_config_payload()
        payload["allowed_images"] = {"img": {}}

        with self.assertRaisesRegex(ValueError, "dockerfile"):
            self.write_config_and_load(payload)

    def test_load_config_local_backend_rejects_non_string_dockerfile(self):
        for value in [42, None, False]:
            with self.subTest(value=value):
                payload = self.valid_config_payload()
                payload["backend"] = "local"
                payload["allowed_images"] = {"wsl-openmc": {"dockerfile": value}}

                with self.assertRaisesRegex(ValueError, "dockerfile/context"):
                    self.write_config_and_load(payload)

    def test_translate_mount_target_converts_windows_paths(self):
        cases = [
            ("D:\\Codes\\MetBench", "/mnt/d/Codes/MetBench"),
            ("c:/Users/lemon/AppData/Local/Temp", "/mnt/c/Users/lemon/AppData/Local/Temp"),
            ("/opt/openmc-data", "/opt/openmc-data"),
            ("relative/path", "relative/path"),
        ]

        for source, expected in cases:
            with self.subTest(source=source):
                self.assertEqual(expected, self.server.translate_mount_target(source))


if __name__ == "__main__":
    unittest.main()

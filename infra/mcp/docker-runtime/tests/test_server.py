import contextlib
import importlib.util
import io
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
            "allowed_tools": {
                "openmoc-runner": {
                    "executable": "/opt/metbench-tools/openmoc-runner",
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
            allowed_tools={
                "openmoc-runner": self.server.ToolConfig(
                    executable="/opt/metbench-tools/openmoc-runner",
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
            allowed_tools={
                "openmoc-runner": self.server.ToolConfig(
                    executable="/opt/metbench-tools/openmoc-runner",
                ),
            },
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
                "allowed_tools",
                "allowed_mount_roots",
                "default_timeout_seconds",
                "max_output_bytes",
                "backend",
            ],
            [field.name for field in fields(config)],
        )
        self.assertEqual("docker/sut/Dockerfile", config.allowed_images["metbench-sut:latest"].dockerfile)

    def test_load_config_requires_allowed_tools(self):
        payload = self.valid_config_payload()
        payload.pop("allowed_tools")

        with self.assertRaisesRegex(ValueError, "allowed_tools"):
            self.write_config_and_load(payload)

    def test_validate_run_request_rejects_non_allowlisted_image(self):
        config = self.valid_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.validate_run_request(
                config,
                {"image": "untrusted:latest", "tool": "openmoc-runner", "args": []},
            )

    def test_validate_run_request_rejects_raw_argv(self):
        config = self.valid_runtime_config()

        with self.assertRaisesRegex(ValueError, "raw argv"):
            self.server.validate_run_request(
                config,
                {"image": "metbench-sut:latest", "argv": ["python", "runner.py"]},
            )

    def test_validate_run_request_allows_allowlisted_tool_args(self):
        config = self.valid_runtime_config()

        command = self.server.validate_run_request(
            config,
            {
                "image": "metbench-sut:latest",
                "tool": "openmoc-runner",
                "args": ["--input", "source.json", "--output", "out.json"],
            },
        )

        self.assertEqual(
            ["/opt/metbench-tools/openmoc-runner", "--input", "source.json", "--output", "out.json"],
            command,
        )

    def test_validate_run_request_allows_data_paths_under_mount_roots(self):
        config = self.valid_runtime_config()

        command = self.server.validate_run_request(
            config,
            {
                "image": "metbench-sut:latest",
                "tool": "openmoc-runner",
                "args": ["--input", "/tmp/metbench/source.json", "--output", f"{REPO_ROOT}/out.json"],
            },
        )

        self.assertEqual(
            [
                "/opt/metbench-tools/openmoc-runner",
                "--input",
                "/tmp/metbench/source.json",
                "--output",
                f"{REPO_ROOT}/out.json",
            ],
            command,
        )

    def test_validate_run_request_requires_list_of_non_blank_string_args(self):
        config = self.valid_runtime_config()
        cases = [
            ("--input source.json", "args"),
            ([None], "args"),
            ([123], "args"),
            ([""], "args"),
            (["--input", "  "], "args"),
        ]

        for args, expected_message in cases:
            with self.subTest(args=args):
                with self.assertRaisesRegex(ValueError, expected_message):
                    self.server.validate_run_request(
                        config,
                        {"image": "metbench-sut:latest", "tool": "openmoc-runner", "args": args},
                    )

    def test_validate_run_request_rejects_unsafe_tool_args(self):
        config = self.valid_runtime_config()
        cases = [
            ["-c", "id"],
            ["/c", "id"],
            ["-m", "module"],
            ["/m", "module"],
            ["../secret.json"],
            ["/etc/passwd"],
            ["/tmp/runner.py"],
            [r"C:\Windows\system32\cmd.exe"],
            ["runner.py"],
            ["RUNNER.PY"],
            ["RUNNER.PYC"],
            ["&&"],
            ["|"],
            ["$(id)"],
            ["`id`"],
        ]

        for args in cases:
            with self.subTest(args=args):
                with self.assertRaises(ValueError):
                    self.server.validate_run_request(
                        config,
                        {"image": "metbench-sut:latest", "tool": "openmoc-runner", "args": args},
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
            "openmoc-runner",
            ["--input", "source.json"],
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
        self.assertEqual(
            ["metbench-sut:latest", "/opt/metbench-tools/openmoc-runner", "--input", "source.json"],
            command[-4:],
        )
        self.assertNotIn("--privileged", command)
        self.assertNotIn("--network", command)
        self.assertNotIn("host", command)

    def test_build_docker_run_command_rejects_non_allowlisted_image_and_non_positive_timeout(self):
        config = self.valid_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.build_docker_run_command(
                config,
                "untrusted:latest",
                "openmoc-runner",
                [],
            )

        for timeout_seconds in [0, -1]:
            with self.subTest(timeout_seconds=timeout_seconds):
                with self.assertRaisesRegex(ValueError, "timeout"):
                    self.server.build_docker_run_command(
                        config,
                        "metbench-sut:latest",
                        "openmoc-runner",
                        [],
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
                        "openmoc-runner",
                        [],
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
                        "tool": "openmoc-runner",
                        "args": ["--input", "source.json"],
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
        self.assertEqual(
            ["metbench-sut:latest", "/opt/metbench-tools/openmoc-runner", "--input", "source.json"],
            calls[0][0][-4:],
        )

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
                        "tool": "openmoc-runner",
                        "args": ["--input", "first.json"],
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
                        "tool": "openmoc-runner",
                        "args": ["--input", "second.json"],
                    },
            },
            runner=fake_runner,
            id_factory=lambda: "generated-b",
        )

        self.assertEqual("generated-a", first["run_id"])
        self.assertEqual("generated-b", second["run_id"])
        self.assertNotIn("attacker-controlled", self.server.RUN_RECORDS)
        self.assertEqual(["--input", "first.json"], self.server.RUN_RECORDS["generated-a"]["args"])
        self.assertEqual(["--input", "second.json"], self.server.RUN_RECORDS["generated-b"]["args"])

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
            "allowed_tools": {
                "openmoc-runner": {
                    "executable": "/opt/metbench-tools/openmoc-runner",
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
        self.assertIn("allowed_tools", payload)
        self.assertIn("openmoc-runner", payload["allowed_tools"])
        self.assertIn("executable", payload["allowed_tools"]["openmoc-runner"])
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

    def test_build_docker_run_command_mounts_allowed_roots_with_translated_targets(self):
        config = self.valid_runtime_config()
        config.repo_root = "D:\\Codes\\MetBench"
        config.allowed_mount_roots = [
            "D:\\Codes\\MetBench",
            "C:\\Users\\lemon\\AppData\\Local\\Temp",
        ]

        command = self.server.build_docker_run_command(
            config,
            "metbench-sut:latest",
            "openmoc-runner",
            ["--input", "source.json"],
            timeout_seconds=30,
        )

        self.assertIn("D:\\Codes\\MetBench:/mnt/d/Codes/MetBench", command)
        self.assertIn(
            "C:\\Users\\lemon\\AppData\\Local\\Temp:/mnt/c/Users/lemon/AppData/Local/Temp",
            command,
        )
        self.assertNotIn("/tmp:/tmp", command)
        w_index = command.index("-w")
        self.assertEqual("/mnt/d/Codes/MetBench", command[w_index + 1])
        self.assertEqual(
            ["metbench-sut:latest", "/opt/metbench-tools/openmoc-runner", "--input", "source.json"],
            command[-4:],
        )

    def test_build_docker_run_command_mounts_extra_linux_roots(self):
        config = self.valid_runtime_config()
        config.allowed_mount_roots = ["/tmp", "/opt/openmc-data"]

        command = self.server.build_docker_run_command(
            config,
            "metbench-sut:latest",
            "openmoc-runner",
            ["--output", "out.json"],
            timeout_seconds=30,
        )

        self.assertIn(f"{REPO_ROOT}:{REPO_ROOT}", command)
        self.assertIn("/tmp:/tmp", command)
        self.assertIn("/opt/openmc-data:/opt/openmc-data", command)

    def local_runtime_config(self):
        return self.server.RuntimeConfig(
            bind_host="auto-private-ipv4",
            bind_port=8766,
            auth_token="secret",
            repo_root="/home/mt",
            allowed_images={
                "wsl-openmc": self.server.ImageConfig(dockerfile="", context=""),
            },
            allowed_tools={
                "openmc-runner": self.server.ToolConfig(executable="/usr/local/bin/openmc-runner"),
            },
            allowed_mount_roots=["/tmp"],
            default_timeout_seconds=60,
            max_output_bytes=1024,
            backend="local",
        )

    def test_local_backend_runs_allowlisted_tool_without_docker(self):
        config = self.local_runtime_config()
        calls = []

        def fake_runner(command, timeout_seconds):
            calls.append((command, timeout_seconds))
            return self.server.CommandResult(returncode=0, stdout="ok", stderr="")

        response = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "run_sut_command",
                "arguments": {
                    "image": "wsl-openmc",
                    "tool": "openmc-runner",
                    "args": ["--input", "in.json"],
                    "timeout_seconds": 9,
                },
            },
            runner=fake_runner,
            id_factory=lambda: "local-run-1",
        )

        self.assertEqual("completed", response["status"])
        self.assertEqual(1, len(calls))
        self.assertEqual(["/usr/local/bin/openmc-runner", "--input", "in.json"], calls[0][0])
        self.assertEqual(9, calls[0][1])
        self.assertEqual(["/usr/local/bin/openmc-runner", "--input", "in.json"], response["command"])
        self.assertNotIn("docker", response["command"])

    def test_local_backend_still_rejects_non_allowlisted_image(self):
        config = self.local_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.dispatch_tool(
                config,
                "Bearer secret",
                {
                    "tool": "run_sut_command",
                    "arguments": {"image": "other", "tool": "openmc-runner", "args": []},
                },
                runner=lambda command, timeout_seconds: self.server.CommandResult(0, "", ""),
            )

    def test_local_backend_rejects_build_runtime_image(self):
        config = self.local_runtime_config()

        with self.assertRaisesRegex(ValueError, "local"):
            self.server.dispatch_tool(
                config,
                "Bearer secret",
                {
                    "tool": "build_runtime_image",
                    "arguments": {"image": "wsl-openmc"},
                },
                runner=lambda command, timeout_seconds: self.server.CommandResult(0, "", ""),
            )

    def test_run_sut_command_logs_run_id_line(self):
        config = self.local_runtime_config()

        def fake_runner(command, timeout_seconds):
            return self.server.CommandResult(returncode=0, stdout="ok", stderr="")

        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            response = self.server.dispatch_tool(
                config,
                "Bearer secret",
                {
                    "tool": "run_sut_command",
                    "arguments": {
                        "image": "wsl-openmc",
                        "tool": "openmc-runner",
                        "args": [],
                    },
                },
                runner=fake_runner,
                id_factory=lambda: "log-test-run-1",
            )

        output = buf.getvalue()
        self.assertIn("run_id=", output)
        self.assertIn(response["run_id"], output)

    def test_acceptance_config_examples_load(self):
        base = Path(__file__).resolve().parents[1]
        cases = [
            ("config.local-win.example.json", "local", 8764),
            ("config.docker-win.example.json", "docker", 8765),
            ("config.local-wsl.example.json", "local", 8766),
        ]

        for name, backend, port in cases:
            with self.subTest(name=name):
                config = self.server.load_config(base / name)

                self.assertEqual(backend, config.backend)
                self.assertEqual(port, config.bind_port)
                self.assertEqual("change-me", config.auth_token)
    def test_main_supports_serve_subcommand_for_cli_wrapper(self):
        payload = self.valid_config_payload()
        with tempfile.NamedTemporaryFile("w", suffix=".json") as handle:
            json.dump(payload, handle)
            handle.flush()

            served = []

            exit_code = self.server.main(
                [
                    "metbench-docker-runtime-mcp",
                    "serve",
                    "--config",
                    handle.name,
                ],
                serve=lambda config: served.append(config),
            )

        self.assertEqual(0, exit_code)
        self.assertEqual(1, len(served))
        self.assertEqual("auto-private-ipv4", served[0].bind_host)

    def test_main_prints_profile_uri_for_ui_copy_paste(self):
        output = []

        exit_code = self.server.main(
            [
                "metbench-docker-runtime-mcp",
                "profile-uri",
                "--runtime-key",
                "docker-linux",
                "--endpoint",
                "http://192.168.1.42:8765",
                "--image",
                "metbench/runtime-python:latest",
                "--tool",
                "openmoc-runner",
                "--local",
                "/host/openmoc-runner",
                "--python",
                "python3",
                "--auth-token-env",
                "METBENCH_DOCKER_MCP_TOKEN",
            ],
            out=lambda value: output.append(value),
        )

        self.assertEqual(0, exit_code)
        self.assertEqual(
            "docker-mcp://docker-linux?image=metbench%2Fruntime-python%3Alatest"
            "&tool=openmoc-runner"
            "&local=%2Fhost%2Fopenmoc-runner"
            "&python=python3"
            "&endpoint=http%3A%2F%2F192.168.1.42%3A8765"
            "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
            output[0],
        )


if __name__ == "__main__":
    unittest.main()

"""plain-text wire format for metbench_io.

For SUTs whose native input is a single string (e.g. a Lua expression, a
command-line argument blob, a raw mesh description). The dict shape is
intentionally minimal:

    {"text": "<file body>"}

Trailing newline of the source file is preserved verbatim — round-trip is
byte-identical.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any


def read_plain_text(path: Path) -> dict[str, Any]:
    return {"text": path.read_text(encoding="utf-8")}


def write_plain_text(data: dict[str, Any], path: Path) -> None:
    text = data.get("text", "")
    if not isinstance(text, str):
        raise ValueError(
            f"metbench_io.plain-text: expected str 'text'; got {type(text).__name__}"
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")

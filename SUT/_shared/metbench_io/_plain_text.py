"""plain-text wire format for metbench_io.

For SUTs whose native input is a single string (e.g. a Lua expression, a
command-line argument blob, a raw mesh description). The dict shape is
intentionally minimal:

    {"text": "<file body>"}

Round-trip is byte-identical on every platform. The read/write paths open
files with ``newline=""`` to disable Python's text-mode universal newline
translation; without this, ``write_text`` on Windows would silently rewrite
``"\\n"`` to ``"\\r\\n"`` and break the byte-identical contract pinned by
``PlainText_round_trip_byte_identical``.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any


def read_plain_text(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return {"text": handle.read()}


def write_plain_text(data: dict[str, Any], path: Path) -> None:
    text = data.get("text", "")
    if not isinstance(text, str):
        raise ValueError(
            f"metbench_io.plain-text: expected str 'text'; got {type(text).__name__}"
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        handle.write(text)

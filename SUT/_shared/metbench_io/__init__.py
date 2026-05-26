"""metbench_io — cross-format SUT I/O helper.

Pure-stdlib helper used by SUT parser scripts whose native input/output is not
JSON. The MetBench C# adapter layer stays protocol-agnostic about the on-disk
wire format; this helper is the SINGLE place the wire format (CSV, plain text,
...) is encoded and decoded.

Importing this package is intentionally cheap and has no side effects so
parser scripts that only do JSON keep importing it without slowing down.

Supported wire formats:

    json         — passthrough; uses stdlib `json.loads` / `json.dumps`.
    csv-row      — single-record key/value CSV (two columns, header "key,value").
    plain-text   — single string body, no parsing.

For format-specific behaviour see the submodules; this `__init__` re-exports the
two top-level functions `read_input` and `write_input` so call sites stay tidy.

The helper does NOT type-coerce. CSV reads return string values; SUT runners
are responsible for `float(...)` / `int(...)` casts because only the SUT
author knows which columns are numeric vs categorical.

Caller pattern (one line per parser script):

    from metbench_io import read_input as _read
    data = _read(input_file, fmt="csv-row")

"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from . import _csv, _plain_text

__all__ = ["read_input", "write_input"]


def read_input(path: str, fmt: str) -> dict[str, Any]:
    """Read an input file at <path> in wire format <fmt> and return a dict.

    Raises ValueError on unknown <fmt> so callers can fail closed at import
    time rather than emitting silently corrupted dicts downstream.
    """
    p = Path(path)
    if fmt == "json":
        return json.loads(p.read_text(encoding="utf-8"))
    if fmt == "csv-row":
        return _csv.read_csv_row(p)
    if fmt == "plain-text":
        return _plain_text.read_plain_text(p)
    raise ValueError(
        f"metbench_io: unknown fmt '{fmt}'; "
        f"supported: json, csv-row, plain-text"
    )


def write_input(data: dict[str, Any], path: str, fmt: str) -> None:
    """Write <data> to <path> in wire format <fmt>.

    Raises ValueError on unknown <fmt>.
    """
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    if fmt == "json":
        p.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
        return
    if fmt == "csv-row":
        _csv.write_csv_row(data, p)
        return
    if fmt == "plain-text":
        _plain_text.write_plain_text(data, p)
        return
    raise ValueError(
        f"metbench_io: unknown fmt '{fmt}'; "
        f"supported: json, csv-row, plain-text"
    )

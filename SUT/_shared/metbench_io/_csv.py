"""csv-row wire format for metbench_io.

Single-record key/value CSV with the header `key,value`. Every other row is a
(key, value) pair where the value is preserved as a STRING — type coercion is
the SUT runner's job.

The dict shape exposed to MetBench's C# pipeline is intentionally JSON-pointer
friendly:

    {
      "params": {
        "<key1>": "<value1>",
        "<key2>": "<value2>",
        ...
      }
    }

so MR `transform_steps` can target `/params/<key>` and the JsonPointer resolver
sees a regular dict regardless of the on-disk wire format.

Round-trip rules pinned by tests:

* `read_csv_row` → dict; `write_csv_row(dict)` → same wire bytes (modulo line endings).
* Empty file or header-only file → `{"params": {}}`.
* Quoted strings preserved (`csv.QUOTE_MINIMAL` on write, default reader on read).
* Numeric-looking strings are NOT coerced; SUT runners decide.

This module is pure-stdlib (`csv` only).
"""

from __future__ import annotations

import csv
from pathlib import Path
from typing import Any


def read_csv_row(path: Path) -> dict[str, Any]:
    """Read a single-record key/value CSV into the canonical params dict."""
    params: dict[str, str] = {}
    with path.open("r", newline="", encoding="utf-8") as handle:
        reader = csv.reader(handle)
        rows = list(reader)
    if not rows:
        return {"params": params}
    header = rows[0]
    if header != ["key", "value"]:
        raise ValueError(
            f"metbench_io.csv-row: expected header 'key,value', got {header!r}"
        )
    for row_index, row in enumerate(rows[1:], start=1):
        if len(row) != 2:
            raise ValueError(
                f"metbench_io.csv-row: row {row_index} has {len(row)} cells, "
                f"expected 2"
            )
        key, value = row[0], row[1]
        if not key:
            raise ValueError(f"metbench_io.csv-row: row {row_index} has empty key")
        params[key] = value
    return {"params": params}


def write_csv_row(data: dict[str, Any], path: Path) -> None:
    """Write a canonical params dict back to CSV."""
    params = data.get("params", {})
    if not isinstance(params, dict):
        raise ValueError(
            f"metbench_io.csv-row: expected dict 'params'; got {type(params).__name__}"
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle, quoting=csv.QUOTE_MINIMAL)
        writer.writerow(["key", "value"])
        for key, value in params.items():
            writer.writerow([str(key), str(value)])

"""LLM filter for the NOETHER candidate MR catalogue.

Reads `tools/noether_candidates.py`, sends each candidate to an
Anthropic-compatible Messages API endpoint (Anthropic, DeepSeek's
anthropic-compatible gateway, or any other), asks the model for a
physical-reasonableness verdict, and writes the verdict JSON to
`docs/experiments/_data/noether/llm-verdicts.json`.

The verdict file is the dependency the next step
(`tools/noether_run_mt.py`) reads to decide which MRs to implement.

Reproducibility
---------------

API credentials live in `.env` (gitignored). The script reads it with a
minimal parser (no `python-dotenv` dependency) and falls back to
process environment so a CI host can set the same variables and run
the same script.

Required environment:
    ANTHROPIC_BASE_URL   (optional; defaults to the SDK default —
                          https://api.anthropic.com)
    ANTHROPIC_API_KEY    (required)
    ANTHROPIC_MODEL      (optional; defaults to claude-sonnet-4-6)

Prompt-caching
--------------

The framing block (paper excerpt + MetaPattern definitions + scoring
rubric + SUT description) is identical across candidates and so is
marked with `cache_control: {type: "ephemeral"}`. Only the per-
candidate user turn changes; subsequent candidates hit the cache and
cost a fraction of the first.

Output schema
-------------

`llm-verdicts.json` is a list of objects, one per candidate:

    {
      "id":                 "N01-inv-quarter-rotation-90",
      "verdict":            "valid" | "invalid" | "uncertain",
      "confidence":         0.0-1.0,
      "suggested_assertion": "approx" | "greater" | "less" | "bound" | "<custom>",
      "reasoning":          "...",
      "raw_response":       "...",   # full assistant message text for audit
      "elapsed_s":          float,
      "tokens":             {"input": int, "output": int, "cache_read": int}
    }
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
TOOLS = REPO_ROOT / "tools"
DATA_DIR = REPO_ROOT / "docs" / "experiments" / "_data" / "noether"
VERDICTS_PATH = DATA_DIR / "llm-verdicts.json"

sys.path.insert(0, str(TOOLS))
from noether_candidates import ALL_CANDIDATES, METAPATTERN_TABLE  # noqa: E402


def load_dotenv(path: Path = REPO_ROOT / ".env") -> None:
    """Minimal .env loader: lines of `KEY=VALUE`, comments `#`, no expansion.

    Values in .env OVERRIDE the parent process environment — we want the
    repo-local credentials to win over any inherited tooling defaults
    (Claude Code, in particular, pre-sets ANTHROPIC_BASE_URL).
    """
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        s = line.strip()
        if not s or s.startswith("#"):
            continue
        m = re.match(r"^([A-Za-z_][A-Za-z0-9_]*)=(.*)$", s)
        if not m:
            continue
        k, v = m.group(1), m.group(2).strip()
        if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
            v = v[1:-1]
        os.environ[k] = v


# ---------------------------------------------------------------------------
# Prompts
# ---------------------------------------------------------------------------

SYSTEM_PROMPT = """\
You are a reactor-physics + scientific-software-testing expert reviewing
candidate Metamorphic Relations (MRs) for a 2D pin-cell neutron-transport
test bench (MetBench).

The MR framework is NOETHER (Pham et al., 2026). NOETHER derives MR
classes ("MetaPatterns", MPs) from an operator-algebra decomposition of
the program family. The seven MPs are:

    m_inv  (B1 symmetry)        — group-action invariance.
    m_mono (B2 order)           — parameter-monotonicity.
    m_adj  (B3 self-adjoint)    — adjoint-flux reciprocity.
    m_rev  (B4 time-reversal)   — collisionless reversibility.
    m_conv (B5 limit)           — discretisation convergence.
    m_dyn  (B6 qualitative-dyn) — trajectory shape invariants.
    m_cmp  (B7 method-cmp)      — method-comparison partial orders.
    m_rel  (B8 relational-eq)   — semiring-rewriting equivalence (empty here).

SUT under test: a 2D pin-cell with reflective boundaries, 2 energy groups,
solved by either OpenMOC (deterministic method-of-characteristics) or
OpenMC (Monte-Carlo, multi-group mode). The case JSON declares
materials.fuel and materials.moderator with per-group `sigma_t, sigma_a,
sigma_s (4-elt flat scattering matrix), sigma_f, nu_sigma_f, chi`, plus
`geometry.{x_extent_cm, y_extent_cm, fuel_radius_cm}` and tracking/solver
sections.

Your job for each candidate MR is to decide whether it is:

  * **valid**     — physically correct, testable, informative for fault
                    detection on this SUT.
  * **invalid**   — physically wrong, vacuous, or unobservable on this SUT.
  * **uncertain** — could go either way depending on a parameter we
                    haven't pinned down.

Output a single JSON object (no markdown, no prose around it) with keys:

    {
      "verdict":             "valid" | "invalid" | "uncertain",
      "confidence":          float in [0, 1],
      "suggested_assertion": "approx" | "greater" | "less" | "bound" | string,
      "reasoning":           "...",
      "caveats":             "...",  // optional; empty string if none
      "implementation_hint": "..."   // one-sentence sketch of the input-adapter transformation
    }

Be strict but constructive. If a candidate is broadly correct but the
proposed assertion direction is wrong, mark it `valid` and put the correct
direction in `suggested_assertion`.
"""


def candidate_block(c) -> str:
    return f"""\
CANDIDATE MR
============
id:                  {c.id}
meta_pattern:        {c.meta_pattern}   (paper block: {c.block})
parameter:           {c.parameter}
description:         {c.description}
proposed hypothesis: {c.hypothesis}
proposed assertion:  {c.assertion}
physical rationale:  {c.physical_rationale}
realizable with current SUT? {c.realizable_with_current_sut}

Evaluate per the system prompt. Return only the JSON object.
"""


# ---------------------------------------------------------------------------
# API call
# ---------------------------------------------------------------------------

def call_llm(client, model: str, system_prompt: str, candidate_text: str) -> tuple[dict, dict]:
    """Send one candidate through the API; return (parsed_verdict, telemetry)."""
    t0 = time.monotonic()
    # max_tokens budget covers both thinking-block content (for extended-thinking
    # models like deepseek-v4-pro) AND the actual text answer. 1024 was too tight —
    # the thinking block alone consumed the budget and no text block was emitted.
    msg = client.messages.create(
        model=model,
        max_tokens=4096,
        system=[
            {
                "type": "text",
                "text": system_prompt,
                "cache_control": {"type": "ephemeral"},
            }
        ],
        messages=[{"role": "user", "content": candidate_text}],
    )
    elapsed = time.monotonic() - t0
    # Concatenate the text blocks (assistant may return one or more).
    body = "".join(b.text for b in msg.content if getattr(b, "type", "") == "text").strip()
    parsed = _parse_json_blob(body)
    usage = getattr(msg, "usage", None)
    telemetry = {
        "elapsed_s": round(elapsed, 3),
        "tokens": {
            "input": getattr(usage, "input_tokens", 0) if usage else 0,
            "output": getattr(usage, "output_tokens", 0) if usage else 0,
            "cache_read": getattr(usage, "cache_read_input_tokens", 0) if usage else 0,
            "cache_creation": getattr(usage, "cache_creation_input_tokens", 0) if usage else 0,
        },
        "raw_response": body,
    }
    return parsed, telemetry


def _parse_json_blob(text: str) -> dict:
    """Pull a JSON object out of a possibly fenced response."""
    # Strip ```json ... ``` fences if present.
    m = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, flags=re.DOTALL)
    if m:
        text = m.group(1)
    # Fallback: find the first {...} balanced block.
    start = text.find("{")
    end = text.rfind("}")
    if start == -1 or end == -1 or end <= start:
        return {"verdict": "uncertain", "confidence": 0.0,
                "suggested_assertion": "approx",
                "reasoning": "(could not extract JSON from response)",
                "caveats": "", "implementation_hint": ""}
    blob = text[start : end + 1]
    try:
        return json.loads(blob)
    except json.JSONDecodeError as e:
        return {"verdict": "uncertain", "confidence": 0.0,
                "suggested_assertion": "approx",
                "reasoning": f"(JSON decode failed: {e})",
                "caveats": "", "implementation_hint": ""}


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--ids", nargs="*", default=None,
                        help="Filter to specific candidate ids (default: all).")
    parser.add_argument("--force", action="store_true",
                        help="Re-evaluate even if cached verdict exists.")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print the prompts that would be sent and exit (no API call).")
    parser.add_argument("--print-table", action="store_true",
                        help="Pretty-print the MetaPattern table + candidate list and exit.")
    args = parser.parse_args()

    load_dotenv()

    if args.print_table:
        _print_table()
        return 0

    DATA_DIR.mkdir(parents=True, exist_ok=True)

    # Resume from cache if present.
    cache: dict[str, dict] = {}
    if VERDICTS_PATH.exists() and not args.force:
        cache = {v["id"]: v for v in json.loads(VERDICTS_PATH.read_text())}

    candidates = ALL_CANDIDATES
    if args.ids:
        wanted = set(args.ids)
        candidates = tuple(c for c in ALL_CANDIDATES if c.id in wanted)
        if not candidates:
            print(f"No candidates matched ids: {args.ids}", file=sys.stderr)
            return 2

    if args.dry_run:
        for c in candidates:
            print("=" * 80)
            print(candidate_block(c))
        return 0

    api_key = os.environ.get("ANTHROPIC_API_KEY")
    base_url = os.environ.get("ANTHROPIC_BASE_URL")
    model = os.environ.get("ANTHROPIC_MODEL", "claude-sonnet-4-6")
    if not api_key:
        print("ANTHROPIC_API_KEY not set. Populate .env or export the variable.",
              file=sys.stderr)
        return 2

    try:
        import anthropic  # local import so --dry-run / --print-table work without it
    except ImportError as e:
        print(f"Cannot import anthropic SDK ({e}). `pip install anthropic`.",
              file=sys.stderr)
        return 2

    kwargs: dict = {"api_key": api_key}
    if base_url:
        kwargs["base_url"] = base_url
    client = anthropic.Anthropic(**kwargs)

    print(f"Endpoint: {base_url or 'https://api.anthropic.com'}", file=sys.stderr)
    print(f"Model:    {model}", file=sys.stderr)
    print(f"Candidates queued: {len(candidates)} "
          f"(cache hits: {sum(1 for c in candidates if c.id in cache)})", file=sys.stderr)

    results: list[dict] = []
    for c in candidates:
        if c.id in cache and not args.force:
            results.append(cache[c.id])
            print(f"  {c.id}: cached verdict={cache[c.id].get('verdict','?')} (skipped)", file=sys.stderr)
            continue
        try:
            parsed, telem = call_llm(client, model, SYSTEM_PROMPT, candidate_block(c))
        except Exception as e:
            verdict_record = {
                "id": c.id, "meta_pattern": c.meta_pattern, "block": c.block,
                "verdict": "error", "confidence": 0.0,
                "reasoning": f"API error: {type(e).__name__}: {e}",
                "caveats": "", "implementation_hint": "",
                "suggested_assertion": c.assertion,
                "elapsed_s": 0.0, "tokens": {},
                "raw_response": "",
            }
            print(f"  {c.id}: ERROR {type(e).__name__}: {e}", file=sys.stderr)
            results.append(verdict_record)
            cache[c.id] = verdict_record
            # Persist cache after every iteration so partial runs are recoverable.
            VERDICTS_PATH.write_text(json.dumps(list(cache.values()), indent=2) + "\n")
            continue
        record = {
            "id": c.id,
            "meta_pattern": c.meta_pattern,
            "block": c.block,
            "parameter": c.parameter,
            "verdict": parsed.get("verdict", "uncertain"),
            "confidence": float(parsed.get("confidence", 0.0) or 0.0),
            "suggested_assertion": parsed.get("suggested_assertion", c.assertion),
            "reasoning": parsed.get("reasoning", ""),
            "caveats": parsed.get("caveats", ""),
            "implementation_hint": parsed.get("implementation_hint", ""),
            "elapsed_s": telem["elapsed_s"],
            "tokens": telem["tokens"],
            "raw_response": telem["raw_response"],
        }
        results.append(record)
        cache[c.id] = record
        VERDICTS_PATH.write_text(json.dumps(list(cache.values()), indent=2) + "\n")
        print(f"  {c.id}: verdict={record['verdict']} "
              f"confidence={record['confidence']:.2f} "
              f"({record['elapsed_s']:.1f}s)", file=sys.stderr)

    VERDICTS_PATH.write_text(json.dumps(results, indent=2) + "\n")
    print(f"\nWrote {VERDICTS_PATH.relative_to(REPO_ROOT)} "
          f"({len(results)} records)", file=sys.stderr)

    by_verdict: dict[str, int] = {}
    for r in results:
        by_verdict[r["verdict"]] = by_verdict.get(r["verdict"], 0) + 1
    print("Verdicts:", by_verdict, file=sys.stderr)
    return 0


def _print_table() -> None:
    print("MetaPattern table (paper §3.9, restricted to this SUT)")
    print("======================================================")
    for mp, info in METAPATTERN_TABLE.items():
        applicable = "✓" if info["applicable"] else "✗"
        print(f"  {applicable} {mp:8s} (block {info['block']:8s}) — {info['definition']}")
        print(f"        {info['notes']}")
    print()
    print(f"Candidates ({len(ALL_CANDIDATES)} total):")
    print("======================================================")
    for c in ALL_CANDIDATES:
        flag = "" if c.realizable_with_current_sut else " [out-of-scope-for-now]"
        print(f"  {c.id:36s} {c.meta_pattern:8s} {c.parameter}{flag}")


if __name__ == "__main__":
    raise SystemExit(main())

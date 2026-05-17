#!/usr/bin/env bash
# Monthly OpenMOC adjoint-support monitor.
#
# Implements F11 m_adj unlock RFC §4 (path A: passive watch). Run monthly via
# cron / GitHub Action; logs results to docs/superpowers/plans/2026-05-17-f11-status.md
# so cloud + Windows agents share the same history.
#
# Trigger condition: any commit in OpenMOC upstream (mit-crpg/OpenMOC) whose
# message or diff references "adjoint" implies adjoint-flux export may be
# imminent; team should evaluate pulling the patch into the metbench OpenMOC venv.
#
# Exit code:
#   0  monitor ran cleanly (regardless of whether matches were found)
#   1  cannot reach github / parse failure (treat as "monitor unhealthy")
#
# Usage:
#   tools/check_openmoc_adjoint.sh                      # human-readable output
#   tools/check_openmoc_adjoint.sh --quiet              # only emit if matches found
#   tools/check_openmoc_adjoint.sh --since 2026-01-01   # custom window
#
# RFC: docs/superpowers/plans/2026-05-17-f11-unlock-rfc.md

set -euo pipefail

QUIET=0
SINCE=""
while [ $# -gt 0 ]; do
    case "$1" in
        --quiet) QUIET=1; shift ;;
        --since) SINCE="$2"; shift 2 ;;
        --help|-h)
            sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
            exit 0 ;;
        *) echo "unknown arg: $1" >&2; exit 2 ;;
    esac
done

# Default window: last 90 days (RFC says monthly, but pad to 90d to catch slow
# upstream PRs that may straddle a monthly check).
if [ -z "$SINCE" ]; then
    if date --version >/dev/null 2>&1; then
        SINCE="$(date -u -d '90 days ago' +%Y-%m-%dT%H:%M:%SZ)"
    else
        # BSD date (macOS)
        SINCE="$(date -u -v-90d +%Y-%m-%dT%H:%M:%SZ)"
    fi
fi

OWNER=mit-crpg
REPO=OpenMOC
API="https://api.github.com/repos/${OWNER}/${REPO}/commits?since=${SINCE}&per_page=100"

if ! command -v curl >/dev/null; then
    echo "[f11-monitor] curl not on PATH" >&2
    exit 1
fi

if ! command -v jq >/dev/null; then
    echo "[f11-monitor] jq not on PATH (apt install jq)" >&2
    exit 1
fi

# Unauthenticated rate limit is 60/hour per IP; if multiple monitors share an
# IP (cloud sandboxes do) this trips fast. Set GITHUB_TOKEN in env to bump to
# 5000/hour. Plain read-only public-repo access — no scopes required.
TMPFILE="$(mktemp --suffix=.json)"
trap 'rm -f "$TMPFILE"' EXIT

AUTH_HEADER=()
if [ -n "${GITHUB_TOKEN:-}" ]; then
    AUTH_HEADER=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
fi

HTTP_CODE="$(curl -sS -o "$TMPFILE" -w '%{http_code}' "${AUTH_HEADER[@]}" "$API")"
if [ "$HTTP_CODE" != "200" ]; then
    echo "[f11-monitor] github API HTTP $HTTP_CODE" >&2
    echo "[f11-monitor] body:" >&2
    head -c 500 "$TMPFILE" >&2
    if [ "$HTTP_CODE" = "403" ] && [ -z "${GITHUB_TOKEN:-}" ]; then
        echo "" >&2
        echo "[f11-monitor] hint: unauthenticated rate limit hit; set GITHUB_TOKEN" >&2
    fi
    exit 1
fi

MATCHES="$(jq -r '
    .[] | select(.commit.message | test("adjoint"; "i")) |
    "  - \(.commit.author.date[0:10])  \(.sha[0:8])  \(.commit.message | split("\n")[0])"
' "$TMPFILE")"

if [ -n "$MATCHES" ]; then
    echo "[f11-monitor] ⚠ OpenMOC commits mentioning 'adjoint' since ${SINCE}:"
    echo "$MATCHES"
    echo
    echo "[f11-monitor] action: evaluate each commit; if adjoint export is implemented,"
    echo "                     pull the patch into /opt/openmoc-venv and re-enable m_adj"
    echo "                     in MetaPattern seed (change Status from 'out-of-scope' to 'active')."
    exit 0
fi

if [ "$QUIET" -eq 0 ]; then
    TOTAL="$(jq '. | length' "$TMPFILE")"
    echo "[f11-monitor] no adjoint-related commits in OpenMOC upstream since ${SINCE} (${TOTAL} total commits scanned)"
    echo "[f11-monitor] F11 status: m_adj remains out-of-scope (path A passive-wait per RFC)"
fi
exit 0

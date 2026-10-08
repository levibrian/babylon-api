#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

export CLAUDE_CODE_DISABLE_AUTO_MEMORY=1

exec claude --setting-sources project,local --strict-mcp-config "$@"

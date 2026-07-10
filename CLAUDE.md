Read @AGENTS.md

## Knowledge graph (graphify) - use FIRST to navigate

Persistent AST code-graph of `MinecraftClient/` lives at `graphify-out/graph.json` (~5085 nodes / ~12060 edges: classes, methods, calls, imports, 110 dir-based communities). Query it to locate code and trace relations INSTEAD of blind repo-wide grep/rg or guessing file paths.

Query (token-cheap, no full-file reads):
- `/graphify explain "<Symbol>"` - one node + every edge (definition, callers, callees).
- `/graphify query "<question>"` - BFS neighborhood ("what touches X"); add `--dfs` to trace one call/dependency chain.
- `/graphify path "<A>" "<B>"` - shortest relation path between two symbols.
- `graphify-out/GRAPH_REPORT.md` - god nodes + community map + suggested questions.

Maintain:
- After editing `MinecraftClient/` code: `/graphify MinecraftClient --update` (AST-only, no LLM, seconds).
- `graphify-out/` is gitignored (local cache). Rebuild if missing: `/graphify MinecraftClient`.

Scope: code structure only (docs and palette/data JSON are excluded by design).
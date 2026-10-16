"""Builds the AFK account version picker list from MCC's own source.

MinecraftClient/Protocol/ProtocolHandler.cs is the source of truth:

- MCVer2ProtocolVersion(): every `case "x.y.z":` label and the `return <protocol>;` after it.
- GetProtocolHandler(): the protocol numbers MCC creates a handler for. A version whose protocol
  is not there cannot join a server and is left out.

Versions sharing a protocol become one entry, newest first. `value` is the lowest version of the
group (what MCC's MinecraftVersion setting accepts), `label` is "<low> - <high>" with an en dash,
or the version alone. `x.y.0` aliases are dropped when `x.y` maps to the same protocol.

Anything unexpected in the source fails the build instead of producing an empty or partial list.

Usage: python3 -I minecraft_versions.py <MinecraftClient dir> <output json>
"""

import json
import os
import pathlib
import re
import sys

EN_DASH = "\u2013"
VERSION_RE = re.compile(r"^\d+\.\d+(?:\.\d+)?$")


def fail(message):
    print(f"::error title=Minecraft versions::{message}")
    sys.exit(1)


def skip_literal(text, index):
    """Index just past the string, char literal or comment starting at `index`, else `index`."""
    if text.startswith("//", index):
        end = text.find("\n", index)
        return len(text) if end < 0 else end
    if text.startswith("/*", index):
        end = text.find("*/", index + 2)
        return len(text) if end < 0 else end + 2
    if text.startswith('@"', index) or text.startswith('$@"', index) or text.startswith('@$"', index):
        index = text.index('"', index) + 1
        while index < len(text):
            if text[index] == '"':
                if text.startswith('""', index):
                    index += 2
                    continue
                return index + 1
            index += 1
        return index
    if text[index] in "\"'":
        quote, index = text[index], index + 1
        while index < len(text) and text[index] != quote:
            index += 2 if text[index] == "\\" else 1
        return index + 1
    return index


def block_after(text, start, what):
    """The text inside the first `{ ... }` block at or after `start` (strings and comments skipped)."""
    index, depth, open_at = start, 0, None
    while index < len(text):
        after = skip_literal(text, index)
        if after != index:
            index = after
            continue
        char = text[index]
        if char == "{":
            if depth == 0:
                open_at = index
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0 and open_at is not None:
                return text[open_at + 1:index]
            if depth < 0:
                break
        index += 1
    fail(f"could not find the body of {what}")


def method_body(source, pattern, what):
    match = re.search(pattern, source)
    if not match:
        fail(f"{what} not found in ProtocolHandler.cs")
    return block_after(source, match.end(), what)


def strip_comments(text):
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    return re.sub(r"//[^\n]*", " ", text)


def version_key(version):
    return tuple(int(part) for part in version.split("."))


def parse_switch(source):
    """`MCVer2ProtocolVersion` as {version string: protocol}."""
    method = method_body(source, r"\bstatic\s+int\s+MCVer2ProtocolVersion\s*\(\s*string\b",
                         "MCVer2ProtocolVersion()")
    switch = re.search(r"\bswitch\s*\(", method)
    if not switch:
        fail("MCVer2ProtocolVersion() has no switch")
    body = strip_comments(block_after(method, switch.end(), "the MCVer2ProtocolVersion() switch"))
    token = re.compile(r'case\s+"(?P<case>[^"\\]*)"\s*:'
                       r"|return\s+(?P<ret>\d+)\s*;"
                       r"|(?P<default>default)\s*:")
    mapping, pending, after_default, position = {}, [], False, 0
    for match in token.finditer(body):
        gap = body[position:match.start()].strip()
        if gap:
            fail(f"unexpected code in the MCVer2ProtocolVersion() switch: {gap[:80]!r}")
        position = match.end()
        if match.group("case") is not None:
            version = match.group("case")
            if not VERSION_RE.match(version):
                fail(f"unexpected version label {version!r}")
            if version in mapping or version in pending:
                fail(f"version {version} is listed twice")
            pending.append(version)
        elif match.group("default"):
            if pending:
                fail("a default label shares a block with version labels")
            after_default = True
        else:
            protocol = int(match.group("ret"))
            if after_default and not pending:
                if protocol != 0:
                    fail(f"the default case returns {protocol}, expected 0")
                after_default = False
                continue
            if not pending:
                fail(f"return {protocol} has no version label")
            for version in pending:
                mapping[version] = protocol
            pending = []
    tail = body[position:].strip()
    if tail or pending:
        fail(f"unexpected end of the MCVer2ProtocolVersion() switch: {tail[:80]!r}")
    if not mapping:
        fail("no version labels found in MCVer2ProtocolVersion()")
    return mapping


def parse_int_arrays(text):
    """{name: [numbers]} for every `int[] name = { ... }` or `= [ ... ]` initializer in `text`."""
    arrays = {}
    pattern = r"int\s*\[\s*\]\s+(\w+)\s*=\s*(?:new\s+int\s*\[\s*\]\s*)?[{\[]([^}\]]*)[}\]]"
    for name, items in re.findall(pattern, text):
        numbers = [item.strip() for item in items.split(",") if item.strip()]
        if not numbers or not all(item.isdigit() for item in numbers):
            fail(f"unexpected protocol list {name}: {items.strip()[:80]!r}")
        arrays[name] = [int(item) for item in numbers]
    return arrays


def main():
    if len(sys.argv) != 3:
        fail("usage: minecraft_versions.py <MinecraftClient dir> <output json>")
    client_dir, output = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    source = (client_dir / "Protocol" / "ProtocolHandler.cs").read_text(encoding="utf-8-sig")
    program = (client_dir / "Program.cs").read_text(encoding="utf-8-sig")

    mapping = parse_switch(source)

    handler = strip_comments(method_body(source, r"\bstatic\s+IMinecraftCom\s+GetProtocolHandler\s*\(",
                                         "GetProtocolHandler()"))
    handler_arrays = parse_int_arrays(handler)
    if len(handler_arrays) < 2:
        fail(f"expected the Protocol16 and Protocol18 lists in GetProtocolHandler(), found {sorted(handler_arrays)}")
    joinable_protocols = {p for numbers in handler_arrays.values() for p in numbers}

    # Cross-check only: SupportedProtocols18 (multi-version servers) should match the handler list.
    upgrade = re.search(r"\bSupportedProtocols18\s*=\s*[{\[]([^}\]]*)[}\]]", source)
    handler18 = next((v for k, v in handler_arrays.items() if k.endswith("18")), None)
    upgrade_set = {int(n) for n in re.findall(r"\d+", upgrade.group(1))} if upgrade else None
    if upgrade_set is None or handler18 is None or upgrade_set != set(handler18):
        difference = sorted(upgrade_set ^ set(handler18)) if upgrade_set and handler18 else "not found"
        print("::warning title=Minecraft versions::SupportedProtocols18 and the GetProtocolHandler() "
              f"Protocol18 list differ: {difference}")

    joinable = {v: p for v, p in mapping.items() if p in joinable_protocols}
    unnamed = joinable_protocols - set(joinable.values())
    if unnamed:
        fail(f"protocols with a handler but no version in MCVer2ProtocolVersion(): {sorted(unnamed)}")
    for version in [v for v in joinable if v.endswith(".0")]:
        if joinable.get(version[:-2]) == joinable[version]:
            del joinable[version]

    # Protocol numbers grow with versions inside each era (they restarted at 1.7).
    era = lambda version: "legacy" if version_key(version) < (1, 7) else "netty"
    ordered = sorted(joinable, key=version_key)
    for lower, higher in zip(ordered, ordered[1:]):
        if era(lower) == era(higher) and joinable[higher] < joinable[lower]:
            fail(f"protocol order does not follow version order: {lower} -> {joinable[lower]}, "
                 f"{higher} -> {joinable[higher]}")
    groups = {}
    for version in ordered:
        groups.setdefault(joinable[version], []).append(version)
    for protocol, versions in groups.items():
        if len({era(v) for v in versions}) != 1:
            fail(f"protocol {protocol} spans pre-1.7 and 1.7+ versions: {versions}")

    entries = sorted(groups.values(), key=lambda versions: version_key(versions[0]), reverse=True)
    lowest = re.search(r'\bMCLowestVersion\s*=\s*"([^"]+)"', program)
    highest = re.search(r'\bMCHighestVersion\s*=\s*"([^"]+)"', program)
    if not lowest or not highest:
        fail("MCLowestVersion or MCHighestVersion not found in Program.cs")
    if entries[0][-1] != highest.group(1) or entries[-1][0] != lowest.group(1):
        fail(f"the list spans {entries[-1][0]} to {entries[0][-1]}, but Program.cs declares "
             f"{lowest.group(1)} to {highest.group(1)}")

    result = [{"value": versions[0],
               "label": versions[0] if len(versions) == 1 else f"{versions[0]} {EN_DASH} {versions[-1]}"}
              for versions in entries]
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    excluded = sorted((v for v in mapping if mapping[v] not in joinable_protocols), key=version_key)
    lines = [f"{len(result)} versions, {result[0]['value']} to {result[-1]['value']}:",
             ", ".join(entry["label"] for entry in result),
             f"Not joinable (no protocol handler): {', '.join(excluded) or 'none'}"]
    print("\n".join(lines))
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write("### Minecraft versions\n\n" + "\n\n".join(lines) + "\n")


if __name__ == "__main__":
    main()

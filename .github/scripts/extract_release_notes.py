#!/usr/bin/env python3
"""Extract and validate Russian player-facing notes for one release tag."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


VERSION_HEADING = re.compile(
    r"^##[ \t]+(?P<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)"
    r"(?:[ \t]+[—-][ \t]+\d{4}-\d{2}-\d{2})?[ \t]*$"
)
CYRILLIC = re.compile(r"[А-Яа-яЁё]")
BULLET = re.compile(r"(?m)^-[ \t]+\S")


class ChangelogError(ValueError):
    """Raised when release notes do not satisfy the publication contract."""


def normalize_tag(tag: str) -> str:
    version = tag[1:] if tag.startswith("v") else tag
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version):
        raise ChangelogError(f"Unsupported release tag: {tag!r}")
    return version


def extract_section(changelog: str, version: str) -> str:
    lines = changelog.splitlines()
    start: int | None = None

    for index, line in enumerate(lines):
        match = VERSION_HEADING.fullmatch(line)
        if not match or match.group("version") != version:
            continue
        if start is not None:
            raise ChangelogError(f"Duplicate changelog section for {version}")
        start = index + 1

    if start is None:
        raise ChangelogError(
            f"CHANGELOG.md has no '## {version} — YYYY-MM-DD' section for this tag"
        )

    end = len(lines)
    for index in range(start, len(lines)):
        if lines[index].startswith("## "):
            end = index
            break

    notes = "\n".join(lines[start:end]).strip()
    validate_notes(notes, version)
    return notes + "\n"


def validate_notes(notes: str, version: str) -> None:
    if not notes:
        raise ChangelogError(f"Changelog section {version} is empty")
    if not BULLET.search(notes):
        raise ChangelogError(f"Changelog section {version} must contain at least one bullet")
    if len(CYRILLIC.findall(notes)) < 20:
        raise ChangelogError(
            f"Changelog section {version} does not look like Russian release notes"
        )
    if "Пока нет изменений" in notes:
        raise ChangelogError(f"Changelog section {version} still contains a placeholder")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tag", required=True, help="Release tag, for example v0.10.0")
    parser.add_argument(
        "--changelog", type=Path, default=Path("CHANGELOG.md"), help="Changelog path"
    )
    parser.add_argument("--output", type=Path, required=True, help="Output Markdown path")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        version = normalize_tag(args.tag)
        changelog = args.changelog.read_text(encoding="utf-8")
        notes = extract_section(changelog, version)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(notes, encoding="utf-8")
    except (ChangelogError, OSError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    print(f"Prepared Russian release notes for {args.tag}: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

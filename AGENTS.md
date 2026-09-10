# Project rules

## Documentation format

- Markdown (`.md`) is the only documentation format for this project.
- Keep game design, technical design, plans, decision logs, and future section documents as Markdown files.
- `docs/GDD.md` is the canonical game design source of truth. Update it when a cross-system rule, interface, timing rule, ID contract, or save field changes.
- Do not create Word, PDF, slide, spreadsheet, or other export/duplicate document files unless the user explicitly requests one.
- Avoid parallel copies of the same design content; link between Markdown documents or split a section into a focused Markdown file when the GDD becomes too large.

## Release changelog

- `CHANGELOG.md` is the source for GitHub Release notes and must be written in Russian for players, not as a developer-oriented commit log.
- Whenever an agent makes a user-visible gameplay, UI, content, compatibility, or bug-fix change, it must add a concise entry under `## Не выпущено` without waiting for a separate changelog request. Pure refactors, tests, CI maintenance, and documentation-only changes do not need player-facing entries.
- When preparing version `vX.Y.Z`, an agent must move the relevant unreleased entries into `## X.Y.Z — YYYY-MM-DD`, leave an empty `## Не выпущено` section at the top, and verify that the version exactly matches the tag. Do not reuse notes from another version.
- Describe the result and its benefit in plain language. Do not mention commit hashes, internal class/file names, implementation details, or claim changes that are not present in the release.

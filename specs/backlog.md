# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
-->

- **B-1** — Forbidden dependency 1 checks type usage, not package/project references: an unused `PackageReference` (e.g. EF Core) in a `*.Domain.csproj` passes until the first type is used — Source: spec 0001, review round 1 (`tests/PolsanAuction.ArchitectureTests/LayerDependencyTests.cs:9-25`) — Severity: Low
  — Recommendation: add a test that scans `*.Domain.csproj` `PackageReference`/`ProjectReference` items — Deferred by: ferzurum, 2026-11-02, harm is only delayed (first type use turns the test red); add with the first spec that gives a Domain layer real types

- **B-2** — AC-7 of spec 0001 (residue clean, full + fast) not proven: this corporate-managed Windows machine is never idle (endpoint agent dcconfig.exe, OneDrive, Defender, Windows Update); every survivor measured was outside check, memory within tolerance — Source: spec 0001, VERIFY (AC-7) — Severity: Medium
  — Recommendation: once the remote exists, measure on a clean CI runner (add a `./scripts/residue` + `--fast` job to `.github/workflows/check.yml`) and record both RESIDUE CLEAN lines — Deferred by: ferzurum, 2026-11-02, no idle machine available; CI gives a clean measurement
- **B-3** — AC-9 of spec 0001 (green CI run of the full check) not proven: no remote repository yet — Source: spec 0001, VERIFY (AC-9) — Severity: Medium
  — Recommendation: add the remote, push, cite the green CI run (link + SHA) — Deferred by: ferzurum, 2026-11-02, per spec 0001 Constraints (no remote before SHIP)

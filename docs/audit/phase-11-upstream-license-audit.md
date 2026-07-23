# Phase 11 upstream license and provenance audit

Audit date: 2026-07-22

This document records technical evidence and is not legal advice. Status terms are `VERIFIED`, `INFERRED`, `UNVERIFIED`, and `CONFLICTING`.

## Gate result

**DEFERRED — USER-MANAGED UPSTREAM PERMISSION**

**PRIVATE DEVELOPMENT ONLY**

On 2026-07-22, the user confirmed that the WinLic owner permitted continued development from the original WinLic baseline. The permission record is managed by the user outside this repository. This audit does not adjudicate that permission, infer MIT terms, or replace the unresolved public-distribution evidence described below.

No `LICENSE`, `LICENSE.md`, `COPYING`, `COPYING.md`, `NOTICE`, or `NOTICE.md` path exists in the current tree, path history, or object list reachable from the local repository history. The README claims MIT and links to `LICENSE`, including in the first commit, but the linked license text is absent. A badge or README statement is not sufficient evidence of the exact terms applicable to the inherited source.

Consequences: do not select or add a license for inherited code, publish source or binaries, or claim that all inherited code belongs to the new publisher. Local private development may continue under the user-managed permission confirmation. Public distribution remains outside Phase 11.

## Provenance evidence

| Item | Status | Evidence |
|---|---|---|
| First reachable commit | VERIFIED | `f2633e82845aaf137ffeacab2d1436ab550ca56e`, 2026-06-28, Arden Nguyen Duc Huy, initial WinLic Manager release |
| Last upstream commit before the audit phases | VERIFIED | `bf8dd86fafb90780dc6c54bf533f16a1664fbfde`, 2026-07-17, Arden Nguyen Duc Huy |
| First new architecture commit | VERIFIED | `40845d2c4700acf8f6d8b6955a4809ac538afcf5`, 2026-07-21, Nguyễn Hồng Sơn |
| Current technical baseline | VERIFIED | `55ab75ecf6da64116ac0d8528901f14a585a4a4b` |
| Upstream repository | VERIFIED | `https://github.com/ardennguyen/WinLic.git` |
| License text in history | VERIFIED absent | `git log`, addition-history and `git rev-list --all --objects` searches returned no license/notice path |
| README license claim | CONFLICTING | Initial README says MIT and links to `LICENSE`; the referenced file is absent from the commit and all reachable history |
| License applicable to inherited source | UNVERIFIED | No exact license grant/text found |
| Copyright notice in source | UNVERIFIED | No general source copyright/license header found by the repository scan |
| Publisher ownership of upstream code | UNVERIFIED | Commit authorship and publisher identity do not establish ownership of all inherited code |

## Assets and third-party boundaries

- Upstream PNG/ICO assets entered in the initial history, but no separate asset license or permission was found: **UNVERIFIED**. They must not become final License Scope branding without permission or replacement.
- `installer/languages/Vietnamese.isl` entered with the Phase 9 installer work and contains no provenance/license header: **UNVERIFIED**. Its origin and compatibility with distribution must be established.
- NuGet locks identify Microsoft test-platform packages and transitive dependencies. Package identities and versions are verified from lock files; their individual license obligations have not been audited here: **UNVERIFIED**.
- The offline .NET Framework prerequisite is a redistributable boundary documented by Phase 9, not source owned by this project. Redistribution terms remain an independent release gate.
- Microsoft SBOM Tool is build-only and is not committed or bundled. It must remain outside the product bundle.
- Inno Setup is build tooling. Its own distribution terms do not establish rights to distribute inherited WinLic source or assets.
- No evidence was found that the entire repository was written from scratch by the License Scope publisher.

## Required resolution

Obtain written clarification or a complete license grant from the upstream owner that identifies the repository/commit range and covers source and bundled assets. Preserve the original grant and required notices. After that evidence is available, review every third-party asset/package/prerequisite separately before creating `LICENSE`, `UPSTREAM.md`, `NOTICE.md`, publishing source, or distributing a binary.

Until resolved for public distribution, no public source or binary release may rely on an assumed license. Phase 11 local rebranding may proceed under the user-managed permission confirmation, while attribution, the conflicting README claim, and all unverified findings above remain preserved.

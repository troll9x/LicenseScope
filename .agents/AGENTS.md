# WinLic Manager — Agent Rules
# Workspace: f:/Coding/winlic
# Scope: project-specific rules applied to all agents working in this repo

---

## Artifact Storage Policy

### Where to save files

| Content type | Save to |
|---|---|
| Long-form documentation, implementation plans, design decisions | `agent/docs/` (project root) |
| Release notes (new or updated) | `agent/docs/` AND `releases/<version>/` |
| Generated images, icons, media | `agent/work/` |
| Generated/scaffolded code snippets not yet in source | `agent/work/` |
| Temporary one-off scripts, debug dumps, intermediate data | Agent brain dir scratch only (`<brainDir>/scratch/`) |
| Final source code | The actual source path (`WinLicApp/`, `WinLicPS/`) |

### Rules

1. **Never use the brain `artifacts/` or brain root for project documentation.** The brain dir is ephemeral — it is not part of the repository and will not persist between conversations. Save any document the project should keep in `agent/docs/` instead.

2. **`agent/docs/` is the canonical home for:**
   - `implementation_plan.md` — the active plan for a feature or fix
   - `task.md` — the task checklist while executing a plan
   - `walkthrough.md` — post-implementation summary
   - All release notes (`RELEASE_NOTES_v*.md`)
   - Any architecture or design notes

3. **`agent/work/` is the canonical home for:**
   - Generated images and icons
   - Scaffold or template files not yet integrated into source
   - Any build artefacts an agent produces that are not release deliverables

4. **Release deliverables** (EXE, PS1, ZIP, SHA256 files) always go in `releases/<version>/` as they already do.

5. **After completing a plan**, update or create `agent/docs/walkthrough.md` to summarise what changed, what was tested, and any known follow-up items.

6. **At the start of a new task**, check `agent/docs/` for an existing `implementation_plan.md` or `walkthrough.md` to understand prior context before beginning research.

---

## Project Conventions

- Version strings must be kept in sync across **three files** every release bump:
  - `WinLicApp/Localization.cs` → `About_Version`
  - `WinLicApp/AboutDialog.xaml.cs` → `CurrentVersion`
  - `WinLicPS/WinLicManager.ps1` → `$SCRIPT_VERSION` + header comment line 2

- Release branches follow the naming `v<MAJOR>.<MINOR>-<STAGE><N>` (e.g. `v1.3-beta2`). Hotfixes increment the stage number, not add `-hotfix` suffixes.

- Every release requires all 8 artifacts:
  `*-<ver>.exe`, `*-<ver>.exe.sha256`, `*-<ver>.zip`, `*-<ver>.zip.sha256`,
  `WinLicManager-<ver>.ps1`, `WinLicManager-<ver>.ps1.sha256`,
  `WinLicPS-<ver>.zip`, `WinLicPS-<ver>.zip.sha256`

- Release notes are always bilingual (🇺🇸 EN + 🇻🇳 VI) and placed in both `releases/<version>/` and copied to `agent/docs/`.

- `releases/` is in `.gitignore` — local only. Tag + branch the source; upload artifacts manually to the GitHub Release page.

- Build command: `dotnet build WinLicApp/WinLicApp.csproj --configuration Release`
- Output: `WinLicApp/bin/Release/net4.8-windows/WinLicApp.exe`

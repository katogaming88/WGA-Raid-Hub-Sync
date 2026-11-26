# Contributing to WGA Raid Hub Sync

How to build the app, and the rules pull requests follow. The plan and the
order of work live on the site repo's tracking issue,
[WGA-Raid-Hub #1487](https://github.com/katogaming88/WGA-Raid-Hub/issues/1487),
where this repo's issues are linked as `sync #N`.

---

## Local development

Needs Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
From the repo root:

```
dotnet build      # must finish with no warnings: warnings count as errors
dotnet test
```

CI runs the same two commands on every push and pull request.

---

## Branches and commits

Never commit to `main`; every change, docs included, goes through a pull
request. One pull request per branch, off `main`, and one issue per pull
request. Once a branch's pull request has closed, don't push to it again:
start a fresh branch off `main`.

| Prefix | Use |
|---|---|
| `feat/<topic>` | A new feature someone can see |
| `fix/<topic>` | A bug fix |
| `refactor/<topic>` | Internal change, nothing behaves differently |
| `chore/<topic>` | Repo upkeep: CI, tests, tooling |
| `docs/<topic>` | Docs only |

Pull requests are squash-merged, so the title becomes the commit on `main`.
Start every title with a Conventional Commit type (`feat:`, `fix:`, `docs:`,
`chore:`, `refactor:`, `test:`, `ci:`), keep it under 72 characters, and
write it as a line an officer would understand: it becomes the line in
`CHANGELOG.md` (see Releases). The body says why; the diff shows what. Link
the issue with `Closes #NNN`.

---

## Releases

[release-please](https://github.com/googleapis/release-please) writes
`CHANGELOG.md` and the version. Never edit either by hand, and never push a
tag.

- After each merge to `main`, release-please opens or updates a **release
  pull request** with the next version and the changelog lines so far.
  `feat:` and `fix:` titles become lines; `docs:`, `chore:` and the rest
  don't.
- Below 1.0, a `feat:` moves to the next minor version (0.1.0 to 0.2.0) and
  a `fix:` to the next patch (0.1.0 to 0.1.1).
- Merging the release pull request tags `vX.Y.Z`, publishes a GitHub
  release, and sets `<Version>` in `src/Sync/Sync.csproj`, which the window
  shows. The installer and auto-update come with #5.
- To pick a version yourself, put `Release-As: X.Y.Z` as a footer in a
  commit on `main`.
- The release pull request is pushed by GitHub Actions, which can't start CI
  on its own, so `release-please.yml` starts the Build and test check on it.
  Kat merges it like any other pull request.

---

## Pull request checklist

- [ ] `dotnet build` finishes with no warnings
- [ ] `dotnet test` passes
- [ ] New or changed behaviour has a test under `tests/`, or the pull request
      says why not
- [ ] The title starts with a Conventional Commit type
- [ ] README updated if what the app does has changed
- [ ] Screenshots of any window or tray menu that changed

CodeRabbit reviews every pull request. Kat merges.

---

## Branch protection

`main` requires a pull request, the `Build and test` check passing and the
branch up to date, linear history, and no force pushes or deletions.
`gh pr merge --merge` fails under linear history; use `--squash`.

---

## Style

- C# with nullable reference types on. Prefer the .NET standard library over
  a new package; a package needs a reason in its pull request.
- Comments say why something is not obvious. Names say what.
- No guild-specific names in code: the app serves any team on the site.
- Files the game reads (anything Sync writes into `Interface/AddOns`) are
  ASCII only: no arrows, em dashes or curly quotes.
- Never write outside the WoW folders the officer picked, and never follow a
  link out of them.

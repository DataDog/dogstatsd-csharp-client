---
name: prepare-release
description: "Prepare a DogStatsD C# client release PR. Creates a release branch, updates CHANGELOG.md, runs pimpmychangelog, bumps version in StatsdClient.csproj, commits, and opens a GitHub PR targeting master. Triggers on: prepare release, create release PR, start release, open release branch."
user-invocable: true
---

# Prepare Release PR

Create a release branch with version bump and changelog, then open a PR for review.

---

## Step 1: Confirm Version

If the user did not supply a version number, read `src/StatsdClient/StatsdClient.csproj` and show the current version. Use `<PackageVersion>` if present; otherwise fall back to `<Version>`. Ask: "Prepare release for X.Y.Z?" and wait for confirmation before proceeding.

---

## Step 2: Create Release Branch

```bash
git checkout master
git pull origin master
git checkout -b release/X.Y.Z
```

---

## Step 3: Update CHANGELOG.md

- Open `CHANGELOG.md`.
- If a section for the target version already exists, leave it as-is.
- If not, add a new section at the top (below the `CHANGELOG\n=========` header) in the same format as existing entries:

```
# X.Y.Z / YYYY-MM-DD

## Changes

* [FEATURE/IMPROVEMENT/BUGFIX] Description here. See [#NNN][].
```

Use today's date. Do **not** delete any existing entries.

After writing, run `pimpmychangelog` from the repo root. It rewrites `CHANGELOG.md` in-place to add GitHub-style reference links at the bottom. Show the user the diff and ask if they want to adjust the changelog before continuing.

---

## Step 4: Update Version in StatsdClient.csproj

Edit `src/StatsdClient/StatsdClient.csproj`:
- Set `<PackageVersion>` to the release version.
- Set `<Version>` to the major version, do not change the minor version (always `MAJOR.MINOR.PATCH`, no pre-release suffix).

> <Version>: This is the assembly version. You must update this value only when there is a major version change. See Create strong named .NET libraries for explanations.
> Example: When updating from version 4.0.0 to version 4.1.0, you must set <PackageVersion>4.1.0</PackageVersion> and <Version>4.0.0</Version> (and NOT <Version>4.1.0</Version>).

Example — releasing `9.2.0`:
```xml
<PackageVersion>9.2.0</PackageVersion>
<Version>9.0.0</Version>
```

---

## Step 5: Commit and Push

```bash
git add CHANGELOG.md src/StatsdClient/StatsdClient.csproj
git commit -m "Release version X.Y.Z"
git push -u origin release/X.Y.Z
```

---

## Step 6: Open Pull Request

Create a PR targeting `master`:

```bash
gh pr create \
  --base master \
  --title "Release X.Y.Z" \
  --body "$(cat <<'EOF'
## Release X.Y.Z

Changelog and version bump for the X.Y.Z NuGet release.

### Checklist
- [ ] Changelog entries are accurate
- [ ] Version numbers are correct
- [ ] PR approved and ready to merge

Once merged, run `/publish-release` to tag, build, and push to NuGet.
EOF
)"
```

Show the user the PR URL.

---

## Summary Checklist

- [ ] Branch `release/X.Y.Z` created from latest master
- [ ] CHANGELOG.md updated with release entry
- [ ] `pimpmychangelog` run
- [ ] `<PackageVersion>` and `<Version>` bumped in csproj
- [ ] Commit pushed to `release/X.Y.Z`
- [ ] PR opened targeting master

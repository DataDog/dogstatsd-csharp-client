---
name: publish-release
description: "Publish a merged DogStatsD C# client release. Tags the merged master commit, pushes the tag, builds the NuGet package, and uploads to NuGet.org. Run after the release PR is merged. Triggers on: publish release, push to nuget, tag release, ship release."
user-invocable: true
---

# Publish Release

Tag the merged release, build the NuGet package, and push to NuGet.org.

**Pre-condition:** The release PR must already be merged into master before running this.

---

## Step 1: Confirm Version

Read the version from `src/StatsdClient/StatsdClient.csproj` (`<PackageVersion>`). Show it to the user and confirm: "Publish release X.Y.Z?" before proceeding.

---

## Step 2: Switch to Master and Pull

```bash
git checkout master
git pull origin master
```

Re-read `<PackageVersion>` from `src/StatsdClient/StatsdClient.csproj` and verify it still matches X.Y.Z. If it does not, stop and tell the user to ensure the release PR is merged before running this skill.

---

## Step 3: Tag the Release

```bash
git tag X.Y.Z
git push origin X.Y.Z
```

---

## Step 4: Build NuGet Package

```bash
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release -o artifacts/nuget
```

Verify the command exits 0 and that `artifacts/nuget/DogStatsD-CSharp-Client.X.Y.Z.nupkg` exists.

---

## Step 5: Push to NuGet.org

**Pause here.** Ask the user:

> "Ready to push `DogStatsD-CSharp-Client.X.Y.Z.nupkg` to NuGet.org. Is `NUGET_API_KEY` set? Proceed?"

Only run after explicit confirmation.

Push the main package:

```bash
dotnet nuget push artifacts/nuget/DogStatsD-CSharp-Client.X.Y.Z.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

Push symbols if present:

```bash
dotnet nuget push artifacts/nuget/DogStatsD-CSharp-Client.X.Y.Z.snupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

---

## Summary Checklist

- [ ] On latest master with release commit at HEAD
- [ ] Git tag X.Y.Z created and pushed
- [ ] `dotnet pack` succeeded, .nupkg exists
- [ ] .nupkg pushed to NuGet.org
- [ ] .snupkg (symbols) pushed to NuGet.org

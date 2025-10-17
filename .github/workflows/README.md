# GitHub Workflows

This directory contains the CI/CD workflows for BusOps.

## Workflows

### PR CI (`pr-ci.yml`)
Runs on pull requests to `main` and `develop` branches.

**Purpose**: 
- Build and test the application on all supported platforms
- Run unit and integration tests
- Perform code quality checks

**Triggers**:
- Pull requests to `main` or `develop`
- Manual dispatch

### Release (`release.yml`)
Creates releases and publishes application binaries.

**Purpose**:
- Automatically version the application
- Build platform-specific binaries
- Create GitHub releases with artifacts

**Triggers**:
- Push to `main` branch (production releases)
- Push to `develop` branch (prerelease/beta versions)
- Manual dispatch (with optional version override)

**Versioning**:
- Uses **GitVersion** for automatic semantic versioning
- **Main branch**: Stable releases (e.g., `1.0.0`, `1.0.1`)
  - Auto-increments patch version from latest tag
  - Creates stable releases
- **Develop branch**: Prerelease versions (e.g., `1.0.1-beta.5`)
  - Includes prerelease counter
  - Marked as prerelease on GitHub
- **Feature branches**: Alpha versions (e.g., `1.0.1-alpha.my-feature.3`)
  - For testing feature branch builds

See [GitVersion Guide](../../docs/gitversion-guide.md) for detailed versioning documentation.

**Platforms**:
- Windows x64 and ARM64
- macOS x64 (Intel) and ARM64 (Apple Silicon)
- Linux x64

**Artifacts**:
- Windows: ZIP archives (`.zip`)
- macOS/Linux: Compressed tarballs (`.tar.gz`)
- All artifacts are self-contained and include the .NET runtime

## Manual Release

To create a release manually:

1. Go to Actions → Release → Run workflow
2. Select the branch (`main` or `develop`)
3. Click "Run workflow"

GitVersion will automatically determine the version based on your Git history and tags.

## Initial Setup Required

Before the workflow can run successfully, you need to create an initial version tag:

```bash
# Create the initial version tag
git tag v0.1.0

# Push the tag to GitHub
git push origin v0.1.0
```

See the [GitVersion Guide](../../docs/gitversion-guide.md) for complete setup instructions.

## Version Tags

GitVersion automatically determines versions based on Git tags and branch names:
- Production releases: `v1.0.0`, `v1.0.1`, etc. (from `main` branch)
- Prereleases: `v1.0.1-beta.5` (from `develop` branch)
- Alpha releases: `v1.0.1-alpha.3` (from `feature/*` branches)
- Latest tag (main only): `latest` (force-updated to point to the latest stable release)

You control version increments through commit messages:
- `+semver: major` - Increment major version (breaking changes)
- `+semver: minor` - Increment minor version (new features)
- `+semver: patch` - Increment patch version (bug fixes, default)
- `+semver: none` - Skip version increment

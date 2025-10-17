# GitVersion Setup Guide

This project uses [GitVersion](https://gitversion.net/) for automated semantic versioning based on Git history and branching strategy.

## What is GitVersion?

GitVersion automatically determines the next version number based on:
- Git tags
- Branch names
- Commit messages
- Merge history

## Initial Setup

### 1. Create Your First Version Tag

Before GitVersion can work properly, you need to create an initial version tag:

```bash
# Create the initial version tag (0.1.0 or 1.0.0)
git tag v0.1.0

# Push the tag to remote
git push origin v0.1.0
```

### 2. Install GitVersion (Optional - for local testing)

To test versioning locally, install GitVersion:

```bash
# Using .NET tool
dotnet tool install --global GitVersion.Tool

# Or using Homebrew (macOS)
brew install gitversion
```

### 3. Test Local Versioning

```bash
# Show the calculated version
dotnet-gitversion

# Show detailed version information
dotnet-gitversion /showvariable FullSemVer
```

## Branching Strategy & Version Behavior

### Main Branch (`main`)
- **Version Format**: `1.0.0`, `1.0.1`, etc.
- **Increment**: Patch version auto-increments
- **Tag Required**: Creates stable releases
- **Example**: `1.2.3`

### Develop Branch (`develop`)
- **Version Format**: `1.0.1-beta.5`
- **Increment**: Patch version + beta counter
- **Prerelease**: Always marked as prerelease
- **Example**: `1.2.4-beta.12`

### Feature Branches (`feature/*`)
- **Version Format**: `1.0.1-alpha.5`
- **Increment**: Based on parent branch
- **Prerelease**: Alpha builds
- **Example**: `1.2.4-alpha.branch-name.7`

### Release Branches (`release/*`)
- **Version Format**: `1.0.0-rc.1`
- **Increment**: Creates release candidates
- **Example**: `2.0.0-rc.3`

### Hotfix Branches (`hotfix/*`)
- **Version Format**: `1.0.1-beta.1`
- **Increment**: Patch version
- **Example**: `1.2.4-beta.1`

## Version Bump via Commit Messages

You can control version increments using commit message tags:

```bash
# Increment MAJOR version (breaking change)
git commit -m "Breaking change to API +semver: major"

# Increment MINOR version (new feature)
git commit -m "Add new feature +semver: minor"

# Increment PATCH version (bug fix) - this is the default
git commit -m "Fix bug +semver: patch"

# Skip version increment
git commit -m "Update documentation +semver: none"
```

## Workflow Integration

The GitHub Actions workflow automatically:
1. Installs GitVersion
2. Calculates version based on branch and history
3. Uses the version for:
   - Assembly versioning
   - Package versioning
   - Git tags
   - Release names

## Common Scenarios

### Starting a New Feature

```bash
# Create feature branch from develop
git checkout develop
git pull
git checkout -b feature/my-new-feature

# Work on feature...
git commit -m "Add awesome feature"

# Merge back to develop
git checkout develop
git merge feature/my-new-feature
git push
```

Version on feature branch: `1.0.1-alpha.my-new-feature.1`
Version after merge to develop: `1.0.1-beta.1`

### Creating a Release

```bash
# Create release branch from develop
git checkout develop
git pull
git checkout -b release/1.0.0

# Make any final adjustments
git commit -m "Prepare for 1.0.0 release"

# Merge to main
git checkout main
git merge release/1.0.0
git push

# GitVersion will create version 1.0.0
```

### Creating a Hotfix

```bash
# Create hotfix from main
git checkout main
git pull
git checkout -b hotfix/critical-bug

# Fix the bug
git commit -m "Fix critical bug +semver: patch"

# Merge to main
git checkout main
git merge hotfix/critical-bug
git push

# Merge back to develop
git checkout develop
git merge hotfix/critical-bug
git push
```

## Version Outputs Available

The workflow provides these version variables:

- **`semVer`**: `1.2.3` or `1.2.3-beta.4`
- **`fullSemVer`**: `1.2.3` or `1.2.3-beta.4+5`
- **`assemblySemVer`**: `1.2.3.0` (for .NET assemblies)
- **`nuGetVersionV2`**: `1.2.3` or `1.2.3-beta0004`
- **`informationalVersion`**: Full version with metadata
- **`preReleaseTag`**: `beta` or empty for stable

## Troubleshooting

### No version is calculated
**Solution**: Create an initial tag:
```bash
git tag v0.1.0
git push origin v0.1.0
```

### Version doesn't increment
**Solution**: Ensure you have commits since the last tag and you're on a tracked branch.

### Wrong version on feature branch
**Solution**: Make sure your branch follows the naming convention (`feature/*`, `hotfix/*`, etc.)

## Configuration

The GitVersion configuration is in `GitVersion.yml` at the repository root. 

Key settings:
- **mode**: `ContinuousDeployment` (auto-increment for all branches)
- **tag-prefix**: Supports `v` or `V` prefix on tags
- **increment**: Controls how versions are incremented

## Resources

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Semantic Versioning](https://semver.org/)
- [GitVersion Configuration](https://gitversion.net/docs/reference/configuration)

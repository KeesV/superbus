# Quick Start: GitVersion Setup

Follow these steps to get GitVersion working for your BusOps project.

## Step 1: Create Initial Version Tag

```bash
# Make sure you're on the main or develop branch
git checkout develop

# Create the initial version tag (choose one)
git tag v0.1.0        # If you're just starting
# OR
git tag v1.0.0        # If you're ready for a 1.0 release

# Push the tag to GitHub
git push origin v0.1.0
```

## Step 2: Verify GitVersion Configuration

The `GitVersion.yml` file should already be at the root of your repository. It's configured for:
- ✅ Main branch → stable releases (`1.0.0`)
- ✅ Develop branch → beta releases (`1.0.1-beta.5`)
- ✅ Feature branches → alpha releases (`1.0.1-alpha.feature.3`)

## Step 3: Test the Release Workflow

```bash
# Make a small change
echo "# Test" >> README.md

# Commit and push to develop
git add README.md
git commit -m "Test GitVersion setup"
git push origin develop
```

The GitHub Actions workflow will:
1. Automatically calculate the version using GitVersion
2. Build for all platforms
3. Create a prerelease on GitHub (since you're on develop)

## Step 4: Check the Results

Go to GitHub and verify:
1. **Actions tab**: The "Release" workflow should run successfully
2. **Releases page**: You should see a new prerelease (e.g., `v0.1.1-beta.1`)
3. The release should contain artifacts for all platforms

## Step 5: Create Your First Production Release

When you're ready for a production release:

```bash
# Merge develop to main
git checkout main
git merge develop
git push origin main
```

This will create a stable release (e.g., `v0.1.1`) without any prerelease suffix.

## Optional: Install GitVersion Locally

To test versioning locally before pushing:

```bash
# Install GitVersion CLI
dotnet tool install --global GitVersion.Tool

# Run it in your project root
cd /path/to/busops
dotnet-gitversion

# You should see output like:
# {
#   "Major": 0,
#   "Minor": 1,
#   "Patch": 1,
#   "PreReleaseTag": "beta.1",
#   "FullSemVer": "0.1.1-beta.1",
#   ...
# }
```

## Troubleshooting

### Error: "No version could be determined"
**Solution**: You need to create an initial tag (see Step 1)

### Workflow fails at GitVersion step
**Solution**: Ensure you have at least one tag in your repository

### Version doesn't increment
**Solution**: Make sure you have new commits since the last tag

## Next Steps

- Read the [full GitVersion guide](gitversion-guide.md) for advanced usage
- Learn about [commit message conventions](gitversion-guide.md#version-bump-via-commit-messages) for version control
- Understand the [branching strategy](gitversion-guide.md#branching-strategy--version-behavior)

---

**Need Help?** Check the [GitVersion documentation](https://gitversion.net/docs/) or the detailed guide in `docs/gitversion-guide.md`.

# CI builds and GitHub releases

The repository uses `.github/workflows/build-release.yml` to build the Unity project for Windows x64 and Android.

## Cost

This repository is public. [Standard GitHub-hosted runners are free for public repositories](https://docs.github.com/en/billing/concepts/product-billing/github-actions). The workflow uses `ubuntu-latest` for both targets; GameCI cross-builds the Windows player in a Linux container, so a Windows runner is not required.

GitHub Free also includes 10 GB of Actions cache storage per repository. The workflow keeps a separate Unity `Library` cache for each target. Packaged artifacts from non-release builds expire after 7 days. Files attached to a GitHub Release follow GitHub's release-asset retention rules instead.

## Required Unity secrets

Unity must be activated before GameCI can build the project. For Unity Personal:

1. In Unity Hub, open **Preferences > Licenses**, select **Add**, and activate a free Personal license. Do this even when Hub already shows a license so it writes the license file.
2. Find the generated license at `C:\ProgramData\Unity\Unity_lic.ulf` on Windows.
3. In the GitHub repository, open **Settings > Secrets and variables > Actions**.
4. Add these repository secrets:
   - `UNITY_LICENSE`: the complete contents of `Unity_lic.ulf`
   - `UNITY_EMAIL`: the Unity account email
   - `UNITY_PASSWORD`: the Unity account password

Never commit the `.ulf` file or Unity credentials. GameCI's current activation instructions are at <https://game.ci/docs/github/activation/>.

## Pipeline behavior

- A push to `master`, a pull request targeting `master`, or a manual run builds both platforms and exposes the packages under the workflow run's **Artifacts** section.
- Pull requests from forks skip the build because GitHub does not provide repository secrets to forked workflows.
- A tag beginning with `v`, such as `v0.6.0`, builds both platforms and creates a GitHub Release with generated notes.
- Re-running a tag workflow replaces its attached build files instead of failing because the release already exists.
- Windows and Android builds run serially to avoid concurrent Unity Personal license activation.

The Android artifact is an installable APK signed using Unity's default debug keystore. Before distributing through Google Play or treating Android builds as production-signed releases, configure a private Android keystore through GitHub Actions secrets and switch the workflow to a signed Android App Bundle (`.aab`).

The Android application ID is `ru.sergeiwork.threerow`. It is explicit in Unity Player settings so local and CI builds produce the same package identity.

## Create a release

After the workflow file and Unity secrets are present on GitHub, create and push an annotated semantic-version tag:

```powershell
git tag -a v0.6.0 -m "Release v0.6.0"
git push origin v0.6.0
```

The release is published only after both builds succeed. Use a new version number if `v0.6.0` already exists.

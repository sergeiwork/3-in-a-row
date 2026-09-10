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
- A tag beginning with `v`, such as `v0.11.0`, builds both platforms and creates a GitHub Release with Russian player-facing notes taken from the matching `CHANGELOG.md` section.
- A release is rejected when its changelog section is missing, empty, lacks a bullet point, or does not contain enough Cyrillic text to be a Russian note.
- Re-running a tag workflow replaces both the attached build files and the release description instead of failing because the release already exists.
- Windows and Android builds run serially to avoid concurrent Unity Personal license activation.

The Android artifact is an installable APK containing ARMv7 and ARM64 native players and supporting Android 6.0 / API 23 or newer. CI verifies that the APK is a valid ZIP and contains the ARM64 Unity player before publishing it.

Android packages are signed with the persistent `threerow-release` key. The workflow requires these repository secrets and stops before building when any is absent:

- `ANDROID_KEYSTORE_BASE64`: the complete keystore encoded as base64
- `ANDROID_KEYSTORE_PASS`: the keystore password
- `ANDROID_KEYALIAS_NAME`: `threerow-release`
- `ANDROID_KEYALIAS_PASS`: the key password

Keep an offline backup of the keystore and both passwords. They are required to publish updates that install over an existing version. APKs from `v0.0.2` and earlier used a different debug certificate, so uninstall those once before installing the first persistently signed release:

```powershell
adb uninstall ru.sergeiwork.threerow
```

The Android application ID is `ru.sergeiwork.threerow`. It is explicit in Unity Player settings so local and CI builds produce the same package identity.

## Create a release

Player-visible changes are added to `## Не выпущено` in `CHANGELOG.md` as part of ordinary agent work; a separate changelog prompt is not required. Before tagging, move those entries into a dated heading whose version exactly matches the tag, for example:

```markdown
## 0.11.0 — 2026-09-10

### Новое

- Добавлено краткое описание изменения и его пользы для игрока.
```

Commit that changelog section together with the release-ready code. After the workflow file and Unity secrets are present on GitHub, create and push an annotated semantic-version tag on that commit:

```powershell
git tag -a v0.11.0 -m "Release v0.11.0"
git push origin v0.11.0
```

The release is published only after both builds succeed and CI validates the matching changelog section. Use a new version number if `v0.11.0` already exists.

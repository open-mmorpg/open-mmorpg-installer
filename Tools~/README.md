# Release tooling

This folder is ignored by Unity (trailing `~`) and is not part of the installed package.

## Automated build (recommended)

The **Build installer** GitHub Action (`.github/workflows/build-installer.yml`) rebuilds both archives on a plain Ubuntu runner; no Unity install or license is needed.

1. Open the repository's **Actions** tab, pick **Build installer**, and click **Run workflow**.
2. Enter the kit ref to package (for example the tag `v1.1.0` in the kit repo) and the installer version to stamp (for example `1.1.0`).
3. The workflow builds `OpenMMORPG.unitypackage` from the kit ref and `OpenMMORPG_Settings.unitypackage` from `Tools~/ProjectSettings/`, stamps the version into `package.json` and the wizard's shown-once key, commits, tags `v<version>`, and (optionally) publishes a GitHub Release with the archives attached.

While the kit repository is private, the workflow needs a repository secret named **`KIT_REPO_TOKEN`**: a fine-grained personal access token with read access to *Contents* on `open-mmorpg/open-mmorpg`. Once the kit repository is public the secret can be removed; the default job token is enough.

## Manual build

`build_unitypackage.py` produces the same archives locally:

```sh
# kit: package the contents of an Assets/OpenMMORPG checkout
python Tools~/build_unitypackage.py kit "<project>/Assets/OpenMMORPG" Assets/OpenMMORPG OpenMMORPG.unitypackage

# settings: package the six ProjectSettings files the wizard imports
python Tools~/build_unitypackage.py settings Tools~/ProjectSettings OpenMMORPG_Settings.unitypackage
```

`Tools~/ProjectSettings/` holds the sanitized copies of the six settings files. When updating them from a Unity project, clear project-specific values in `ProjectSettings.asset` (`productName`, `cloudProjectId`, `organizationId`, `projectName`, `metroPackageName`, `metroApplicationDescription`) so they do not leak into other people's projects.

Manual release checklist:

1. Tag the kit release in the [open-mmorpg](https://github.com/open-mmorpg/open-mmorpg) repository.
2. Rebuild both archives from that tag.
3. Bump `version` in `package.json` and the `PREF_KEY_SHOWN` suffix in `Editor/OpenMMORPG_InstallWizard.cs` so the wizard shows again after an update.
4. Commit, tag (for example `v1.1.0`), and push.

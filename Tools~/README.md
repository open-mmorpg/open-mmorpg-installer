# Release tooling

This folder is ignored by Unity (trailing `~`) and is not part of the installed package.

`build_unitypackage.py` rebuilds the two archives shipped by this package without opening the editor:

```sh
# kit: package the contents of an Assets/OpenMMORPG checkout
python Tools~/build_unitypackage.py kit "<project>/Assets/OpenMMORPG" Assets/OpenMMORPG OpenMMORPG.unitypackage

# settings: package the six ProjectSettings files the wizard imports
python Tools~/build_unitypackage.py settings "<project>/ProjectSettings" OpenMMORPG_Settings.unitypackage
```

Before packaging settings, clear project-specific values in `ProjectSettings.asset`
(`productName`, `cloudProjectId`, `organizationId`, `projectName`, `metroPackageName`,
`metroApplicationDescription`) so they do not leak into other people's projects.

Release checklist:

1. Tag the kit release in the [open-mmorpg](https://github.com/open-mmorpg/open-mmorpg) repository.
2. Rebuild both archives from that tag.
3. Bump `version` in `package.json` and the `PREF_KEY_SHOWN` suffix in `Editor/OpenMMORPG_InstallWizard.cs` so the wizard shows again after an update.
4. Commit, tag (for example `v1.1.0`), and push.

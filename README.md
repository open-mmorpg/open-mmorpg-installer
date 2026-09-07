# Open MMORPG Installer

![image](Resources/OpenMMORPG.png)

Unity package that installs [Open MMORPG](https://github.com/open-mmorpg/open-mmorpg), a free, community-maintained distribution of MMORPG Kit, together with the Unity packages it depends on.

## Install

Open MMORPG targets **Unity 6000.3** or newer.

1. Open Window → **Package Manager** and click **Add package from git URL**
```
https://github.com/open-mmorpg/open-mmorpg-installer.git
```

2. A welcome screen opens once the package is installed. It appears once per project for each version of the package, tracks which steps are already done, and can be reopened at any time from Open MMORPG → Install → **Show Setup Wizard**. Untick **Show this window when the project opens** to stop it appearing.

3. Click **Import Settings** to apply the base project settings. The following files are overwritten:

 - ProjectSettings/DynamicsManager.asset
 - ProjectSettings/InputManager.asset
 - ProjectSettings/ProjectSettings.asset
 - ProjectSettings/QualitySettings.asset
 - ProjectSettings/TagManager.asset
 - ProjectSettings/TimeManager.asset

4. Click **Import Open MMORPG** to import the latest release into `Assets/OpenMMORPG`.

After installation, browse addons at Open MMORPG → Develop → **Addon Manager**.

The kit archive also carries a Package Manager manifest, so importing `OpenMMORPG.unitypackage` on its own (for example from a GitHub Release) installs the same Unity package dependencies.

## Update

Update the package in the Package Manager. The welcome screen reappears after an update so you can reimport the kit.

## Develop the kit

Delete the imported `Assets/OpenMMORPG` folder and clone the kit repository in its place:

```sh
git clone https://github.com/open-mmorpg/open-mmorpg.git Assets/OpenMMORPG
```

See the kit's [CONTRIBUTING.md](https://github.com/open-mmorpg/open-mmorpg/blob/master/CONTRIBUTING.md) for the branch model.

## What is in this package

| Path | Purpose |
| --- | --- |
| `package.json` | Package manifest and Unity package dependencies |
| `Editor/OpenMMORPG_InstallWizard.cs` | The welcome screen and its setup steps |
| `Editor/OpenMMORPG.Installer.Editor.asmdef` | Editor assembly for the welcome screen; Unity ignores package scripts without one |
| `OpenMMORPG.unitypackage` | The kit, imported into `Assets/OpenMMORPG` |
| `OpenMMORPG_Settings.unitypackage` | The six ProjectSettings files listed above |
| `Tools~/` | Release tooling, ignored by Unity; see its README to rebuild the archives |

## Releasing a new version

Releases are built by the **Build installer** GitHub Action: run it from the Actions tab with the kit ref to package and the version to stamp. It rebuilds both archives, commits them, tags the installer, and publishes a GitHub Release. See [Tools~/README.md](Tools~/README.md) for details and the manual fallback.

## License

MIT, see the kit's [LICENSE](https://github.com/open-mmorpg/open-mmorpg/blob/master/LICENSE).

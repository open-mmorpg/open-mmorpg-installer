using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenMMORPG
{
    /// <summary>
    /// Welcome screen shown the first time Open MMORPG is installed in a project.
    /// It walks through importing the base project settings and the kit itself, and
    /// reflects what is already installed when it is reopened later.
    /// </summary>
    public class OpenMMORPG_InstallWizard : EditorWindow
    {
        private const string PACKAGE_NAME = "com.openmmorpg.installer";
        private const string SETTINGS_PACKAGE_FILE = "OpenMMORPG_Settings.unitypackage";
        private const string KIT_ARCHIVE_NAME = "OpenMMORPG.unitypackage";
        private const string INSTALLER_REPO_URL = "https://github.com/open-mmorpg/open-mmorpg-installer";
        private const string KIT_FOLDER = "Assets/OpenMMORPG";
        private const string ADDON_MANAGER_MENU = "Open MMORPG/Develop/Addon Manager";

        private const string KIT_REPO_URL = "https://github.com/open-mmorpg/open-mmorpg";
        private const string DOCS_URL = KIT_REPO_URL + "/blob/master/README.md";
        private const string ISSUES_URL = KIT_REPO_URL + "/issues";

        // A layer that only exists once the kit project settings are imported.
        private const string KIT_LAYER = "WarpPortalOrSafeArea";

        private Texture2D iconTexture;
        private string packageVersion = "";
        private bool autoShow;

        private GUIStyle richTextStyle;
        private GUIStyle bodyStyle;
        private GUIStyle titleStyle;
        private GUIStyle stepTitleStyle;

        #region Show once per project

        // EditorPrefs are shared by every project on this machine, so these keys are
        // scoped by project path. Otherwise installing into a second project would
        // silently skip the welcome screen.
        private static string ProjectKey(string key)
        {
            return "OpenMMORPG.Welcome." + key + "." + StableHash(Application.dataPath);
        }

        // Deliberately not string.GetHashCode: that is only guaranteed to be stable
        // within a single process, and these keys have to survive editor restarts.
        private static string StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }
                return hash.ToString("X8");
            }
        }

        private static double idleSince = -1;

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            if (Application.isBatchMode)
                return;

            // Deliberately not EditorApplication.delayCall. A cold start runs through
            // several domain reloads while packages resolve and assemblies compile, and
            // a one-shot callback registered before one of those reloads is discarded,
            // so the screen never appeared on a first install. Polling the update loop
            // is re-registered by every reload and survives that.
            EditorApplication.update += ShowWhenEditorIsIdle;
        }

        private static void ShowWhenEditorIsIdle()
        {
            // Wait for imports and compilation to finish; a window opened while the
            // editor is still churning through a fresh install can be discarded.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                idleSince = -1;
                return;
            }

            if (idleSince < 0)
            {
                idleSince = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - idleSince < 1.0)
                return;

            EditorApplication.update -= ShowWhenEditorIsIdle;

            if (!EditorPrefs.GetBool(ProjectKey("AutoShow"), true))
                return;

            // Show once per project for each version of the package, so an update
            // surfaces the screen again. An unreadable version still resolves to a
            // non-empty marker, otherwise it would match the default and the screen
            // would never appear.
            string version = ReadPackageVersion();
            if (string.IsNullOrEmpty(version))
                version = "unknown";
            if (EditorPrefs.GetString(ProjectKey("ShownVersion"), "") == version)
                return;

            EditorPrefs.SetString(ProjectKey("ShownVersion"), version);
            ShowWizard();
        }

        [MenuItem("Open MMORPG/Install/Show Setup Wizard", false, -1000)]
        public static void ShowWizard()
        {
            OpenMMORPG_InstallWizard window = GetWindow<OpenMMORPG_InstallWizard>(true, "Welcome to Open MMORPG");

            // Window size is in device pixels while the GUI inside is drawn at
            // pixelsPerPoint, so a constant size leaves the content about a third less
            // room than it needs on a scaled display and the text clips. Scale it.
            //
            // minSize and maxSize are deliberately equal. A utility window otherwise
            // keeps the size it remembers from a previous session while the layout uses
            // the size assigned here, and the mismatch clips the content just the same.
            float scale = Mathf.Clamp(EditorGUIUtility.pixelsPerPoint, 1f, 3f);
            Vector2 size = FitToMainWindow(new Vector2(640f * scale, 660f * scale));
            window.minSize = size;
            window.maxSize = size;
            CenterOnMainWindow(window, size.x, size.y);
            window.Show();
        }

        /// <summary>Shrinks a desired window size so it still fits the editor window.</summary>
        private static Vector2 FitToMainWindow(Vector2 size)
        {
            try
            {
                Rect main = EditorGUIUtility.GetMainWindowPosition();
                if (main.width > 1 && main.height > 1)
                {
                    size.x = Mathf.Min(size.x, main.width - 40f);
                    size.y = Mathf.Min(size.y, main.height - 40f);
                }
            }
            catch (System.Exception)
            {
                // GetMainWindowPosition can fail very early in startup; the default is fine.
            }
            return size;
        }

        // Utility windows remember their last position for the whole machine, which can
        // leave the screen entirely when monitors change. Place it over the editor.
        private static void CenterOnMainWindow(EditorWindow window, float width, float height)
        {
            Rect main;
            try
            {
                main = EditorGUIUtility.GetMainWindowPosition();
            }
            catch (System.Exception)
            {
                return;
            }

            if (main.width < 1 || main.height < 1)
                return;

            width = Mathf.Min(width, main.width);
            height = Mathf.Min(height, main.height);
            window.position = new Rect(
                main.x + (main.width - width) * 0.5f,
                main.y + (main.height - height) * 0.5f,
                width, height);
        }

        #endregion

        #region Package paths

        private static UnityEditor.PackageManager.PackageInfo InstallerPackage
        {
            get { return UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE_NAME); }
        }

        private static string ReadPackageVersion()
        {
            UnityEditor.PackageManager.PackageInfo info = InstallerPackage;
            return info != null ? info.version : "";
        }

        /// <summary>Path of a file shipped inside this package, or null when missing.</summary>
        private static string PackageFilePath(string fileName)
        {
            UnityEditor.PackageManager.PackageInfo info = InstallerPackage;
            if (info != null)
            {
                string resolved = Path.Combine(info.resolvedPath, fileName);
                if (File.Exists(resolved))
                    return resolved;
            }

            // Fallback for a copy embedded under the Packages folder of the project.
            string virtualPath = "Packages/" + PACKAGE_NAME + "/" + fileName;
            return File.Exists(virtualPath) ? virtualPath : null;
        }

        #endregion

        #region Installed state

        private static bool SettingsImported
        {
            get { return LayerMask.NameToLayer(KIT_LAYER) != -1; }
        }

        private static bool KitImported
        {
            get { return AssetDatabase.IsValidFolder(KIT_FOLDER); }
        }

        #endregion

        private void OnEnable()
        {
            packageVersion = ReadPackageVersion();
            autoShow = EditorPrefs.GetBool(ProjectKey("AutoShow"), true);

            iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/" + PACKAGE_NAME + "/Resources/OpenMMORPG.png");
            if (iconTexture == null)
                iconTexture = Resources.Load<Texture2D>("OpenMMORPG");

            AssetDatabase.importPackageCompleted += OnPackageImported;
        }

        private void OnDisable()
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
        }

        private void OnPackageImported(string packageName)
        {
            Repaint();
        }

        private void EnsureStyles()
        {
            if (richTextStyle != null)
                return;

            richTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true, wordWrap = true };
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };
            stepTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        }

        private Vector2 scroll;

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSteps();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            if (iconTexture != null)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(18);
                GUILayout.Box(iconTexture, GUIStyle.none, GUILayout.Width(72), GUILayout.Height(72));
                GUILayout.EndVertical();
                GUILayout.Space(14);
            }

            GUILayout.BeginVertical();
            GUILayout.Space(20);
            GUILayout.Label("Welcome to Open MMORPG", titleStyle);
            GUILayout.Label(string.IsNullOrEmpty(packageVersion) ? "Installer" : "Installer version " + packageVersion, EditorStyles.miniLabel);
            GUILayout.Space(6);
            GUILayout.Label("A free, community-maintained distribution of MMORPG Kit. Follow the steps below to set up your project.", richTextStyle);
            GUILayout.EndVertical();

            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(14);
            DrawSeparator();
            GUILayout.Space(10);
        }

        private void DrawSteps()
        {
            bool settingsDone = SettingsImported;
            bool kitDone = KitImported;
            bool idle = !IsBusy;

            DrawStep(1, "Import project settings", settingsDone,
                "Applies the recommended Input, Physics, Tags and Layers, Quality and Time settings. Those project settings files are overwritten, so do this on a new project or to reset them.",
                settingsDone ? "Reimport Settings" : "Import Settings",
                delegate { ImportArchive(SETTINGS_PACKAGE_FILE); }, idle);

            DrawStep(2, "Import Open MMORPG", kitDone,
                "Downloads the kit release and imports it into " + KIT_FOLDER + ", along with the Unity packages it needs.",
                kitDone ? "Reimport Open MMORPG" : "Import Open MMORPG",
                delegate { DownloadAndImportKit(); }, idle);

            DrawStep(3, "Customize with addons", false,
                "Browse and install community addons from the Addon Manager. Available once the kit is imported.",
                "Open Addon Manager",
                delegate { EditorApplication.ExecuteMenuItem(ADDON_MANAGER_MENU); },
                kitDone && idle);

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                GUILayout.Label(statusMessage, bodyStyle);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawStep(int number, string title, bool done, string body, string buttonLabel, System.Action action, bool enabled = true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            DrawStepBadge(number, done);
            GUILayout.Space(8);
            GUILayout.Label(title, stepTitleStyle);
            GUILayout.FlexibleSpace();
            if (done)
                GUILayout.Label("Installed", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(32);
            GUILayout.Label(body, bodyStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(32);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(buttonLabel, GUILayout.Width(190), GUILayout.Height(24)))
                    action();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.EndVertical();
            GUILayout.Space(16);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        private void DrawStepBadge(int number, bool done)
        {
            Rect rect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
            Color fill = done
                ? new Color(0.16f, 0.55f, 0.24f)
                : (EditorGUIUtility.isProSkin ? new Color(0.32f, 0.32f, 0.32f) : new Color(0.62f, 0.62f, 0.62f));
            EditorGUI.DrawRect(rect, fill);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUI.Label(rect, done ? "✓" : number.ToString(), style);
        }

        private void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            rect.xMin += 16;
            rect.xMax -= 16;
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.15f));
        }

        private void DrawFooter()
        {
            DrawSeparator();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);

            if (GUILayout.Button("Documentation", EditorStyles.miniButton, GUILayout.Width(110)))
                Application.OpenURL(DOCS_URL);
            if (GUILayout.Button("GitHub", EditorStyles.miniButton, GUILayout.Width(80)))
                Application.OpenURL(KIT_REPO_URL);
            if (GUILayout.Button("Report an Issue", EditorStyles.miniButton, GUILayout.Width(110)))
                Application.OpenURL(ISSUES_URL);

            GUILayout.FlexibleSpace();
            GUILayout.Space(16);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);

            EditorGUI.BeginChangeCheck();
            autoShow = GUILayout.Toggle(autoShow, " Show this window when the project opens");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(ProjectKey("AutoShow"), autoShow);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(100), GUILayout.Height(24)))
                Close();

            GUILayout.Space(16);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
        }

        private void ImportArchive(string fileName)
        {
            string path = PackageFilePath(fileName);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("File Missing",
                    "Cannot find " + fileName + " inside the " + PACKAGE_NAME + " package.\n\n" +
                    "Reinstall the installer package through Window > Package Manager, or download the archive from " + KIT_REPO_URL + "/releases.",
                    "OK");
                return;
            }

            AssetDatabase.ImportPackage(path, true);
        }

        #region Kit download

        // The kit is not shipped inside this package. It is published as a release asset
        // so the installer stays small and the kit can be updated on its own.
        private UnityWebRequest kitRequest;
        private string statusMessage = "";

        private bool IsBusy
        {
            get { return kitRequest != null; }
        }

        /// <summary>Release asset matching this installer version, when the version is known.</summary>
        private string KitArchiveUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(packageVersion))
                    return INSTALLER_REPO_URL + "/releases/download/v" + packageVersion + "/" + KIT_ARCHIVE_NAME;
                return LatestKitArchiveUrl;
            }
        }

        private static string LatestKitArchiveUrl
        {
            get { return INSTALLER_REPO_URL + "/releases/latest/download/" + KIT_ARCHIVE_NAME; }
        }

        private void DownloadAndImportKit()
        {
            StartKitDownload(KitArchiveUrl);
        }

        private void StartKitDownload(string url)
        {
            if (IsBusy)
                return;

            string tempPath = "Temp/" + KIT_ARCHIVE_NAME;
            statusMessage = "Downloading the kit...";

            kitRequest = UnityWebRequest.Get(url);
            EditorApplication.update += RepaintWhileDownloading;

            kitRequest.SendWebRequest().completed += _ =>
            {
                UnityWebRequest request = kitRequest;
                kitRequest = null;
                EditorApplication.update -= RepaintWhileDownloading;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error = request.error;
                    request.Dispose();

                    // A tag without a published asset would otherwise dead-end here, so
                    // fall back to whatever the newest release offers.
                    if (url != LatestKitArchiveUrl)
                    {
                        Debug.LogWarning("[Open MMORPG] no kit archive at " + url + " (" + error + "); trying the latest release instead.");
                        StartKitDownload(LatestKitArchiveUrl);
                        return;
                    }

                    statusMessage = "";
                    Debug.LogError("[Open MMORPG] could not download the kit from " + url + ": " + error);
                    EditorUtility.DisplayDialog("Download Failed",
                        "Could not download " + KIT_ARCHIVE_NAME + ".\n\n" + error +
                        "\n\nCheck your connection, or download it yourself from " + INSTALLER_REPO_URL + "/releases and import it through Assets > Import Package > Custom Package.",
                        "OK");
                    Repaint();
                    return;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                    File.WriteAllBytes(tempPath, request.downloadHandler.data);
                }
                catch (System.Exception e)
                {
                    statusMessage = "";
                    Debug.LogError("[Open MMORPG] could not write the downloaded kit: " + e.Message);
                    request.Dispose();
                    Repaint();
                    return;
                }

                request.Dispose();
                statusMessage = "Importing the kit...";
                Repaint();
                ImportDownloadedKit(tempPath);
            };
        }

        private void RepaintWhileDownloading()
        {
            if (kitRequest == null)
                return;

            statusMessage = string.Format("Downloading the kit... {0:P0}", kitRequest.downloadProgress);
            Repaint();
        }

        /// <summary>
        /// Imports the downloaded archive. ImportPackage is asynchronous, so the file can
        /// only be deleted once Unity reports it is finished with it.
        /// </summary>
        private void ImportDownloadedKit(string tempPath)
        {
            AssetDatabase.ImportPackageCallback onCompleted = null;
            AssetDatabase.ImportPackageCallback onCancelled = null;
            AssetDatabase.ImportPackageFailedCallback onFailed = null;

            System.Action finish = () =>
            {
                AssetDatabase.importPackageCompleted -= onCompleted;
                AssetDatabase.importPackageCancelled -= onCancelled;
                AssetDatabase.importPackageFailed -= onFailed;
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (System.Exception)
                {
                    //a leftover file under Temp is harmless
                }
            };

            onCompleted = _ => { finish(); statusMessage = ""; Repaint(); };
            onCancelled = _ => { finish(); statusMessage = ""; Repaint(); };
            onFailed = (name, error) =>
            {
                finish();
                statusMessage = "";
                Debug.LogError("[Open MMORPG] failed to import " + name + ": " + error);
                Repaint();
            };

            AssetDatabase.importPackageCompleted += onCompleted;
            AssetDatabase.importPackageCancelled += onCancelled;
            AssetDatabase.importPackageFailed += onFailed;

            AssetDatabase.ImportPackage(tempPath, true);
        }

        #endregion
    }
}

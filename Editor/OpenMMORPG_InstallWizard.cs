using System.IO;
using UnityEditor;
using UnityEngine;

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
        private const string KIT_PACKAGE_FILE = "OpenMMORPG.unitypackage";
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

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += () =>
            {
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
            };
        }

        [MenuItem("Open MMORPG/Install/Show Setup Wizard", false, -1000)]
        public static void ShowWizard()
        {
            OpenMMORPG_InstallWizard window = GetWindow<OpenMMORPG_InstallWizard>(true, "Welcome to Open MMORPG");
            window.minSize = new Vector2(620, 620);
            window.maxSize = new Vector2(620, 620);
            window.Show();
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

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            DrawSteps();
            GUILayout.FlexibleSpace();
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

            DrawStep(1, "Import project settings", settingsDone,
                "Applies the recommended Input, Physics, Tags and Layers, Quality and Time settings. Those project settings files are overwritten, so do this on a new project or to reset them.",
                settingsDone ? "Reimport Settings" : "Import Settings",
                delegate { ImportArchive(SETTINGS_PACKAGE_FILE); });

            DrawStep(2, "Import Open MMORPG", kitDone,
                "Imports the kit into " + KIT_FOLDER + " and adds the Unity packages it needs. Gathering the contents takes a moment after you click.",
                kitDone ? "Reimport Open MMORPG" : "Import Open MMORPG",
                delegate { ImportArchive(KIT_PACKAGE_FILE); });

            DrawStep(3, "Customize with addons", false,
                "Browse and install community addons from the Addon Manager. Available once the kit is imported.",
                "Open Addon Manager",
                delegate { EditorApplication.ExecuteMenuItem(ADDON_MANAGER_MENU); },
                kitDone);
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
    }
}

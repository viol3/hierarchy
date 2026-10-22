using UnityEditor;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // Per-user display options, stored in EditorPrefs and edited under Preferences > Viol3's Hierarchy.
    internal static class HierarchySettings
    {
        const string Prefix = "Viol3.Hierarchy.";

        public static bool Enabled { get => Get(nameof(Enabled)); set => Set(nameof(Enabled), value); }
        public static bool ComponentIcons { get => Get(nameof(ComponentIcons)); set => Set(nameof(ComponentIcons), value); }
        public static bool SmartIcons { get => Get(nameof(SmartIcons)); set => Set(nameof(SmartIcons), value); }
        public static bool Toolbar { get => Get(nameof(Toolbar)); set => Set(nameof(Toolbar), value); }
        public static bool DefaultParentKey { get => Get(nameof(DefaultParentKey)); set => Set(nameof(DefaultParentKey), value); }
        public static bool IsolateKey { get => Get(nameof(IsolateKey)); set => Set(nameof(IsolateKey), value); }
        public static bool GradientColors { get => Get(nameof(GradientColors)); set => Set(nameof(GradientColors), value); }
        public static bool ActiveToggle { get => Get(nameof(ActiveToggle)); set => Set(nameof(ActiveToggle), value); }
        public static bool TreeLines { get => Get(nameof(TreeLines)); set => Set(nameof(TreeLines), value); }
        public static bool ZebraStripes { get => Get(nameof(ZebraStripes)); set => Set(nameof(ZebraStripes), value); }
        public static bool FolderIcons { get => Get(nameof(FolderIcons)); set => Set(nameof(FolderIcons), value); }
        public static bool Headers { get => Get(nameof(Headers)); set => Set(nameof(Headers), value); }

        public static string HeaderPrefix
        {
            get => EditorPrefs.GetString(Prefix + nameof(HeaderPrefix), "---");
            set => EditorPrefs.SetString(Prefix + nameof(HeaderPrefix), value);
        }

        static bool Get(string key) => EditorPrefs.GetBool(Prefix + key, true);
        static void Set(string key, bool value) => EditorPrefs.SetBool(Prefix + key, value);

        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Viol3's Hierarchy", SettingsScope.User)
            {
                guiHandler = _ => DrawGUI(),
                keywords = new[] { "hierarchy", "icons", "toggle", "tree", "lines", "zebra", "folder", "header", "color" },
            };
        }

        static void DrawGUI()
        {
            EditorGUIUtility.labelWidth = 200f;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10f);

            EditorGUI.BeginChangeCheck();
            Enabled = EditorGUILayout.Toggle(new GUIContent("Enabled", "Turn every Hierarchy enhancement on or off."), Enabled);
            using (new EditorGUI.DisabledScope(!Enabled))
            {
                EditorGUILayout.Space();
                ComponentIcons = EditorGUILayout.Toggle(new GUIContent("Component Icons",
                    "Show component icons on the right. Click an icon to enable or disable that component, Ctrl/Cmd+click to open it in a small window."), ComponentIcons);
                SmartIcons = EditorGUILayout.Toggle(new GUIContent("Smart Row Icons",
                    "Replace the default cube icon with the icon of the object's main component, like a camera or a light."), SmartIcons);
                ActiveToggle = EditorGUILayout.Toggle(new GUIContent("Active Toggle",
                    "Show a checkbox that turns the GameObject (or every selected one) on and off."), ActiveToggle);
                TreeLines = EditorGUILayout.Toggle(new GUIContent("Tree Lines", "Draw lines connecting children to their parents."), TreeLines);
                ZebraStripes = EditorGUILayout.Toggle(new GUIContent("Zebra Stripes", "Shade every other row."), ZebraStripes);
                FolderIcons = EditorGUILayout.Toggle(new GUIContent("Folder Icons",
                    "Show a folder icon for empty GameObjects that only group other objects."), FolderIcons);
                Headers = EditorGUILayout.Toggle(new GUIContent("Headers",
                    "Draw GameObjects whose name starts with the header prefix as section headers."), Headers);
                using (new EditorGUI.DisabledScope(!Headers))
                {
                    EditorGUI.indentLevel++;
                    HeaderPrefix = EditorGUILayout.DelayedTextField(new GUIContent("Header Prefix"), HeaderPrefix);
                    EditorGUI.indentLevel--;
                }
                Toolbar = EditorGUILayout.Toggle(new GUIContent("Toolbar",
                    "Add folder, header and collapse buttons to the Hierarchy toolbar and fold the search field into an icon."), Toolbar);
                DefaultParentKey = EditorGUILayout.Toggle(new GUIContent("Default Parent Key (D)",
                    "Press D over a row to make it the default parent that new and dragged-in objects are placed under."), DefaultParentKey);
                IsolateKey = EditorGUILayout.Toggle(new GUIContent("Isolate Key (Shift+E)",
                    "Press Shift+E over a row to collapse everything else and open the path down to it."), IsolateKey);
                GradientColors = EditorGUILayout.Toggle(new GUIContent("Gradient Row Colors",
                    "Fade row colors out from left to right instead of filling the whole row."), GradientColors);
            }
            if (EditorGUI.EndChangeCheck())
                EditorApplication.RepaintHierarchyWindow();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Row Styles", EditorStyles.boldLabel);
            HierarchyStyles styles = HierarchyStyles.instance;
            EditorGUILayout.LabelField("Styled objects", styles.Count.ToString());
            EditorGUILayout.HelpBox("Row colors and icons are saved in ProjectSettings/Viol3Hierarchy.asset, so they are shared with your team " +
                "through version control. Alt+click a row in the Hierarchy to set them.", MessageType.None);
            using (new EditorGUI.DisabledScope(styles.Count == 0))
            {
                if (GUILayout.Button("Clear All Row Styles", GUILayout.Width(200f)) &&
                    EditorUtility.DisplayDialog("Clear Row Styles", "Remove every row color and icon in this project?", "Clear", "Cancel"))
                {
                    styles.ClearAll();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}

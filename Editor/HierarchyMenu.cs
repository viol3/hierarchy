using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Viol3.Hierarchy
{
    // Hierarchy right-click menu: row colors, section headers and folders.
    internal static class HierarchyMenu
    {
        const string Root = "GameObject/Viol3's Hierarchy/";
        const string ColorRoot = Root + "Color/";
        const int Priority = 10;

        static bool batchRunning;

        [MenuItem(ColorRoot + "Red", false, Priority)] static void Red() => ApplyColor(StylePalette.Colors[0]);
        [MenuItem(ColorRoot + "Orange", false, Priority)] static void Orange() => ApplyColor(StylePalette.Colors[1]);
        [MenuItem(ColorRoot + "Yellow", false, Priority)] static void Yellow() => ApplyColor(StylePalette.Colors[2]);
        [MenuItem(ColorRoot + "Green", false, Priority)] static void Green() => ApplyColor(StylePalette.Colors[3]);
        [MenuItem(ColorRoot + "Teal", false, Priority)] static void Teal() => ApplyColor(StylePalette.Colors[4]);
        [MenuItem(ColorRoot + "Blue", false, Priority)] static void Blue() => ApplyColor(StylePalette.Colors[5]);
        [MenuItem(ColorRoot + "Purple", false, Priority)] static void Purple() => ApplyColor(StylePalette.Colors[6]);
        [MenuItem(ColorRoot + "Pink", false, Priority)] static void Pink() => ApplyColor(StylePalette.Colors[7]);
        [MenuItem(ColorRoot + "Gray", false, Priority)] static void Gray() => ApplyColor(StylePalette.Colors[8]);
        [MenuItem(ColorRoot + "None", false, Priority + 11)] static void None() => ApplyColor(null);

        [MenuItem(ColorRoot + "Red", true)]
        [MenuItem(ColorRoot + "Orange", true)]
        [MenuItem(ColorRoot + "Yellow", true)]
        [MenuItem(ColorRoot + "Green", true)]
        [MenuItem(ColorRoot + "Teal", true)]
        [MenuItem(ColorRoot + "Blue", true)]
        [MenuItem(ColorRoot + "Purple", true)]
        [MenuItem(ColorRoot + "Pink", true)]
        [MenuItem(ColorRoot + "Gray", true)]
        [MenuItem(ColorRoot + "None", true)]
        static bool HasSelection() => Selection.gameObjects.Length > 0;

        [MenuItem(Root + "Create Header", false, Priority)]
        static void CreateHeaderMenu()
        {
            if (BeginBatch())
                CreateHeader();
        }

        [MenuItem(Root + "Create Folder", false, Priority)]
        static void CreateFolderMenu()
        {
            if (BeginBatch())
                CreateFolder();
        }

        // A header goes above the selected object, so it introduces it.
        public static void CreateHeader()
        {
            var go = CreateNextToSelection(HierarchySettings.HeaderPrefix + " Header", "Create Header", 0);
            go.tag = "EditorOnly";
            BeginRename();
        }

        // A folder goes below the selected object and gets the folder icon right away, before it has any children.
        public static void CreateFolder()
        {
            var go = CreateNextToSelection("New Folder", "Create Folder", 1);
            HierarchyStyles.instance.SetIcon(new[] { go }, "Folder Icon");
            BeginRename();
        }

        // Creates an empty object beside the selected one (offset 0 = above, 1 = below), or at the root of the
        // scene or prefab being edited, and selects it.
        static GameObject CreateNextToSelection(string name, string undoName, int offset)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, undoName);

            Transform anchor = Selection.activeTransform;
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (anchor != null)
            {
                if (anchor.parent != null)
                    go.transform.SetParent(anchor.parent, false);
                else if (go.scene != anchor.gameObject.scene)
                    SceneManager.MoveGameObjectToScene(go, anchor.gameObject.scene);
                go.transform.SetSiblingIndex(anchor.GetSiblingIndex() + offset);
            }
            else if (stage != null)
            {
                go.transform.SetParent(stage.prefabContentsRoot.transform, false);
            }

            Selection.activeGameObject = go;
            return go;
        }

        // Starts renaming the new object once the Hierarchy has caught up with the selection.
        static void BeginRename()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
                EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Rename");
            };
        }

        static void ApplyColor(Color? color)
        {
            if (!BeginBatch())
                return;

            int skipped = HierarchyStyles.instance.SetColor(Selection.gameObjects, color);
            if (skipped > 0)
                Debug.LogWarning($"[Viol3's Hierarchy] {skipped} object(s) couldn't be colored. Save the scene first; row colors aren't supported in Prefab Mode.");
            EditorApplication.RepaintHierarchyWindow();
        }

        // The Hierarchy context menu runs an item once per selected object; only the first call does the work.
        static bool BeginBatch()
        {
            if (batchRunning)
                return false;
            batchRunning = true;
            EditorApplication.delayCall += () => batchRunning = false;
            return true;
        }
    }
}

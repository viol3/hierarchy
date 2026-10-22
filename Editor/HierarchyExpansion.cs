using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Viol3.Hierarchy
{
    // Expands and collapses rows in a Hierarchy window. Unity keeps this internal, so it goes through reflection.
    internal static class HierarchyExpansion
    {
        const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Type WindowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static readonly FieldInfo SceneHierarchyField = WindowType?.GetField("m_SceneHierarchy", MemberFlags);

        static bool warned;

        public static bool IsHierarchyWindow(EditorWindow window) => window != null && WindowType != null && WindowType.IsInstanceOfType(window);

        // Collapses every object, leaving the scenes (or the prefab root in Prefab Mode) open.
        public static void CollapseAll(EditorWindow window) => SetExpanded(window, null);

        // Collapses everything except the path down to the object. The object is expanded and its children keep
        // whatever state they had.
        public static void Isolate(EditorWindow window, GameObject go) => SetExpanded(window, go.transform);

        static void SetExpanded(EditorWindow window, Transform focus)
        {
            try
            {
                object hierarchy = SceneHierarchyField.GetValue(window);
                var expanded = (int[])hierarchy.GetType().GetMethod("GetExpandedIDs", MemberFlags).Invoke(hierarchy, null);

                var roots = new HashSet<int>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    roots.Add(SceneManager.GetSceneAt(i).handle);
                PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    roots.Add(stage.scene.handle);
                    roots.Add(stage.prefabContentsRoot.GetInstanceID());
                }

                var keep = new HashSet<int>(expanded.Where(id => roots.Contains(id) || IsInside(id, focus)));
                for (Transform t = focus; t != null; t = t.parent)
                    keep.Add(t.gameObject.GetInstanceID());

                // The tree view looks expanded IDs up with a binary search, so they have to be sorted.
                int[] ids = keep.ToArray();
                Array.Sort(ids);
                object dataSource = hierarchy.GetType().GetProperty("dataSource", MemberFlags).GetValue(hierarchy);
                dataSource.GetType().GetMethod("SetExpandedIDs", MemberFlags).Invoke(dataSource, new object[] { ids });
                window.Repaint();
            }
            catch (Exception e)
            {
                if (!warned)
                    Debug.LogWarning($"[Viol3's Hierarchy] Collapsing isn't supported in this Unity version: {e.GetBaseException().Message}");
                warned = true;
            }
        }

        static bool IsInside(int id, Transform focus)
        {
            return focus != null && EditorUtility.InstanceIDToObject(id) is GameObject go && go.transform.IsChildOf(focus);
        }
    }
}

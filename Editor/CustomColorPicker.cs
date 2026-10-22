using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // Opens Unity's color picker for the palette's + button. The rows preview the color while it is picked, and the
    // final color is added to the palette when the picker closes. Unity's ColorPicker is internal, so it is reached
    // through reflection; the palette popup can't host a ColorField because it closes as soon as the picker opens.
    internal static class CustomColorPicker
    {
        static readonly Type PickerType = typeof(Editor).Assembly.GetType("UnityEditor.ColorPicker");
        static readonly MethodInfo Show = PickerType?.GetMethod("Show", BindingFlags.Static | BindingFlags.Public,
            null, new[] { typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool), typeof(bool) }, null);
        static readonly PropertyInfo Visible = PickerType?.GetProperty("visible", BindingFlags.Static | BindingFlags.Public);

        public static void Open(GameObject[] targets, bool recursive)
        {
            if (Show == null || Visible == null)
            {
                Debug.LogWarning("[Viol3's Hierarchy] Unity's color picker isn't available in this Unity version.");
                return;
            }

            // Remember the current colors so a cancelled pick (Esc puts the start color back) can restore them.
            var originals = new List<(GameObject go, HierarchyStyles.Entry style)>();
            foreach (GameObject go in targets)
                originals.Add((go, HierarchyStyles.instance.Get(go)));
            HierarchyStyles.Entry first = originals.Count > 0 ? originals[0].style : null;
            Color start = first != null && first.HasColor ? first.color : StylePalette.Colors[5];
            Color picked = start;

            Action<Color> changed = color =>
            {
                color.a = 1f;
                picked = color;
                HierarchyStyles.instance.SetColor(targets, color, recursive, save: false);
                EditorApplication.RepaintHierarchyWindow();
            };
            Show.Invoke(null, new object[] { changed, start, false, false, false });

            EditorApplication.CallbackFunction waitForClose = null;
            waitForClose = () =>
            {
                if ((bool)Visible.GetValue(null))
                    return;
                EditorApplication.update -= waitForClose;
                Finish(targets, recursive, start, picked, originals);
            };
            EditorApplication.update += waitForClose;
        }

        static void Finish(GameObject[] targets, bool recursive, Color start, Color picked,
            List<(GameObject go, HierarchyStyles.Entry style)> originals)
        {
            if (picked == start)
            {
                foreach ((GameObject go, HierarchyStyles.Entry style) in originals)
                {
                    if (go == null)
                        continue;
                    bool hadColor = style != null && style.HasColor;
                    HierarchyStyles.instance.SetColor(new[] { go }, hadColor ? style.color : (Color?)null, hadColor && style.recursive);
                }
            }
            else
            {
                HierarchyStyles.instance.AddCustomColor(picked);
                HierarchyStyles.instance.SetColor(targets, picked, recursive);
            }
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}

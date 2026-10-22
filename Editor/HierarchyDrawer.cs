using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // Draws on top of every Hierarchy row: headers, row icons, row colors, stripes, tree lines, component icons and the active toggle.
    [InitializeOnLoad]
    internal static class HierarchyDrawer
    {
        const float Indent = 14f;
        const float IconSize = 16f;
        // The scene visibility and picking toggles live left of this.
        const float LeftGutter = 32f;
        const float ColorAlpha = 0.4f;
        const float GradientAlpha = 0.8f;
        const float GradientTail = 0.18f;
        const float GradientWidth = 300f;
        // Children colored through their parent are drawn dimmer, so the parent reads as the group's header.
        const float InheritedStrength = 0.55f;

        static readonly List<Component> Components = new List<Component>();
        static readonly GUIContent NameContent = new GUIContent();
        static readonly GUIContent TooltipContent = new GUIContent();
        static readonly Dictionary<string, GUIStyle> NameStyles = new Dictionary<string, GUIStyle>();
        static GUIContent missingContent;
        static Texture folderIcon;
        static Texture2D gradientTexture;
        static GUIStyle headerStyle;

        static bool Pro => EditorGUIUtility.isProSkin;
        static Color LineColor => Pro ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.25f);
        static Color StripeColor => Pro ? new Color(1f, 1f, 1f, 0.04f) : new Color(0f, 0f, 0f, 0.05f);
        static Color HeaderColor => Pro ? new Color(0.32f, 0.32f, 0.32f) : new Color(0.6f, 0.6f, 0.6f);
        static Color SelectedColor => Pro ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.69f);
        static Color UnfocusedColor => Pro ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.68f, 0.68f, 0.68f);
        static Color HoverColor => Pro ? new Color(0.27f, 0.27f, 0.27f) : new Color(0.7f, 0.7f, 0.7f);
        static Color BackgroundColor => Pro ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.78f, 0.78f, 0.78f);

        static HierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnItemGUI;
            EditorSceneManager.sceneOpened += (scene, mode) => HierarchyStyles.instance.ClearCache();
        }

        static void OnItemGUI(int instanceID, Rect rect)
        {
            if (!HierarchySettings.Enabled)
                return;
            // Duplicate, Copy, Cut and Paste reach the Hierarchy as commands while its rows are drawn.
            StyleDuplication.OnCommand(Event.current);
            // Scene header rows have no GameObject behind them.
            if (!(EditorUtility.InstanceIDToObject(instanceID) is GameObject go))
                return;

            HierarchyStyles.Entry style = HierarchyStyles.instance.Get(go);
            bool hasColor = style != null && style.HasColor;
            if (HierarchySettings.Headers && IsHeader(go))
            {
                DrawHeader(go, instanceID, rect, hasColor ? style.color : HeaderColor);
                HandlePaletteClick(go, rect);
                return;
            }

            if (IsStriped(rect))
                EditorGUI.DrawRect(FullRow(rect), StripeColor);

            float alpha = go.activeInHierarchy ? 1f : 0.5f;
            Texture icon = RowIcon(go, style);
            Color rowColor = hasColor ? style.color : Color.clear;
            float strength = 1f;
            if (!hasColor && TryGetInheritedColor(go.transform, out rowColor))
                strength = InheritedStrength;
            bool defaultParent = IsDefaultParent(go);
            // Selected rows keep Unity's plain selection highlight across the whole row.
            if ((rowColor.a > 0f || defaultParent) && !Selection.Contains(instanceID))
                DrawColoredRow(go, instanceID, rect, rowColor, strength, icon != null ? icon : PrefabUtility.GetIconForGameObject(go), alpha, defaultParent);
            else if (icon != null)
                DrawRowIcon(instanceID, rect, icon, alpha);
            if (HierarchySettings.TreeLines)
                DrawTreeLines(go.transform, rect);

            float right = rect.xMax;
            if (HierarchySettings.ActiveToggle)
            {
                right -= IconSize;
                DrawActiveToggle(go, new Rect(right, rect.y, IconSize, rect.height));
            }
            if (defaultParent)
                DrawDefaultParentLabel(go, rect);
            if (HierarchySettings.ComponentIcons)
                DrawComponentIcons(go, rect, right, defaultParent);

            // Runs last so clicks on the toggle and component icons are already taken.
            HandlePaletteClick(go, rect);
            HandleDefaultParentKey(go, rect, defaultParent);
            HandleIsolateKey(go, rect);
        }

        // Shift+E with the mouse over a row collapses everything else and opens the path down to it.
        static void HandleIsolateKey(GameObject go, Rect rect)
        {
            Event e = Event.current;
            const EventModifiers others = EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;
            if (!HierarchySettings.IsolateKey || e.type != EventType.KeyDown || e.keyCode != KeyCode.E ||
                !e.shift || (e.modifiers & others) != 0 || EditorGUIUtility.editingTextField)
                return;
            if (e.mousePosition.y < rect.y || e.mousePosition.y >= rect.yMax)
                return;

            // Keys go to the focused window, which is the Hierarchy being drawn.
            EditorWindow window = HierarchyExpansion.IsHierarchyWindow(EditorWindow.focusedWindow)
                ? EditorWindow.focusedWindow
                : EditorWindow.mouseOverWindow;
            if (!HierarchyExpansion.IsHierarchyWindow(window))
                return;

            HierarchyExpansion.Isolate(window, go);
            e.Use();
        }

        #region Default parent

        static readonly MethodInfo GetDefaultParentForSession = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchy")
            ?.GetMethod("GetDefaultParentForSession", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly GUIContent DefaultParentContent = new GUIContent("Default parent");
        static GUIStyle defaultParentStyle;

        // Reads Unity's own default parent, so one set from Unity's context menu shows up here too.
        static bool IsDefaultParent(GameObject go)
        {
            if (GetDefaultParentForSession == null || !go.scene.IsValid())
                return false;
            string sceneGuid = AssetDatabase.AssetPathToGUID(go.scene.path);
            return (int)GetDefaultParentForSession.Invoke(null, new object[] { sceneGuid }) == go.GetInstanceID();
        }

        static float NameEnd(GameObject go, Rect rect, GUIStyle style)
        {
            NameContent.text = go.name;
            return rect.x + IconSize + 2f + style.CalcSize(NameContent).x + 4f;
        }

        static GUIStyle DefaultParentStyle => defaultParentStyle ?? (defaultParentStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.4f) : new Color(0f, 0f, 0f, 0.45f) },
        });

        static float DefaultParentLabelEnd(GameObject go, Rect rect) =>
            NameEnd(go, rect, EditorStyles.boldLabel) + 2f + DefaultParentStyle.CalcSize(DefaultParentContent).x;

        static void DrawDefaultParentLabel(GameObject go, Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            float x = NameEnd(go, rect, EditorStyles.boldLabel) + 2f;
            GUI.Label(new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x), rect.height), DefaultParentContent, DefaultParentStyle);
        }

        // Pressing D with the mouse over a row makes it the default parent of its scene, or clears it.
        static void HandleDefaultParentKey(GameObject go, Rect rect, bool isDefaultParent)
        {
            Event e = Event.current;
            const EventModifiers held = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;
            if (!HierarchySettings.DefaultParentKey || e.type != EventType.KeyDown || e.keyCode != KeyCode.D ||
                (e.modifiers & held) != 0 || EditorGUIUtility.editingTextField)
                return;
            if (e.mousePosition.y < rect.y || e.mousePosition.y >= rect.yMax)
                return;

            if (isDefaultParent)
                EditorUtility.ClearDefaultParentObject(go.scene);
            else
                EditorUtility.SetDefaultParentObject(go);
            e.Use();
            EditorApplication.RepaintHierarchyWindow();
        }

        #endregion

        static void HandlePaletteClick(GameObject go, Rect rect)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !e.alt || !rect.Contains(e.mousePosition))
                return;

            // Styling a selected object styles the whole selection.
            GameObject[] targets = Selection.Contains(go) ? Selection.gameObjects : new[] { go };
            PopupWindow.Show(new Rect(rect.x, rect.y, IconSize, rect.height), new StylePalette(targets));
            e.Use();
        }

        static Rect FullRow(Rect rect) => new Rect(LeftGutter, rect.y, rect.xMax - LeftGutter, rect.height);

        // Unity draws the row before this callback runs, so a tint on top would wash out the name.
        // Instead cover the row, lay down the color, then draw the icon and name again on top of it.
        static void DrawColoredRow(GameObject go, int instanceID, Rect rect, Color color, float strength, Texture icon, float alpha, bool bold)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.xMax - rect.x, rect.height), RowBackground(instanceID, rect));
            if (color.a > 0f)
                DrawRowColor(rect, color, strength);

            if (icon != null)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(rect.x, rect.y, IconSize, rect.height), icon, ScaleMode.ScaleToFit);
                GUI.color = previous;
            }

            NameContent.text = go.name;
            var labelRect = new Rect(rect.x + LabelOffset, rect.y, rect.xMax - rect.x - LabelOffset, rect.height);
            NameStyle(go, bold).Draw(labelRect, NameContent, false, false, Selection.Contains(instanceID), HierarchyFocused());
        }

        // Unity's own row label styles, so prefab, broken prefab, inactive and default parent names look the same.
        // GameObjectTreeViewGUI.OnContentGUI picks them in this order: the default parent always uses the bold line
        // style, otherwise the prefab state and active state decide.
        static GUIStyle NameStyle(GameObject go, bool bold)
        {
            string field;
            if (bold)
                field = "lineBoldStyle";
            else if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.Connected)
                field = go.activeInHierarchy ? "prefabLabel" : "disabledPrefabLabel";
            else if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                field = go.activeInHierarchy ? "brokenPrefabLabel" : "disabledBrokenPrefabLabel";
            else
                field = go.activeInHierarchy ? "lineStyle" : "disabledLabel";

            if (NameStyles.TryGetValue(field, out GUIStyle style))
                return style;

            Type owner = field.StartsWith("line") ? TreeViewStylesType : GameObjectStylesType;
            var unityStyle = owner?.GetField(field, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) as GUIStyle;
            // Unity draws the label with no left padding and moves the rect past the icon instead.
            style = new GUIStyle(unityStyle ?? FindSkinStyle(SkinStyleNames[field])) { padding = { left = 0 } };
            if (bold)
                style.fontStyle = FontStyle.Bold;
            // Unity creates its styles lazily; only keep the copy once the real one exists.
            if (unityStyle != null)
                NameStyles[field] = style;
            return style;
        }

        static readonly Type TreeViewStylesType = typeof(Editor).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewGUI+Styles");
        static readonly Type GameObjectStylesType = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewGUI+GameObjectStyles");

        static readonly Dictionary<string, string> SkinStyleNames = new Dictionary<string, string>
        {
            { "lineStyle", "PR Label" },
            { "lineBoldStyle", "PR Label" },
            { "disabledLabel", "PR DisabledLabel" },
            { "prefabLabel", "PR PrefabLabel" },
            { "disabledPrefabLabel", "PR DisabledPrefabLabel" },
            { "brokenPrefabLabel", "PR BrokenPrefabLabel" },
            { "disabledBrokenPrefabLabel", "PR DisabledBrokenPrefabLabel" },
        };

        static float labelOffset = -1f;

        // How far the name starts after the icon's left edge: the icon, its padding and the gap before the text,
        // read from the Hierarchy's own tree view so the redrawn name lands exactly on Unity's.
        static float LabelOffset
        {
            get
            {
                if (labelOffset >= 0f)
                    return labelOffset;
                try
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    Type windowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                    object window = Resources.FindObjectsOfTypeAll(windowType).FirstOrDefault();
                    object hierarchy = windowType.GetField("m_SceneHierarchy", flags).GetValue(window);
                    object treeView = hierarchy.GetType().GetProperty("treeView", flags).GetValue(hierarchy);
                    object gui = treeView.GetType().GetProperty("gui", flags).GetValue(treeView);
                    Type guiType = gui.GetType();
                    float iconWidth = (float)FindField(guiType, "k_IconWidth").GetValue(gui);
                    float space = (float)FindField(guiType, "k_SpaceBetweenIconAndText").GetValue(gui);
                    float padding = (float)guiType.GetProperty("iconTotalPadding", flags).GetValue(gui);
                    labelOffset = iconWidth + space + padding;
                }
                catch (Exception)
                {
                    // Unity's defaults: a 16px icon and 2px before the text.
                    return IconSize + 2f;
                }
                return labelOffset;
            }
        }

        static FieldInfo FindField(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field;
            }
            return null;
        }

        static GUIStyle FindSkinStyle(string name)
        {
            return GUI.skin.FindStyle(name) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(name) ?? EditorStyles.label;
        }

        // The nearest colored ancestor decides: its color carries down only if it was set for its children too.
        static bool TryGetInheritedColor(Transform transform, out Color color)
        {
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                HierarchyStyles.Entry entry = HierarchyStyles.instance.Get(parent.gameObject);
                if (entry == null || !entry.HasColor)
                    continue;
                color = entry.recursive ? entry.color : Color.clear;
                return entry.recursive;
            }
            color = Color.clear;
            return false;
        }

        static void DrawRowColor(Rect rect, Color color, float strength)
        {
            if (!HierarchySettings.GradientColors)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.xMax - rect.x, rect.height), new Color(color.r, color.g, color.b, ColorAlpha * strength));
                return;
            }

            if (gradientTexture == null)
            {
                gradientTexture = new Texture2D(128, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                // Drops off quickly, then settles on a faint tint that carries on to the end of the row.
                for (int i = 0; i < gradientTexture.width; i++)
                {
                    float t = i / (gradientTexture.width - 1f);
                    gradientTexture.SetPixel(i, 0, new Color(1f, 1f, 1f, Mathf.Lerp(GradientTail, 1f, Mathf.Pow(1f - t, 2.2f))));
                }
                gradientTexture.Apply();
            }

            // A fixed fade width keeps the look the same however wide the Hierarchy is.
            Rect row = FullRow(rect);
            var fade = new Rect(row.x, row.y, Mathf.Min(row.width, GradientWidth), row.height);
            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, GradientAlpha * strength);
            GUI.DrawTexture(fade, gradientTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
            if (row.xMax > fade.xMax)
                EditorGUI.DrawRect(new Rect(fade.xMax, row.y, row.xMax - fade.xMax, row.height), new Color(color.r, color.g, color.b, GradientAlpha * GradientTail * strength));
        }

        #region Headers

        static bool IsHeader(GameObject go)
        {
            string prefix = HierarchySettings.HeaderPrefix;
            return !string.IsNullOrEmpty(prefix) && go.name.StartsWith(prefix, System.StringComparison.Ordinal);
        }

        static void DrawHeader(GameObject go, int instanceID, Rect rect, Color color)
        {
            Rect row = FullRow(rect);
            Color background = Selection.Contains(instanceID) ? Color.Lerp(color, SelectedColor, 0.6f) : color;
            EditorGUI.DrawRect(row, background);

            if (headerStyle == null)
                headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            headerStyle.normal.textColor = luminance > 0.55f ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.95f, 0.95f, 0.95f);

            string label = go.name.Substring(HierarchySettings.HeaderPrefix.Length).Trim();
            GUI.Label(row, label.ToUpperInvariant(), headerStyle);
        }

        #endregion

        #region Tree lines and folders

        static void DrawTreeLines(Transform transform, Rect rect)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            Transform top = stage != null ? stage.prefabContentsRoot.transform : null;
            if (transform.parent == null || transform == top)
                return;

            Color lineColor = LineColor;
            float middle = Mathf.Floor(rect.y + rect.height * 0.5f);
            // Lines run through the centre of the parent's foldout arrow, which sits one indent left of our own arrow.
            float x = rect.x - Indent * 1.5f;

            float height = IsLastSibling(transform) ? middle - rect.y + 1f : rect.height;
            EditorGUI.DrawRect(new Rect(x, rect.y, 1f, height), lineColor);
            float end = transform.childCount > 0 ? rect.x - Indent : rect.x - 2f;
            EditorGUI.DrawRect(new Rect(x, middle, end - x, 1f), lineColor);

            for (Transform ancestor = transform.parent; ancestor.parent != null && ancestor != top; ancestor = ancestor.parent)
            {
                x -= Indent;
                if (!IsLastSibling(ancestor))
                    EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), lineColor);
            }
        }

        static bool IsLastSibling(Transform transform)
        {
            return transform.GetSiblingIndex() == transform.parent.childCount - 1;
        }

        static bool IsFolder(GameObject go)
        {
            if (go.transform.childCount == 0 || PrefabUtility.IsAnyPrefabInstanceRoot(go))
                return false;
            go.GetComponents(Components);
            return Components.Count == 1;
        }

        static Texture FolderIcon => folderIcon != null ? folderIcon : folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;

        // Picks the component that best describes the object, skipping the ones almost every object has.
        static bool TryGetMainComponent(GameObject go, out Component main)
        {
            main = null;
            // Keep Unity's prefab icon so prefab instances stay recognizable.
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                return false;

            go.GetComponents(Components);
            foreach (Component component in Components)
            {
                if (component == null || component is Transform || component is CanvasRenderer || component is MeshFilter ||
                    component is MeshRenderer || component is Collider || component is Rigidbody)
                    continue;
                main = component;
                return true;
            }
            return false;
        }

        // A picked icon wins over the folder icon, which wins over the main component's icon.
        static Texture RowIcon(GameObject go, HierarchyStyles.Entry style)
        {
            if (style != null && style.HasIcon)
                return StylePalette.LoadIcon(style.icon);
            if (HierarchySettings.FolderIcons && IsFolder(go))
                return FolderIcon;
            if (HierarchySettings.SmartIcons && TryGetMainComponent(go, out Component main))
                return AssetPreview.GetMiniThumbnail(main);
            return null;
        }

        static void DrawRowIcon(int instanceID, Rect rect, Texture icon, float alpha)
        {

            // Hide Unity's default icon under a patch of the row background, then draw ours on top.
            var iconRect = new Rect(rect.x, rect.y, IconSize, rect.height);
            EditorGUI.DrawRect(iconRect, RowBackground(instanceID, rect));
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.color = previous;
        }

        static Color RowBackground(int instanceID, Rect rect)
        {
            if (Selection.Contains(instanceID))
                return HierarchyFocused() ? SelectedColor : UnfocusedColor;

            float mouseY = Event.current.mousePosition.y;
            Color background = mouseY >= rect.y && mouseY < rect.yMax ? HoverColor : BackgroundColor;
            // Patches drawn with this color must match the stripe, or they show up as holes in it.
            if (IsStriped(rect))
            {
                Color stripe = StripeColor;
                background = Color.Lerp(background, new Color(stripe.r, stripe.g, stripe.b), stripe.a);
            }
            return background;
        }

        // Rows are a fixed height and start at 0, so the row index comes straight from the rect.
        static bool IsStriped(Rect rect) => HierarchySettings.ZebraStripes && Mathf.RoundToInt(rect.y / rect.height) % 2 == 1;

        static bool HierarchyFocused()
        {
            EditorWindow focused = EditorWindow.focusedWindow;
            return focused != null && focused.GetType().Name == "SceneHierarchyWindow";
        }

        #endregion

        #region Toggle and component icons

        static void DrawActiveToggle(GameObject go, Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            bool active = GUI.Toggle(rect, go.activeSelf, GUIContent.none);
            if (!EditorGUI.EndChangeCheck())
                return;

            // Toggling a selected object toggles the whole selection, like the Inspector does.
            GameObject[] targets = Selection.Contains(go) ? Selection.gameObjects : new[] { go };
            Undo.RecordObjects(targets, active ? "Activate GameObject" : "Deactivate GameObject");
            foreach (GameObject target in targets)
                target.SetActive(active);
        }

        static void DrawComponentIcons(GameObject go, Rect rect, float right, bool defaultParent)
        {
            float nameEnd = defaultParent ? DefaultParentLabelEnd(go, rect) : NameEnd(go, rect, EditorStyles.label);

            go.GetComponents(Components);
            Color previous = GUI.color;
            float alpha = go.activeInHierarchy ? 1f : 0.5f;

            // Walk backwards so the icons keep component order, ending at the right edge.
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                Component component = Components[i];
                if (component is Transform || component is CanvasRenderer)
                    continue;
                if (right - IconSize < nameEnd)
                    break;

                right -= IconSize;
                DrawComponentIcon(component, new Rect(right, rect.y, IconSize, rect.height), alpha);
            }
            GUI.color = previous;
        }

        static void DrawComponentIcon(Component component, Rect rect, float alpha)
        {
            if (component == null)
            {
                if (missingContent == null)
                    missingContent = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml").image, "Missing script");
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(rect, missingContent, GUIStyle.none);
                return;
            }

            // -1 means the component has no enabled checkbox.
            int enabled = EditorUtility.GetObjectEnabled(component);
            GUI.color = new Color(1f, 1f, 1f, enabled == 0 ? alpha * 0.35f : alpha);
            GUI.DrawTexture(rect, AssetPreview.GetMiniThumbnail(component), ScaleMode.ScaleToFit);

            string title = ObjectNames.GetInspectorTitle(component);
            TooltipContent.tooltip = enabled == -1
                ? $"{title}\nClick to open"
                : $"{title}\nClick to {(enabled == 1 ? "disable" : "enable")}, {(Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl")}+click to open";
            // Alt+click belongs to the style palette.
            if (Event.current.alt || !GUI.Button(rect, TooltipContent, GUIStyle.none))
                return;

            if (enabled == -1 || EditorGUI.actionKey)
            {
                ComponentWindow.Open(component, rect);
            }
            else
            {
                Undo.RecordObject(component, enabled == 1 ? "Disable Component" : "Enable Component");
                EditorUtility.SetObjectEnabled(component, enabled == 0);
            }
        }

        #endregion
    }
}

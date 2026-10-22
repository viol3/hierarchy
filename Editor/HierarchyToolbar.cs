using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viol3.Hierarchy
{
    // Covers the Hierarchy toolbar to the right of Unity's create button with our own buttons, and folds the
    // search field into an icon that brings Unity's search field back when clicked.
    // Unity has no API for any of this, so the bar is a UI Toolkit overlay and the window is driven through reflection.
    [InitializeOnLoad]
    internal static class HierarchyToolbar
    {
        const string ElementName = "Viol3HierarchyToolbar";
        const float ButtonWidth = 26f;
        const float RightMargin = 4f;
        const double ScanInterval = 0.5;
        const long PlaceIntervalMs = 100;
        // Matches SceneHierarchyWindow.DoToolbarLayout: create button, 6px space, flexible space, search field, sort button.
        const float SpaceAfterCreate = 6f;
        const float SortButtonWidth = 28f;
        // How long a click on the search icon keeps the field open while Unity hands it focus.
        const double SearchGrace = 1.0;
        const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Type WindowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static readonly FieldInfo SceneHierarchyField = WindowType?.GetField("m_SceneHierarchy", MemberFlags);
        static readonly PropertyInfo HasSearchFilter = WindowType?.GetProperty("hasSearchFilter", MemberFlags);
        static readonly PropertyInfo HasSearchFilterFocus = WindowType?.GetProperty("hasSearchFilterFocus", MemberFlags);
        static readonly MethodInfo FocusSearchField = WindowType?.GetMethod("FocusSearchField", MemberFlags);
        static readonly FieldInfo ParentField = typeof(EditorWindow).GetField("m_Parent", MemberFlags);
        static readonly FieldInfo OnGUIField = typeof(Editor).Assembly.GetType("UnityEditor.HostView")?.GetField("m_OnGUI", MemberFlags);
        static readonly PropertyInfo CreateButtonStyle = typeof(EditorStyles).GetProperty("toolbarCreateAddNewDropDown", MemberFlags);
        static readonly PropertyInfo LabelFloatMaxW = typeof(EditorGUILayout).GetProperty("kLabelFloatMaxW", MemberFlags);
        static readonly FieldInfo CurrentStyles = typeof(EditorStyles).GetField("s_Current", MemberFlags);

        static double nextScan;
        static Texture folderIcon;

        // Per-window state, kept on the overlay element.
        class State
        {
            public EditorWindow window;
            public double searchUntil;
            // Where the bar sits, in the window's GUI coordinates.
            public Rect rect;
            public bool visible;

            // Runs inside the window's own OnGUI, before and after Unity draws it. Unity's search field under the bar
            // registers a text cursor on every repaint, so the arrow has to be registered in the same pass to win.
            public void ClaimCursor()
            {
                if (visible && Event.current != null && Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);
            }
        }

        struct Layout
        {
            public float createEnd;
            public float searchX;
            public float sortX;
        }

        static HierarchyToolbar()
        {
            if (WindowType != null && SceneHierarchyField != null)
                EditorApplication.update += Scan;
        }

        // EditorStyles only exist once the editor has drawn its first GUI; reading them earlier throws.
        static bool StylesReady => CurrentStyles == null || CurrentStyles.GetValue(null) != null;

        static float ToolbarHeight => EditorStyles.toolbar.fixedHeight > 0f ? EditorStyles.toolbar.fixedHeight : 20f;

        // Hierarchy windows come and go, so new ones are picked up on a slow timer.
        static void Scan()
        {
            if (EditorApplication.timeSinceStartup < nextScan || !StylesReady)
                return;
            nextScan = EditorApplication.timeSinceStartup + ScanInterval;

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(WindowType))
            {
                VisualElement root = window.rootVisualElement;
                if (root.Q(ElementName)?.userData is State existing)
                {
                    // Unity recreates the OnGUI delegate when a window is docked elsewhere, dropping our hook.
                    HookOnGUI(existing);
                    continue;
                }

                var state = new State { window = window };
                var bar = new IMGUIContainer() { name = ElementName, userData = state };
                bar.onGUIHandler = () => DrawBar(bar, state);
                bar.style.position = Position.Absolute;
                bar.style.top = 0f;
                bar.RegisterCallback<PointerMoveEvent>(_ => bar.MarkDirtyRepaint());
                bar.RegisterCallback<PointerLeaveEvent>(_ => bar.MarkDirtyRepaint());
                root.Add(bar);
                // Searching starts and stops inside Unity's own GUI, so the bar keeps checking where it belongs.
                bar.schedule.Execute(() => Place(bar, state)).Every(PlaceIntervalMs);
                Place(bar, state);
                HookOnGUI(state);
            }
        }

        static void HookOnGUI(State state)
        {
            object hostView = ParentField?.GetValue(state.window);
            if (hostView == null || OnGUIField == null || !OnGUIField.DeclaringType.IsInstanceOfType(hostView))
                return;
            if (!(OnGUIField.GetValue(hostView) is Delegate onGUI) || onGUI.GetInvocationList().Any(d => d.Target is State))
                return;

            Delegate claim = Delegate.CreateDelegate(OnGUIField.FieldType, state, nameof(State.ClaimCursor));
            OnGUIField.SetValue(hostView, Delegate.Combine(claim, onGUI, claim));
        }

        static void Place(IMGUIContainer bar, State state)
        {
            if (state.window == null || !StylesReady)
                return;

            Layout layout = Measure(state.window);
            float right = IsSearching(state) ? layout.searchX - 2f : layout.sortX;
            float width = right - layout.createEnd;
            state.visible = HierarchySettings.Enabled && HierarchySettings.Toolbar && width >= ButtonWidth;
            state.rect = new Rect(layout.createEnd, 0f, Mathf.Max(0f, width), ToolbarHeight);

            bar.style.display = state.visible ? DisplayStyle.Flex : DisplayStyle.None;
            bar.style.left = state.rect.x;
            bar.style.width = state.rect.width;
            bar.style.height = state.rect.height;
        }

        static Layout Measure(EditorWindow window)
        {
            GUIStyle toolbar = EditorStyles.toolbar;
            var createStyle = CreateButtonStyle != null ? (GUIStyle)CreateButtonStyle.GetValue(null) : EditorStyles.toolbarDropDown;
            float createWidth = createStyle.CalcSize(EditorGUIUtility.TrIconContent("CreateAddNew")).x;
            float labelMax = LabelFloatMaxW != null ? (float)LabelFloatMaxW.GetValue(null) : 205f;

            var layout = new Layout();
            layout.createEnd = toolbar.padding.left + createWidth;
            layout.sortX = window.position.width - toolbar.padding.right - (HasSortMethods(window) ? SortButtonWidth : 0f);
            float available = layout.sortX - layout.createEnd - SpaceAfterCreate;
            layout.searchX = layout.sortX - Mathf.Clamp(available, labelMax * 0.2f, labelMax * 1.5f);
            return layout;
        }

        static bool IsSearching(State state)
        {
            if (EditorApplication.timeSinceStartup < state.searchUntil)
                return true;
            return GetBool(HasSearchFilter, state.window) || GetBool(HasSearchFilterFocus, state.window);
        }

        static bool GetBool(PropertyInfo property, EditorWindow window)
        {
            if (property == null)
                return false;
            return (bool)property.GetValue(property.GetMethod.IsStatic ? null : window);
        }

        static bool HasSortMethods(EditorWindow window)
        {
            object hierarchy = SceneHierarchyField.GetValue(window);
            PropertyInfo property = hierarchy?.GetType().GetProperty("hasSortMethods", MemberFlags);
            return property != null && (bool)property.GetValue(hierarchy);
        }

        #region Drawing

        // Buttons sit at the right end, like Unity's own toolbars: folder, header | collapse, search.
        static void DrawBar(IMGUIContainer bar, State state)
        {
            float width = bar.contentRect.width;
            float height = ToolbarHeight;
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbar.Draw(new Rect(0f, 0f, width, height), false, false, false, false);
            EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, width, height), MouseCursor.Arrow);

            float x = width - RightMargin;
            if (!IsSearching(state))
            {
                x -= ButtonWidth;
                if (IconButton(new Rect(x, 0f, ButtonWidth, height), "Search", DrawSearchIcon))
                {
                    state.searchUntil = EditorApplication.timeSinceStartup + SearchGrace;
                    FocusSearchField?.Invoke(state.window, null);
                    Place(bar, state);
                    state.window.Repaint();
                }
            }

            x -= ButtonWidth;
            if (IconButton(new Rect(x, 0f, ButtonWidth, height), "Collapse all objects", DrawCollapseIcon))
                HierarchyExpansion.CollapseAll(state.window);

            x -= 5f;
            EditorGUI.DrawRect(new Rect(x + 2f, 4f, 1f, height - 8f), new Color(0.5f, 0.5f, 0.5f, 0.4f));

            x -= ButtonWidth;
            if (IconButton(new Rect(x, 0f, ButtonWidth, height), "Create a header above the selection", DrawHeaderIcon))
                HierarchyMenu.CreateHeader();
            x -= ButtonWidth;
            if (IconButton(new Rect(x, 0f, ButtonWidth, height), "Create a folder below the selection", DrawFolderIcon))
                HierarchyMenu.CreateFolder();
        }

        // A flat button: no frame, just a rounded highlight under the mouse.
        static bool IconButton(Rect rect, string tooltip, Action<Rect> drawIcon)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    var hover = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
                    Color hoverColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f);
                    GUI.DrawTexture(hover, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, hoverColor, 0f, 3f);
                }
                drawIcon(rect);
            }
            return GUI.Button(rect, new GUIContent("", tooltip), GUIStyle.none);
        }

        static Color IconColor => EditorStyles.label.normal.textColor;

        static void DrawFolderIcon(Rect rect)
        {
            if (folderIcon == null)
                folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;
            GUI.DrawTexture(new Rect(rect.center.x - 8f, rect.center.y - 8f, 16f, 16f), folderIcon, ScaleMode.ScaleToFit);
        }

        // A filled bar over two text lines: a section title.
        static void DrawHeaderIcon(Rect rect)
        {
            float cx = Mathf.Round(rect.center.x);
            float top = Mathf.Round(rect.center.y) - 6f;
            Color color = IconColor;
            GUI.DrawTexture(new Rect(cx - 6f, top, 12f, 5f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 1.5f);
            EditorGUI.DrawRect(new Rect(cx - 6f, top + 7.5f, 12f, 1.5f), new Color(color.r, color.g, color.b, 0.6f));
            EditorGUI.DrawRect(new Rect(cx - 6f, top + 10.5f, 8f, 1.5f), new Color(color.r, color.g, color.b, 0.6f));
        }

        // An arrow pointing up at a bar.
        static void DrawCollapseIcon(Rect rect)
        {
            float cx = Mathf.Round(rect.center.x);
            float top = Mathf.Round(rect.center.y) - 6f;
            Color color = IconColor;
            EditorGUI.DrawRect(new Rect(cx - 5f, top, 11f, 1.5f), color);
            EditorGUI.DrawRect(new Rect(cx - 0.25f, top + 3f, 1.5f, 9f), color);
            using (new Handles.DrawingScope(color))
                Handles.DrawAAPolyLine(2f, new Vector3(cx - 3.5f, top + 7f), new Vector3(cx + 0.5f, top + 3f), new Vector3(cx + 4.5f, top + 7f));
        }

        static void DrawSearchIcon(Rect rect)
        {
            // Prefer Unity's own magnifier; the drawn one is only a fallback.
            Texture icon = StylePalette.LoadIcon("Search Icon") ?? StylePalette.LoadIcon("ViewToolZoom");
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.center.x - 8f, rect.center.y - 8f, 16f, 16f), icon, ScaleMode.ScaleToFit);
                return;
            }

            var center = new Vector3(Mathf.Round(rect.center.x) - 1.5f, Mathf.Round(rect.center.y) - 1.5f);
            using (new Handles.DrawingScope(IconColor))
            {
                Handles.DrawWireDisc(center, Vector3.forward, 4.5f, 1.5f);
                Handles.DrawAAPolyLine(2f, center + new Vector3(3.3f, 3.3f), center + new Vector3(7f, 7f));
            }
        }

        #endregion
    }
}

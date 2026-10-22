using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viol3.Hierarchy
{
    // A small floating Inspector for one component, opened by clicking its icon in the Hierarchy.
    // Unpinned windows close when they lose focus; pinning or dragging the header keeps them open.
    internal class ComponentWindow : EditorWindow
    {
        const float Width = 340f;
        const float HeaderHeight = 22f;
        const float MaxHeight = 600f;

        // Pickers opened from inside the Inspector take focus, but the window must stay open for them.
        static readonly string[] HelperWindows =
        {
            "ObjectSelector", "ColorPicker", "GradientPicker", "CurveEditorWindow",
            "PopupWindow", "PopupWindowWithoutFocus", "AdvancedDropdownWindow", "TooltipView",
        };

        static ComponentWindow floating;
        static GUIStyle titleStyle;
        static GUIStyle closeStyle;

        [SerializeField] Component component;
        [SerializeField] bool pinned;

        VisualElement inspector;
        Vector2 dragOffset;

        // Call from inside an OnGUI with the icon's GUI rect; the window opens on the next editor update.
        public static void Open(Component component, Rect iconRect)
        {
            var screenRect = new Rect(GUIUtility.GUIToScreenPoint(iconRect.position), iconRect.size);
            EditorApplication.delayCall += () =>
            {
                if (component != null)
                    OpenAt(component, screenRect);
            };
        }

        static void OpenAt(Component component, Rect iconRect)
        {
            if (floating != null && !floating.pinned)
                floating.Close();

            // Open beside the Hierarchy, flipping to the left when there's no room on the right.
            var topLeft = new Vector2(iconRect.xMax + 8f, iconRect.y - 4f);
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            if (topLeft.x + Width > main.xMax)
                topLeft.x = iconRect.x - Width - 8f;

            var window = CreateInstance<ComponentWindow>();
            window.component = component;
            window.position = new Rect(topLeft, new Vector2(Width, HeaderHeight + 120f));
            window.ShowPopup();
            window.Focus();
            floating = window;
        }

        void CreateGUI()
        {
            if (component == null)
            {
                Close();
                return;
            }

            VisualElement root = rootVisualElement;
            Color border = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.55f, 0.55f, 0.55f);
            root.style.borderTopWidth = root.style.borderBottomWidth = root.style.borderLeftWidth = root.style.borderRightWidth = 1f;
            root.style.borderTopColor = root.style.borderBottomColor = root.style.borderLeftColor = root.style.borderRightColor = border;
            root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                    Close();
            }, TrickleDown.TrickleDown);

            root.Add(new IMGUIContainer(DrawHeader) { style = { height = HeaderHeight, flexShrink = 0f } });

            var scroll = new ScrollView();
            inspector = new InspectorElement(component);
            // The default Inspector padding is sized for a full window; keep this one tight.
            inspector.style.paddingLeft = 4f;
            inspector.style.paddingRight = 4f;
            inspector.style.paddingTop = 2f;
            inspector.style.paddingBottom = 4f;
            inspector.RegisterCallback<GeometryChangedEvent>(_ => FitHeight());
            scroll.Add(inspector);
            root.Add(scroll);
        }

        void FitHeight()
        {
            float height = Mathf.Min(HeaderHeight + inspector.layout.height + 2f, MaxHeight);
            if (Mathf.Abs(position.height - height) > 1f)
                position = new Rect(position.x, position.y, position.width, height);
        }

        void OnInspectorUpdate()
        {
            // The component or its scene went away.
            if (component == null)
                Close();
        }

        void OnLostFocus()
        {
            // Wait a frame so focusedWindow points at whatever took focus.
            EditorApplication.delayCall += () =>
            {
                if (this == null || pinned || focusedWindow == this)
                    return;
                if (focusedWindow != null && System.Array.IndexOf(HelperWindows, focusedWindow.GetType().Name) >= 0)
                    return;
                Close();
            };
        }

        void OnDestroy()
        {
            if (floating == this)
                floating = null;
        }

        void DrawHeader()
        {
            var header = new Rect(0f, 0f, position.width, HeaderHeight);
            EditorGUI.DrawRect(header, EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f) : new Color(0.8f, 0.8f, 0.8f));
            if (component == null)
                return;

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
                closeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            }

            float x = 4f;
            GUI.DrawTexture(new Rect(x, 3f, 16f, 16f), AssetPreview.GetMiniThumbnail(component), ScaleMode.ScaleToFit);
            x += 20f;

            int enabled = EditorUtility.GetObjectEnabled(component);
            if (enabled != -1)
            {
                EditorGUI.BeginChangeCheck();
                bool on = EditorGUI.Toggle(new Rect(x, 3f, 16f, 16f), enabled == 1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(component, on ? "Enable Component" : "Disable Component");
                    EditorUtility.SetObjectEnabled(component, on);
                }
                x += 20f;
            }

            var closeRect = new Rect(position.width - 21f, 1f, 18f, 20f);
            var pinRect = new Rect(closeRect.x - 19f, 4f, 16f, 16f);
            GUI.Label(new Rect(x, 0f, pinRect.x - x - 4f, HeaderHeight),
                $"{ObjectNames.GetInspectorTitle(component)}  ·  {component.gameObject.name}", titleStyle);

            EditorGUI.BeginChangeCheck();
            pinned = GUI.Toggle(pinRect, pinned, new GUIContent("", "Keep this window open"), "IN LockButton");
            if (EditorGUI.EndChangeCheck() && pinned && floating == this)
                floating = null;
            if (GUI.Button(closeRect, new GUIContent("✕", "Close"), closeStyle))
                Close();

            HandleDrag(header);
        }

        void HandleDrag(Rect header)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button != 0 || !header.Contains(e.mousePosition))
                        break;
                    GUIUtility.hotControl = id;
                    dragOffset = GUIUtility.GUIToScreenPoint(e.mousePosition) - position.position;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;
                    // A window that has been moved is clearly meant to stay.
                    pinned = true;
                    if (floating == this)
                        floating = null;
                    position = new Rect(GUIUtility.GUIToScreenPoint(e.mousePosition) - dragOffset, position.size);
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        break;
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // The Alt+click popup: pick a row color and a row icon for the clicked object or the whole selection.
    internal class StylePalette : PopupWindowContent
    {
        const float Cell = 24f;
        const int Columns = 10;
        const float Padding = 5f;
        const float RowGap = 6f;
        const float Radius = 5f;
        const string TypePrefix = "t:";

        public static readonly Color[] Colors =
        {
            new Color(0.9f, 0.25f, 0.25f),
            new Color(0.95f, 0.55f, 0.2f),
            new Color(0.95f, 0.85f, 0.25f),
            new Color(0.35f, 0.8f, 0.35f),
            new Color(0.2f, 0.75f, 0.7f),
            new Color(0.3f, 0.55f, 0.95f),
            new Color(0.65f, 0.4f, 0.95f),
            new Color(0.95f, 0.45f, 0.75f),
            new Color(0.55f, 0.55f, 0.55f),
        };

        // Icons are stored as "t:<assembly qualified type>" to use that type's colored Inspector icon, or as a
        // built-in editor icon name. Each group starts on a new row; entries missing from this Unity version or
        // project (for example TextMeshPro or VFX Graph) are skipped.
        static readonly string[][] IconGroups =
        {
            new[]
            {
                "Folder Icon", "GameObject Icon", "Prefab Icon", "PrefabVariant Icon",
                TypeIcon("UnityEditor.MonoScript", "UnityEditor.CoreModule"), TypeIcon("UnityEngine.ScriptableObject"),
                TypeIcon("UnityEditor.SceneAsset", "UnityEditor.CoreModule"), "Favorite Icon", "SettingsIcon",
                "console.infoicon", "console.warnicon", "console.erroricon",
            },
            new[]
            {
                TypeIcon("UnityEngine.RectTransform"), TypeIcon("UnityEngine.Canvas", "UnityEngine.UIModule"),
                TypeIcon("UnityEngine.CanvasGroup", "UnityEngine.UIModule"), UIIcon("CanvasScaler"),
                TypeIcon("UnityEngine.EventSystems.EventSystem", "UnityEngine.UI"),
                TypeIcon("UnityEngine.EventSystems.StandaloneInputModule", "UnityEngine.UI"),
                UIIcon("Image"), UIIcon("RawImage"), UIIcon("Text"), TypeIcon("TMPro.TextMeshProUGUI", "Unity.TextMeshPro"),
                UIIcon("Button"), UIIcon("Toggle"), UIIcon("Slider"), UIIcon("Scrollbar"), UIIcon("ScrollRect"), UIIcon("InputField"),
                UIIcon("Dropdown"), UIIcon("Mask"), UIIcon("RectMask2D"), UIIcon("GridLayoutGroup"), UIIcon("HorizontalLayoutGroup"),
                UIIcon("VerticalLayoutGroup"), UIIcon("ContentSizeFitter"), UIIcon("AspectRatioFitter"),
            },
            new[]
            {
                TypeIcon("UnityEngine.Camera"), TypeIcon("UnityEngine.Rendering.Volume", "Unity.RenderPipelines.Core.Runtime"),
                TypeIcon("UnityEngine.ParticleSystem", "UnityEngine.ParticleSystemModule"), TypeIcon("UnityEngine.TrailRenderer"),
                TypeIcon("UnityEngine.LineRenderer"), TypeIcon("UnityEngine.VFX.VisualEffect", "UnityEngine.VFXModule"),
                TypeIcon("UnityEngine.Skybox"), TypeIcon("UnityEngine.LensFlare"), TypeIcon("UnityEngine.Projector"),
                TypeIcon("UnityEngine.Rendering.Universal.DecalProjector", "Unity.RenderPipelines.Universal.Runtime"),
                TypeIcon("UnityEngine.SpriteRenderer"), TypeIcon("UnityEngine.Rendering.SortingGroup"),
                TypeIcon("UnityEngine.Grid", "UnityEngine.GridModule"), TypeIcon("UnityEngine.Tilemaps.Tilemap", "UnityEngine.TilemapModule"),
                TypeIcon("UnityEngine.TextMesh", "UnityEngine.TextRenderingModule"), TypeIcon("UnityEngine.BillboardRenderer"),
            },
            new[]
            {
                TypeIcon("UnityEngine.Light"), "DirectionalLight Icon", TypeIcon("UnityEngine.LightProbeGroup"),
                TypeIcon("UnityEngine.LightProbeProxyVolume"), TypeIcon("UnityEngine.ReflectionProbe"),
                TypeIcon("UnityEngine.Rendering.Universal.Light2D", "Unity.RenderPipelines.Universal.2D.Runtime"),
                TypeIcon("UnityEngine.Rendering.Universal.Light2D", "Unity.RenderPipelines.Universal.Runtime"),
                TypeIcon("UnityEngine.OcclusionArea"), TypeIcon("UnityEngine.OcclusionPortal"), TypeIcon("UnityEngine.LightingSettings"),
            },
            new[]
            {
                TypeIcon("UnityEngine.Animator", "UnityEngine.AnimationModule"), TypeIcon("UnityEngine.Animation", "UnityEngine.AnimationModule"),
                TypeIcon("UnityEngine.Playables.PlayableDirector", "UnityEngine.DirectorModule"),
                TypeIcon("UnityEngine.Video.VideoPlayer", "UnityEngine.VideoModule"),
                TypeIcon("UnityEngine.AnimationClip", "UnityEngine.AnimationModule"), TypeIcon("UnityEditor.Animations.AnimatorController", "UnityEditor.CoreModule"),
                TypeIcon("UnityEngine.Avatar", "UnityEngine.AnimationModule"),
                TypeIcon("UnityEngine.Animations.AimConstraint", "UnityEngine.AnimationModule"),
                TypeIcon("UnityEngine.Animations.LookAtConstraint", "UnityEngine.AnimationModule"),
                TypeIcon("UnityEngine.Animations.ParentConstraint", "UnityEngine.AnimationModule"),
                TypeIcon("UnityEngine.Animations.PositionConstraint", "UnityEngine.AnimationModule"),
            },
            new[]
            {
                TypeIcon("UnityEngine.Rigidbody", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.BoxCollider", "UnityEngine.PhysicsModule"),
                TypeIcon("UnityEngine.SphereCollider", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.CapsuleCollider", "UnityEngine.PhysicsModule"),
                TypeIcon("UnityEngine.MeshCollider", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.WheelCollider", "UnityEngine.VehiclesModule"),
                TypeIcon("UnityEngine.TerrainCollider", "UnityEngine.TerrainPhysicsModule"),
                TypeIcon("UnityEngine.CharacterController", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.HingeJoint", "UnityEngine.PhysicsModule"),
                TypeIcon("UnityEngine.FixedJoint", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.SpringJoint", "UnityEngine.PhysicsModule"),
                TypeIcon("UnityEngine.ConfigurableJoint", "UnityEngine.PhysicsModule"), TypeIcon("UnityEngine.ConstantForce", "UnityEngine.PhysicsModule"),
                TypeIcon("UnityEngine.Cloth", "UnityEngine.ClothModule"),
                TypeIcon("UnityEngine.Rigidbody2D", "UnityEngine.Physics2DModule"), TypeIcon("UnityEngine.BoxCollider2D", "UnityEngine.Physics2DModule"),
                TypeIcon("UnityEngine.CircleCollider2D", "UnityEngine.Physics2DModule"), TypeIcon("UnityEngine.CapsuleCollider2D", "UnityEngine.Physics2DModule"),
                TypeIcon("UnityEngine.PolygonCollider2D", "UnityEngine.Physics2DModule"), TypeIcon("UnityEngine.EdgeCollider2D", "UnityEngine.Physics2DModule"),
                TypeIcon("UnityEngine.AI.NavMeshAgent", "UnityEngine.AIModule"), TypeIcon("UnityEngine.AI.NavMeshObstacle", "UnityEngine.AIModule"),
                TypeIcon("Unity.AI.Navigation.NavMeshSurface", "Unity.AI.Navigation"),
            },
            new[]
            {
                TypeIcon("UnityEngine.MeshFilter"), TypeIcon("UnityEngine.MeshRenderer"), TypeIcon("UnityEngine.SkinnedMeshRenderer"),
                TypeIcon("UnityEngine.LODGroup"), TypeIcon("UnityEngine.Terrain", "UnityEngine.TerrainModule"), TypeIcon("UnityEngine.Tree"),
                TypeIcon("UnityEngine.WindZone", "UnityEngine.WindModule"), TypeIcon("UnityEngine.Mesh"), TypeIcon("UnityEngine.Material"), TypeIcon("UnityEngine.Texture2D"),
                TypeIcon("UnityEngine.Sprite"), TypeIcon("UnityEngine.Cubemap"), TypeIcon("UnityEngine.Shader"), TypeIcon("UnityEngine.ComputeShader"),
                TypeIcon("UnityEngine.Font", "UnityEngine.TextRenderingModule"), TypeIcon("UnityEngine.TextAsset"),
            },
            new[]
            {
                TypeIcon("UnityEngine.AudioSource", "UnityEngine.AudioModule"), TypeIcon("UnityEngine.AudioListener", "UnityEngine.AudioModule"),
                TypeIcon("UnityEngine.AudioReverbZone", "UnityEngine.AudioModule"), TypeIcon("UnityEngine.AudioClip", "UnityEngine.AudioModule"),
                TypeIcon("UnityEngine.Audio.AudioMixer", "UnityEngine.AudioModule"), TypeIcon("UnityEngine.AudioChorusFilter", "UnityEngine.AudioModule"),
                TypeIcon("UnityEngine.AudioDistortionFilter", "UnityEngine.AudioModule"), TypeIcon("UnityEngine.AudioEchoFilter", "UnityEngine.AudioModule"),
                TypeIcon("UnityEngine.AudioHighPassFilter", "UnityEngine.AudioModule"), TypeIcon("UnityEngine.AudioLowPassFilter", "UnityEngine.AudioModule"),
                TypeIcon("UnityEngine.AudioReverbFilter", "UnityEngine.AudioModule"),
            },
            new[]
            {
                "PreMatCube", "PreMatSphere", "PreMatCylinder", "PreMatTorus", "PreMatQuad",
            },
        };

        static readonly Dictionary<string, Texture> IconCache = new Dictionary<string, Texture>();
        static List<string[]> availableGroups;
        static Texture2D swatchTexture;
        static GUIStyle centeredLabel;

        readonly GameObject[] targets;

        public StylePalette(GameObject[] targets)
        {
            this.targets = targets;
        }

        static string TypeIcon(string fullName, string assembly = "UnityEngine.CoreModule") => $"{TypePrefix}{fullName}, {assembly}";
        static string UIIcon(string name) => TypeIcon("UnityEngine.UI." + name, "UnityEngine.UI");

        public static Texture LoadIcon(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (IconCache.TryGetValue(key, out Texture texture))
                return texture;

            if (key.StartsWith(TypePrefix, StringComparison.Ordinal))
            {
                Type type = System.Type.GetType(key.Substring(TypePrefix.Length));
                texture = type != null ? AssetPreview.GetMiniTypeThumbnail(type) : null;
            }
            else
            {
                texture = EditorGUIUtility.FindTexture(key);
                if (texture == null)
                {
                    // IconContent logs an error for names that don't exist, and a missing icon is expected here.
                    bool logEnabled = Debug.unityLogger.logEnabled;
                    Debug.unityLogger.logEnabled = false;
                    try
                    {
                        texture = EditorGUIUtility.IconContent(key).image;
                    }
                    finally
                    {
                        Debug.unityLogger.logEnabled = logEnabled;
                    }
                }
            }

            IconCache[key] = texture;
            return texture;
        }

        static List<string[]> AvailableGroups
        {
            get
            {
                if (availableGroups != null)
                    return availableGroups;

                availableGroups = new List<string[]>();
                var seen = new HashSet<Texture>();
                var keys = new List<string>();
                foreach (string[] group in IconGroups)
                {
                    keys.Clear();
                    foreach (string key in group)
                    {
                        // Different entries can resolve to the same texture (for example an unknown type's fallback icon).
                        Texture texture = LoadIcon(key);
                        if (texture != null && seen.Add(texture))
                            keys.Add(key);
                    }
                    if (keys.Count > 0)
                        availableGroups.Add(keys.ToArray());
                }
                return availableGroups;
            }
        }

        // Every icon cell of the grid, placed after the "Automatic" cell.
        static IEnumerable<(string icon, Rect cell)> IconCells(float top)
        {
            int column = 1;
            int row = 0;
            foreach (string[] group in AvailableGroups)
            {
                foreach (string icon in group)
                {
                    if (column == Columns)
                    {
                        column = 0;
                        row++;
                    }
                    yield return (icon, CellRect(column++, top + row * Cell));
                }
                column = Columns;
            }
        }

        static float IconGridHeight()
        {
            float bottom = Cell;
            foreach ((string _, Rect cell) in IconCells(0f))
                bottom = cell.yMax;
            return bottom;
        }

        enum ColorCellKind
        {
            None,
            Preset,
            Custom,
            Add,
        }

        // The color rows: no color, the presets, the project's custom colors and the + button, wrapping as needed.
        static IEnumerable<(ColorCellKind kind, Color color, Rect cell)> ColorCells(float top)
        {
            int index = 0;
            Rect Next()
            {
                Rect cell = CellRect(index % Columns, top + index / Columns * Cell);
                index++;
                return cell;
            }

            yield return (ColorCellKind.None, Color.clear, Next());
            foreach (Color color in Colors)
                yield return (ColorCellKind.Preset, color, Next());
            foreach (Color color in HierarchyStyles.instance.CustomColors)
                yield return (ColorCellKind.Custom, color, Next());
            yield return (ColorCellKind.Add, Color.clear, Next());
        }

        static float ColorGridHeight()
        {
            float bottom = Cell;
            foreach ((ColorCellKind _, Color _, Rect cell) in ColorCells(0f))
                bottom = cell.yMax;
            return bottom;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Columns * Cell + Padding * 2f, Padding + ColorGridHeight() + RowGap + IconGridHeight() + Padding);
        }

        public override void OnOpen()
        {
            // Needed for the hover highlight.
            editorWindow.wantsMouseMove = true;
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
                editorWindow.Repaint();

            HierarchyStyles.Entry current = targets.Length > 0 && targets[0] != null ? HierarchyStyles.instance.Get(targets[0]) : null;
            float y = Padding;

            float colorBottom = y;
            // Copied up front, because removing a custom color changes the list while it is drawn.
            foreach ((ColorCellKind kind, Color color, Rect cell) in ColorCells(y).ToList())
            {
                colorBottom = cell.yMax;
                switch (kind)
                {
                    case ColorCellKind.None:
                        if (Swatch(cell, null, current == null || !current.HasColor))
                            Apply(HierarchyStyles.instance.SetColor(targets, null));
                        break;

                    case ColorCellKind.Preset:
                    case ColorCellKind.Custom:
                        if (kind == ColorCellKind.Custom && IsContextClick(cell))
                        {
                            ShowRemoveMenu(color);
                            break;
                        }
                        // Holding Alt colors the children too, like Alt+clicking a foldout expands them.
                        if (Swatch(cell, color, current != null && current.HasColor && current.color == color))
                            Apply(HierarchyStyles.instance.SetColor(targets, color, Event.current.alt));
                        break;

                    case ColorCellKind.Add:
                        if (AddButton(cell))
                        {
                            // The picker opens on the next editor update and takes focus, which closes this popup on
                            // its own. Closing it here as well destroys the popup twice.
                            GameObject[] pickTargets = targets;
                            bool recursive = Event.current.alt;
                            EditorApplication.delayCall += () => CustomColorPicker.Open(pickTargets, recursive);
                        }
                        break;
                }
            }
            y = colorBottom + RowGap;

            if (IconCell(CellRect(0, y), null, current == null || !current.HasIcon))
                Apply(HierarchyStyles.instance.SetIcon(targets, null));
            foreach ((string icon, Rect cell) in IconCells(y))
            {
                if (IconCell(cell, icon, current != null && current.icon == icon))
                    Apply(HierarchyStyles.instance.SetIcon(targets, icon));
            }
        }

        void Apply(int skipped)
        {
            if (skipped > 0)
                Debug.LogWarning($"[Viol3's Hierarchy] {skipped} object(s) couldn't be styled. Save the scene first; row styles aren't supported in Prefab Mode.");
            EditorApplication.RepaintHierarchyWindow();
            editorWindow.Repaint();
        }

        #region Cells

        static Rect CellRect(int column, float y) => new Rect(Padding + column * Cell, y, Cell, Cell);

        static bool Swatch(Rect cell, Color? color, bool selected)
        {
            DrawCellBackground(cell, selected);
            if (color.HasValue)
            {
                // A soft top-to-bottom shade on a rounded square, like a little button.
                var inner = new Rect(cell.x + 4f, cell.y + 4f, cell.width - 8f, cell.height - 8f);
                GUI.DrawTexture(inner, SwatchTexture, ScaleMode.StretchToFill, true, 0f, color.Value, 0f, Radius - 1f);
            }
            else
            {
                GUI.Label(cell, new GUIContent("✕", "No color"), CenteredLabel);
            }
            return GUI.Button(cell, color.HasValue ? SwatchTooltip : GUIContent.none, GUIStyle.none);
        }

        static readonly GUIContent SwatchTooltip = new GUIContent("", "Hold Alt to color the children too");
        static readonly GUIContent AddTooltip = new GUIContent("", "Add a color of your own. Right-click it later to remove it.");

        static bool AddButton(Rect cell)
        {
            DrawCellBackground(cell, false);
            GUI.Label(cell, "+", CenteredLabel);
            return GUI.Button(cell, AddTooltip, GUIStyle.none);
        }

        static bool IsContextClick(Rect cell)
        {
            Event e = Event.current;
            if (!cell.Contains(e.mousePosition) || !(e.type == EventType.ContextClick || (e.type == EventType.MouseDown && e.button == 1)))
                return false;
            e.Use();
            return true;
        }

        void ShowRemoveMenu(Color color)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                HierarchyStyles.instance.RemoveCustomColor(color);
                editorWindow.Repaint();
            });
            menu.ShowAsContext();
        }

        static bool IconCell(Rect cell, string icon, bool selected)
        {
            DrawCellBackground(cell, selected);
            if (icon == null)
            {
                GUI.Label(cell, new GUIContent("✕", "Automatic icon"), CenteredLabel);
                return GUI.Button(cell, GUIContent.none, GUIStyle.none);
            }

            GUI.DrawTexture(new Rect(cell.x + 4f, cell.y + 4f, 16f, 16f), LoadIcon(icon), ScaleMode.ScaleToFit);
            return GUI.Button(cell, new GUIContent("", IconTooltip(icon)), GUIStyle.none);
        }

        static void DrawCellBackground(Rect cell, bool selected)
        {
            var frame = new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f);
            if (selected)
                GUI.DrawTexture(frame, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, SelectionColor, 0f, Radius);
            else if (cell.Contains(Event.current.mousePosition))
                GUI.DrawTexture(frame, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HoverColor, 0f, Radius);
        }

        static string IconTooltip(string icon)
        {
            if (icon.StartsWith(TypePrefix, StringComparison.Ordinal))
            {
                string typeName = icon.Substring(TypePrefix.Length, icon.IndexOf(',') - TypePrefix.Length);
                icon = typeName.Substring(typeName.LastIndexOf('.') + 1);
            }
            return ObjectNames.NicifyVariableName(icon.Replace(" Icon", "").Replace("PreMat", "").Replace("console.", "").Replace("icon", ""));
        }

        static Texture2D SwatchTexture
        {
            get
            {
                if (swatchTexture == null)
                {
                    swatchTexture = new Texture2D(1, 16, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    // Row 0 is the bottom of the texture.
                    for (int i = 0; i < 16; i++)
                        swatchTexture.SetPixel(0, i, Color.Lerp(new Color(0.78f, 0.78f, 0.78f), Color.white, i / 15f));
                    swatchTexture.Apply();
                }
                return swatchTexture;
            }
        }

        static Color SelectionColor => EditorGUIUtility.isProSkin ? new Color(0.24f, 0.45f, 0.8f) : new Color(0.3f, 0.55f, 0.95f);
        static Color HoverColor => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f);
        static GUIStyle CenteredLabel => centeredLabel ?? (centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });

        #endregion
    }
}

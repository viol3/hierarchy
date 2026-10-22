using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // Carries row colors and icons over to duplicated and pasted objects. Unity doesn't say which object a copy came
    // from, so the selection is snapshotted when a Duplicate, Copy or Cut command runs, and the objects Unity selects
    // after Duplicate or Paste are matched against that snapshot by their whole shape.
    [InitializeOnLoad]
    internal static class StyleDuplication
    {
        // Unity numbers copies as "Name (1)", "Name.1" or "Name_1" depending on the project settings.
        static readonly Regex CopyNumber = new Regex(@"[\s._]*\(?\d+\)?$");
        static readonly List<Component> Components = new List<Component>();

        // A selected object as it was when the command ran: enough to recognise its copy and restyle it.
        class Node
        {
            public string baseName;
            public Type[] components;
            public HierarchyStyles.Entry style;
            public Node[] children;
        }

        static Node[] clipboard = new Node[0];
        static Node[] duplicating;
        static HashSet<GameObject> duplicateSources;
        static bool pasting;
        static bool scheduled;

        static StyleDuplication()
        {
            SceneView.duringSceneGui += _ => OnCommand(Event.current);
        }

        // Called for every Hierarchy row and Scene view GUI event; only the first sighting of a command counts.
        public static void OnCommand(Event e)
        {
            if (e == null || e.type != EventType.ExecuteCommand || scheduled)
                return;

            switch (e.commandName)
            {
                case "Duplicate":
                    duplicating = Snapshot(Selection.gameObjects);
                    duplicateSources = new HashSet<GameObject>(Selection.gameObjects);
                    break;
                case "Copy":
                case "Cut":
                    clipboard = Snapshot(Selection.gameObjects);
                    return;
                case "Paste":
                    pasting = true;
                    break;
                default:
                    return;
            }

            // Unity selects the new objects once the command has run.
            scheduled = true;
            EditorApplication.delayCall += ApplyToNewSelection;
        }

        static void ApplyToNewSelection()
        {
            scheduled = false;
            Node[] templates = duplicating ?? (pasting ? clipboard : null);
            HashSet<GameObject> sources = duplicateSources;
            duplicating = null;
            duplicateSources = null;
            pasting = false;
            if (templates == null || templates.Length == 0)
                return;

            var unmatched = new List<Node>(templates);
            var styles = new List<(GameObject, HierarchyStyles.Entry)>();
            foreach (GameObject copy in Selection.gameObjects)
            {
                if (copy == null || (sources != null && sources.Contains(copy)))
                    continue;
                Node template = unmatched.FirstOrDefault(t => Matches(t, copy.transform));
                if (template == null)
                    continue;
                unmatched.Remove(template);
                CollectStyles(template, copy.transform, styles);
            }

            if (styles.Count > 0)
            {
                HierarchyStyles.instance.ApplyStyles(styles);
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        static Node[] Snapshot(GameObject[] objects)
        {
            return objects.Where(go => go != null).Select(go => Snapshot(go.transform)).ToArray();
        }

        static Node Snapshot(Transform transform)
        {
            HierarchyStyles.Entry style = HierarchyStyles.instance.Get(transform.gameObject);
            var node = new Node
            {
                baseName = BaseName(transform.name),
                components = ComponentTypes(transform),
                style = style == null ? null : new HierarchyStyles.Entry { color = style.color, icon = style.icon, recursive = style.recursive },
                children = new Node[transform.childCount],
            };
            for (int i = 0; i < transform.childCount; i++)
                node.children[i] = Snapshot(transform.GetChild(i));
            return node;
        }

        // The whole subtree has to line up: names, components and children at every level.
        static bool Matches(Node node, Transform transform)
        {
            if (node.baseName != BaseName(transform.name) || node.children.Length != transform.childCount ||
                !node.components.SequenceEqual(ComponentTypes(transform)))
                return false;
            for (int i = 0; i < node.children.Length; i++)
            {
                if (!Matches(node.children[i], transform.GetChild(i)))
                    return false;
            }
            return true;
        }

        static void CollectStyles(Node node, Transform transform, List<(GameObject, HierarchyStyles.Entry)> styles)
        {
            if (node.style != null)
                styles.Add((transform.gameObject, node.style));
            for (int i = 0; i < node.children.Length; i++)
                CollectStyles(node.children[i], transform.GetChild(i), styles);
        }

        static Type[] ComponentTypes(Transform transform)
        {
            transform.GetComponents(Components);
            // Missing scripts come back as null and stay in the list, so they still have to line up.
            return Components.Select(c => c != null ? c.GetType() : null).ToArray();
        }

        static string BaseName(string name) => CopyNumber.Replace(name, "");
    }
}

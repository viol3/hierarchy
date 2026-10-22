using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Viol3.Hierarchy
{
    // Per-object row colors and icons keyed by GlobalObjectId, saved in ProjectSettings so they survive
    // scene reloads and reach the whole team.
    [FilePath("ProjectSettings/Viol3Hierarchy.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class HierarchyStyles : ScriptableSingleton<HierarchyStyles>
    {
        [Serializable]
        internal class Entry
        {
            public string id;
            public Color color;
            public string icon;
            // The color also applies to every child that has no color of its own.
            public bool recursive;

            public bool HasColor => color.a > 0f;
            public bool HasIcon => !string.IsNullOrEmpty(icon);
            public bool IsEmpty => !HasColor && !HasIcon;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        // Colors added to the palette with its + button, shared with the team like the styles.
        [SerializeField] List<Color> customColors = new List<Color>();

        // Domain reloads also restore private fields, which would bring back "indexed" without the dictionaries,
        // so the caches are explicitly excluded and rebuilt.
        [NonSerialized] readonly Dictionary<string, Entry> byId = new Dictionary<string, Entry>();
        // Instance IDs stay valid while an object is loaded, so this avoids a slow GlobalObjectId lookup on every repaint.
        [NonSerialized] readonly Dictionary<int, Entry> byInstance = new Dictionary<int, Entry>();
        [NonSerialized] bool indexed;

        public int Count => entries.Count;
        public IReadOnlyList<Color> CustomColors => customColors;

        public Entry Get(GameObject go)
        {
            if (entries.Count == 0)
                return null;

            int instanceID = go.GetInstanceID();
            if (!byInstance.TryGetValue(instanceID, out Entry entry))
            {
                Index();
                string id = IdOf(go);
                if (id == null || !byId.TryGetValue(id, out entry))
                    entry = null;
                byInstance[instanceID] = entry;
            }
            return entry;
        }

        // Each Set returns how many objects were skipped because they have no stable ID yet.
        // Pass save: false for live previews that change many times a second; a later call saves.
        public int SetColor(IEnumerable<GameObject> objects, Color? color, bool recursive = false, bool save = true)
        {
            return Modify(objects, e =>
            {
                e.color = color ?? Color.clear;
                e.recursive = color.HasValue && recursive;
            }, save);
        }

        public void AddCustomColor(Color color)
        {
            color.a = 1f;
            if (customColors.Contains(color))
                return;
            customColors.Add(color);
            Save(true);
        }

        public void RemoveCustomColor(Color color)
        {
            if (customColors.Remove(color))
                Save(true);
        }

        public int SetIcon(IEnumerable<GameObject> objects, string icon) => Modify(objects, e => e.icon = icon);

        // Gives each object a copy of a style, for objects that were just duplicated or pasted.
        public void ApplyStyles(IEnumerable<(GameObject target, Entry style)> styles)
        {
            bool changed = false;
            foreach ((GameObject target, Entry from) in styles)
            {
                string id = IdOf(target);
                if (id == null)
                    continue;

                Index();
                if (!byId.TryGetValue(id, out Entry entry))
                {
                    entry = new Entry { id = id };
                    entries.Add(entry);
                    byId[id] = entry;
                }
                entry.color = from.color;
                entry.icon = from.icon;
                entry.recursive = from.recursive;
                changed = true;
            }
            if (changed)
                Changed();
        }

        public void ClearAll()
        {
            entries.Clear();
            Changed();
        }

        public void ClearCache() => byInstance.Clear();

        int Modify(IEnumerable<GameObject> objects, Action<Entry> change, bool save = true)
        {
            Index();
            int skipped = 0;
            foreach (GameObject go in objects)
            {
                string id = IdOf(go);
                if (id == null)
                {
                    skipped++;
                    continue;
                }

                if (!byId.TryGetValue(id, out Entry entry))
                {
                    entry = new Entry { id = id };
                    entries.Add(entry);
                    byId[id] = entry;
                }
                change(entry);
                if (entry.IsEmpty)
                {
                    entries.Remove(entry);
                    byId.Remove(id);
                }
            }
            Changed(save);
            return skipped;
        }

        void Changed(bool save = true)
        {
            indexed = false;
            byInstance.Clear();
            if (save)
                Save(true);
        }

        void Index()
        {
            if (indexed)
                return;
            indexed = true;

            // Walk backwards so the newest entry wins, and drop older duplicates of the same object.
            byId.Clear();
            int before = entries.Count;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (byId.ContainsKey(entries[i].id))
                    entries.RemoveAt(i);
                else
                    byId[entries[i].id] = entries[i];
            }
            if (entries.Count != before)
                Save(true);
        }

        static string IdOf(GameObject go)
        {
            // Only objects saved in a scene have an ID that stays the same between editor sessions.
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(go);
            return id.identifierType == 2 && id.targetObjectId != 0 ? id.ToString() : null;
        }
    }
}

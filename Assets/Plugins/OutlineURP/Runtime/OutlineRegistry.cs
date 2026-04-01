using System.Collections.Generic;
using UnityEngine;

namespace OutlineURP
{
    internal static class OutlineRegistry
    {
        internal readonly struct RenderEntry
        {
            public readonly Renderer renderer;
            public readonly OutlineGroup group;
            public readonly OutlineState state;

            public RenderEntry(Renderer renderer, OutlineGroup group, OutlineState state)
            {
                this.renderer = renderer;
                this.group = group;
                this.state = state;
            }
        }

        private sealed class TargetEntry
        {
            public readonly List<Renderer> renderers = new(4);
            public OutlineGroup group;
            public bool hovered;
            public bool selected;
        }

        private static readonly Dictionary<OutlineTarget, TargetEntry> Targets = new(128);
        private static readonly List<RenderEntry> ActiveEntries = new(256);
        private static bool dirty;

        public static bool HasAnyActive
        {
            get
            {
                EnsureUpToDate();
                return ActiveEntries.Count > 0;
            }
        }

        public static int ActiveEntryCount
        {
            get
            {
                EnsureUpToDate();
                return ActiveEntries.Count;
            }
        }

        public static IReadOnlyList<RenderEntry> Entries
        {
            get
            {
                EnsureUpToDate();
                return ActiveEntries;
            }
        }

        public static bool IsRegistered(OutlineTarget target)
        {
            return target != null && Targets.ContainsKey(target);
        }

        public static void Register(OutlineTarget target, IReadOnlyList<Renderer> renderers, OutlineGroup group)
        {
            if (target == null)
            {
                return;
            }

            if (!Targets.TryGetValue(target, out var entry))
            {
                entry = new TargetEntry();
                Targets.Add(target, entry);
            }

            entry.renderers.Clear();
            if (renderers != null)
            {
                for (var i = 0; i < renderers.Count; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null || entry.renderers.Contains(renderer))
                    {
                        continue;
                    }

                    entry.renderers.Add(renderer);
                }
            }

            entry.group = group;
            entry.hovered = target.Hovered;
            entry.selected = target.Selected;
            MarkDirty();
        }

        public static void Unregister(OutlineTarget target)
        {
            if (target == null)
            {
                return;
            }

            if (Targets.Remove(target))
            {
                MarkDirty();
            }
        }

        public static void SetHovered(OutlineTarget target, bool value)
        {
            if (target == null || !Targets.TryGetValue(target, out var entry))
            {
                return;
            }

            if (entry.hovered == value)
            {
                return;
            }

            entry.hovered = value;
            MarkDirty();
        }

        public static void SetSelected(OutlineTarget target, bool value)
        {
            if (target == null || !Targets.TryGetValue(target, out var entry))
            {
                return;
            }

            if (entry.selected == value)
            {
                return;
            }

            entry.selected = value;
            MarkDirty();
        }

        public static void SetGroup(OutlineTarget target, OutlineGroup group)
        {
            if (target == null || !Targets.TryGetValue(target, out var entry))
            {
                return;
            }

            if (entry.group == group)
            {
                return;
            }

            entry.group = group;
            MarkDirty();
        }

        public static void SetExclusiveSelected(OutlineTarget target)
        {
            if (Targets.Count == 0)
            {
                return;
            }

            var anyChanged = false;
            foreach (var pair in Targets)
            {
                var isSelected = pair.Key == target;
                if (pair.Value.selected == isSelected)
                {
                    continue;
                }

                pair.Value.selected = isSelected;
                pair.Key.SetSelectedFromRegistry(isSelected);
                anyChanged = true;
            }

            if (anyChanged)
            {
                MarkDirty();
            }
        }

        public static void ClearAllStates()
        {
            if (Targets.Count == 0)
            {
                return;
            }

            var anyChanged = false;
            foreach (var pair in Targets)
            {
                if (!pair.Value.hovered && !pair.Value.selected)
                {
                    continue;
                }

                pair.Value.hovered = false;
                pair.Value.selected = false;
                pair.Key.SetHoveredFromRegistry(false);
                pair.Key.SetSelectedFromRegistry(false);
                anyChanged = true;
            }

            if (anyChanged)
            {
                MarkDirty();
            }
        }

        private static void MarkDirty()
        {
            dirty = true;
        }

        private static void EnsureUpToDate()
        {
            if (!dirty)
            {
                return;
            }

            RebuildActiveList();
            dirty = false;
        }

        private static void RebuildActiveList()
        {
            ActiveEntries.Clear();

            foreach (var pair in Targets)
            {
                var entry = pair.Value;
                var state = entry.selected ? OutlineState.Selected : entry.hovered ? OutlineState.Hover : OutlineState.None;
                if (state == OutlineState.None)
                {
                    continue;
                }

                for (var i = 0; i < entry.renderers.Count; i++)
                {
                    var renderer = entry.renderers[i];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    ActiveEntries.Add(new RenderEntry(renderer, entry.group, state));
                }
            }
        }
    }
}

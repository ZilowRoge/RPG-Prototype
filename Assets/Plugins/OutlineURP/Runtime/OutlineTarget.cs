using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutlineURP
{
    [DisallowMultipleComponent]
    public sealed class OutlineTarget : MonoBehaviour
    {
        [SerializeField]
        private OutlineGroup group = OutlineGroup.Enemy;

        [SerializeField]
        private bool includeChildren = true;

        [SerializeField]
        private Renderer[] explicitRenderers = Array.Empty<Renderer>();

        [SerializeField]
        private bool selectedOnStart;

        private readonly List<Renderer> resolvedRenderers = new(8);

        public bool Hovered { get; private set; }
        public bool Selected { get; private set; }

        public OutlineGroup Group => group;

        private void Awake()
        {
            Selected = selectedOnStart;
            Hovered = false;
            CacheRenderers();
        }

        private void OnEnable()
        {
            CacheRenderers();
            OutlineRegistry.Register(this, resolvedRenderers, group);
        }

        private void OnDisable()
        {
            OutlineRegistry.Unregister(this);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            CacheRenderers();
            OutlineRegistry.Register(this, resolvedRenderers, group);
        }

        public void SetHovered(bool value)
        {
            if (Hovered == value)
            {
                EnsureRegisteredIfActive(onlyIfMissing: true);
                return;
            }

            EnsureRegisteredIfActive(onlyIfMissing: true);
            Hovered = value;
            OutlineRegistry.SetHovered(this, value);
        }

        public void SetSelected(bool value)
        {
            if (Selected == value)
            {
                EnsureRegisteredIfActive(onlyIfMissing: true);
                return;
            }

            EnsureRegisteredIfActive(onlyIfMissing: true);
            Selected = value;
            OutlineRegistry.SetSelected(this, value);
        }

        public void SetGroup(OutlineGroup value)
        {
            if (group == value)
            {
                EnsureRegisteredIfActive(onlyIfMissing: true);
                return;
            }

            EnsureRegisteredIfActive(onlyIfMissing: true);
            group = value;
            OutlineRegistry.SetGroup(this, value);
        }

        public void RefreshRenderers()
        {
            CacheRenderers();
            OutlineRegistry.Register(this, resolvedRenderers, group);
        }

        internal void SetSelectedFromRegistry(bool value)
        {
            Selected = value;
        }

        internal void SetHoveredFromRegistry(bool value)
        {
            Hovered = value;
        }

        private void CacheRenderers()
        {
            resolvedRenderers.Clear();

            if (explicitRenderers != null && explicitRenderers.Length > 0)
            {
                for (var i = 0; i < explicitRenderers.Length; i++)
                {
                    AddRendererIfValid(explicitRenderers[i]);
                }
            }

            if (includeChildren)
            {
                var temp = ListPool<Renderer>.Get();
                try
                {
                    GetComponentsInChildren(true, temp);
                    for (var i = 0; i < temp.Count; i++)
                    {
                        AddRendererIfValid(temp[i]);
                    }
                }
                finally
                {
                    ListPool<Renderer>.Release(temp);
                }
            }
            else
            {
                AddRendererIfValid(GetComponent<Renderer>());
            }
        }

        private void AddRendererIfValid(Renderer renderer)
        {
            if (renderer == null || resolvedRenderers.Contains(renderer))
            {
                return;
            }

            resolvedRenderers.Add(renderer);
        }

        private void EnsureRegisteredIfActive(bool onlyIfMissing)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (onlyIfMissing && OutlineRegistry.IsRegistered(this))
            {
                return;
            }

            if (resolvedRenderers.Count == 0)
            {
                CacheRenderers();
            }

            OutlineRegistry.Register(this, resolvedRenderers, group);
        }
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>(8);
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}

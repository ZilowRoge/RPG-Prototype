using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutlineURP.Testing
{
    public sealed class OutlineDemoConfigurator : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] enemyRoots = Array.Empty<GameObject>();

        [SerializeField]
        private GameObject[] pickupRoots = Array.Empty<GameObject>();

        [SerializeField]
        private bool configureOnStart = true;

        [SerializeField]
        private bool clearStatesOnStart = true;

        [SerializeField]
        private bool autoDiscoverByLayerWhenRootsEmpty = true;

        [SerializeField]
        private string enemyLayerName = "Enemy";

        [SerializeField]
        private string pickupLayerName = "Pickup";

        private void Start()
        {
            if (configureOnStart)
            {
                ApplyConfiguration();
            }

            if (clearStatesOnStart)
            {
                OutlineController.ClearAllStates();
            }
        }

        [ContextMenu("Apply Outline Demo Configuration")]
        public void ApplyConfiguration()
        {
            var configuredEnemies = ConfigureRoots(enemyRoots, OutlineGroup.Enemy);
            var configuredPickups = ConfigureRoots(pickupRoots, OutlineGroup.Pickup);

            if (autoDiscoverByLayerWhenRootsEmpty && configuredEnemies == 0)
            {
                ConfigureByLayer(enemyLayerName, OutlineGroup.Enemy);
            }

            if (autoDiscoverByLayerWhenRootsEmpty && configuredPickups == 0)
            {
                ConfigureByLayer(pickupLayerName, OutlineGroup.Pickup);
            }
        }

        private static int ConfigureRoots(GameObject[] roots, OutlineGroup group)
        {
            if (roots == null)
            {
                return 0;
            }

            var configured = 0;
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var target = root.GetComponent<OutlineTarget>();
                if (target == null)
                {
                    target = root.AddComponent<OutlineTarget>();
                }

                target.SetGroup(group);
                target.RefreshRenderers();
                target.SetHovered(false);
                target.SetSelected(false);
                configured++;
            }

            return configured;
        }

        private static void ConfigureByLayer(string layerName, OutlineGroup group)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return;
            }

            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var configuredRoots = new HashSet<GameObject>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.layer != layer)
                {
                    continue;
                }

                var root = renderer.transform.root.gameObject;
                if (!configuredRoots.Add(root))
                {
                    continue;
                }

                var target = root.GetComponent<OutlineTarget>();
                if (target == null)
                {
                    target = root.AddComponent<OutlineTarget>();
                }

                target.SetGroup(group);
                target.RefreshRenderers();
                target.SetHovered(false);
                target.SetSelected(false);
            }
        }
    }
}

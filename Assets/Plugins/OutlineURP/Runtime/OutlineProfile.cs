using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutlineURP
{
    [CreateAssetMenu(menuName = "Outline URP/Outline Profile", fileName = "OutlineProfile")]
    public sealed class OutlineProfile : ScriptableObject
    {
        [Serializable]
        public struct GroupStyle
        {
            public OutlineGroup group;
            public Color hoverColor;
            public Color selectedColor;
        }

        [SerializeField]
        [Range(0.5f, 8f)]
        private float thickness = 2f;

        [SerializeField]
        private OutlineOcclusionMode defaultOcclusionMode = OutlineOcclusionMode.RespectDepth;

        [SerializeField]
        private Color fallbackHoverColor = new Color(1f, 0.95f, 0.25f, 1f);

        [SerializeField]
        private Color fallbackSelectedColor = new Color(1f, 0.35f, 0.2f, 1f);

        [SerializeField]
        private List<GroupStyle> groupStyles = new()
        {
            new GroupStyle
            {
                group = OutlineGroup.Enemy,
                hoverColor = new Color(1f, 0.72f, 0.2f, 1f),
                selectedColor = new Color(1f, 0.2f, 0.2f, 1f)
            },
            new GroupStyle
            {
                group = OutlineGroup.Pickup,
                hoverColor = new Color(0.35f, 1f, 0.7f, 1f),
                selectedColor = new Color(0.2f, 0.9f, 1f, 1f)
            }
        };

        public float Thickness => Mathf.Max(0.5f, thickness);
        public OutlineOcclusionMode DefaultOcclusionMode => defaultOcclusionMode;

        public Color GetColor(OutlineGroup group, OutlineState state)
        {
            var fallback = state == OutlineState.Selected ? fallbackSelectedColor : fallbackHoverColor;
            if (state == OutlineState.None)
            {
                return fallback;
            }

            for (var i = 0; i < groupStyles.Count; i++)
            {
                if (groupStyles[i].group != group)
                {
                    continue;
                }

                return state == OutlineState.Selected ? groupStyles[i].selectedColor : groupStyles[i].hoverColor;
            }

            return fallback;
        }
    }
}

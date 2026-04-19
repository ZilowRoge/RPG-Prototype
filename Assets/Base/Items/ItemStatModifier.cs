using System;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// A single stat change applied by an item instance.
    /// </summary>
    [Serializable]
    public struct ItemStatModifier
    {
        [SerializeField] private ItemStatType stat;
        [SerializeField] private ModifierMode mode;
        [SerializeField] private float value;

        public ItemStatType Stat => stat;
        public ModifierMode Mode => mode;
        public float Value => value;

        public ItemStatModifier(ItemStatType stat, ModifierMode mode, float value)
        {
            this.stat = stat;
            this.mode = mode;
            this.value = value;
        }
    }

    public enum ModifierMode
    {
        Add = 0,
        Multiply = 1,
        Override = 2
    }
}

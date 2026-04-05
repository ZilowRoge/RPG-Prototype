using System;
using UnityEngine;

namespace Crafting
{
    [Serializable]
    public struct RecipeItemAmount
    {
        [SerializeField] private string itemId;
        [Min(1)]
        [SerializeField] private int amount;

        public RecipeItemAmount(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = Mathf.Max(1, amount);
        }

        public string ItemId => itemId;
        public int Amount => Mathf.Max(1, amount);
    }
}

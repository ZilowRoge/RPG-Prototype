using System;
using System.Collections.Generic;

namespace Crafting
{
    public readonly struct CraftingRecipeAvailability
    {
        public CraftingRecipeAvailability(
            bool hasRequiredMaterials,
            bool hasOutputSpace,
            IReadOnlyList<bool> materialAvailability,
            IReadOnlyList<bool> productSpaceAvailability)
        {
            HasRequiredMaterials = hasRequiredMaterials;
            HasOutputSpace = hasOutputSpace;
            MaterialAvailability = materialAvailability ?? Array.Empty<bool>();
            ProductSpaceAvailability = productSpaceAvailability ?? Array.Empty<bool>();
        }

        public bool HasRequiredMaterials { get; }
        public bool HasOutputSpace { get; }
        public bool CanCraft => HasRequiredMaterials && HasOutputSpace;
        public IReadOnlyList<bool> MaterialAvailability { get; }
        public IReadOnlyList<bool> ProductSpaceAvailability { get; }
    }
}

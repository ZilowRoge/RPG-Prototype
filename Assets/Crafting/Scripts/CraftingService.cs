using System;
using System.Collections.Generic;
using Inventory;
using Items;
using UnityEngine;

namespace Crafting
{
    public static class CraftingService
    {
        private struct SimulatedSlot
        {
            public ItemDefinition Definition;
            public int StackCount;

            public bool IsEmpty => Definition == null || StackCount <= 0;
        }

        public static bool SupportsStation(CraftingRecipe recipe, CraftingStationType stationType)
        {
            if (recipe == null)
                return false;

            return recipe.StationType == CraftingStationType.Any || recipe.StationType == stationType;
        }

        public static CraftingRecipeAvailability Evaluate(
            CraftingRecipe recipe,
            InventoryController inventoryController,
            ItemDefinitionDatabase itemDatabase)
        {
            int materialCount = recipe != null ? recipe.Materials.Count : 0;
            int productCount = recipe != null ? recipe.Products.Count : 0;

            if (recipe == null || inventoryController == null || inventoryController.Inventory == null || itemDatabase == null)
            {
                return new CraftingRecipeAvailability(
                    hasRequiredMaterials: false,
                    hasOutputSpace: false,
                    materialAvailability: CreateFilledFlags(materialCount, false),
                    productSpaceAvailability: CreateFilledFlags(productCount, false));
            }

            var simulatedSlots = CreateSimulation(inventoryController.Inventory);
            var materialAvailability = CreateFilledFlags(materialCount, false);
            bool hasRequiredMaterials = true;

            for (int i = 0; i < materialCount; i++)
            {
                var entry = recipe.Materials[i];
                if (!TryResolveDefinition(itemDatabase, entry.ItemId, out var definition))
                {
                    hasRequiredMaterials = false;
                    continue;
                }

                bool removed = TryRemoveFromSimulation(simulatedSlots, definition, entry.Amount);
                materialAvailability[i] = removed;
                if (!removed)
                    hasRequiredMaterials = false;
            }

            var productSimulation = CreateSimulation(inventoryController.Inventory);
            if (hasRequiredMaterials)
            {
                for (int i = 0; i < materialCount; i++)
                {
                    var entry = recipe.Materials[i];
                    if (!TryResolveDefinition(itemDatabase, entry.ItemId, out var definition))
                        continue;

                    TryRemoveFromSimulation(productSimulation, definition, entry.Amount);
                }
            }

            var productSpaceAvailability = CreateFilledFlags(productCount, false);
            bool hasOutputSpace = true;

            for (int i = 0; i < productCount; i++)
            {
                var entry = recipe.Products[i];
                if (!TryResolveDefinition(itemDatabase, entry.ItemId, out var definition))
                {
                    hasOutputSpace = false;
                    continue;
                }

                bool added = TryAddToSimulation(productSimulation, definition, entry.Amount);
                productSpaceAvailability[i] = added;
                if (!added)
                    hasOutputSpace = false;
            }

            return new CraftingRecipeAvailability(
                hasRequiredMaterials,
                hasOutputSpace,
                materialAvailability,
                productSpaceAvailability);
        }

        public static bool TryCraft(
            CraftingRecipe recipe,
            InventoryController inventoryController,
            ItemDefinitionDatabase itemDatabase,
            out CraftingRecipeAvailability availability)
        {
            availability = Evaluate(recipe, inventoryController, itemDatabase);
            if (!availability.CanCraft || recipe == null || inventoryController == null || inventoryController.Inventory == null || itemDatabase == null)
                return false;

            var removedMaterials = new List<(ItemDefinition definition, int amount)>();
            var addedProducts = new List<(ItemDefinition definition, int amount)>();

            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                var entry = recipe.Materials[i];
                if (!TryResolveDefinition(itemDatabase, entry.ItemId, out var definition))
                {
                    RollbackRemovedMaterials(inventoryController, removedMaterials);
                    return false;
                }

                if (!inventoryController.TryRemoveItem(definition, entry.Amount))
                {
                    RollbackRemovedMaterials(inventoryController, removedMaterials);
                    return false;
                }

                removedMaterials.Add((definition, entry.Amount));
            }

            for (int i = 0; i < recipe.Products.Count; i++)
            {
                var entry = recipe.Products[i];
                if (!TryResolveDefinition(itemDatabase, entry.ItemId, out var definition))
                {
                    RollbackAddedProducts(inventoryController, addedProducts);
                    RollbackRemovedMaterials(inventoryController, removedMaterials);
                    availability = Evaluate(recipe, inventoryController, itemDatabase);
                    return false;
                }

                int countBeforeAdd = inventoryController.GetItemCount(definition);
                bool addedSuccessfully = inventoryController.TryAddItem(definition, entry.Amount);
                int addedAmount = Mathf.Max(0, inventoryController.GetItemCount(definition) - countBeforeAdd);

                if (addedAmount > 0)
                    addedProducts.Add((definition, addedAmount));

                if (!addedSuccessfully || addedAmount != entry.Amount)
                {
                    RollbackAddedProducts(inventoryController, addedProducts);
                    RollbackRemovedMaterials(inventoryController, removedMaterials);
                    availability = Evaluate(recipe, inventoryController, itemDatabase);
                    return false;
                }
            }

            availability = Evaluate(recipe, inventoryController, itemDatabase);
            return true;
        }

        private static bool TryResolveDefinition(ItemDefinitionDatabase itemDatabase, string itemId, out ItemDefinition definition)
        {
            definition = null;
            return itemDatabase != null &&
                   !string.IsNullOrWhiteSpace(itemId) &&
                   itemDatabase.TryGetById(itemId, out definition) &&
                   definition != null;
        }

        private static bool[] CreateFilledFlags(int count, bool value)
        {
            if (count <= 0)
                return Array.Empty<bool>();

            var flags = new bool[count];
            if (value)
            {
                for (int i = 0; i < count; i++)
                    flags[i] = true;
            }

            return flags;
        }

        private static List<SimulatedSlot> CreateSimulation(global::Inventory.Inventory inventory)
        {
            var simulation = new List<SimulatedSlot>();
            if (inventory?.Slots == null)
                return simulation;

            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var slot = inventory.Slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    simulation.Add(new SimulatedSlot());
                    continue;
                }

                simulation.Add(new SimulatedSlot
                {
                    Definition = slot.ItemInstance.Definition,
                    StackCount = slot.ItemInstance.StackCount
                });
            }

            return simulation;
        }

        private static bool TryRemoveFromSimulation(List<SimulatedSlot> slots, ItemDefinition definition, int amount)
        {
            if (slots == null || definition == null || amount <= 0)
                return false;

            int totalAvailable = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Definition != definition)
                    continue;

                totalAvailable += slots[i].StackCount;
                if (totalAvailable >= amount)
                    break;
            }

            if (totalAvailable < amount)
                return false;

            int remaining = amount;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Definition != definition)
                    continue;

                var slot = slots[i];
                int toRemove = Mathf.Min(remaining, slot.StackCount);
                slot.StackCount -= toRemove;
                if (slot.StackCount <= 0)
                {
                    slot.Definition = null;
                    slot.StackCount = 0;
                }

                slots[i] = slot;
                remaining -= toRemove;
                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        private static bool TryAddToSimulation(List<SimulatedSlot> slots, ItemDefinition definition, int amount)
        {
            if (slots == null || definition == null || amount <= 0)
                return false;

            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.Definition != definition)
                    continue;

                int freeSpace = definition.MaxStack - slot.StackCount;
                if (freeSpace <= 0)
                    continue;

                int toAdd = Mathf.Min(remaining, freeSpace);
                slot.StackCount += toAdd;
                slots[i] = slot;
                remaining -= toAdd;

                if (remaining <= 0)
                    return true;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty)
                    continue;

                int toPlace = Mathf.Min(remaining, definition.MaxStack);
                slots[i] = new SimulatedSlot
                {
                    Definition = definition,
                    StackCount = toPlace
                };
                remaining -= toPlace;

                if (remaining <= 0)
                    return true;
            }

            return remaining <= 0;
        }

        private static void RollbackRemovedMaterials(
            InventoryController inventoryController,
            List<(ItemDefinition definition, int amount)> removedMaterials)
        {
            if (inventoryController == null || removedMaterials == null)
                return;

            for (int i = 0; i < removedMaterials.Count; i++)
            {
                var entry = removedMaterials[i];
                if (entry.definition == null || entry.amount <= 0)
                    continue;

                inventoryController.TryAddItem(entry.definition, entry.amount);
            }
        }

        private static void RollbackAddedProducts(
            InventoryController inventoryController,
            List<(ItemDefinition definition, int amount)> addedProducts)
        {
            if (inventoryController == null || addedProducts == null)
                return;

            for (int i = 0; i < addedProducts.Count; i++)
            {
                var entry = addedProducts[i];
                if (entry.definition == null || entry.amount <= 0)
                    continue;

                inventoryController.TryRemoveItem(entry.definition, entry.amount);
            }
        }
    }
}

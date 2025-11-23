using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "ArmorData", menuName = "Items/Type Data/Armor")]
    public class ArmorItemData : ItemTypeData
    {
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Chest;

        [Min(0f)]
        [SerializeField] private float armorValue;

        public EquipmentSlot Slot => slot;
        public float ArmorValue => armorValue;

        public override string GetString()
        {
            return $"Slot: {slot}\n" + 
                    "Armor: {armorValue:0.#}";
        }
    }
}

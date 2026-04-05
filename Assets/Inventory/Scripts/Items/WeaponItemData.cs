using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Items/Type Data/Weapon")]
    public class WeaponItemData : ItemTypeData
    {
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Weapon;

        [Min(0f)]
        [SerializeField] private float minDamage = 10f;
        [Min(0f)]
        [SerializeField] private float maxDamage = 5f;

        public EquipmentSlot Slot => slot;
        public float MinDamage => minDamage;
        public float MaxDamage => maxDamage;

        public override string GetString()
        {
            return $"Slot: {slot}\n" + 
                   $"Damage: {minDamage} - {maxDamage}";
        }
    }
}

// AmmoItemData.cs
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ammo Item", menuName = "Inventory/Items/Ammo")]
    public class AmmoItemData : ItemData
    {
        public AmmoType ammoType;
       
        public float baseDamage = 50f;
        public float armorPenetration = 20f;
        public float fragmentationChance = 0.1f;
        public bool subsonic = false;
        public bool tracer = false;
        public Color tracerColor = Color.red;
       
        public void OnValidate()
        {
            category = ItemCategory.Ammunition;
            stackable = true;
            maxStackSize = 60;
           
            properties.Clear();
           
            properties.Add(new ItemProperty {
                name = "Damage",
                value = baseDamage.ToString(),
                unit = "",
                color = baseDamage > 60 ? Color.red : Color.white
            });
           
            properties.Add(new ItemProperty {
                name = "Penetration",
                value = armorPenetration.ToString(),
                unit = "",
                color = armorPenetration > 30 ? Color.green : Color.white
            });
           
            properties.Add(new ItemProperty {
                name = "Fragmentation",
                value = (fragmentationChance * 100).ToString(),
                unit = "%",
                color = fragmentationChance > 0.15f ? Color.green : Color.white
            });
           
            string ammoTypeName = "";
            switch (ammoType)
            {
                case AmmoType.Pistol_9x19: ammoTypeName = "9x19mm"; break;
                case AmmoType.Pistol_45ACP: ammoTypeName = ".45 ACP"; break;
                case AmmoType.Rifle_556x45: ammoTypeName = "5.56x45mm"; break;
                case AmmoType.Rifle_762x39: ammoTypeName = "7.62x39mm"; break;
                case AmmoType.Rifle_762x51: ammoTypeName = "7.62x51mm"; break;
                case AmmoType.Rifle_762x54R: ammoTypeName = "7.62x54R"; break;
                case AmmoType.Shotgun_12Gauge: ammoTypeName = "12 Gauge"; break;
                case AmmoType.Shotgun_20Gauge: ammoTypeName = "20 Gauge"; break;
            }
           
            displayName = $"{ammoTypeName} {(tracer ? "Tracer " : "")}{(subsonic ? "Subsonic " : "")}Ammo";
           
            if (tracer)
            {
                properties.Add(new ItemProperty {
                    name = "Tracer",
                    value = "Yes",
                    unit = "",
                    color = tracerColor
                });
            }
           
            if (subsonic)
            {
                properties.Add(new ItemProperty {
                    name = "Subsonic",
                    value = "Yes",
                    unit = "",
                    color = Color.cyan
                });
            }
        }
    }
}
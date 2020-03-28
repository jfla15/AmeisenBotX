﻿using AmeisenBotX.Core.Character.Comparators.Objects;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Character.Inventory.Objects;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Character.Comparators
{
    public abstract class BasicComparator : IWowItemComparator
    {
        public BasicComparator(List<ArmorType> armorTypeBlacklist, List<WeaponType> weaponTypeBlacklist)
        {
            ArmorTypeBlacklist = armorTypeBlacklist;
            WeaponTypeBlacklist = weaponTypeBlacklist;
        }

        protected GearscoreFactory GearscoreFactory { get; set; }

        private List<ArmorType> ArmorTypeBlacklist { get; }

        private List<WeaponType> WeaponTypeBlacklist { get; }

        public bool IsBetter(IWowItem current, IWowItem item)
        {
            if ((ArmorTypeBlacklist != null && item.GetType() == typeof(WowArmor) && ArmorTypeBlacklist.Contains(((WowArmor)item).ArmorType))
                || (WeaponTypeBlacklist != null && item.GetType() == typeof(WowWeapon) && WeaponTypeBlacklist.Contains(((WowWeapon)item).WeaponType)))
            {
                return false;
            }

            double scoreCurrent = GearscoreFactory.Calculate(current);
            double scoreNew = GearscoreFactory.Calculate(item);
            return scoreCurrent < scoreNew;
        }
    }
}
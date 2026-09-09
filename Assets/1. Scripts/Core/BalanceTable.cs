using System;

namespace Churub.Core
{
    // Runtime source of truth. Keep costs, effects and eligibility together.
    public static class BalanceTable
    {
        public const int Version = 2;
        public const string VersionKey = "balanceVersion";
        public const string FirstSaleKey = "firstTruckSale";
        public const string FirstEmployeeKey = "firstEmployeeReward";
        public const int EmployeeLimit = 3;
        public const int IngredientCapacity = 20;
        public const float IngredientInterval = 3f;
        public const int ItemsPerBox = 5;
        public const int TruckCapacity = 5;
        public const float StallIncome = 2f;
        public const float StoreIncome = 4f;
        public const float BuffDuration = 120f;
        public const float SpeedBuffMultiplier = 1.5f;
        public const float GoldBuffBonus = .5f;
        public const int CapacityBuff = 3;
        public const float BreakdownProtection = 300f;
        public const float BreakdownProbability = .03f;

        private static readonly int[][] Costs = {
            new[] {100, 250, 500, 900, 1500},
            new[] {150, 300, 600, 1000, 1600},
            new[] {300, 600, 1000, 1600, 2400},
            new[] {100, 200, 400, 700, 1100},
            new[] {150, 300, 600, 1000, 1600},
            new[] {0, 1200, 3000}
        };
        private static readonly float[] Speed = {1f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f};
        private static readonly int[] PlayerCapacity = {3, 4, 5, 7, 9, 12};
        private static readonly int[] EmployeeCapacity = {3, 4, 5, 6, 8, 10};
        private static readonly int[] SalePrice = {50, 65, 80, 95, 110, 125};
        private static readonly int[] Rewards = {25, 25, 50, 50, 50, 100, 100};

        public static int MaxLevel(UpgradeType type) => type == UpgradeType.EmployeeAdd ? 3 : 5;
        public static int Cost(UpgradeType type, int level)
        {
            if ((int)type < 0 || (int)type >= Costs.Length) throw new ArgumentOutOfRangeException(nameof(type));
            return level >= 0 && level < Costs[(int)type].Length ? Costs[(int)type][level] : 0;
        }
        public static float Effect(UpgradeType type, int level)
        {
            int index = Math.Max(0, Math.Min(5, level));
            switch (type)
            {
                case UpgradeType.PlayerSpeed: return 5f * Speed[index];
                case UpgradeType.EmployeeSpeed: return 3f * Speed[index];
                case UpgradeType.PlayerMaxStack: return PlayerCapacity[index];
                case UpgradeType.EmployeeMaxStack: return EmployeeCapacity[index];
                case UpgradeType.GoldPerBox: return SalePrice[index];
                default: return level;
            }
        }
        public static int TutorialReward(int step) => step >= 0 && step < Rewards.Length ? Rewards[step] : 0;
        public static int Lines(GameDataState s) => 1 +
            (s.IsUnlocked("Container1") && s.IsUnlocked("Machine1") ? 1 : 0) +
            (s.IsUnlocked("Container2") && s.IsUnlocked("Machine2") ? 1 : 0);

        public static string UpgradeLock(GameDataState s, UpgradeType type, int level)
        {
            if (type == UpgradeType.EmployeeAdd && level == 0) return "첫 납품 보상으로 받기";
            if (!s.IsUnlocked("Office")) return "사무실 건설 필요";
            int lines = Lines(s);
            if (type == UpgradeType.EmployeeAdd)
                return level == 2 && lines < 2 ? "생산라인 2개 필요" : null;
            if (type == UpgradeType.EmployeeSpeed || type == UpgradeType.EmployeeMaxStack)
            {
                if (level >= 2) return lines < 3 ? "생산라인 3개 필요" : null;
                return s.EmployeeAddCount < 2 && lines < 2 ? "직원 2명 또는 라인 2개 필요" : null;
            }
            int required = level < 2 ? 1 : level < 4 ? 2 : 3;
            return lines < required ? $"생산라인 {required}개 필요" : null;
        }
        public static int FacilityCost(string key)
        {
            switch (key)
            {
                case "Office": return 300;
                case "Container1": return 600;
                case "Machine1": return 900;
                case "Container2": return 1800;
                case "Machine2": return 2700;
                case "Stall": return 1200;
                case "Store": return 2400;
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }
        public static string FacilityLock(GameDataState s, string key)
        {
            if (key == "Office") return s.EmployeeAddCount < 1 ? "첫 직원 받기 필요" : null;
            if (!s.IsUnlocked("Office")) return "사무실 건설 필요";
            switch (key)
            {
                case "Container1": return null;
                case "Machine1": return s.IsUnlocked("Container1") ? null : "추가 컨테이너 1 필요";
                case "Container2": case "Stall": return Lines(s) >= 2 ? null : "생산라인 2개 필요";
                case "Machine2": return s.IsUnlocked("Container2") ? null : "추가 컨테이너 2 필요";
                case "Store": return !s.IsUnlocked("Stall") ? "노점 건설 필요" : Lines(s) < 3 ? "생산라인 3개 필요" : null;
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }
        public static bool CanClaimEmployee(GameDataState s) => s.IsUnlocked(FirstSaleKey) &&
            !s.IsUnlocked(FirstEmployeeKey) && s.EmployeeAddCount == 0 && s.employeeList.Count == 0;
        public static bool ClaimEmployee(GameDataState s)
        {
            if (!CanClaimEmployee(s)) return false;
            s.EmployeeAddCount = 1;
            s.SetUnlocked(FirstEmployeeKey, true);
            s.EmployeeAddCost = Cost(UpgradeType.EmployeeAdd, 1);
            return true;
        }

        // Preserve currency, ownership and progress. Legacy cached prices cease to be authoritative.
        public static void Synchronize(GameDataState s)
        {
            s.EmployeeAddCount = Math.Max(s.EmployeeAddCount, Math.Min(EmployeeLimit, s.employeeList.Count));
            if (s.guideStep >= 7 || s.EmployeeAddCount > 0) s.SetUnlocked(FirstSaleKey, true);
            if (s.EmployeeAddCount > 0) s.SetUnlocked(FirstEmployeeKey, true);
            s.SpeedUpgradeCost = Cost(UpgradeType.PlayerSpeed, s.SpeedUpgradeCount);
            s.MaxStackUpgradeCost = Cost(UpgradeType.PlayerMaxStack, s.MaxStackUpgradeCount);
            s.GoldPerBoxUpgradeCost = Cost(UpgradeType.GoldPerBox, s.GoldPerBoxUpgradeCount);
            s.EmployeeSpeedUpgradeCost = Cost(UpgradeType.EmployeeSpeed, s.EmployeeSpeedUpgradeCount);
            s.EmployeeMaxStackUpgradeCost = Cost(UpgradeType.EmployeeMaxStack, s.EmployeeMaxStackUpgradeCount);
            s.EmployeeAddCost = Cost(UpgradeType.EmployeeAdd, s.EmployeeAddCount);
            s.PlayerSpeed = Math.Max(s.PlayerSpeed, Effect(UpgradeType.PlayerSpeed, s.SpeedUpgradeCount));
            s.PlayerCartSpeed = Math.Max(s.PlayerCartSpeed, Effect(UpgradeType.PlayerSpeed, s.SpeedUpgradeCount) / 2);
            s.EmployeeSpeed = Math.Max(s.EmployeeSpeed, Effect(UpgradeType.EmployeeSpeed, s.EmployeeSpeedUpgradeCount));
            s.EmployeeCartSpeed = Math.Max(s.EmployeeCartSpeed, Effect(UpgradeType.EmployeeSpeed, s.EmployeeSpeedUpgradeCount) / 2);
            s.PlayerMaxStackCount = Math.Max(s.PlayerMaxStackCount, Effect(UpgradeType.PlayerMaxStack, s.MaxStackUpgradeCount));
            s.EmployeeMaxStackCount = Math.Max(s.EmployeeMaxStackCount, Effect(UpgradeType.EmployeeMaxStack, s.EmployeeMaxStackUpgradeCount));
            s.PlayerGoldPerBox = Math.Max(s.PlayerGoldPerBox, Effect(UpgradeType.GoldPerBox, s.GoldPerBoxUpgradeCount));
            s.objectData[VersionKey] = Version;
        }
    }
}

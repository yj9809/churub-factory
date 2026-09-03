using System;

namespace Churub.Core
{
    public enum UpgradeType
    {
        PlayerSpeed = 0,
        PlayerMaxStack = 1,
        GoldPerBox = 2,
        EmployeeSpeed = 3,
        EmployeeMaxStack = 4,
        EmployeeAdd = 5
    }

    public enum UpgradePurchaseStatus
    {
        Success,
        InsufficientGold,
        MaxLevel,
        InvalidUpgrade,
        InvalidState
    }

    public readonly struct UpgradeProgress
    {
        public UpgradeProgress(UpgradeType type, int level, int cost, int maxLevel)
        {
            Type = type;
            Level = level;
            Cost = cost;
            MaxLevel = maxLevel;
        }

        public UpgradeType Type { get; }
        public int Level { get; }
        public int Cost { get; }
        public int MaxLevel { get; }
        public bool IsMaxLevel => Level >= MaxLevel;
        public bool NextPurchaseCreatesPackagingEmployee =>
            Type == UpgradeType.EmployeeAdd &&
            Level == UpgradeService.EmployeeLimit - 1;
    }

    public readonly struct UpgradePurchaseResult
    {
        public UpgradePurchaseResult(
            UpgradePurchaseStatus status,
            UpgradeType type,
            int spentGold,
            int previousLevel,
            int newLevel)
        {
            Status = status;
            Type = type;
            SpentGold = spentGold;
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
        }

        public UpgradePurchaseStatus Status { get; }
        public UpgradeType Type { get; }
        public int SpentGold { get; }
        public int PreviousLevel { get; }
        public int NewLevel { get; }
        public bool Succeeded => Status == UpgradePurchaseStatus.Success;
        public bool RequiresEmployeeSpawn => Succeeded && Type == UpgradeType.EmployeeAdd;
        public bool CreatesPackagingEmployee => RequiresEmployeeSpawn && NewLevel == UpgradeService.EmployeeLimit;
    }

    public sealed class UpgradeService
    {
        public const int EmployeeLimit = 3;
        private const int StandardUpgradeLimit = 5;

        private const float BasePlayerSpeed = 5f;
        private const float BasePlayerCartSpeed = 2.5f;
        private const float BaseGoldPerBox = 50f;
        private const float BaseEmployeeSpeed = 3f;
        private const float BaseEmployeeCartSpeed = 1.5f;

        private static readonly float[] SpeedMultipliers = { 1.1f, 1.2f, 1.3f, 1.4f, 1.5f };
        private static readonly int[] SpeedCosts = { 500, 1000, 3000, 5000, 10000 };
        private static readonly float[] GoldPerBoxMultipliers = { 1.2f, 1.4f, 1.6f, 1.8f, 2f };
        private static readonly int[] GoldPerBoxCosts = { 5000, 7000, 10000, 20000, 30000 };

        private readonly GameDataState state;

        public UpgradeService(GameDataState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public UpgradeProgress GetProgress(UpgradeType type)
        {
            EnsureValidType(type);

            return new UpgradeProgress(
                type,
                GetLevel(type),
                GetCost(type),
                GetMaxLevel(type));
        }

        public UpgradePurchaseResult TryPurchase(UpgradeType type)
        {
            UpgradePurchaseStatus status = EvaluatePurchase(type);
            if (status != UpgradePurchaseStatus.Success)
            {
                int level = IsValidType(type) ? GetLevel(type) : 0;
                return Failure(status, type, level);
            }

            UpgradeProgress progress = GetProgress(type);
            state.PlayerGold -= progress.Cost;
            ApplyUpgrade(type, progress.Level);

            return new UpgradePurchaseResult(
                UpgradePurchaseStatus.Success,
                type,
                progress.Cost,
                progress.Level,
                progress.Level + 1);
        }

        public UpgradePurchaseStatus EvaluatePurchase(UpgradeType type)
        {
            if (!IsValidType(type))
            {
                return UpgradePurchaseStatus.InvalidUpgrade;
            }

            UpgradeProgress progress = GetProgress(type);
            if (progress.Level < 0 || progress.Cost <= 0 || progress.MaxLevel <= 0)
            {
                return UpgradePurchaseStatus.InvalidState;
            }

            if (progress.IsMaxLevel)
            {
                return UpgradePurchaseStatus.MaxLevel;
            }

            return state.PlayerGold < progress.Cost
                ? UpgradePurchaseStatus.InsufficientGold
                : UpgradePurchaseStatus.Success;
        }

        private void ApplyUpgrade(UpgradeType type, int currentLevel)
        {
            switch (type)
            {
                case UpgradeType.PlayerSpeed:
                    state.PlayerSpeed = BasePlayerSpeed * SpeedMultipliers[currentLevel];
                    state.PlayerCartSpeed = BasePlayerCartSpeed * SpeedMultipliers[currentLevel];
                    state.SpeedUpgradeCount++;
                    state.SpeedUpgradeCost = GetNextCost(SpeedCosts, currentLevel, state.SpeedUpgradeCost);
                    break;

                case UpgradeType.PlayerMaxStack:
                    state.PlayerMaxStackCount++;
                    state.MaxStackUpgradeCount++;
                    state.MaxStackUpgradeCost *= 2;
                    break;

                case UpgradeType.GoldPerBox:
                    state.PlayerGoldPerBox = BaseGoldPerBox * GoldPerBoxMultipliers[currentLevel];
                    state.GoldPerBoxUpgradeCount++;
                    state.GoldPerBoxUpgradeCost = GetNextCost(
                        GoldPerBoxCosts,
                        currentLevel,
                        state.GoldPerBoxUpgradeCost);
                    break;

                case UpgradeType.EmployeeSpeed:
                    state.EmployeeSpeed = BaseEmployeeSpeed * SpeedMultipliers[currentLevel];
                    state.EmployeeCartSpeed = BaseEmployeeCartSpeed * SpeedMultipliers[currentLevel];
                    state.EmployeeSpeedUpgradeCount++;
                    state.EmployeeSpeedUpgradeCost = GetNextCost(
                        SpeedCosts,
                        currentLevel,
                        state.EmployeeSpeedUpgradeCost);
                    break;

                case UpgradeType.EmployeeMaxStack:
                    state.EmployeeMaxStackCount++;
                    state.EmployeeMaxStackUpgradeCount++;
                    state.EmployeeMaxStackUpgradeCost *= 2;
                    break;

                case UpgradeType.EmployeeAdd:
                    state.EmployeeAddCount++;
                    state.EmployeeAddCost = state.EmployeeAddCount >= EmployeeLimit
                        ? 25000
                        : state.EmployeeAddCost * 2;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private int GetLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.PlayerSpeed: return state.SpeedUpgradeCount;
                case UpgradeType.PlayerMaxStack: return state.MaxStackUpgradeCount;
                case UpgradeType.GoldPerBox: return state.GoldPerBoxUpgradeCount;
                case UpgradeType.EmployeeSpeed: return state.EmployeeSpeedUpgradeCount;
                case UpgradeType.EmployeeMaxStack: return state.EmployeeMaxStackUpgradeCount;
                case UpgradeType.EmployeeAdd: return state.EmployeeAddCount;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private int GetCost(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.PlayerSpeed: return state.SpeedUpgradeCost;
                case UpgradeType.PlayerMaxStack: return state.MaxStackUpgradeCost;
                case UpgradeType.GoldPerBox: return state.GoldPerBoxUpgradeCost;
                case UpgradeType.EmployeeSpeed: return state.EmployeeSpeedUpgradeCost;
                case UpgradeType.EmployeeMaxStack: return state.EmployeeMaxStackUpgradeCost;
                case UpgradeType.EmployeeAdd: return state.EmployeeAddCost;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private int GetMaxLevel(UpgradeType type)
        {
            if (type == UpgradeType.EmployeeAdd)
            {
                return EmployeeLimit;
            }

            return Math.Min(state.UpgradeMaxCount, StandardUpgradeLimit);
        }

        private static int GetNextCost(int[] costs, int currentLevel, int currentCost)
        {
            int nextLevel = currentLevel + 1;
            return nextLevel < costs.Length ? costs[nextLevel] : currentCost;
        }

        private static UpgradePurchaseResult Failure(
            UpgradePurchaseStatus status,
            UpgradeType type,
            int level)
        {
            return new UpgradePurchaseResult(status, type, 0, level, level);
        }

        private static bool IsValidType(UpgradeType type)
        {
            return type >= UpgradeType.PlayerSpeed && type <= UpgradeType.EmployeeAdd;
        }

        private static void EnsureValidType(UpgradeType type)
        {
            if (!IsValidType(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}

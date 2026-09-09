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
        InvalidState,
        Locked
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
        public const int EmployeeLimit = BalanceTable.EmployeeLimit;

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
            if (progress.Level < 0 || progress.MaxLevel <= 0 || float.IsNaN(state.PlayerGold) || float.IsInfinity(state.PlayerGold))
            {
                return UpgradePurchaseStatus.InvalidState;
            }

            if (progress.IsMaxLevel)
            {
                return UpgradePurchaseStatus.MaxLevel;
            }

            if (BalanceTable.UpgradeLock(state, type, progress.Level) != null)
                return UpgradePurchaseStatus.Locked;

            return state.PlayerGold < progress.Cost
                ? UpgradePurchaseStatus.InsufficientGold
                : UpgradePurchaseStatus.Success;
        }

        private void ApplyUpgrade(UpgradeType type, int currentLevel)
        {
            switch (type)
            {
                case UpgradeType.PlayerSpeed: state.SpeedUpgradeCount++; break;
                case UpgradeType.PlayerMaxStack: state.MaxStackUpgradeCount++; break;
                case UpgradeType.GoldPerBox: state.GoldPerBoxUpgradeCount++; break;
                case UpgradeType.EmployeeSpeed: state.EmployeeSpeedUpgradeCount++; break;
                case UpgradeType.EmployeeMaxStack: state.EmployeeMaxStackUpgradeCount++; break;
                case UpgradeType.EmployeeAdd: state.EmployeeAddCount++; break;
            }
            BalanceTable.Synchronize(state);
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

        private int GetCost(UpgradeType type) => BalanceTable.Cost(type, GetLevel(type));
        private int GetMaxLevel(UpgradeType type) => BalanceTable.MaxLevel(type);

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

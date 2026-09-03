using System.Collections.Generic;

namespace Churub.Core
{
    public class GameDataState
    {
        public string guestID = string.Empty;

        public Dictionary<string, int> upgradeCosts = new Dictionary<string, int>
        {
            { GameDataSchema.Upgrades.SpeedCost, 500 },
            { GameDataSchema.Upgrades.MaxStackCost, 500 },
            { GameDataSchema.Upgrades.GoldPerBoxCost, 5000 },
            { GameDataSchema.Upgrades.EmployeeSpeedCost, 500 },
            { GameDataSchema.Upgrades.EmployeeMaxStackCost, 500 },
            { GameDataSchema.Upgrades.EmployeeAddCost, 5000 },
            { GameDataSchema.Upgrades.MaxCount, 5 },
            { GameDataSchema.Upgrades.SpeedCount, 0 },
            { GameDataSchema.Upgrades.MaxStackCount, 0 },
            { GameDataSchema.Upgrades.GoldPerBoxCount, 0 },
            { GameDataSchema.Upgrades.EmployeeSpeedCount, 0 },
            { GameDataSchema.Upgrades.EmployeeMaxStackCount, 0 },
            { GameDataSchema.Upgrades.EmployeeAddCount, 0 }
        };

        public Dictionary<string, float> playerData = new Dictionary<string, float>
        {
            { GameDataSchema.Player.Speed, 5 },
            { GameDataSchema.Player.CartSpeed, 2.5f },
            { GameDataSchema.Player.MaxStackCount, 3 },
            { GameDataSchema.Player.Gold, 100 },
            { GameDataSchema.Player.GoldPerBox, 50 }
        };

        public List<string> employeeList = new List<string>();

        public Dictionary<string, float> employeeData = new Dictionary<string, float>
        {
            { GameDataSchema.Employee.Speed, 3 },
            { GameDataSchema.Employee.CartSpeed, 1.5f },
            { GameDataSchema.Employee.MaxStackCount, 3 }
        };

        public Dictionary<string, int> objectData = new Dictionary<string, int>
        {
            { GameDataSchema.Objects.ConveyorStorageCount, 0 },
            { GameDataSchema.Objects.ChuruStorageCount, 0 },
            { GameDataSchema.Objects.PackagingWaitCount, 0 },
            { GameDataSchema.Objects.PackagingCount, 0 },
            { GameDataSchema.Objects.PackagingStorageCount, 0 },
            { GameDataSchema.Objects.TruckBoxCount, 0 }
        };

        public Dictionary<string, bool> gameProgressBool = new Dictionary<string, bool>
        {
            { GameDataSchema.Progress.Office, false },
            { GameDataSchema.Progress.Container1, false },
            { GameDataSchema.Progress.Machine1, false },
            { GameDataSchema.Progress.Container2, false },
            { GameDataSchema.Progress.Machine2, false },
            { GameDataSchema.Progress.Stall, false },
            { GameDataSchema.Progress.Store, false }
        };

        public int guideStep;
        public bool newGame = true;

        public int UpgradeMaxCount => upgradeCosts[GameDataSchema.Upgrades.MaxCount];

        public int SpeedUpgradeCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.SpeedCost];
            set => upgradeCosts[GameDataSchema.Upgrades.SpeedCost] = value;
        }

        public int SpeedUpgradeCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.SpeedCount];
            set => upgradeCosts[GameDataSchema.Upgrades.SpeedCount] = value;
        }

        public int MaxStackUpgradeCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.MaxStackCost];
            set => upgradeCosts[GameDataSchema.Upgrades.MaxStackCost] = value;
        }

        public int MaxStackUpgradeCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.MaxStackCount];
            set => upgradeCosts[GameDataSchema.Upgrades.MaxStackCount] = value;
        }

        public int GoldPerBoxUpgradeCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.GoldPerBoxCost];
            set => upgradeCosts[GameDataSchema.Upgrades.GoldPerBoxCost] = value;
        }

        public int GoldPerBoxUpgradeCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.GoldPerBoxCount];
            set => upgradeCosts[GameDataSchema.Upgrades.GoldPerBoxCount] = value;
        }

        public int EmployeeSpeedUpgradeCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeSpeedCost];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeSpeedCost] = value;
        }

        public int EmployeeSpeedUpgradeCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeSpeedCount];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeSpeedCount] = value;
        }

        public int EmployeeMaxStackUpgradeCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeMaxStackCost];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeMaxStackCost] = value;
        }

        public int EmployeeMaxStackUpgradeCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeMaxStackCount];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeMaxStackCount] = value;
        }

        public int EmployeeAddCost
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeAddCost];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeAddCost] = value;
        }

        public int EmployeeAddCount
        {
            get => upgradeCosts[GameDataSchema.Upgrades.EmployeeAddCount];
            set => upgradeCosts[GameDataSchema.Upgrades.EmployeeAddCount] = value;
        }

        public float PlayerSpeed
        {
            get => playerData[GameDataSchema.Player.Speed];
            set => playerData[GameDataSchema.Player.Speed] = value;
        }

        public float PlayerCartSpeed
        {
            get => playerData[GameDataSchema.Player.CartSpeed];
            set => playerData[GameDataSchema.Player.CartSpeed] = value;
        }

        public float PlayerMaxStackCount
        {
            get => playerData[GameDataSchema.Player.MaxStackCount];
            set => playerData[GameDataSchema.Player.MaxStackCount] = value;
        }

        public float PlayerGold
        {
            get => playerData[GameDataSchema.Player.Gold];
            set => playerData[GameDataSchema.Player.Gold] = value;
        }

        public float PlayerGoldPerBox
        {
            get => playerData[GameDataSchema.Player.GoldPerBox];
            set => playerData[GameDataSchema.Player.GoldPerBox] = value;
        }

        public float EmployeeSpeed
        {
            get => employeeData[GameDataSchema.Employee.Speed];
            set => employeeData[GameDataSchema.Employee.Speed] = value;
        }

        public float EmployeeCartSpeed
        {
            get => employeeData[GameDataSchema.Employee.CartSpeed];
            set => employeeData[GameDataSchema.Employee.CartSpeed] = value;
        }

        public float EmployeeMaxStackCount
        {
            get => employeeData[GameDataSchema.Employee.MaxStackCount];
            set => employeeData[GameDataSchema.Employee.MaxStackCount] = value;
        }

        public int PackagingWaitCount
        {
            get => objectData[GameDataSchema.Objects.PackagingWaitCount];
            set => objectData[GameDataSchema.Objects.PackagingWaitCount] = value;
        }

        public int ConveyorStorageCount
        {
            get => objectData[GameDataSchema.Objects.ConveyorStorageCount];
            set => objectData[GameDataSchema.Objects.ConveyorStorageCount] = value;
        }

        public int PackagingCount
        {
            get => objectData[GameDataSchema.Objects.PackagingCount];
            set => objectData[GameDataSchema.Objects.PackagingCount] = value;
        }

        public int ChuruStorageCount
        {
            get => objectData[GameDataSchema.Objects.ChuruStorageCount];
            set => objectData[GameDataSchema.Objects.ChuruStorageCount] = value;
        }

        public int PackagingStorageCount
        {
            get => objectData[GameDataSchema.Objects.PackagingStorageCount];
            set => objectData[GameDataSchema.Objects.PackagingStorageCount] = value;
        }

        public int TruckBoxCount
        {
            get => objectData[GameDataSchema.Objects.TruckBoxCount];
            set => objectData[GameDataSchema.Objects.TruckBoxCount] = value;
        }

        public bool IsUnlocked(string progressKey)
        {
            return gameProgressBool.TryGetValue(progressKey, out var unlocked) && unlocked;
        }

        public void SetUnlocked(string progressKey, bool unlocked)
        {
            gameProgressBool[progressKey] = unlocked;
        }
    }
}

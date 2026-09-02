namespace Churub.Core
{
    public static class GameDataSchema
    {
        public const string TableName = "TestUserData";

        public static class Fields
        {
            public const string GuestId = "guestID";
            public const string UpgradeCosts = "upgradeCosts";
            public const string PlayerData = "playerData";
            public const string EmployeeList = "employeeList";
            public const string EmployeeData = "employeeData";
            public const string ObjectData = "objectData";
            public const string GameProgress = "gameProgressBool";
            public const string GuideStep = "guideStep";
            public const string NewGame = "newGame";
        }

        public static class Upgrades
        {
            public const string SpeedCost = "baseSpeedUpgradeCost";
            public const string MaxStackCost = "baseMaxObjStackCountUpgradeCost";
            public const string GoldPerBoxCost = "baseGoldPerBoxUpgradeCost";
            public const string EmployeeSpeedCost = "baseEmployeeSpeedUpgradeCost";
            public const string EmployeeMaxStackCost = "baseEmployeeMaxObjStackCountUpgradeCost";
            public const string EmployeeAddCost = "baseEmployeeAddCost";
            public const string MaxCount = "baseUpgradeMaxCount";
            public const string SpeedCount = "baseSpeedUpgradeCount";
            public const string MaxStackCount = "baseMaxObjStackCountUpgradeCount";
            public const string GoldPerBoxCount = "baseGoldPerBoxUpgradeCount";
            public const string EmployeeSpeedCount = "baseEmployeeSpeedUpgradeCount";
            public const string EmployeeMaxStackCount = "baseEmployeeMaxObjStackCountUpgradeCount";
            public const string EmployeeAddCount = "baseEmployeeAddCount";
        }

        public static class Player
        {
            public const string Speed = "baseSpeed";
            public const string CartSpeed = "baseCartSpeed";
            public const string MaxStackCount = "maxObjStackCount";
            public const string Gold = "gold";
            public const string GoldPerBox = "goldPerBox";
        }

        public static class Employee
        {
            public const string Speed = "employeeSpeed";
            public const string CartSpeed = "employeeCartSpeed";
            public const string MaxStackCount = "employeeMaxObjStackCount";
        }

        public static class Objects
        {
            public const string ConveyorStorageCount = "conveyorBeltBoxStorageStackCount";
            public const string ChuruStorageCount = "churuStorageStackCount";
            public const string PackagingWaitCount = "packagingWaitObjCount";
            public const string PackagingCount = "boxPackagingCount";
            public const string PackagingStorageCount = "packagingBoxStorageStackCount";
            public const string TruckBoxCount = "truckBoxStackCount";
        }

        public static class Progress
        {
            public const string Office = "Office";
            public const string Container1 = "Container1";
            public const string Machine1 = "Machine1";
            public const string Container2 = "Container2";
            public const string Machine2 = "Machine2";
            public const string Stall = "Stall";
            public const string Store = "Store";
        }
    }
}

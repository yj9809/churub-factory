using Churub.Core;
using NUnit.Framework;

public sealed class UpgradeServiceTests
{
    [Test]
    public void TryPurchase_PlayerSpeed_UpdatesStatsLevelCostAndGold()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        UpgradePurchaseResult result = service.TryPurchase(UpgradeType.PlayerSpeed);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(state.PlayerGold, Is.EqualTo(99500f));
        Assert.That(state.PlayerSpeed, Is.EqualTo(5.5f).Within(0.001f));
        Assert.That(state.PlayerCartSpeed, Is.EqualTo(2.75f).Within(0.001f));
        Assert.That(state.SpeedUpgradeCount, Is.EqualTo(1));
        Assert.That(state.SpeedUpgradeCost, Is.EqualTo(1000));
    }

    [Test]
    public void TryPurchase_PlayerMaxStack_UpdatesCapacityAndDoublesCost()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        service.TryPurchase(UpgradeType.PlayerMaxStack);

        Assert.That(state.PlayerMaxStackCount, Is.EqualTo(4f));
        Assert.That(state.MaxStackUpgradeCount, Is.EqualTo(1));
        Assert.That(state.MaxStackUpgradeCost, Is.EqualTo(1000));
    }

    [Test]
    public void TryPurchase_GoldPerBox_UpdatesNextCostWithoutCorruptingLevel()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        service.TryPurchase(UpgradeType.GoldPerBox);

        Assert.That(state.PlayerGoldPerBox, Is.EqualTo(60f).Within(0.001f));
        Assert.That(state.GoldPerBoxUpgradeCount, Is.EqualTo(1));
        Assert.That(state.GoldPerBoxUpgradeCost, Is.EqualTo(7000));
    }

    [Test]
    public void TryPurchase_EmployeeSpeed_UpdatesBothMovementSpeeds()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        service.TryPurchase(UpgradeType.EmployeeSpeed);

        Assert.That(state.EmployeeSpeed, Is.EqualTo(3.3f).Within(0.001f));
        Assert.That(state.EmployeeCartSpeed, Is.EqualTo(1.65f).Within(0.001f));
        Assert.That(state.EmployeeSpeedUpgradeCount, Is.EqualTo(1));
        Assert.That(state.EmployeeSpeedUpgradeCost, Is.EqualTo(1000));
    }

    [Test]
    public void TryPurchase_EmployeeMaxStack_UpdatesCapacityAndDoublesCost()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        service.TryPurchase(UpgradeType.EmployeeMaxStack);

        Assert.That(state.EmployeeMaxStackCount, Is.EqualTo(4f));
        Assert.That(state.EmployeeMaxStackUpgradeCount, Is.EqualTo(1));
        Assert.That(state.EmployeeMaxStackUpgradeCost, Is.EqualTo(1000));
    }

    [Test]
    public void TryPurchase_EmployeeAdd_ReportsPackagingEmployeeOnThirdHire()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        UpgradePurchaseResult first = service.TryPurchase(UpgradeType.EmployeeAdd);
        Assert.That(
            service.GetProgress(UpgradeType.EmployeeAdd).NextPurchaseCreatesPackagingEmployee,
            Is.False);

        UpgradePurchaseResult second = service.TryPurchase(UpgradeType.EmployeeAdd);
        Assert.That(
            service.GetProgress(UpgradeType.EmployeeAdd).NextPurchaseCreatesPackagingEmployee,
            Is.True);

        UpgradePurchaseResult third = service.TryPurchase(UpgradeType.EmployeeAdd);

        Assert.That(first.RequiresEmployeeSpawn, Is.True);
        Assert.That(first.CreatesPackagingEmployee, Is.False);
        Assert.That(second.CreatesPackagingEmployee, Is.False);
        Assert.That(third.CreatesPackagingEmployee, Is.True);
        Assert.That(state.EmployeeAddCount, Is.EqualTo(3));
        Assert.That(state.EmployeeAddCost, Is.EqualTo(25000));
        Assert.That(state.PlayerGold, Is.EqualTo(65000f));
    }

    [Test]
    public void TryPurchase_InsufficientGold_DoesNotMutateState()
    {
        var state = new GameDataState();
        var service = new UpgradeService(state);
        float originalGold = state.PlayerGold;
        int originalLevel = state.SpeedUpgradeCount;

        UpgradePurchaseResult result = service.TryPurchase(UpgradeType.PlayerSpeed);

        Assert.That(result.Status, Is.EqualTo(UpgradePurchaseStatus.InsufficientGold));
        Assert.That(state.PlayerGold, Is.EqualTo(originalGold));
        Assert.That(state.SpeedUpgradeCount, Is.EqualTo(originalLevel));
        Assert.That(state.PlayerSpeed, Is.EqualTo(5f));
    }

    [Test]
    public void TryPurchase_MaxLevel_DoesNotSpendGold()
    {
        var state = CreateFundedState();
        state.SpeedUpgradeCount = state.UpgradeMaxCount;
        var service = new UpgradeService(state);
        float originalGold = state.PlayerGold;

        UpgradePurchaseResult result = service.TryPurchase(UpgradeType.PlayerSpeed);

        Assert.That(result.Status, Is.EqualTo(UpgradePurchaseStatus.MaxLevel));
        Assert.That(state.PlayerGold, Is.EqualTo(originalGold));
    }

    [Test]
    public void TryPurchase_InvalidUpgrade_DoesNotSpendGold()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);
        float originalGold = state.PlayerGold;

        UpgradePurchaseResult result = service.TryPurchase((UpgradeType)999);

        Assert.That(result.Status, Is.EqualTo(UpgradePurchaseStatus.InvalidUpgrade));
        Assert.That(state.PlayerGold, Is.EqualTo(originalGold));
    }

    [Test]
    public void TryPurchase_AllPlayerSpeedLevels_PreservesReleasedProgression()
    {
        var state = CreateFundedState();
        var service = new UpgradeService(state);

        for (int i = 0; i < state.UpgradeMaxCount; i++)
        {
            Assert.That(service.TryPurchase(UpgradeType.PlayerSpeed).Succeeded, Is.True);
        }

        Assert.That(state.PlayerGold, Is.EqualTo(80500f));
        Assert.That(state.PlayerSpeed, Is.EqualTo(7.5f).Within(0.001f));
        Assert.That(state.PlayerCartSpeed, Is.EqualTo(3.75f).Within(0.001f));
        Assert.That(state.SpeedUpgradeCount, Is.EqualTo(5));
        Assert.That(service.GetProgress(UpgradeType.PlayerSpeed).IsMaxLevel, Is.True);
    }

    private static GameDataState CreateFundedState()
    {
        return new GameDataState { PlayerGold = 100000f };
    }
}

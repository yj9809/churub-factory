using Churub.Core;
using NUnit.Framework;

public sealed class UpgradeServiceTests
{
    private static GameDataState Funded()
    {
        var s = new GameDataState { PlayerGold = 100000 };
        foreach (var key in new[] {"Office", "Container1", "Machine1", "Container2", "Machine2"}) s.SetUnlocked(key, true);
        return s;
    }

    [Test] public void PlayerProgression_ChargesCorrectTotalAndAppliesAbsoluteValues()
    {
        var s = Funded(); var service = new UpgradeService(s);
        int[] capacity = {4,5,7,9,12};
        for (int i=0;i<5;i++)
        {
            Assert.That(service.TryPurchase(UpgradeType.PlayerMaxStack).Succeeded, Is.True);
            Assert.That(s.PlayerMaxStackCount, Is.EqualTo(capacity[i]));
        }
        Assert.That(s.PlayerGold, Is.EqualTo(96350));
        Assert.That(service.TryPurchase(UpgradeType.PlayerMaxStack).Status, Is.EqualTo(UpgradePurchaseStatus.MaxLevel));
    }
    [Test] public void SalesUpgrade_PaysBackInTwentyBoxes()
    {
        var s=Funded(); var result=new UpgradeService(s).TryPurchase(UpgradeType.GoldPerBox);
        Assert.That(result.SpentGold, Is.EqualTo(300));
        Assert.That(s.PlayerGoldPerBox, Is.EqualTo(65));
        Assert.That(s.GoldPerBoxUpgradeCost, Is.EqualTo(600));
        Assert.That((s.PlayerGoldPerBox-50)*20, Is.EqualTo(result.SpentGold));
    }
    [Test] public void AllFiveTracks_ReachApprovedStatsAndCosts()
    {
        var s=Funded(); var service=new UpgradeService(s);
        for(int type=0;type<5;type++)
            for(int level=0;level<5;level++) Assert.That(service.TryPurchase((UpgradeType)type).Succeeded, Is.True);
        Assert.That(s.PlayerGold, Is.EqualTo(81050));
        Assert.That(s.PlayerSpeed, Is.EqualTo(7.5f));
        Assert.That(s.PlayerCartSpeed, Is.EqualTo(3.75f));
        Assert.That(s.EmployeeSpeed, Is.EqualTo(4.5f));
        Assert.That(s.EmployeeCartSpeed, Is.EqualTo(2.25f));
        Assert.That(s.EmployeeMaxStackCount, Is.EqualTo(10));
        Assert.That(s.PlayerMaxStackCount, Is.EqualTo(12));
        Assert.That(s.PlayerGoldPerBox, Is.EqualTo(125));
    }
    [Test] public void FreeEmployee_IsSaleGatedAndCannotBePurchasedOrClaimedTwice()
    {
        var s=Funded(); var service=new UpgradeService(s);
        Assert.That(service.TryPurchase(UpgradeType.EmployeeAdd).Status, Is.EqualTo(UpgradePurchaseStatus.Locked));
        Assert.That(BalanceTable.ClaimEmployee(s), Is.False);
        s.SetUnlocked(BalanceTable.FirstSaleKey,true);
        Assert.That(BalanceTable.ClaimEmployee(s), Is.True);
        Assert.That(BalanceTable.ClaimEmployee(s), Is.False);
        Assert.That(s.PlayerGold, Is.EqualTo(100000));
        Assert.That(s.EmployeeAddCount, Is.EqualTo(1));
        Assert.That(service.TryPurchase(UpgradeType.EmployeeAdd).SpentGold, Is.EqualTo(1200));
        Assert.That(service.GetProgress(UpgradeType.EmployeeAdd).NextPurchaseCreatesPackagingEmployee, Is.True);
        var third=service.TryPurchase(UpgradeType.EmployeeAdd);
        Assert.That(third.CreatesPackagingEmployee, Is.True);
        Assert.That(third.SpentGold, Is.EqualTo(3000));
        Assert.That(s.PlayerGold, Is.EqualTo(95800));
        Assert.That(service.TryPurchase(UpgradeType.EmployeeAdd).Status, Is.EqualTo(UpgradePurchaseStatus.MaxLevel));
    }
    [Test] public void StaffUnlock_AllowsEitherSecondEmployeeOrSecondLine()
    {
        var s=new GameDataState {PlayerGold=10000,EmployeeAddCount=1};s.SetUnlocked("Office",true);
        var service=new UpgradeService(s);
        Assert.That(service.EvaluatePurchase(UpgradeType.EmployeeSpeed),Is.EqualTo(UpgradePurchaseStatus.Locked));
        s.EmployeeAddCount=2;
        Assert.That(service.EvaluatePurchase(UpgradeType.EmployeeSpeed),Is.EqualTo(UpgradePurchaseStatus.Success));
        s.EmployeeAddCount=1;s.SetUnlocked("Container1",true);
        Assert.That(service.EvaluatePurchase(UpgradeType.EmployeeSpeed),Is.EqualTo(UpgradePurchaseStatus.Locked));
        s.SetUnlocked("Machine1",true);
        Assert.That(service.TryPurchase(UpgradeType.EmployeeSpeed).Succeeded,Is.True);
        Assert.That(service.TryPurchase(UpgradeType.EmployeeSpeed).Succeeded,Is.True);
        Assert.That(service.EvaluatePurchase(UpgradeType.EmployeeSpeed),Is.EqualTo(UpgradePurchaseStatus.Locked));
        s.SetUnlocked("Container2",true);s.SetUnlocked("Machine2",true);
        Assert.That(service.EvaluatePurchase(UpgradeType.EmployeeSpeed),Is.EqualTo(UpgradePurchaseStatus.Success));
    }
    [Test] public void PlayerUnlock_PreventsSkippingExpansion()
    {
        var s=new GameDataState {PlayerGold=10000};var service=new UpgradeService(s);
        Assert.That(service.EvaluatePurchase(UpgradeType.PlayerMaxStack),Is.EqualTo(UpgradePurchaseStatus.Locked));
        s.SetUnlocked("Office",true);
        service.TryPurchase(UpgradeType.PlayerMaxStack);service.TryPurchase(UpgradeType.PlayerMaxStack);
        Assert.That(service.EvaluatePurchase(UpgradeType.PlayerMaxStack),Is.EqualTo(UpgradePurchaseStatus.Locked));
        s.SetUnlocked("Container1",true);s.SetUnlocked("Machine1",true);
        service.TryPurchase(UpgradeType.PlayerMaxStack);service.TryPurchase(UpgradeType.PlayerMaxStack);
        Assert.That(service.EvaluatePurchase(UpgradeType.PlayerMaxStack),Is.EqualTo(UpgradePurchaseStatus.Locked));
    }
    [Test] public void FailedPurchases_DoNotSpendOrAdvance()
    {
        var s=Funded();s.PlayerGold=0;var service=new UpgradeService(s);
        Assert.That(service.TryPurchase(UpgradeType.PlayerSpeed).Status,Is.EqualTo(UpgradePurchaseStatus.InsufficientGold));
        Assert.That(service.TryPurchase((UpgradeType)999).Status,Is.EqualTo(UpgradePurchaseStatus.InvalidUpgrade));
        Assert.That(s.SpeedUpgradeCount,Is.Zero);Assert.That(s.PlayerGold,Is.Zero);
        s.PlayerGold=float.NaN;
        Assert.That(service.TryPurchase(UpgradeType.PlayerSpeed).Status,Is.EqualTo(UpgradePurchaseStatus.InvalidState));
    }
    [Test] public void LegacySave_SynchronizationPreservesOwnershipCurrencyAndHigherStats()
    {
        var s=new GameDataState {PlayerGold=1234,MaxStackUpgradeCount=5,PlayerMaxStackCount=8,SpeedUpgradeCount=1,PlayerSpeed=9,guideStep=10};
        s.employeeList.Add("existing cat");s.SetUnlocked("Office",true);
        BalanceTable.Synchronize(s);BalanceTable.Synchronize(s);
        Assert.That(s.PlayerGold,Is.EqualTo(1234));Assert.That(s.PlayerMaxStackCount,Is.EqualTo(12));
        Assert.That(s.PlayerSpeed,Is.EqualTo(9));Assert.That(s.EmployeeAddCount,Is.EqualTo(1));
        Assert.That(s.SpeedUpgradeCost,Is.EqualTo(250));Assert.That(s.EmployeeAddCost,Is.EqualTo(1200));
        Assert.That(BalanceTable.CanClaimEmployee(s),Is.False);Assert.That(s.guideStep,Is.EqualTo(10));
    }
    [Test] public void TutorialBudget_CoversOfficeCapacityAndSaleUpgrade()
    {
        var s=new GameDataState();for(int i=0;i<7;i++)s.PlayerGold+=BalanceTable.TutorialReward(i);
        s.PlayerGold+=250;s.SetUnlocked(BalanceTable.FirstSaleKey,true);BalanceTable.ClaimEmployee(s);
        Assert.That(s.PlayerGold,Is.EqualTo(750));
        s.PlayerGold-=BalanceTable.FacilityCost("Office");s.SetUnlocked("Office",true);
        var service=new UpgradeService(s);
        service.TryPurchase(UpgradeType.PlayerMaxStack);service.TryPurchase(UpgradeType.GoldPerBox);
        Assert.That(s.PlayerGold,Is.Zero);
        s.PlayerGold+=4*5*s.PlayerGoldPerBox;
        Assert.That(service.TryPurchase(UpgradeType.EmployeeAdd).Succeeded,Is.True);
        Assert.That(s.PlayerGold,Is.EqualTo(100));
    }
    [Test] public void Facilities_RespectPairsAndStorePrerequisites()
    {
        var s=new GameDataState();Assert.That(BalanceTable.FacilityLock(s,"Office"),Is.Not.Null);
        s.EmployeeAddCount=1;Assert.That(BalanceTable.FacilityLock(s,"Office"),Is.Null);
        s.SetUnlocked("Office",true);Assert.That(BalanceTable.FacilityLock(s,"Machine1"),Is.Not.Null);
        s.SetUnlocked("Container1",true);Assert.That(BalanceTable.FacilityLock(s,"Machine1"),Is.Null);
        Assert.That(BalanceTable.FacilityLock(s,"Container2"),Is.Not.Null);
        s.SetUnlocked("Machine1",true);Assert.That(BalanceTable.FacilityLock(s,"Stall"),Is.Null);
        Assert.That(BalanceTable.FacilityLock(s,"Store"),Is.Not.Null);
        s.SetUnlocked("Stall",true);s.SetUnlocked("Container2",true);s.SetUnlocked("Machine2",true);
        Assert.That(BalanceTable.FacilityLock(s,"Store"),Is.Null);
    }
}

using Churub.Core;
using NUnit.Framework;

public sealed class WorkSchedulerTests
{
    [Test]
    public void TryReserveBest_SelectsTargetWithHighestStackCount()
    {
        var scheduler = new WorkScheduler<FakeWorkTarget>();
        var lowStock = new FakeWorkTarget(2);
        var highStock = new FakeWorkTarget(5);
        scheduler.Register(lowStock);
        scheduler.Register(highStock);

        var reserved = scheduler.TryReserveBest(out var target);

        Assert.That(reserved, Is.True);
        Assert.That(target, Is.SameAs(highStock));
    }

    [Test]
    public void TryReserveBest_DoesNotAssignSameTargetTwice()
    {
        var scheduler = new WorkScheduler<FakeWorkTarget>();
        var first = new FakeWorkTarget(5);
        var second = new FakeWorkTarget(3);
        scheduler.Register(first);
        scheduler.Register(second);

        scheduler.TryReserveBest(out var firstAssignment);
        scheduler.TryReserveBest(out var secondAssignment);

        Assert.That(firstAssignment, Is.SameAs(first));
        Assert.That(secondAssignment, Is.SameAs(second));
    }

    [Test]
    public void Release_MakesTargetAvailableAgain()
    {
        var scheduler = new WorkScheduler<FakeWorkTarget>();
        var workTarget = new FakeWorkTarget(1);
        scheduler.Register(workTarget);
        scheduler.TryReserveBest(out var firstAssignment);

        scheduler.Release(firstAssignment);
        var reservedAgain = scheduler.TryReserveBest(out var secondAssignment);

        Assert.That(reservedAgain, Is.True);
        Assert.That(secondAssignment, Is.SameAs(workTarget));
    }

    [Test]
    public void TryReserveBest_IgnoresTargetsWithoutStock()
    {
        var scheduler = new WorkScheduler<FakeWorkTarget>();
        scheduler.Register(new FakeWorkTarget(0));

        var reserved = scheduler.TryReserveBest(out var target);

        Assert.That(reserved, Is.False);
        Assert.That(target, Is.Null);
    }

    private sealed class FakeWorkTarget : IWorkTarget
    {
        private readonly int stackCount;

        public FakeWorkTarget(int stackCount)
        {
            this.stackCount = stackCount;
        }

        public int GetStackCount()
        {
            return stackCount;
        }
    }
}

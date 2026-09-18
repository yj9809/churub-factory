using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ItemBufferTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    private Item CreateItem(ItemType type)
    {
        var gameObject = new GameObject(type.ToString());
        objects.Add(gameObject);
        var item = gameObject.AddComponent<Item>();
        var serialized = new SerializedObject(item);
        serialized.FindProperty("type").intValue = (int)type;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var gameObject in objects)
            Object.DestroyImmediate(gameObject);
        objects.Clear();
    }

    [Test]
    public void EmptyStationStillRejectsTheWrongType()
    {
        var station = new ItemBuffer(40, ItemType.Churu);
        Assert.That(station.TryAdd(CreateItem(ItemType.Box)), Is.False);
        Assert.That(station.IsEmpty, Is.True);
        Assert.That(station.TryAdd(CreateItem(ItemType.Churu)), Is.True);
    }

    [Test]
    public void SameWorldItemCannotBeCollectedByTwoOwners()
    {
        var first = new CarrierInventory(2);
        var second = new ItemBuffer(40, ItemType.Ingredient);
        var item = CreateItem(ItemType.Ingredient);
        Assert.That(first.TryAdd(item), Is.True);
        Assert.That(item.IsStored, Is.True);
        Assert.That(second.TryAdd(item), Is.False);
        Assert.That(second.Count, Is.Zero);
        Assert.That(first.TryAdd(item), Is.False);
        Assert.That(first.Count, Is.EqualTo(1));
    }

    [Test]
    public void FullDestinationPreservesSourceAndBothTopItems()
    {
        var source = new CarrierInventory(2);
        var destination = new ItemBuffer(1, ItemType.Box);
        var carried = CreateItem(ItemType.Box);
        var stored = CreateItem(ItemType.Box);
        source.TryAdd(carried);
        destination.TryAdd(stored);
        Assert.That(source.TryMoveTo(destination, out var moved), Is.False);
        Assert.That(moved, Is.Null);
        source.TryPeek(out var sourceTop);
        destination.TryPeek(out var destinationTop);
        Assert.That(sourceTop, Is.SameAs(carried));
        Assert.That(destinationTop, Is.SameAs(stored));
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(destination.Count, Is.EqualTo(1));
        Assert.That(carried.IsStored, Is.True);
    }

    [Test]
    public void WrongTypeDestinationPreservesSourceAndCanTransferElsewhere()
    {
        var source = new CarrierInventory(2);
        var ingredientInput = new ItemBuffer(int.MaxValue, ItemType.Ingredient);
        var churuInput = new ItemBuffer(int.MaxValue, ItemType.Churu);
        var item = CreateItem(ItemType.Churu);
        source.TryAdd(item);
        Assert.That(source.TryMoveTo(ingredientInput, out _), Is.False);
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(ingredientInput.IsEmpty, Is.True);
        Assert.That(source.TryMoveTo(churuInput, out var moved), Is.True);
        Assert.That(moved, Is.SameAs(item));
        Assert.That(source.IsEmpty, Is.True);
        Assert.That(churuInput.Count, Is.EqualTo(1));
    }

    [Test]
    public void TransferMovesOnlyTheTopAndPopReleasesOwnership()
    {
        var source = new ItemBuffer(40, ItemType.Churu);
        var destination = new CarrierInventory(2);
        var first = CreateItem(ItemType.Churu);
        var last = CreateItem(ItemType.Churu);
        source.TryAdd(first);
        source.TryAdd(last);
        Assert.That(source.TryMoveTo(destination, out var moved), Is.True);
        Assert.That(moved, Is.SameAs(last));
        source.TryPeek(out var remaining);
        Assert.That(remaining, Is.SameAs(first));
        Assert.That(source.TryAdd(last), Is.False);
        destination.TryPop(out var popped);
        Assert.That(popped, Is.SameAs(last));
        Assert.That(last.IsStored, Is.False);
        Assert.That(source.TryAdd(last), Is.True);
    }

    [Test]
    public void InvalidTransfersLeaveTheItemOwnedByItsSource()
    {
        var source = new CarrierInventory(1);
        var item = CreateItem(ItemType.Box);
        source.TryAdd(item);
        Assert.That(source.TryMoveTo(source, out _), Is.False);
        Assert.That(source.TryMoveTo(null, out _), Is.False);
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(item.IsStored, Is.True);
        source.TryPop(out _);
        Assert.That(source.TryMoveTo(new CarrierInventory(1), out _), Is.False);
    }

    [Test]
    public void ProcessingOutputCannotBeCollectedUntilReleased()
    {
        var outputInTransit = new ItemBuffer(1, ItemType.Box);
        var storage = new ItemBuffer(40, ItemType.Box);
        var box = CreateItem(ItemType.Box);
        outputInTransit.TryAdd(box);
        Assert.That(storage.TryAdd(box), Is.False);
        outputInTransit.TryPop(out _);
        Assert.That(storage.TryAdd(box), Is.True);
    }
}

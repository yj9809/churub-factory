using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CarrierInventoryTests
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
            UnityEngine.Object.DestroyImmediate(gameObject);
        objects.Clear();
    }

    [TestCase(ItemType.Ingredient)]
    [TestCase(ItemType.Churu)]
    [TestCase(ItemType.Box)]
    public void EmptyInventoryAcceptsAnyType(ItemType type)
    {
        var inventory = new CarrierInventory(2);
        var item = CreateItem(type);
        Assert.That(inventory.TryAdd(item), Is.True);
        Assert.That(inventory.TryPeek(out var top), Is.True);
        Assert.That(top, Is.SameAs(item));
    }

    [Test]
    public void MixedTypeIsRejectedWithoutChangingContents()
    {
        var inventory = new CarrierInventory(2);
        var ingredient = CreateItem(ItemType.Ingredient);
        inventory.TryAdd(ingredient);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Churu)), Is.False);
        Assert.That(inventory.Count, Is.EqualTo(1));
        inventory.TryPeek(out var top);
        Assert.That(top, Is.SameAs(ingredient));
    }

    [Test]
    public void CapacityIsEnforcedAndItemsLeaveInReverseOrder()
    {
        var inventory = new CarrierInventory(2);
        var first = CreateItem(ItemType.Box);
        var second = CreateItem(ItemType.Box);
        Assert.That(inventory.TryAdd(first), Is.True);
        Assert.That(inventory.TryAdd(second), Is.True);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.False);
        Assert.That(inventory.Count, Is.EqualTo(2));
        Assert.That(inventory.TryPop(out var top), Is.True);
        Assert.That(top, Is.SameAs(second));
        inventory.TryPop(out top);
        Assert.That(top, Is.SameAs(first));
        Assert.That(inventory.IsEmpty, Is.True);
        Assert.That(inventory.TryPeek(out top), Is.False);
        Assert.That(top, Is.Null);
        Assert.That(inventory.TryPop(out top), Is.False);
        Assert.That(top, Is.Null);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Churu)), Is.True);
    }

    [Test]
    public void NullAndDuplicateAreRejected()
    {
        var inventory = new CarrierInventory(3);
        var item = CreateItem(ItemType.Churu);
        Assert.That(inventory.TryAdd(null), Is.False);
        inventory.TryAdd(item);
        Assert.That(inventory.TryAdd(item), Is.False);
        Assert.That(inventory.Count, Is.EqualTo(1));
    }

    [Test]
    public void CapacityReductionPreservesItemsAndBlocksAdditionsUntilSpaceExists()
    {
        var inventory = new CarrierInventory(2);
        inventory.TryAdd(CreateItem(ItemType.Box));
        inventory.TryAdd(CreateItem(ItemType.Box));
        inventory.Capacity = 1;
        Assert.That(inventory.Count, Is.EqualTo(2));
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.False);
        inventory.TryPop(out _);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.False);
        inventory.TryPop(out _);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.True);
        inventory.Capacity = 2;
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.True);
    }

    [Test]
    public void TypeQueriesFollowContentsWithoutRememberingThePreviousType()
    {
        var inventory = new CarrierInventory(2);
        Assert.That(inventory.ContainsType(ItemType.Ingredient), Is.False);
        inventory.TryAdd(CreateItem(ItemType.Ingredient));
        Assert.That(inventory.ContainsType(ItemType.Ingredient), Is.True);
        Assert.That(inventory.ContainsType(ItemType.Churu), Is.False);
        inventory.TryPop(out _);
        Assert.That(inventory.ContainsType(ItemType.Ingredient), Is.False);
        inventory.TryAdd(CreateItem(ItemType.Churu));
        Assert.That(inventory.ContainsType(ItemType.Churu), Is.True);
    }

    [Test]
    public void ZeroCapacityRejectsItemsAndNegativeCapacityIsInvalid()
    {
        var inventory = new CarrierInventory(0);
        Assert.That(inventory.TryAdd(CreateItem(ItemType.Box)), Is.False);
        Assert.That(inventory.IsEmpty, Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Capacity = -1);
        Assert.That(inventory.Capacity, Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CarrierInventory(-1));
    }
}

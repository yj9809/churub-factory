using System;
using System.Collections.Generic;

// Runtime storage only. Visual movement, processing and save formats belong to stations.
public class ItemBuffer
{
    private readonly Stack<Item> items = new Stack<Item>();
    private readonly ItemType? acceptedType;
    private int capacity;

    public ItemBuffer(int capacity, ItemType? acceptedType = null)
    {
        Capacity = capacity;
        this.acceptedType = acceptedType;
    }

    public int Count => items.Count;
    public bool IsEmpty => items.Count == 0;
    public bool IsFull => items.Count >= capacity;

    // Lowering a limit preserves items already stored.
    public int Capacity
    {
        get => capacity;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            capacity = value;
        }
    }

    public bool ContainsType(ItemType type)
    {
        return TryPeek(out var item) && item != null && item.Type == type;
    }

    private bool AcceptsTypeAndCount(Item item)
    {
        return item != null && !IsFull
            && (!acceptedType.HasValue || acceptedType.Value == item.Type)
            && (items.Count == 0 || items.Peek().Type == item.Type);
    }

    public bool CanAccept(Item item)
    {
        return AcceptsTypeAndCount(item) && item.Owner == null;
    }

    public bool TryAdd(Item item)
    {
        if (!CanAccept(item)) return false;
        items.Push(item);
        item.Owner = this;
        return true;
    }

    public bool TryPeek(out Item item)
    {
        item = items.Count == 0 ? null : items.Peek();
        return items.Count != 0;
    }

    public bool TryPop(out Item item)
    {
        if (items.Count == 0)
        {
            item = null;
            return false;
        }
        item = items.Pop();
        if (item != null) item.Owner = null;
        return true;
    }

    // No callbacks/await between validation and commit; rejection preserves both buffers.
    public bool TryMoveTo(ItemBuffer destination, out Item item)
    {
        item = null;
        if (destination == null || ReferenceEquals(this, destination)
            || !TryPeek(out var candidate) || candidate == null
            || !ReferenceEquals(candidate.Owner, this)
            || !destination.AcceptsTypeAndCount(candidate))
            return false;

        items.Pop();
        destination.items.Push(candidate);
        candidate.Owner = destination;
        item = candidate;
        return true;
    }
}

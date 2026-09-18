using UnityEngine;

public enum ItemType
{
    Ingredient = 0,
    Churu = 1,
    Box = 2
}

[DisallowMultipleComponent]
public sealed class Item : MonoBehaviour
{
    [SerializeField] private ItemType type;

    public ItemType Type => type;

    // Runtime ownership is not serialized; pooled and freshly spawned items are unowned.
    internal ItemBuffer Owner { get; set; }
    public bool IsStored => Owner != null;
}

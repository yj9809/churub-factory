// A carrier accepts any one type at a time; stations can fix their accepted type.
public sealed class CarrierInventory : ItemBuffer
{
    public CarrierInventory(int capacity) : base(capacity) { }
}

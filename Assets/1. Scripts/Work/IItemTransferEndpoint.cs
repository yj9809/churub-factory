using UnityEngine;

// Each station owns transfer direction, acceptance rules, and storage placement.
public interface IItemTransferEndpoint
{
    bool TryTransfer(CarrierInventory inventory, Transform carryParent);
}

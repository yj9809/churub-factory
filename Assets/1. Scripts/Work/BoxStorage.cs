using UnityEngine;
using Sirenix.OdinInspector;

public enum BoxStorageType
{
    ChuruStorage,
    BoxStorage,
    IngredientStorage
}

public class BoxStorage : MonoBehaviour, IStackable, IObjectDataSave, IItemTransferEndpoint
{
    [TabGroup("Storage Transform"), SerializeField] private Transform[] boxTransform;
    [EnumToggleButtons] public BoxStorageType bsType;
    [TabGroup("Game Object"), SerializeField] private GameObject churu;
    [TabGroup("Game Object"), SerializeField] private GameObject box;
    [TabGroup("Game Object"), SerializeField] private IngredientMaker ingredientMaker;

    private const int Capacity = 40;
    private ItemBuffer storage;
    private ItemBuffer Storage => storage ?? (storage = new ItemBuffer(Capacity,
        bsType == BoxStorageType.ChuruStorage ? ItemType.Churu : ItemType.Box));
    private DataManager data;

    public int Count => Storage.Count;
    public bool IsFull => Storage.IsFull;

    public bool TryTransfer(CarrierInventory inventory, Transform carryParent)
    {
        return bsType != BoxStorageType.IngredientStorage
            && ItemTransferUtility.TryMove(Storage, inventory, carryParent);
    }

    private void Start()
    {
        data = DataManager.Instance;
        if (bsType != BoxStorageType.IngredientStorage)
        {
            data.AddObjStackCountList(this);
            SetSaveStackObj();
        }
        if (bsType == BoxStorageType.ChuruStorage)
            GameManager.Instance.AddStackable(this);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Item>(out var item))
            TryCollect(item);
    }

    private bool TryCollect(Item item)
    {
        if (boxTransform == null || boxTransform.Length == 0)
            return false;
        if (bsType == BoxStorageType.IngredientStorage)
            return ingredientMaker != null && ingredientMaker.TryCollect(item, boxTransform[0]);

        int column = Mathf.Clamp(Count / 10, 0, boxTransform.Length - 1);
        return ItemTransferUtility.TryCollect(item, Storage, boxTransform[column], Count % 10);
    }

    public void SetSaveStackObj()
    {
        if (bsType == BoxStorageType.IngredientStorage) return;
        int savedCount = bsType == BoxStorageType.ChuruStorage
            ? data.baseCost.ChuruStorageCount : data.baseCost.PackagingStorageCount;
        GameObject prefab = bsType == BoxStorageType.ChuruStorage ? churu : box;
        // Preserve legacy saved counts even when they exceed the normal intake limit.
        Storage.Capacity = Mathf.Max(Capacity, savedCount);
        try
        {
            while (Count < savedCount)
            {
                var restored = PoolingManager.Instance.GetObj(prefab);
                if (restored != null && restored.TryGetComponent<Item>(out var item) && TryCollect(item))
                    continue;
                PoolingManager.Instance.ReturnObjecte(restored);
                Debug.LogError("Cannot restore storage Item. Check its prefab and stack positions.", this);
                break;
            }
        }
        finally
        {
            Storage.Capacity = Capacity;
        }
    }

    public int GetStackCount() => Count;
    public Transform GetTransform() => transform.GetChild(0);
    public int GetTypeNum() => 1;

    public void ObjectDataSave()
    {
        if (bsType == BoxStorageType.ChuruStorage)
            data.baseCost.ChuruStorageCount = Count;
        else if (bsType == BoxStorageType.BoxStorage)
            data.baseCost.PackagingStorageCount = Count;
    }
}

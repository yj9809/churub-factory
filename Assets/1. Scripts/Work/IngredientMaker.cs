using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class IngredientMaker : MonoBehaviour, IStackable, IItemTransferEndpoint
{
    public bool TryTransfer(CarrierInventory inventory, Transform carryParent)
    {
        return ItemTransferUtility.TryMove(storage, inventory, carryParent);
    }

    [TabGroup("IngredientMaker"), SerializeField] private GameObject objPrefab;
    [TabGroup("IngredientMaker"), SerializeField] private Transform objSpawnPoint;
    [TabGroup("IngredientMaker"), SerializeField] private float objSpawnTime = 2f;
    [TabGroup("IngredientMaker"), SerializeField] private int maxObj = 10;

    // Intake stays unbounded; maxObj controls generation, as in the original flow.
    private readonly ItemBuffer storage = new ItemBuffer(int.MaxValue, ItemType.Ingredient);

    public bool TryCollect(Item item, Transform stackParent)
    {
        return ItemTransferUtility.TryCollect(item, storage, stackParent);
    }

    private float spawnTimer = 0f;

    public float ObjSpawnTime
    {
        get { return objSpawnTime; }
        set { objSpawnTime = value; }
    }

    private GameManager gm;

    private void Start()
    {
        objSpawnTime = Churub.Core.BalanceTable.IngredientInterval;
        maxObj = Churub.Core.BalanceTable.IngredientCapacity;
        gm = GameManager.Instance;
        gm.AddStackable(this); // 리스트에 추가
    }

    private void Update()
    {
        SpawnGameObject();

        // 타겟 업데이트 로직
        if (storage.Count == 0)
        {
            gm.UpdateTargets();
        }
    }

    private void SpawnGameObject()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= objSpawnTime)
        {
            if (storage.Count < maxObj)
            {
                GameObject newChurub = PoolingManager.Instance.GetObj(objPrefab);
                newChurub.transform.position = objSpawnPoint.position;
                newChurub.name = objPrefab.name;

                if (!newChurub.GetComponent<Rigidbody>())
                    newChurub.AddComponent<Rigidbody>();
            }
            spawnTimer = 0f;
        }
    }

    public int GetStackCount() => storage.Count;
    public Transform GetTransform() => transform.GetChild(0).transform;
    public int GetTypeNum() => 0;
}

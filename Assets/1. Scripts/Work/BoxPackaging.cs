using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;

public enum PackagingType
{
    On,
    Off
}
public class BoxPackaging : MonoBehaviour, IObjectDataSave, IItemTransferEndpoint
{
    public bool TryTransfer(CarrierInventory inventory, Transform carryParent)
    {
        return ItemTransferUtility.TryMove(inventory, waiting, storageParent);
    }

    [SerializeField] private Transform storageParent;
    [SerializeField] private Transform boxParent;
    [SerializeField] private Transform packagingBoxParent;
    [SerializeField] private GameObject box;
    [SerializeField] private BoxStorage boxStorage;

    [SerializeField] private GameObject churu;

    private DataManager data;
    private GameObject newBox;
    private TMP_Text boxCountTxt;

    private readonly ItemBuffer waiting = new ItemBuffer(int.MaxValue, ItemType.Churu);
    // Keep a completed box owned until its output tween finishes, so collection
    // cannot cancel the callback that resets packaging progress.
    private readonly ItemBuffer outputInTransit = new ItemBuffer(1, ItemType.Box);
    public int WaitingCount => waiting.Count;

    private PackagingType packaging = PackagingType.On;

    private int count = 0;
    private const int maxCount = Churub.Core.BalanceTable.ItemsPerBox;

    private void Start()
    {
        data = DataManager.Instance;
        SetSaveStackObj();
        data.AddObjStackCountList(this);
    }

    private void SetSaveStackObj()
    {
        for (int i = 0; i < data.baseCost.PackagingWaitCount; i++)
        {
            GameObject newChuru = PoolingManager.Instance.GetObj(churu);
            if (newChuru != null && newChuru.TryGetComponent<Item>(out var restored)
                && ItemTransferUtility.TryCollect(restored, waiting, storageParent,
                    stackIndex: waiting.Count + 1, animate: false))
                continue;
            PoolingManager.Instance.ReturnObjecte(newChuru);
            Debug.LogError("Cannot restore packaging Item.", this);
            break;
        }

        count = data.baseCost.PackagingCount;
        if(count != 0 && count < maxCount)
        {
            newBox = Instantiate(box, boxParent);
            newBox.name = box.name;
            boxCountTxt = newBox.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>();
            boxCountTxt.text = $"{count}/{maxCount}";
            newBox.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    public void Packaging(Player p, Employee employee)
    {
        if (newBox == null && waiting.Count != 0 && !boxStorage.IsFull)
        {
            newBox = Instantiate(box, boxParent);
            newBox.name = box.name;
            boxCountTxt = newBox.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>();
            newBox.transform.GetChild(0).gameObject.SetActive(true);
        }

        if (waiting.Count != 0 && newBox != null)
        {
            ChuruMove(p, employee);
        }
        else
        {
            if (p != null)
                p.StopBoxPackagingAnimationPlayer();
            if (employee != null)
                employee.StopBoxPackagingAnimationEmployee();
        }
    }
    private void ChuruMove(Player p, Employee employee)
    {
        if(packaging != PackagingType.Off)
        {
            if (!waiting.TryPop(out var item)) return;
            GameObject churu = item.gameObject;
            churu.transform.DOKill();

            if(p != null)
                p.DoBoxPackagingAnimationPlayer();

            if (employee != null)
                employee.DoBoxPackagingAnimationEmployee();

            churu.transform.SetParent(newBox.transform);
            churu.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (p != null) Vibration.VibratePop();
                    
                    PoolingManager.Instance.ReturnObjecte(churu);
                    count++;
                    boxCountTxt.text = $"{count}/{maxCount}";
                    BoxMove();
                }
                );
            packaging = PackagingType.Off;
        }
    }
    private void BoxMove()
    {
        if(count == maxCount)
        {
            if (!newBox.TryGetComponent<Item>(out var output) || !outputInTransit.TryAdd(output))
            {
                Debug.LogError("Cannot release packaging Box Item.", this);
                return;
            }
            newBox.AddComponent<Rigidbody>();
            newBox.transform.DOMove(packagingBoxParent.position, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    outputInTransit.TryPop(out _);
                    newBox.transform.GetChild(0).gameObject.SetActive(false);
                    newBox = null;
                    count = 0;
                    packaging = PackagingType.On;
                });
            
        }
        else
        {
            packaging = PackagingType.On;
        }
    }

    public void ObjectDataSave()
    {
        data.baseCost.PackagingWaitCount = waiting.Count;
        data.baseCost.PackagingCount = count;
    }
}

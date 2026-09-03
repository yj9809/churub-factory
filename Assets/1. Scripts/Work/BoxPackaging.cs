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
public class BoxPackaging : MonoBehaviour, IObjectDataSave
{
    [SerializeField] private Transform storageParent;
    [SerializeField] private Transform boxParent;
    [SerializeField] private Transform packagingBoxParent;
    [SerializeField] private GameObject box;
    [SerializeField] private BoxStorage boxStorage;

    [SerializeField] private GameObject churu;

    private DataManager data;
    private GameObject newBox;
    private TMP_Text boxCountTxt;

    public Transform churuStorageParent { get { return storageParent; } }

    private Stack<GameObject> churuStorage = new Stack<GameObject>();
    public Stack<GameObject> ChuruStorage
    {
        get { return churuStorage; }
        set
        {
            churuStorage = value;
        }
    }

    private PackagingType packaging = PackagingType.On;

    private int count = 0;
    private const int maxCount = 5;

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
            newChuru.transform.parent = churuStorageParent;
            churuStorage.Push(newChuru);
            newChuru.transform.localPosition = new Vector3(0, 0 + (Utility.ObjRendererCheck(newChuru) * churuStorage.Count), 0);
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
        if (newBox == null && churuStorage.Count != 0 && boxStorage.BoxStack.Count < 40)
        {
            newBox = Instantiate(box, boxParent);
            newBox.name = box.name;
            boxCountTxt = newBox.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>();
            newBox.transform.GetChild(0).gameObject.SetActive(true);
        }

        if (churuStorage.Count != 0 && newBox != null)
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
            GameObject churu = churuStorage.Pop();

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
        if(count == 5)
        {
            newBox.AddComponent<Rigidbody>();
            newBox.transform.DOMove(packagingBoxParent.position, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
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
        data.baseCost.PackagingWaitCount = churuStorage.Count;
        data.baseCost.PackagingCount = count;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class Store : MonoBehaviour
{
    [SerializeField] private GameObject stall;
    [SerializeField] private GameObject store;
    [SerializeField] private GameObject commonGameObjects;
    [SerializeField] private TextMeshProUGUI goldTxt;

    private float goldPerSecond; // 초당 생산량
    private float timeInterval = 1f; // 생산 주기
    private float timer = 0;
    [HideInInspector]public float totalGold = 0f;

    void Update()
    {
        if (IsObjectActive())
        {
            timer += Time.deltaTime;
            if (timer >= timeInterval)
            {
                GetGold();
                timer = 0f;
            }
        }
    }

    private bool IsObjectActive()
    {
        bool isActive = false;

        var state = DataManager.Instance.baseCost;
        if (state.IsUnlocked("Store"))
        {
            store.SetActive(true);
            stall.SetActive(false);
        }
        if (stall.activeSelf)
        {
            isActive = true;
            goldPerSecond = Churub.Core.BalanceTable.StallIncome;
            store.SetActive(false);
        }
        else if (store.activeSelf)
        {
            isActive = true;
            goldPerSecond = Churub.Core.BalanceTable.StoreIncome;
            stall.SetActive(false);
            UIManager.Instance.storeUpgradeButton.gameObject.SetActive(false);
        }
        
        if (!isActive)
        {
            stall.SetActive(false);
            store.SetActive(false);
        }

        commonGameObjects.SetActive(isActive);
        return isActive;
    }

    private void GetGold()
    {
        totalGold += goldPerSecond;
        UIManager.Instance.StoreUI();
        UIManager.Instance.StoreUI(goldTxt);
    }
}

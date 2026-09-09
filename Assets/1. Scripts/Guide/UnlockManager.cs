using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using Churub.Core;

public enum UnlockType
{
    Office,
    Container1,
    Machine1,
    Container2,
    Machine2,
    Stall,
    Store
}

public class UnlockManager : MonoBehaviour
{
    [EnumToggleButtons, SerializeField] private UnlockType unlockType;
    [SerializeField] private Dictionary<UnlockType, int> unlockAmount;
    [SerializeField] private GameObject _Object;
    [SerializeField] private GameObject _Wall;
    [SerializeField] private GameObject _SideWalk;

    [TitleGroup("UI"), SerializeField] private Image _FillImage;
    [TitleGroup("UI"), ProgressBar(0, 100), SerializeField] private float currentFill;
    private int amount;
    public bool IsPurchased => isUnlocked || (baseCost != null && baseCost.IsUnlocked(unlockType.ToString()));
    private string lockReason;
    private Coroutine unlockRoutine;
    private TMPro.TMP_Text[] priceLabels;

    private const float unlockTime = 3.0f;
    private bool isTrigger = false;
    private bool isUnlocked = false;

    private Player player;
    private BaseCost baseCost;
    private AudioManager audioManager;

    private void Awake()
    {
        player = GameManager.Instance.P;
        baseCost = DataManager.Instance.baseCost;
        audioManager = AudioManager.Instance;
        if (unlockType == UnlockType.Store)
            UIManager.Instance.storeUpgradeButton.onClick.AddListener(UnlockStore);

        _Object.SetActive(false);
        CheckUnlockStatus();

        amount = BalanceTable.FacilityCost(unlockType.ToString());
        priceLabels = GetComponentsInChildren<TMPro.TMP_Text>(true);
    }

    private void Update()
    {
        lockReason = BalanceTable.FacilityLock(baseCost, unlockType.ToString());
        foreach (var label in priceLabels)
            label.text = lockReason ?? amount.ToString();
    }

    private void Start()
    {
        if (unlockType == UnlockType.Store && _Object.activeSelf)
        {
            UIManager.Instance.storeUpgradeButton.gameObject.SetActive(false);
        }
    }

    private void CheckUnlockStatus()
    {
        if (baseCost.IsUnlocked(unlockType.ToString()))
        {
            isUnlocked = true;
            _Object.SetActive(true);
            DisableObjects();
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !IsPurchased && unlockRoutine == null && player.Gold >= amount && BalanceTable.FacilityLock(baseCost, unlockType.ToString()) == null)
        {
            isTrigger = true;
            unlockRoutine = StartCoroutine(UnlockProcess(currentFill));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isUnlocked)
        {
            isTrigger = false;
        }
    }

    private IEnumerator UnlockProcess(float updateProcess)
    {
        currentFill = updateProcess;
        float fillRate = 100f / unlockTime;

        while (currentFill < 100 && isTrigger && player.Gold >= amount && BalanceTable.FacilityLock(baseCost, unlockType.ToString()) == null)
        {
            currentFill += fillRate * Time.deltaTime;
            UpdateUnlockUI(currentFill / 100);
            yield return null;
        }

        if (currentFill >= 100)
        {
            ActivateObject();
        }
        unlockRoutine = null;
    }

    private void ActivateObject()
    {
        if (IsPurchased || BalanceTable.FacilityLock(baseCost, unlockType.ToString()) != null || !UIManager.Instance.SpendGold(amount)) return;
        foreach (Transform item in transform)
        {
            item.gameObject.SetActive(false);
        }
        audioManager.PlayEffect(EffectType.UnLock);
        player.PT = PlayerType.None;
        _Object.SetActive(true);
        AnimateObject();

        isUnlocked = true;
        UpdateProgress();
        DataManager.Instance.GameDataUpdate();
    }

    private void AnimateObject()
    {
        _Object.transform.DOScale(Vector3.zero, 0f);
        _Object.transform.DOScale(Vector3.one, 1f).SetEase(Ease.InBounce)
            .OnComplete(() =>
            {
                GameManager.Instance.NowNavMeshBake();
                Vibration.VibratePop();
                player.PT = PlayerType.Joystick;
            });
    }

    private void UpdateProgress()
    {
        baseCost.SetUnlocked(unlockType.ToString(), true);
        switch (unlockType)
        {
            case UnlockType.Office:
                baseCost.SetUnlocked(GameDataSchema.Progress.Office, true);
                DisableObjects();
                break;
            case UnlockType.Container1:
            case UnlockType.Container2:

                DisableWall();
                break;
            case UnlockType.Machine1:
            case UnlockType.Machine2:

                break;
            case UnlockType.Stall:
                baseCost.SetUnlocked(GameDataSchema.Progress.Stall, true);
                break;
            case UnlockType.Store:
                baseCost.SetUnlocked(GameDataSchema.Progress.Stall, false);
                baseCost.SetUnlocked(GameDataSchema.Progress.Store, true);
                DisableObjects();
                break;
        }
    }

    private void UpdateUnlockUI(float progress)
    {
        if (_FillImage != null)
        {
            _FillImage.fillAmount = progress;
        }
    }

    private void UnlockStore()
    {
        if (unlockType == UnlockType.Store) ActivateObject();
    }

    private void OnDestroy()
    {
        var ui = FindObjectOfType<UIManager>();
        if (unlockType == UnlockType.Store && ui != null && ui.storeUpgradeButton != null)
            ui.storeUpgradeButton.onClick.RemoveListener(UnlockStore);
    }

    private void DisableWall()
    {
        if (_Wall != null)
        {
            _Wall.SetActive(false);
        }
    }
    private void DisableSideWalk()
    {
        if (_SideWalk != null)
        {
            _SideWalk.SetActive(false);
        }
    }
    private void DisableObjects()
    {
        DisableWall();
        DisableSideWalk();
    }
}

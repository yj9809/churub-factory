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
        UIManager.Instance.storeUpgradeButton.onClick.AddListener(UnlockStore);

        _Object.SetActive(false);
        CheckUnlockStatus();

        unlockAmount = new Dictionary<UnlockType, int>
        {
            { UnlockType.Office, 2500 },
            { UnlockType.Container1, 1000 },
            { UnlockType.Machine1, 5000 },
            { UnlockType.Container2, 5000 },
            { UnlockType.Machine2, 10000 },
            { UnlockType.Stall, 10000 },
            { UnlockType.Store, 20000 }
        };

        amount = unlockAmount[unlockType];
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
            _Object.SetActive(true);
            DisableObjects();
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isUnlocked && player.Gold >= amount)
        {
            isTrigger = true;
            StartCoroutine(UnlockProcess(currentFill));
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

        while (currentFill < 100 && isTrigger)
        {
            currentFill += fillRate * Time.deltaTime;
            UpdateUnlockUI(currentFill / 100);
            yield return null;
        }

        if (currentFill >= 100)
        {
            ActivateObject();
        }
    }

    private void ActivateObject()
    {
        foreach (Transform item in transform)
        {
            item.gameObject.SetActive(false);
        }
        audioManager.PlayEffect(EffectType.UnLock);
        player.PT = PlayerType.None;
        _Object.SetActive(true);
        AnimateObject();

        isUnlocked = true;
        UIManager.Instance.SpendGold(amount);
        UpdateProgress();
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
        switch (unlockType)
        {
            case UnlockType.Office:
                baseCost.SetUnlocked(GameDataSchema.Progress.Office, true);
                DisableObjects();
                break;
            case UnlockType.Container1:
            case UnlockType.Container2:
                UpdateContainerProgress();
                DisableWall();
                break;
            case UnlockType.Machine1:
            case UnlockType.Machine2:
                UpdateMachineProgress();
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

    private void UpdateContainerProgress()
    {
        if (!baseCost.IsUnlocked(GameDataSchema.Progress.Container1))
        {
            baseCost.SetUnlocked(GameDataSchema.Progress.Container1, true);
        }
        else if (!baseCost.IsUnlocked(GameDataSchema.Progress.Container2))
        {
            baseCost.SetUnlocked(GameDataSchema.Progress.Container2, true);
        }
    }

    private void UpdateMachineProgress()
    {
        if (!baseCost.IsUnlocked(GameDataSchema.Progress.Machine1))
        {
            baseCost.SetUnlocked(GameDataSchema.Progress.Machine1, true);
        }
        else if (!baseCost.IsUnlocked(GameDataSchema.Progress.Machine2))
        {
            baseCost.SetUnlocked(GameDataSchema.Progress.Machine2, true);
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
        if (player.Gold >= amount)
        {
            if (unlockType == UnlockType.Stall)
            {
                _Object.SetActive(false);
            }
            else if (unlockType == UnlockType.Store)
            {
                ActivateObject();
            }
        }
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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using Churub.Core;

public class RewardedAdsButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private Button _speedBuffButton;
    [SerializeField] private Button _maxObjStackCountBuffButton;
    [SerializeField] private Button _goldBuffButton;
    [SerializeField] private Image[] buffOffImage;
    [SerializeField] private string _androidAdUnitId = "Reword_Android";
    [SerializeField] private string _iOSAdUnitId = "Reword_iOS";
    private Button[] buttons;
    private readonly bool[] active = new bool[3];
    private GameManager gm;
    private bool loaded;
    private int pending = -1;

    private void Awake()
    {
        gm = GameManager.Instance;
        buttons = new[] {_speedBuffButton, _maxObjStackCountBuffButton, _goldBuffButton};
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].interactable = false;
            buttons[i].onClick.AddListener(() => Show(index));
        }
    }
    private void Start() => LoadAd();
    private void Update()
    {
        bool unlocked = DataManager.Instance.baseCost.EmployeeAddCount > 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = unlocked && loaded && pending < 0 && !active[i];
            if (i < buffOffImage.Length) buffOffImage[i].gameObject.SetActive(!active[i]);
        }
    }
    public void LoadAd() => Advertisement.Load(_androidAdUnitId, this);
    public void OnUnityAdsAdLoaded(string id) { if (id == _androidAdUnitId) loaded = true; }
    public void ShowAdAndApplyBuff(string type)
    {
        int index = type == "Speed" ? 0 : type == "MaxObjStackCount" ? 1 : type == "Gold" ? 2 : -1;
        if (index >= 0) Show(index);
    }
    private void Show(int index)
    {
        if (!loaded || pending >= 0 || active[index] || DataManager.Instance.baseCost.EmployeeAddCount < 1) return;
        pending = index;
        loaded = false;
        Advertisement.Show(_androidAdUnitId, this);
    }
    public void OnUnityAdsShowComplete(string id, UnityAdsShowCompletionState completion)
    {
        if (id != _androidAdUnitId) return;
        int index = pending;
        pending = -1;
        if (index >= 0 && completion == UnityAdsShowCompletionState.COMPLETED)
            StartCoroutine(ApplyBuff(index));
        LoadAd();
    }
    private IEnumerator ApplyBuff(int index)
    {
        active[index] = true;
        SetBuff(index, index == 0 ? BalanceTable.SpeedBuffMultiplier - 1f : index == 1 ? BalanceTable.CapacityBuff : BalanceTable.GoldBuffBonus);
        yield return new WaitForSeconds(BalanceTable.BuffDuration);
        SetBuff(index, 0);
        active[index] = false;
    }
    private void SetBuff(int index, float value)
    {
        if (gm == null || gm.P == null) return;
        if (index == 0) gm.P.buffSpeed = value;
        else if (index == 1) gm.P.buffMaxObjStackCount = value;
        else gm.P.buffGold = value;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        for (int i = 0; i < active.Length; i++)
        {
            if (active[i]) SetBuff(i, 0);
            active[i] = false;
        }
    }
    public void OnUnityAdsFailedToLoad(string id, UnityAdsLoadError error, string message)
    {
        loaded = false;
        if (isActiveAndEnabled) StartCoroutine(RetryLoad());
    }
    private IEnumerator RetryLoad() { yield return new WaitForSeconds(5); LoadAd(); }
    public void OnUnityAdsShowFailure(string id, UnityAdsShowError error, string message)
    {
        pending = -1;
        if (isActiveAndEnabled) LoadAd();
    }
    public void OnUnityAdsShowStart(string id) { }
    public void OnUnityAdsShowClick(string id) { }
}

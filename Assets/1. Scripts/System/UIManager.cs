using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;
using Churub.Core;

public class UIManager : Singleton<UIManager>
{
    [TabGroup("Camera Zoom"), SerializeField] private Button cameraZoomButton;
    [TabGroup("Camera Zoom"), SerializeField] private Sprite[] cameraZoomButtonImg;

    [SerializeField] private TextMeshProUGUI goldTxt;
    [SerializeField] private RectTransform gameSceneImageRect;

    [TabGroup("Upgrade"), SerializeField] private GameObject upgradePanel;
    [TabGroup("Upgrade"), SerializeField] private Sprite[] upgradeStepSprite;
    [TabGroup("Upgrade"), SerializeField] private Image[] upgradeStepImage;
    [TabGroup("Upgrade"), SerializeField] private TMP_Text[] upgradeCostText;

    [TabGroup("Store"), SerializeField] private Store store;
    [TabGroup("Store"), SerializeField] private GameObject storePanel;
    [TabGroup("Store"), SerializeField] private TextMeshProUGUI storeGoldTxt;
    [TabGroup("Store"), SerializeField] private Button storeGetGoldButton;
    [TabGroup("Store")] public Button storeUpgradeButton;

    [Title("Debug"), SerializeField] private TextMeshProUGUI logText;

    //여기부터 윤제영에 테스트 참조임
    private Guide guide;
    //여기까지 윤제영에 테스트 참조였음

    private Player p;
    private BaseCost baseCost;
    private GameManager gm;
    private AudioManager audioManager;
    private UpgradeService upgradeService;
    private EmployeeFactory employeeFactory;

    // Start is called before the first frame update
    void Start()
    {
        p = GameManager.Instance.P;
        baseCost = DataManager.Instance.baseCost;
        gm = GameManager.Instance;
        audioManager = AudioManager.Instance;
        upgradeService = new UpgradeService(baseCost);
        employeeFactory = new EmployeeFactory(gm.employee, p.employee);
        upgradePanel.SetActive(false);
        storePanel.SetActive(false);

        cameraZoomButton.onClick.AddListener(ZoomScreen);
        storeGetGoldButton.onClick.AddListener(GetGold);

        logText.text = "";

        UpdateGoldUI();
        StoreUI();
    }

    private void LogMessage(string message)
    {
        logText.text = "";
        if (logText != null)
        {
            logText.text += message + "\n";
            StartCoroutine(MessageClear(0.7f));
        }
    }

    private IEnumerator MessageClear(float delay)
    {
        yield return new WaitForSeconds(delay);
        logText.text = "";
    }

    public void UpdateGoldUI()
    {
        goldTxt.text = ChangeNumbet(p.Gold.ToString());
    }

    private void ZoomScreen()
    {
        gm.MainCamera.ZoomScreen();
        cameraZoomButton.image.sprite =
            cameraZoomButton.image.sprite == cameraZoomButtonImg[0] ? cameraZoomButtonImg[1] : cameraZoomButtonImg[0];
    }

    public void DeleteData()
    {
        DataManager.Instance.DeleteData();
    }

    #region GoldUI
    // 재화 단위 변경
    private string ChangeNumbet(string number)
    {
        char[] unitAlphabet = new char[3] { 'K', 'M', 'B' };
        int unit = 0;

        // 입력된 number가 6자리보다 클 경우 단위 변환
        while (number.Length > 6)
        {
            unit++;
            number = number.Substring(0, number.Length - 3);
        }

        if (number.Length > 3)
        {
            // 숫자로 변환
            double newInt = double.Parse(number);
            // 소수점 이하를 두 자리까지 표시
            return (newInt / 1000).ToString("0.##") + unitAlphabet[unit];
        }
        else
        {
            int newInt = int.Parse(number);
            return newInt.ToString();
        }
    }


    public void SellItem()
    {
        //테스트용
        AddGold(900);
    }

    public void AddGold(int amount)
    {
        p.Gold += amount;
        UpdateGoldUI();
    }

    //골드 사용 함수
    public bool SpendGold(int amount)
    {
        if (p.Gold >= amount)
        {
            p.Gold -= amount;
            UpdateGoldUI();
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region UpgradeUI
    private void UpgradeTextUpdate(int num)
    {
        if (num < 0 || num > (int)UpgradeType.EmployeeAdd || num >= upgradeCostText.Length)
        {
            return;
        }

        UpgradeType type = (UpgradeType)num;
        UpgradeProgress progress = upgradeService.GetProgress(type);

        upgradeCostText[num].text = progress.IsMaxLevel
            ? "Max"
            : ChangeNumbet(progress.Cost.ToString());

        if (upgradeStepSprite.Length <= 0)
        {
            return;
        }

        int spriteIndex = Mathf.Clamp(progress.Level, 0, upgradeStepSprite.Length - 1);
        if (num < upgradeStepImage.Length && upgradeStepImage[num] != null)
        {
            upgradeStepImage[num].sprite = upgradeStepSprite[spriteIndex];
        }
    }

    private void StartUpgradeTextUpdate()
    {
        int upgradeCount = Mathf.Min(upgradeCostText.Length, (int)UpgradeType.EmployeeAdd + 1);
        for (int i = 0; i < upgradeCount; i++)
        {
            UpgradeTextUpdate(i);
        }
    }
    // 오피스 강화 패널 여는 함수
    public void ShowUpgradeUI()
    {
        upgradePanel.SetActive(true);
        StartUpgradeTextUpdate();

        // 옵션 버튼 비활성화
        Option option = FindObjectOfType<Option>();
        if (option != null)
        {
            option.OptionButtonActive(false);
        }
    }
    // 오피스 강화 패널 닫는 함수
    public void CloseUpgradeUI()
    {
        upgradePanel.SetActive(false);

        // 옵션 버튼 활성화
        Option option = FindObjectOfType<Option>();
        if (option != null)
        {
            option.OptionButtonActive(true);
        }
    }
    public void Upgrade(int num)
    {
        UpgradeType type = (UpgradeType)num;
        UpgradePurchaseStatus purchaseStatus = upgradeService.EvaluatePurchase(type);
        if (purchaseStatus != UpgradePurchaseStatus.Success)
        {
            HandleUpgradeFailure(purchaseStatus);
            return;
        }

        if (type == UpgradeType.EmployeeAdd)
        {
            UpgradeProgress progress = upgradeService.GetProgress(type);
            EmployeeCreationStatus creationStatus =
                employeeFactory.Validate(progress.NextPurchaseCreatesPackagingEmployee);
            if (creationStatus != EmployeeCreationStatus.Success)
            {
                HandleEmployeeCreationFailure(creationStatus);
                return;
            }
        }

        UpgradePurchaseResult result = upgradeService.TryPurchase(type);
        if (!result.Succeeded)
        {
            HandleUpgradeFailure(result.Status);
            return;
        }

        if (result.RequiresEmployeeSpawn)
        {
            EmployeeCreationResult creationResult = employeeFactory.TryCreate(result.CreatesPackagingEmployee);
            if (!creationResult.Succeeded)
            {
                HandleEmployeeCreationFailure(creationResult.Status);
                return;
            }
        }

        audioManager.PlayEffect(EffectType.Upgrade);
        UpdateGoldUI();
        UpgradeTextUpdate(num);
    }

    private void HandleUpgradeFailure(UpgradePurchaseStatus status)
    {
        switch (status)
        {
            case UpgradePurchaseStatus.MaxLevel:
                LogMessage("최대 업그레이드 입니다.");
                break;
            case UpgradePurchaseStatus.InsufficientGold:
                LogMessage("골드가 부족합니다.");
                break;
            default:
                LogMessage("업그레이드 정보를 확인해주세요.");
                break;
        }
    }

    private void HandleEmployeeCreationFailure(EmployeeCreationStatus status)
    {
        switch (status)
        {
            case EmployeeCreationStatus.NoAvailablePrefab:
                LogMessage("생성 가능한 종업원이 없습니다.");
                break;
            case EmployeeCreationStatus.MissingEmployeeComponent:
                LogMessage("종업원 프리팹 설정을 확인해주세요.");
                break;
            case EmployeeCreationStatus.MissingPackagingStation:
            case EmployeeCreationStatus.MissingPackagingPoint:
                LogMessage("포장 직원 배치 위치를 확인해주세요.");
                break;
            default:
                LogMessage("종업원 생성 정보를 확인해주세요.");
                break;
        }
    }
    #endregion

    #region StoreUI
    public void ShowStoreUI()
    {
        storePanel.SetActive(true);
    }
    public void CloseStoreUI()
    {
        storePanel.SetActive(false);
    }
    public void StoreUI()
    {
        storeGoldTxt.text = ChangeNumbet(store.totalGold.ToString());
    }
    public void StoreUI(TextMeshProUGUI text)
    {
        text.text = ChangeNumbet(store.totalGold.ToString());
    }

    private void GetGold()
    {
        p.Gold += store.totalGold;
        UpdateGoldUI();

        store.totalGold = 0;
        StoreUI();
    }
    #endregion

    #region GameTest UI

    //private void OnGUI()
    //{
    //    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

    //    // 폰트 사이즈 조정
    //    buttonStyle.fontSize = 25;

    //    if (GUI.Button(new Rect(200, 250, 200, 100), "가이드 넘기기", buttonStyle))
    //        guide.ToNextStep();
    //    if (GUI.Button(new Rect(430, 250, 200, 100), "돈", buttonStyle))
    //        AddGold(9000);
    //}
    //public void SetGuideStep(Guide guide)
    //{
    //    this.guide = guide;
    //}
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Sirenix.OdinInspector;
using Churub.Core;

public class Guide : MonoBehaviour
{
    [Title("Guide")]
    [SerializeField] private GameObject guidePrefab;
    private GameObject curGuidePrefab;
    private TextMeshProUGUI guideTitle;
    private TextMeshProUGUI guideText;
    private TextMeshProUGUI guideTextNum;
    [SerializeField] private Sprite guideClearImage;

    private bool isGuideActive = true;
    [SerializeField] private Button guideButton;
    [SerializeField] private GameObject guideUI;
    [SerializeField] private RectTransform guideLine;
    private bool isGuideLineMoving = false;
    private float guideLineOriginalY;

    [Title("Target")]
    [SerializeField] private GameObject[] targets;

    [Title("Object")]
    [SerializeField] private GameObject _OfficeObject;
    [SerializeField] private GameObject[] _ContainerObjects;
    [SerializeField] private GameObject[] _MachineObjects;
    [SerializeField] private GameObject _StallObject;
    [SerializeField] private GameObject _StoreObject;

    [Title("Employee")]
    [SerializeField] private Button employeeAddButton;

    [Title("Scripts")]
    public bool _Scripts = true;
    [HideIfGroup("_Scripts"), SerializeField] private BoxPackaging boxPackaging;
    [HideIfGroup("_Scripts"), SerializeField] private BoxStorage boxStorage;
    [HideIfGroup("_Scripts"), SerializeField] private Truck truck;
    [HideIfGroup("_Scripts"), SerializeField] private InterstitialAdExample adExample;
    private BaseCost baseCost;
    private Player player;

    private bool _guideDone = false;

    private Button claimButton;

    private void Awake()
    {
        //UIManager.Instance.SetGuideStep(this);
        baseCost = DataManager.Instance.baseCost;
        player = GameManager.Instance.P;
    }

    void Start()
    {
        BalanceTable.Synchronize(baseCost);

        guideButton.onClick.AddListener(GuideButton);

        SetTargetsActive(false);
        CreateGuidePrefab();
        SetWorkPoint();

        if (baseCost.guideStep < 5)
            truck.gameObject.SetActive(false);

        if (!_guideDone)
        {
            GuideLine();
        }

        CreateClaimButton();
    }

    void Update()
    {
        if (!_guideDone)
        {
            if (baseCost.guideStep <= 6) GuideStep();
            else ShowExpansionGoals();
        }
    }

    private void CreateClaimButton()
    {
        var go = new GameObject("First Employee Reward", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(guideUI.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -12);
        rect.sizeDelta = new Vector2(360, 64);
        go.GetComponent<Image>().color = new Color(.2f, .45f, .2f);
        claimButton = go.GetComponent<Button>();
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.font = guideText.font;
        text.text = "첫 직원 맞이하기 (무료)";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        claimButton.onClick.AddListener(() => {
            if (UIManager.Instance.ClaimFirstEmployee()) ShowExpansionGoals();
        });
        go.SetActive(false);
    }

    private void ShowExpansionGoals()
    {
        bool claim = BalanceTable.CanClaimEmployee(baseCost);
        if (claimButton != null) claimButton.gameObject.SetActive(claim);
        if (claim)
        {
            UpdateGuide("첫 직원이 도착했어요", "무료 직원을 받고 원재료 운반을 맡겨보세요.", "", false);
            guideLine.gameObject.SetActive(false);
            return;
        }
        // Conditions are enforced by each facility, not by the guide's selected goal.
        for (int i = 7; i < targets.Length; i++) SetActiveTarget(i);
        guideLine.gameObject.SetActive(false);
        if (!baseCost.IsUnlocked("Office"))
            UpdateGuide("사무실 건설", "강화와 고용을 시작하세요.", BalanceTable.FacilityCost("Office") + " 골드", false);
        else if (baseCost.EmployeeAddCount < 2 || BalanceTable.Lines(baseCost) < 2)
            UpdateGuide("다음 성장을 선택하세요", "가공품 직원 고용 또는 생산라인 확장", "직원 " + BalanceTable.Cost(UpgradeType.EmployeeAdd, 1) + " / 라인 " + (BalanceTable.FacilityCost("Container1") + BalanceTable.FacilityCost("Machine1")), false);
        else if (baseCost.EmployeeAddCount < 3 || BalanceTable.Lines(baseCost) < 3 || !baseCost.IsUnlocked("Store"))
            UpdateGuide("공장 확장", "포장 직원 · 세 번째 라인 · 상점을 자유롭게 선택하세요.", "직원 강화도 확인하세요", false);
        else _GuideDone();
    }

    private void GuideButton()
    {
        isGuideActive = !isGuideActive;
        guideUI.SetActive(isGuideActive);
    }

    private void GuideLine()
    {
        #region 플레이어 중심 화살표 (주석처리)
        /*if (step < targets.Length)
        {
            Transform target = targets[step].transform;
            Vector3 direction = target.position - player.position;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            arrow.rotation = lookRotation * Quaternion.Euler(90, 0, 0);

            float distance = direction.magnitude;
            float guideScaleFactor = Mathf.Clamp(distance / 10f, 0.3f, 1f);
            canvas.localScale = new Vector3(guideScaleFactor, guideScaleFactor, guideScaleFactor);

            arrow.localPosition = Vector3.zero;
        }*/
        #endregion

        #region 타겟 위치 화살표
        if (baseCost.guideStep < targets.Length)
        {
            Transform target = targets[baseCost.guideStep].transform;
            Vector3 targetPosition = new Vector3(target.position.x, guideLine.position.y, target.position.z);

            guideLine.DOMove(targetPosition, 1f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                if (!isGuideLineMoving)
                {
                    guideLineOriginalY = guideLine.localPosition.y;
                    isGuideLineMoving = true;
                    GuideLineMoveMent();
                }
            });
        }
        #endregion
    }

    private void GuideLineMoveMent()
    {
        guideLine.DOLocalMoveY(guideLineOriginalY + 0.35f, 0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                guideLine.DOLocalMoveY(guideLineOriginalY - 0.35f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        isGuideLineMoving = false;
                        GuideLineMoveMent();
                    });
            });
    }

    private void GuideStep()
    {
        switch (baseCost.guideStep)
        {
            case 0: _Step0(); break;
            case 1: _Step1(); break;
            case 2: _Step2(); break;
            case 3: _Step3(); break;
            case 4: _Step4(); break;
            case 5: _Step5(); break;
            case 6: _Step6(); break;
        }
    }

    private void CreateGuidePrefab()
    {
        if (curGuidePrefab != null)
        {
            if (guideClearImage != null)
            {
                curGuidePrefab.GetComponent<Image>().sprite = guideClearImage;
            }
            Destroy(curGuidePrefab, 2f);
        }

        curGuidePrefab = Instantiate(guidePrefab, guideUI.transform);

        guideTitle = curGuidePrefab.transform.Find("Guide_Text_Title (TMP)").GetComponent<TextMeshProUGUI>();
        guideText = curGuidePrefab.transform.Find("Guide_Text (TMP)").GetComponent<TextMeshProUGUI>();
        guideTextNum = curGuidePrefab.transform.Find("Guide_Text_Num (TMP)").GetComponent<TextMeshProUGUI>();
    }

    public void ToNextStep()
    {
        baseCost.guideStep++;
        GuideLine();

    }

    private void UpdateGuide(string title, string text, string numberText, bool isCompleted)
    {
        if (guideTitle != null && guideText != null && guideTextNum != null)
        {
            guideTitle.text = title;
            guideText.text = text;
            guideTextNum.color = isCompleted ? Color.yellow : Color.black;
            guideTextNum.text = numberText;
        }

        if (isCompleted)
        {
            CreateGuidePrefab();
            GiveReward(baseCost.guideStep);
            ToNextStep();
        }
    }

    private void SetWorkPoint()
    {
        for (int i = 0; i <= baseCost.guideStep && i < targets.Length; i++)
        {
            if (targets[i].GetComponent<WorkPoint>())
                targets[i].SetActive(true);
        }
    }

    private void SetTargetsActive(bool isActive)
    {
        foreach (var target in targets)
        {
            target.SetActive(isActive);
        }
    }

    private void SetActiveTarget(int index)
    {
        if (index >= 0 && index < targets.Length)
        {
            var unlock = targets[index].GetComponent<UnlockManager>();
            if (unlock == null || !unlock.IsPurchased) targets[index].SetActive(true);
        }
    }

    private void GiveReward(int step)
    {
        int reward = BalanceTable.TutorialReward(step);

        if (reward > 0)
        {
            player.Gold += reward;
            UIManager.Instance.UpdateGoldUI();
        }
    }

    #region GuideSteps
    private void _Step0()
    {
        SetActiveTarget(0);
        UpdateGuide("공장냥의 첫걸음 1", "원재료 창고로 이동", ""
            , player.IngredientStack.Count > 0);
    }
    private void _Step1()
    {
        SetActiveTarget(1);
        UpdateGuide("공장냥의 첫걸음 2", "원재료를 컨베이어 벨트로 옮기기", ""
            , player.IngredientStack.Count <= 0);
    }
    private void _Step2()
    {
        SetActiveTarget(2);
        UpdateGuide("공장냥의 첫걸음 3", "완성된 츄릅을 박스 포장대로 옮기기", ""
            , player.ChuruStack.Count > 0);
    }
    private void _Step3()
    {
        SetActiveTarget(3);
        UpdateGuide("공장냥의 첫걸음 4", "츄룹 창고 이동 작업", boxPackaging.ChuruStorage.Count.ToString() + " / 5"
            , player.ChuruStack.Count <= 0 && boxPackaging.ChuruStorage.Count >= 5);
    }
    private void _Step4()
    {
        SetActiveTarget(4);
        UpdateGuide("공장냥의 첫걸음 5", "박스 포장대에서 박스 포장하기", ""
            , boxStorage.bsType == BoxStorageType.BoxStorage && boxStorage.BoxStack.Count >= 1);
    }
    private void _Step5()
    {
        truck.gameObject.SetActive(true);
        SetActiveTarget(5);
        UpdateGuide("공장냥의 첫걸음 6", "츄릅박스를 트럭에 싣기", ""
            , player.BoxStack.Count > 0);
    }
    private void _Step6()
    {
        SetActiveTarget(6);
        UpdateGuide("공장냥의 첫걸음 fin", "츄릅박스 5개를 트럭에 실어 판매하기", truck.BoxStack.Count.ToString() + " / 5"
            , baseCost.IsUnlocked(BalanceTable.FirstSaleKey));
    }
    private void _GuideDone()
    {
        _guideDone = true;
        gameObject.SetActive(false);
        guideButton.gameObject.SetActive(false);
        guideUI.gameObject.SetActive(false);
    }
    #endregion

}

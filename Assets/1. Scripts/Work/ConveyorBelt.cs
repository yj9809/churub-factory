using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public enum ConveyorBeltType { Ingredient, Churu }

public class ConveyorBelt : MonoBehaviour
{
    [TabGroup("Setting"), SerializeField] private float speed = 3f;
    [TabGroup("Setting"), SerializeField] private float placeObjectTime = 3f;
    [TabGroup("Setting"), SerializeField] private Vector3 direction = Vector3.forward;

    [TabGroup("Transform"), SerializeField] private Transform onBelt;

    [TabGroup("GameObj"), SerializeField] private BoxStorage boxStorage;

    [TabGroup("BreakEvent"),SerializeField] private Image eventGauge;
    [TabGroup("BreakEvent"), SerializeField] private Image displayImg;
    [TabGroup("BreakEvent"), SerializeField] private Sprite[] displayImgArray;
    [TabGroup("BreakEvent"), SerializeField] private GameObject breakEventPoint;
    [TabGroup("BreakEvent"), ProgressBar(0, 100), SerializeField] private float currentFill;

    [EnumToggleButtons, SerializeField] private ConveyorBeltType conveyorBeltType;

    private GameManager gm;

    public float PlaceObjectTime
    {
        get { return placeObjectTime; }
        set { placeObjectTime = value; }
    }

    private float breakDownProb = Churub.Core.BalanceTable.BreakdownProbability;
    public float BreakDownProb
    {
        get { return breakDownProb; }
        set { breakDownProb = value; }
    }

    private float nonBreakDownTime = Churub.Core.BalanceTable.BreakdownProtection;
    private bool breakdownUnlocked;

    private bool isOn = true;
    private bool isBreakDown = false;

    [TabGroup("Transform"), SerializeField] private Transform ingredientStorage;
    public Transform IngredientStorage
    {
        get { return ingredientStorage; }
    }

    private Stack<GameObject> cbStack = new Stack<GameObject>();
    public Stack<GameObject> CbStack
    {
        get { return cbStack; }
        set { cbStack = value; }
    }

    private void Start()
    {
        gm = GameManager.Instance;

        if(transform.parent.GetChild(0).GetComponent<WorkPoint>())
            gm.cbTrans.Add(transform.parent.GetChild(0));

        StartCoroutine(PlaceObject());
        StartCoroutine(DisplayImgChange());
        eventGauge.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(boxStorage.BoxStack.Count >= 40)
        {
            isOn = false;
        }
        else
        {
            isOn = true;
        }

        var state = DataManager.Instance.baseCost;
        bool eligible = state.EmployeeAddCount >= 2 && Churub.Core.BalanceTable.Lines(state) >= 2;
        if (!breakdownUnlocked && eligible)
        {
            breakdownUnlocked = true;
            nonBreakDownTime = Churub.Core.BalanceTable.BreakdownProtection;
        }
        if (breakdownUnlocked && nonBreakDownTime >= 0) nonBreakDownTime -= Time.deltaTime;
    }

    private IEnumerator PlaceObject()
    {
        while (true)
        {
            float randomValue = Random.value;
            yield return new WaitForSeconds(placeObjectTime);
            if(breakdownUnlocked && cbStack.Count > 0 && randomValue < breakDownProb && nonBreakDownTime <=0)
            {
                BreakDownEvent();
            }

            if (cbStack.Count > 0 && isOn && !isBreakDown)
            {
                OnConveyorObj();
            }
        }
    }

    private IEnumerator DisplayImgChange()
    {
        while (!isBreakDown && conveyorBeltType == ConveyorBeltType.Churu)
        {
            if (displayImg.sprite != displayImgArray[0])
                displayImg.sprite = displayImgArray[0];
            else
                displayImg.sprite = displayImgArray[1];
            yield return new WaitForSeconds(2f);
        }
    }

    // 가독성을 위해 따로 함수로 빼뒀습니다.
    private void OnConveyorObj()
    {
        PushStack();
        GameObject newChuru = cbStack.Pop();
        newChuru.transform.position = onBelt.position;
        newChuru.transform.SetParent(onBelt);

        if (!newChuru.GetComponent<Rigidbody>())
        {
            newChuru.AddComponent<Rigidbody>();
            newChuru.GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    // 고장 이벤트를 위한 테스트 함수들입니다.
    private void BreakDownEvent()
    {
        isBreakDown = true;
        StopAllCoroutines();
        breakEventPoint.SetActive(true);
        displayImg.sprite = displayImgArray[2];
    }

    public void BreakDownSolution()
    {
        eventGauge.gameObject.SetActive(true);
    }

    public void BreakDownSolutionClear()
    {
        isBreakDown = false;
        eventGauge.gameObject.SetActive(false);
        nonBreakDownTime = Churub.Core.BalanceTable.BreakdownProtection;
        StartCoroutine(PlaceObject());
        StartCoroutine(DisplayImgChange());
    }

    // 임시로 스택 관련 버그 발생 문제 해결 코드.
    // 컨베이어 벨트 옮길 때마다 스택 초기화 후 자식 오브젝트들을 다시 푸쉬하는 코드로 변경, 추후 메모리 문제나 다른 문제 발생 할 수 있을꺼 같음.
    // 추후 좋은 방법 생기면 다시 수정 예정.
    private void PushStack()
    {
        if(ingredientStorage.childCount != cbStack.Count)
        {
            Debug.Log("스택 수정");
            cbStack.Clear();
            foreach (Transform item in ingredientStorage)
            {
                cbStack.Push(item.gameObject);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

        // 스택이 가득 쌓였을 때를 대비해서 멈추는 코드 작성. (테스트)
        speed = isOn && !isBreakDown ? 5 : 0;
        // 스택이 가득 쌓이면 멈추고 스택이 없어졌을 경우 다시 작동 확인.

        if (rb != null)
        {
            rb.velocity = speed * direction;
        }
    }
}

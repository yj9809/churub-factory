using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Sirenix.OdinInspector;

public enum EmployeeType { Packaing, Cart}

public class Employee : MonoBehaviour
{
    [SerializeField] private GameObject cart;
    [SerializeField] private Transform cartTransform;

    [SerializeField] private Transform boxTrans;
    [SerializeField] private Transform truckTrans;
    [SerializeField] private Transform target;

    [EnumToggleButtons, SerializeField] private EmployeeType employeeType = EmployeeType.Cart;

    [SerializeField] private bool moving = false;
    private bool isWaiting = false;

    private GameManager gm;
    private Animator animator;
    private NavMeshAgent na;
    private BaseCost baseCost;

    Vector3 previousPosition;
    Vector3 currentPosition;

    public float MaxObjStackCount
    {
        get { return baseCost.EmployeeMaxStackCount; }
        set { baseCost.EmployeeMaxStackCount = value; }
    }

    private Stack<GameObject> ingredientStack = new Stack<GameObject>();
    public Stack<GameObject> IngredientStack
    {
        get { return ingredientStack; }
        set { ingredientStack = value; }
    }

    private Stack<GameObject> churuStack = new Stack<GameObject>();
    public Stack<GameObject> ChuruStack
    {
        get { return churuStack; }
        set { churuStack = value; }
    }

    private Stack<GameObject> boxStack = new Stack<GameObject>();
    public Stack<GameObject> BoxStack
    {
        get { return boxStack; }
        set { boxStack = value; }
    }

    [SerializeField] private int cbTransNum;
    public int CbTransNum
    {
        get { return cbTransNum; }
        set { cbTransNum = value; }
    }

    private bool cbTransNumCheck = false;
    public bool CbTransNumCheck
    {
        get { return cbTransNumCheck; }
        set { cbTransNumCheck = value; }
    }

    [SerializeField] private IStackable currentTarget;
    private Coroutine workCheckCoroutine;

    private void Start()
    {
        try
        {
            gm = GameManager.Instance;
            boxTrans = GameObject.Find("Box Packaging").transform.GetChild(0);
            animator = GetComponent<Animator>();
            na = GetComponent<NavMeshAgent>();
            baseCost = DataManager.Instance.baseCost;
            cbTransNum = Random.Range(0, gm.cbTrans.Count);
        }
        catch(System.Exception err)
        {
            Debug.LogError(err);
        }


        StartWorkCheck();
    }

    private void OnDisable()
    {
        StopWorkCheck();
        ReleaseCurrentTarget();
    }

    private void Update()
    {
        if (employeeType == EmployeeType.Packaing)
        {
            cart.SetActive(false);
            return;
        }
        OnCart();
        Move();
        MovementDetection();
        TargetSwitching();

        if (target != null)
            na.SetDestination(target.position);
    }

    private void Move()
    {
        if (!isWaiting)
        {
            bool isBlend = false;

            if (ingredientStack.Count > 0 || churuStack.Count > 0 || boxStack.Count > 0)
                isBlend = true;

            animator.SetBool("isMove", true);
            animator.SetFloat("Blend", isBlend ? 1 : 0);
        }
        else
        {
            animator.SetBool("isMove", false);
        }
    }
    //이동 판별 함수
    private void MovementDetection()
    {
        currentPosition = GetComponent<CharacterController>().transform.position;

        if (Vector3.Distance(previousPosition, currentPosition) > 0.01f)
            isWaiting = false;
        else
            isWaiting = true;

        previousPosition = currentPosition;
    }
    // 물건을 들고 있는지 판별하는 함수
    private void OnCart()
    {
        if (ingredientStack.Count <= 0 && boxStack.Count <= 0 && churuStack.Count <= 0)
        {
            na.speed = baseCost.EmployeeSpeed;
            cart.transform.DOScale(0, 0.2f);
        }
        else
        {
            na.speed = baseCost.EmployeeCartSpeed;
            cart.transform.DOScale(Vector3.one, 0.2f);
        }
    }
    // 타겟 전환용 함수
    private void TargetSwitching()
    {
        if (target != null && Vector3.Distance(transform.position, target.position) <= 1.3f)
        {
            ChangeTarget();
        }
        else if (currentTarget != null && currentTarget.GetStackCount() == 0 && (ingredientStack.Count <= 0 && churuStack.Count <= 0 && boxStack.Count <= 0))
        {
            // 스택 카운터가 0인 경우 새로운 목표를 설정
            ReleaseCurrentTarget();
            moving = false;
            RequestWorkCheck(); // 목표 재설정
        }
    }
    private void ChangeTarget()
    {
        if (ingredientStack.Count > 0)
        {
            if (currentTarget != null)
            {
                ReleaseCurrentTarget();
            }

            if(!cbTransNumCheck)
            {
                cbTransNumCheck = true;
                target = gm.ConveyorTransform(this);
            }
        }
        else if (churuStack.Count > 0)
        {
            if (currentTarget != null)
            {
                ReleaseCurrentTarget();
            }

            if(boxTrans == null)
            {
                boxTrans = GameObject.Find("Box Packaging").transform.GetChild(0);
            }

            target = boxTrans;
        }
        else if (boxStack.Count > 0)
        {
            if (currentTarget != null)
            {
                ReleaseCurrentTarget();
            }

            target = truckTrans;
        }
        else
        {
            cbTransNumCheck = false;
            moving = false;
            RequestWorkCheck();
        }
    }
    // 스택 카운터를 판별해 적절한 타겟을 찾아주는 함수
    public IEnumerator CheckStack()
    {
        while (true)
        {
            if (!moving)
            {
                if (gm.TryReserveWork(out var bestTarget))
                {
                    target = bestTarget.GetTransform();
                    currentTarget = bestTarget;
                    moving = true;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    // 재료 받아오는 함수
    public void RequestWorkCheck()
    {
        StartWorkCheck();
    }

    private void StartWorkCheck()
    {
        if (workCheckCoroutine == null && isActiveAndEnabled)
        {
            workCheckCoroutine = StartCoroutine(CheckStack());
        }
    }

    private void StopWorkCheck()
    {
        if (workCheckCoroutine == null)
        {
            return;
        }

        StopCoroutine(workCheckCoroutine);
        workCheckCoroutine = null;
    }

    private void ReleaseCurrentTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        if (gm != null)
        {
            gm.SetTargetBeingUsed(currentTarget, false);
        }

        currentTarget = null;
    }

    public void TakeObject(IngredientMaker im)
    {
        if (im.ChuruStack.Count > 0 && MaxObjStackCount > ingredientStack.Count && boxStack.Count <= 0)
        {
            Utility.ObjectDrop(cartTransform, null, im.ChuruStack, ingredientStack, 1);
        }
    }
    // 컨베이어로 옮기는 함수
    public void GiveObject(ConveyorBelt cb)
    {
        if (ingredientStack.Count > 0)
        {
            Utility.ObjectDrop(cb.IngredientStorage, null, ingredientStack, cb.CbStack, 1);
        }
    }
    // 변환 재료 받아오는 함수
    public void GiveObject(BoxStorage bs, bool isChuru)
    {
        Stack<GameObject> newStack = isChuru ? churuStack : boxStack;

        if (bs.BoxStack.Count > 0 && MaxObjStackCount > newStack.Count && ingredientStack.Count <= 0)
        {
            Utility.ObjectDrop(cartTransform, null, bs.BoxStack, newStack, 1);
        }
    }
    // 박스 포장대에 옮기는 함수
    public void GiveObject(BoxPackaging bp)
    {
        if (churuStack.Count > 0)
        {
            Utility.ObjectDrop(bp.churuStorageParent, null, churuStack, bp.ChuruStorage, 4);
        }
    }
    public void PackaingEmployee()
    {
        employeeType = EmployeeType.Packaing;
    }
    public void DoBoxPackagingAnimationEmployee()
    {
        transform.rotation = Quaternion.Euler(0, -90f, 0);
        animator.SetLayerWeight(1, 1);
    }

    public void StopBoxPackagingAnimationEmployee()
    {
        animator.SetLayerWeight(1, 0);
    }
}

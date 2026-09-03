using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public enum PlayerType
{
    Joystick,
    None
}
public class Player : MonoBehaviour, IObjectDataSave
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private GameObject obj;
    [SerializeField] private GameObject cart;
    [SerializeField] private Transform cartTransform;

    public List<Employee> employee;

    private PlayerType pT = PlayerType.Joystick;
    public PlayerType PT
    {
        get { return pT; }
        set { pT = value; }
    }

    private CharacterController cc;
    private Animator animator;
    private Camera mainCamera;
    private BaseCost baseCost;

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

    public float MaxObjStackCount
    {
        get { return baseCost.PlayerMaxStackCount; }
        set { baseCost.PlayerMaxStackCount = value; }
    }
    public float BaseSpeed
    {
        get { return baseCost.PlayerSpeed; }
        set { baseCost.PlayerSpeed = value; }
    }
    public float CartSpeed
    {
        get { return baseCost.PlayerCartSpeed; }
        set { baseCost.PlayerCartSpeed = value; }
    }
    public float Gold
    {
        get { return baseCost.PlayerGold; }
        set { baseCost.PlayerGold = value; }
    }
    public float GoldPerBox
    {
        get { return baseCost.PlayerGoldPerBox; }
        set { baseCost.PlayerGoldPerBox = value; }
    }

    public float buffSpeed = 0;
    public float buffMaxObjStackCount = 0;
    public float buffGold = 0;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        baseCost = DataManager.Instance.baseCost;
        mainCamera = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        Vibration.Init();
        DataManager.Instance.AddObjStackCountList(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(pT == PlayerType.Joystick)
            JoystickMove();

        OnCart();
    }

    private void JoystickMove()
    {
        if (joystick == null)
        {
            joystick = FindObjectOfType<Joystick>();
            return;
        }

        float joyX = joystick.Horizontal;
        float joyZ = joystick.Vertical;

        Vector3 moveDirection = new Vector3(joyX, 0, joyZ);

        if (moveDirection != Vector3.zero)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = mainCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            Vector3 adjustedDirection = (moveDirection.z * cameraForward + moveDirection.x * cameraRight).normalized;

            float currentSpeed = animator.GetFloat("Blend") == 1? CartSpeed + buffSpeed : BaseSpeed + buffSpeed;
            animator.SetBool("isMove", true);

            cc.Move(adjustedDirection * currentSpeed * Time.deltaTime);

            Quaternion newRotation = Quaternion.LookRotation(adjustedDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * 10f);
        }
        else
        {
            animator.SetBool("isMove", false);
        }

        Vector3 currentPosition = transform.position;
        currentPosition.y = 0;
        transform.position = currentPosition;
    }

    public void OnCart()
    {
        if (ingredientStack.Count <= 0 && boxStack.Count <= 0 && churuStack.Count <= 0)
        {
            cart.transform.DOScale(0, 0.2f);
            animator.SetFloat("Blend", 0);
        }
        else
        {
            cart.transform.DOScale(1, 0.2f);
            animator.SetFloat("Blend", 1);
        }
    }

    public void DoBoxPackagingAnimationPlayer()
    {
        transform.rotation = Quaternion.Euler(0, -90f, 0);
        animator.SetLayerWeight(1, 1);
    }

    public void StopBoxPackagingAnimationPlayer()
    {
        animator.SetLayerWeight(1, 0);
    }

    #region 물건 주고 받는 코드들
    // 이건 재료 받는 코드
    public void TakeObject(IngredientMaker im)
    {
        if (im.ChuruStack.Count > 0 && 
            MaxObjStackCount + buffMaxObjStackCount > ingredientStack.Count && 
            boxStack.Count <= 0 && churuStack.Count <= 0)
        {
            Utility.ObjectDrop(cartTransform, null, im.ChuruStack, ingredientStack, 1);
            Vibration.VibratePop();
        }
    }

    // 이건 컨베이어에 올리는 코드
    public void GiveObject(ConveyorBelt cb)
    {
        if (ingredientStack.Count > 0)
        {
            Utility.ObjectDrop(cb.IngredientStorage, null, ingredientStack, cb.CbStack, 1);
            Vibration.VibratePop();
        }
    }

    // 이건 츄룹이나 박스 받을 때 쓰는 코드 bool 값에 따라 츄룹 받을건지 박스 받을건지 달라짐
    public void GiveObject(BoxStorage bs, bool isChuru)
    {
        Stack<GameObject> newStack = isChuru ? churuStack : boxStack;
        Stack<GameObject> checkStack = isChuru ? boxStack : churuStack;

        if (bs.BoxStack.Count > 0 && 
            MaxObjStackCount + buffMaxObjStackCount > newStack.Count && 
            ingredientStack.Count <= 0 && checkStack.Count <= 0)
        {
            Utility.ObjectDrop(cartTransform, null, bs.BoxStack, newStack, 1);
            Vibration.VibratePop();
        }
    }

    // 이건 츄룹을 박스 포장하는 곳으로 옮길 때 쓰는 코드
    public void GiveObject(BoxPackaging bp)
    {
        if (churuStack.Count > 0)
        {
            Utility.ObjectDrop(bp.churuStorageParent, null, churuStack, bp.ChuruStorage, 4);
            Vibration.VibratePop();
        }
    }

    // 이건 트럭에 박스 옮기는 코드
    public void GiveObject(Truck tr)
    {
        if (boxStack.Count > 0 && ingredientStack.Count <= 0)
        {
            Utility.ObjectDrop(tr.BoxLoadingTransform, null, boxStack, tr.BoxStack, 3);
            tr.BoxCountTextUpdate();
            Vibration.VibratePop();
        }
    }
    #endregion

    // 이건 사용중인 종업원들 정보를 플레이어가 가지고 있어서
    // 게임 저장할 때 플레이어에서 데이터 매니저로 처리하는 코드
    public void ObjectDataSave()
    {
        foreach (var item in employee)
        {
            if(!baseCost.employeeList.Contains(item.name))
                baseCost.employeeList.Add(item.name);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Ingredient"))
        {
            Debug.Log("실행");
            Rigidbody rd = collision.transform.GetComponent<Rigidbody>();
            if (rd != null)
                Destroy(collision.transform.GetComponent<Rigidbody>());
            Utility.ObjectDrop(cartTransform, collision.gameObject, null, ingredientStack, 0);
            Vibration.VibratePop();
        }
    }
}

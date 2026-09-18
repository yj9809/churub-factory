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
    private bool? cartVisible;
    private Tween cartScaleTween;

    private readonly CarrierInventory inventory = new CarrierInventory(0);
    public CarrierInventory Inventory
    {
        get
        {
            // count < a fractional limit permits ceil(limit) items, as before.
            inventory.Capacity = baseCost == null ? 0 : Mathf.Max(0, Mathf.CeilToInt(baseCost.PlayerMaxStackCount + buffMaxObjStackCount));
            return inventory;
        }
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

            float currentSpeed = animator.GetFloat("Blend") == 1? CartSpeed * (1f + buffSpeed) : BaseSpeed * (1f + buffSpeed);
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
        bool shouldShowCart = !Inventory.IsEmpty;
        if (cartVisible == shouldShowCart)
            return;

        cartVisible = shouldShowCart;
        cartScaleTween?.Kill();
        cartScaleTween = cart.transform.DOScale(shouldShowCart ? 1f : 0f, 0.2f)
            .OnKill(() => cartScaleTween = null);
        animator.SetFloat("Blend", shouldShowCart ? 1f : 0f);
    }

    private void OnDisable()
    {
        cartScaleTween?.Kill();
        cartVisible = null;
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

    public Transform CarryParent => cartTransform;

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
        if (collision.gameObject.TryGetComponent<Item>(out var item)
            && item.Type == ItemType.Ingredient
            && ItemTransferUtility.TryCollect(item, Inventory, cartTransform))
        {
            Vibration.VibratePop();
        }
    }
}

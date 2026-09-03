using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// 이건 내 작품
// 이 enum은 딱 봐도 박스 스토리지 타입 구분을 위한거임
public enum BoxStorageType
{
    ChuruStorage,
    BoxStorage,
    IngredientStorage
}
public class BoxStorage : MonoBehaviour, IStackable, IObjectDataSave
{
    [TabGroup("Storage Transform"), SerializeField] private Transform[] boxTransform;
    [EnumToggleButtons] public BoxStorageType bsType;

    [TabGroup("Game Object"), SerializeField] private GameObject churu;
    [TabGroup("Game Object"), SerializeField] private GameObject box;
    [TabGroup("Game Object"), SerializeField] private IngredientMaker ingredientMaker;

    private DataManager data;

    private Stack<GameObject> boxStack = new Stack<GameObject>();

    public Stack<GameObject> BoxStack
    {
        get { return boxStack; }
        set { boxStack = value; }
    }

    private int boxTransformNum = 0;

    private void Start()
    {
        data = DataManager.Instance;
        data.AddObjStackCountList(this);

        SetSaveStackObj();

        if (bsType == BoxStorageType.ChuruStorage)
            GameManager.Instance.AddStackable(this);
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((collision.gameObject.CompareTag("Box") || collision.gameObject.CompareTag("Churu")) && boxStack.Count < 40)
        {
            boxTransformNum = Mathf.Clamp(boxStack.Count / 10, 0, boxTransform.Length - 1);
            Rigidbody rd = collision.transform.GetComponent<Rigidbody>();
            if(rd != null)
                Destroy(collision.transform.GetComponent<Rigidbody>());

            Utility.ObjectDrop(boxTransform[boxTransformNum], collision.gameObject, null, boxStack, 2);
        }
        else if(collision.gameObject.CompareTag("Ingredient"))
        {
            boxTransformNum = Mathf.Clamp(boxStack.Count / 10, 0, boxTransform.Length - 1);
            Rigidbody rd = collision.transform.GetComponent<Rigidbody>();
            if (rd != null)
                Destroy(collision.transform.GetComponent<Rigidbody>());
            
            Utility.ObjectDrop(boxTransform[0], collision.gameObject, null, ingredientMaker.ChuruStack, 0);
        }
    }

    // 이건 게임 시작했을 경우 게임 저장 정보 불러오기 함수임
    // 스택 불러오기 함수 추가할 경우 딕셔너리 키 값 제대로 입력해서 추가 바람
    public void SetSaveStackObj()
    {
        if (bsType == BoxStorageType.ChuruStorage)
        {
            for (int i = 0; i < data.baseCost.ChuruStorageCount; i++)
            {
                boxTransformNum = Mathf.Clamp(boxStack.Count / 10, 0, boxTransform.Length - 1);
                GameObject churu = PoolingManager.Instance.GetObj(this.churu);
                churu.transform.parent = transform;

                Utility.ObjectDrop(boxTransform[boxTransformNum], churu, null, boxStack, 2);
            }
        }
        else if(bsType == BoxStorageType.BoxStorage)
        {
            for (int i = 0; i < data.baseCost.PackagingStorageCount; i++)
            {
                boxTransformNum = Mathf.Clamp(boxStack.Count / 10, 0, boxTransform.Length - 1);
                GameObject box = PoolingManager.Instance.GetObj(this.box);
                box.transform.parent = transform;

                Utility.ObjectDrop(boxTransform[boxTransformNum], box, null, boxStack, 2);
            }
        }
    }

    // 이건 종업원이 스택 갯수 파악해서 다른 목적기를 찾아야하기 때문에
    // 액션 비슷하게 스택 카운트를 항상 업데이트 해주는거임
    public int GetStackCount() => boxStack.Count;
    // 이것도 종업원을 위한 타겟 워크 포인트 찾아주는거
    // 뭔가 종업원이 이상하다 싶으면 이 부분 보고 해당 기계에 워크 포인트가
    // 자식 0번에 있나 잘 확인해봐
    public Transform GetTransform() => transform.GetChild(0).transform;
    // 이건 딱히 필요 없는거 같은데 뭔가 참조가 있어서 못 지우겠음
    // 나중에 잘 확인해서 필요없다 싶으면 지워버려
    public int GetTypeNum() => 1;

    // 이 부분이 바로 스택 추가 부분임
    // 저장하고 스택 다시 불러올 때 이상하다 싶으면 이 부분 확인 필요
    public void ObjectDataSave()
    {
        if (bsType == BoxStorageType.ChuruStorage)
            DataManager.Instance.baseCost.ChuruStorageCount = boxStack.Count;
        else
            DataManager.Instance.baseCost.PackagingStorageCount = boxStack.Count;
    }
}

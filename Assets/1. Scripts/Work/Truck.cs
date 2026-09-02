using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.AI;
using Sirenix.OdinInspector;

public enum CarType
{
    Go,
    Come
}

public class Truck : MonoBehaviour, IObjectDataSave
{
    [SerializeField] private Transform[] checkPoint;
    [SerializeField] private GameObject workPoint;
    [SerializeField] private TMP_Text boxCountTxt;
    [SerializeField] private Animator Door;

    private NavMeshAgent na;
    private DataManager data;
    private GameManager gm;

    private CarType ct = CarType.Go;

    private bool doorOpen = false;

    [SerializeField] private Stack<GameObject> boxStack = new Stack<GameObject>();
    public Stack<GameObject> BoxStack
    {
        get { return boxStack; }
        set { boxStack = value;}
    }

    [SerializeField] private Transform boxLoadingTransform;
    public Transform BoxLoadingTransform { get { return boxLoadingTransform; } }


    private int currentCheckPoint = 1;

    // Start is called before the first frame update
    void Start()
    {
        data = DataManager.Instance;
        na = GetComponent<NavMeshAgent>();
        gm = GameManager.Instance;
        na.SetDestination(checkPoint[currentCheckPoint].position);
        data.AddObjStackCountList(this);
        workPoint.SetActive(false);
        SetBoxStackCount();
    }

    // Update is called once per frame
    void Update()
    {
        if(boxStack.Count >= 5)
        {
            workPoint.SetActive(false);
            boxCountTxt.gameObject.SetActive(false);
            Door.SetBool("Open", true);
            ct = CarType.Come;

            // boxStack이 모두 채워졌을 때 골드획득
            Debug.Log($"gold : {gm.P.GoldPerBox}, buff : {gm.P.buffGold}");
            UIManager.Instance.AddGold(boxStack.Count * (int)(gm.P.GoldPerBox + (gm.P.GoldPerBox * gm.P.buffGold)));
            ClearBoxStack();
        }

        if (ct == CarType.Go)
            CheckPointMove();
        else
            ReturnCheckPointMove();
            

    }
    private void SetBoxStackCount()
    {
        for (int i = 0; i < data.baseCost.TruckBoxCount; i++)
        {
            boxStack.Push(null);
        }
        BoxCountTextUpdate();
    }
    private void CheckPointMove()
    {
        //Door.SetBool("Open", true);
        if (!na.pathPending && na.remainingDistance <= na.stoppingDistance)
        {
            // 다음 체크포인트가 있으면 이동
            if (currentCheckPoint < checkPoint.Length - 1 && currentCheckPoint != 2)
            {
                currentCheckPoint++;
                na.SetDestination(checkPoint[currentCheckPoint].position);
            }
            else if(currentCheckPoint == 2)
            {
                if(!doorOpen)
                {
                    na.isStopped = true;
                    Door.SetBool("Open", true);
                }
                else
                {
                    na.isStopped = false;
                    currentCheckPoint++;
                    na.SetDestination(checkPoint[currentCheckPoint].position);
                }
            }
            else
            {
                // 모든 체크포인트를 지나면 이동 종료
                Door.SetBool("Open", false);
                doorOpen = false;
                transform.rotation = Quaternion.Euler(0, 0, 0);
                na.isStopped = true;
                workPoint.SetActive(true);
                boxCountTxt.gameObject.SetActive(true);
            }
        }
    }
    private void ReturnCheckPointMove()
    {
       if(doorOpen)
        {
            if (na.isStopped)
                na.isStopped = false;

            if (!na.pathPending && na.remainingDistance <= na.stoppingDistance)
            {
                // 다음 체크포인트가 있으면 이동
                if (currentCheckPoint > 0)
                {
                    Door.SetBool("Open", false);
                    currentCheckPoint--;
                    na.SetDestination(checkPoint[currentCheckPoint].position);
                }
                else
                {
                    // 다시 갔다가 돌아온다.
                    doorOpen = false;
                    BoxCountTextUpdate();
                    ct = CarType.Go;
                }
            }
        }
    }
    public void BoxCountTextUpdate()
    {
        boxCountTxt.text = $"{boxStack.Count} / 5";
    }
    private void ClearBoxStack()
    {
        // 게임 오브젝트도 함께 없애기 위해 클리어 전에 리턴 풀링 해주는 코드.
        foreach (GameObject item in boxStack)
        {
            PoolingManager.Instance.ReturnObjecte(item);
        }
        boxStack.Clear();
    }
    public void DoorOpen()
    {
        doorOpen = true;
    }

    public void ObjectDataSave()
    {
        if(boxStack.Count <= 4)
            data.baseCost.TruckBoxCount = boxStack.Count;
    }
}

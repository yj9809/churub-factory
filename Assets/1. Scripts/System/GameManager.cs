using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Churub.Core;

public interface IStackable : IWorkTarget
{
    Transform GetTransform();
    int GetTypeNum();
}

public class GameManager : Singleton<GameManager>
{
    private Player p;
    public Player P
    {
        get
        {
            if (p == null)
            {
                p = FindObjectOfType<Player>();
            }
            return p;
        }
    }

    private MainCamera mainCamera;
    public MainCamera MainCamera
    {
        get
        {
            if (mainCamera == null)
                mainCamera = FindObjectOfType<MainCamera>();
            return mainCamera;
        }
    }

    public List<GameObject> employee;

    private NavMeshSurface nms;
    private DataManager data;

    public List<IStackable> stackCount = new List<IStackable>();
    public List<Transform> cbTrans = new List<Transform>();
    private List<Employee> employees = new List<Employee>();

    private readonly WorkScheduler<IStackable> workScheduler = new WorkScheduler<IStackable>();

    public string sceneName;

    //���� ���� �� ���� �ڵ�
    private void OnApplicationQuit()
    {
        DataManager.Instance.GameDataUpdate();
    }
    protected override void Awake()
    {
        base.Awake();
        data = DataManager.Instance;
        Application.targetFrameRate = 60;
        nms = FindObjectOfType<NavMeshSurface>();

#if UNITY_ANDROID
        Application.runInBackground = true;
#endif
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += OnSceneLoaded;
        employees.AddRange(FindObjectsOfType<Employee>());
        foreach (var stackable in stackCount)
        {
            workScheduler.Register(stackable);
        }
    }

    private void OnSceneLoaded(Scene previousScene, Scene newScene)
    {
        if(sceneName == "Game")
        {
            if (nms != null)
            {
                try
                {
                    nms.BuildNavMesh();
                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);
                }
            }
            else
            {
                try
                {
                    nms = FindObjectOfType<NavMeshSurface>();
                    nms.BuildNavMesh();
                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);
                }
            }
#if !UNITY_EDITOR
            if(data.baseCost.newGame)
            {
                Social.Active.ReportProgress(GPGSIds.achievement, 100f, null);
                data.baseCost.newGame = false;
                data.GameDataUpdate();
            }
#endif
            //직원 추가 후 적절히 초기화하여 관리
            EmployeeAdd();
        }
    }

    private void EmployeeAdd()
    {
        List<GameObject> employeesToRemove = new List<GameObject>();
        int employeeNum = 0;
        foreach (var item in employee)
        {
            if (data.baseCost.employeeList.Contains(item.name))
            {
                GameObject newEmployee;
                if (employeeNum != 2)
                {
                    newEmployee = Instantiate(item.gameObject, new Vector3(employeeNum, 0, employeeNum), Quaternion.identity);
                    
                }
                else
                {
                    Vector3 pos = FindObjectOfType<BoxPackaging>().transform.GetChild(1).transform.position;
                    newEmployee = Instantiate(item.gameObject);
                    Destroy(newEmployee.GetComponent<NavMeshAgent>());
                    newEmployee.transform.position = pos;
                    newEmployee.GetComponent<Employee>().PackaingEmployee();
                }
                DontDestroyOnLoad(newEmployee);
                newEmployee.name = item.name;
                P.employee.Add(newEmployee.GetComponent<Employee>());
                employeesToRemove.Add(item);
                employeeNum++;
            }
        }
        // �ҷ����� ������ ����Ʈ ����
        foreach (var item in employeesToRemove)
        {
            employee.Remove(item);
        }
    }

    // ���� �����ϱ� ���� �������̽��� Ȱ���Ͽ� ���� ���尪 ���
    public void AddStackable(IStackable stackable)
    {
        if (!stackCount.Contains(stackable))
        {
            stackCount.Add(stackable);
        }

        workScheduler.Register(stackable);
    }
    
    //�������� ã�� Ÿ�� �������̽��� Ȱ���Ͽ� Ÿ�� ���
    public void AddTarget(IStackable stackable)
    {
        AddStackable(stackable);
    }

    // �������� ���� �ִ� Ÿ�� Ȱ���� ��ġ�� �ʰ� �ϱ� ���� Bool���� ���� ����
    public bool IsTargetBeingUsed(IStackable stackable)
    {
        return workScheduler.IsReserved(stackable);
    }

    // �������� Ÿ���� ���� �ִ� ��� ��ųʸ� Bool �� ������ ���� ���� �ִ��� Ȯ��
    public void SetTargetBeingUsed(IStackable stackable, bool isUsed)
    {
        if (!isUsed)
        {
            workScheduler.Release(stackable);
        }
    }

    public bool TryReserveWork(out IStackable stackable)
    {
        foreach (var target in stackCount)
        {
            workScheduler.Register(target);
        }

        return workScheduler.TryReserveBest(out stackable);
    }

    // �������� Ÿ���� �ٸ� ���������� ����� �ٸ� Ÿ���� ã��
    public void UpdateTargets()
    {
        foreach (var employee in employees)
        {
            if (employee != null)
            {
                // ���� ��ǥ�� �ִ� ��� ���ο� ��ǥ�� ������Ʈ
                employee.RequestWorkCheck();
            }
        }
    }

    // �׺�Ž� ����
    public void NowNavMeshBake()
    {
        nms.BuildNavMesh();
    }

    // �����̾� ��Ʈ�� ���������� �湮�ϱ� ���� ���� �Լ�
    public Transform ConveyorTransform(Employee employee)
    {
        employee.CbTransNum++;
        if (employee.CbTransNum == cbTrans.Count)
            employee.CbTransNum = 0;

        return cbTrans[employee.CbTransNum];
    }

    private void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            if(GameObject.Find("Option"))
            {
                Option option = GameObject.Find("Option").GetComponent<Option>();
                option.ShowOption();
            }
            else
            {
                Time.timeScale = 0;
            }
        }
        else
        {
            if(!GameObject.Find("Option"))
            {
                Time.timeScale = 1;
            }
        }
    }
}

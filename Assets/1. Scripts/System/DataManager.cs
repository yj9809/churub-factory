using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using BackEnd;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

// 각종 스택, 종업원 정보를 담기 위한 인터페이스
public interface IObjectDataSave
{
    void ObjectDataSave();
}

// 플레이 시 저장 값 확인 용 Base 데이터 
// 데이터 추가 할 경우 딕셔너리<string, (int & float & bool)> 값 활용하여 만들고 
// 밑에 있는 dataupdata 함수에서 param 값도 추가해줘야함
public class BaseCost
{
    public string guestID;

    public Dictionary<string, int> upgradeCosts = new Dictionary<string, int>
    {
        { "baseSpeedUpgradeCost", 500 },
        { "baseMaxObjStackCountUpgradeCost", 500 },
        { "baseGoldPerBoxUpgradeCost", 5000 },
        { "baseEmployeeSpeedUpgradeCost", 500 },
        { "baseEmployeeMaxObjStackCountUpgradeCost", 500 },
        { "baseEmployeeAddCost", 5000 },
        { "baseUpgradeMaxCount", 5 },
        { "baseSpeedUpgradeCount", 0 },
        { "baseMaxObjStackCountUpgradeCount", 0 },
        { "baseGoldPerBoxUpgradeCount", 0 },
        { "baseEmployeeSpeedUpgradeCount", 0 },
        { "baseEmployeeMaxObjStackCountUpgradeCount", 0 },
        { "baseEmployeeAddCount", 0 }
    };

    // Player 데이터
    public Dictionary<string, float> playerData = new Dictionary<string, float>
    {   
        { "baseSpeed", 5 },
        { "baseCartSpeed", 2.5f },
        { "maxObjStackCount", 3 },
        { "gold", 100},
        { "goldPerBox", 50 }
    };

    public List<string> employeeList = new List<string>();

    // Employee 데이터
    public Dictionary<string, float> employeeData = new Dictionary<string, float>
    {
        { "employeeSpeed", 3 },
        { "employeeCartSpeed", 1.5f },
        { "employeeMaxObjStackCount", 3 }
    };

    // 오브젝트 데이터
    public Dictionary<string, int> objectData = new Dictionary<string, int>
    {
        { "conveyorBeltBoxStorageStackCount", 0 },
        { "churuStorageStackCount", 0 },
        { "packagingWaitObjCount", 0 },
        { "boxPackagingCount", 0},
        { "packagingBoxStorageStackCount", 0 },
        { "truckBoxStackCount", 0 }
    };

    public Dictionary<string, bool> gameProgressBool = new Dictionary<string, bool>
    {
        { "Office", false },
        { "Container1", false },
        { "Machine1", false },
        { "Container2", false },
        { "Machine2", false },
        { "Stall", false },
        { "Store", false }
    };
    public int guideStep = 0;
    public bool newGame = true;
}

public class DataManager : Singleton<DataManager>
{
    public BaseCost baseCost;

    private List<IObjectDataSave> objectDataList = new List<IObjectDataSave>();

    public string fileName = "SaveFile";

    private const int MaxInsertAttempts = 3;
    private string gameDataRowInDate = string.Empty;

    // Start is called before the first frame update

    public void AddObjStackCountList(IObjectDataSave iStackCountSave)
    {
        objectDataList.Add(iStackCountSave);
    }

    private void ObjStackCountSave()
    {
        foreach (var item in objectDataList)
        {
            item.ObjectDataSave();
        }
    }
    #region 서버 데이터 입출력 함수들
    // 데이터 추가
    private Param CreateGameDataParam(bool includeGuestId)
    {
        Param param = new Param();

        if (includeGuestId)
        {
            param.Add("guestID", baseCost.guestID);
        }

        param.Add("upgradeCosts", baseCost.upgradeCosts);
        param.Add("playerData", baseCost.playerData);
        param.Add("employeeList", baseCost.employeeList);
        param.Add("employeeData", baseCost.employeeData);
        param.Add("objectData", baseCost.objectData);
        param.Add("gameProgressBool", baseCost.gameProgressBool);
        param.Add("guideStep", baseCost.guideStep);
        param.Add("newGame", baseCost.newGame);
        return param;
    }

    public bool GameDataInsert()
    {
        if (baseCost == null)
        {
            baseCost = new BaseCost();
        }

        baseCost.guestID = Backend.BMember.GetGuestID();
        Param param = CreateGameDataParam(true);

        for (int attempt = 1; attempt <= MaxInsertAttempts; attempt++)
        {
            var response = Backend.GameData.Insert("TestUserData", param);
            if (response.IsSuccess())
            {
                gameDataRowInDate = response.GetInDate();
                return true;
            }

            Debug.LogWarning($"Failed to insert game data. Attempt {attempt}/{MaxInsertAttempts}.");
        }

        Debug.LogError("Failed to insert game data after all retry attempts.");
        return false;
    }
    // 데이터가 존재 할 경우 데이터 가져오기
    public void GameDataGet()
    {
        var bro = Backend.GameData.GetMyData("TestUserData", new Where());

        if (bro.IsSuccess())
        {
            LitJson.JsonData gameDataJson = bro.FlattenRows(); // Json으로 리턴된 데이터를 받아옵니다.  

            // 받아온 데이터의 갯수가 0이라면 데이터가 존재하지 않는 것입니다. 
            if(gameDataJson.Count > 0)
            {
                gameDataRowInDate = gameDataJson[0]["inDate"].ToString(); //불러온 게임 정보의 고유값입니다.  

                baseCost = new BaseCost();

                baseCost.guideStep = int.Parse(gameDataJson[0]["guideStep"].ToString());
                baseCost.newGame = bool.Parse(gameDataJson[0]["newGame"].ToString());
                baseCost.guestID = gameDataJson[0]["guestID"].ToString();

                //데이터 추가할 경우 해당 부분에 반복문을 통해 데이터 정보를 넣어줘야함
                // 밑에 foreach 형식이나 for 문 사용하여 해당 형식으로 해당 데이터를 잘 넣어줘야함
                foreach (string itemKey in gameDataJson[0]["upgradeCosts"].Keys)
                {
                    baseCost.upgradeCosts[itemKey] = int.Parse(gameDataJson[0]["upgradeCosts"][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0]["playerData"].Keys)
                {
                    baseCost.playerData[itemKey] = float.Parse(gameDataJson[0]["playerData"][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0]["employeeData"].Keys)
                {
                    baseCost.employeeData[itemKey] = float.Parse(gameDataJson[0]["employeeData"][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0]["objectData"].Keys)
                {
                    baseCost.objectData[itemKey] = int.Parse(gameDataJson[0]["objectData"][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0]["gameProgressBool"].Keys)
                {
                    baseCost.gameProgressBool[itemKey] = bool.Parse(gameDataJson[0]["gameProgressBool"][itemKey].ToString());
                }

                foreach (LitJson.JsonData equip in gameDataJson[0]["employeeList"])
                {
                    baseCost.employeeList.Add(equip.ToString());
                }
            }
        }
    }

    // 게임 정보를 업데이트 하는 함수
    public void GameDataUpdate()
    {
        if (baseCost == null)
        {
            return;
        }

        // 해당 부분도 마찬가지로 데이터 추가할 때 
        // 밑 param.Add로 해당 딕셔너리를 제대로 추가해줘야함
        ObjStackCountSave();
        Param param = CreateGameDataParam(false);

        BackendReturnObject bro = null;

        if (string.IsNullOrEmpty(gameDataRowInDate))
        {
            bro = Backend.GameData.Update("TestUserData", new Where(), param);
        }
        else
        {
            bro = Backend.GameData.UpdateV2("TestUserData", gameDataRowInDate, Backend.UserInDate, param);
        }
    }

    // 데이터 삭제 함수
    public void DeleteData()
    {
        BackendReturnObject bro = Backend.GameData.DeleteV2("TestUserData", gameDataRowInDate, Backend.UserInDate);
        
        // 데이터를 삭제하고 게임을 꺼버림
        if(bro.IsSuccess())
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
    #endregion
}



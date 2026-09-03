using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using Churub.Core;

// 각종 스택, 종업원 정보를 담기 위한 인터페이스
public interface IObjectDataSave
{
    void ObjectDataSave();
}

// 기존 코드와 프리팹 참조를 유지하는 호환 타입입니다.
// 실제 런타임 데이터와 스키마는 Unity 비의존 Core 어셈블리에 있습니다.
public class BaseCost : GameDataState
{
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
            param.Add(GameDataSchema.Fields.GuestId, baseCost.guestID);
        }

        param.Add(GameDataSchema.Fields.UpgradeCosts, baseCost.upgradeCosts);
        param.Add(GameDataSchema.Fields.PlayerData, baseCost.playerData);
        param.Add(GameDataSchema.Fields.EmployeeList, baseCost.employeeList);
        param.Add(GameDataSchema.Fields.EmployeeData, baseCost.employeeData);
        param.Add(GameDataSchema.Fields.ObjectData, baseCost.objectData);
        param.Add(GameDataSchema.Fields.GameProgress, baseCost.gameProgressBool);
        param.Add(GameDataSchema.Fields.GuideStep, baseCost.guideStep);
        param.Add(GameDataSchema.Fields.NewGame, baseCost.newGame);
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
            var response = Backend.GameData.Insert(GameDataSchema.TableName, param);
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
        var bro = Backend.GameData.GetMyData(GameDataSchema.TableName, new Where());

        if (bro.IsSuccess())
        {
            LitJson.JsonData gameDataJson = bro.FlattenRows(); // Json으로 리턴된 데이터를 받아옵니다.

            // 받아온 데이터의 갯수가 0이라면 데이터가 존재하지 않는 것입니다.
            if(gameDataJson.Count > 0)
            {
                gameDataRowInDate = gameDataJson[0]["inDate"].ToString(); //불러온 게임 정보의 고유값입니다.

                baseCost = new BaseCost();

                baseCost.guideStep = int.Parse(gameDataJson[0][GameDataSchema.Fields.GuideStep].ToString());
                baseCost.newGame = bool.Parse(gameDataJson[0][GameDataSchema.Fields.NewGame].ToString());
                baseCost.guestID = gameDataJson[0][GameDataSchema.Fields.GuestId].ToString();

                //데이터 추가할 경우 해당 부분에 반복문을 통해 데이터 정보를 넣어줘야함
                // 밑에 foreach 형식이나 for 문 사용하여 해당 형식으로 해당 데이터를 잘 넣어줘야함
                foreach (string itemKey in gameDataJson[0][GameDataSchema.Fields.UpgradeCosts].Keys)
                {
                    baseCost.upgradeCosts[itemKey] = int.Parse(gameDataJson[0][GameDataSchema.Fields.UpgradeCosts][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0][GameDataSchema.Fields.PlayerData].Keys)
                {
                    baseCost.playerData[itemKey] = float.Parse(gameDataJson[0][GameDataSchema.Fields.PlayerData][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0][GameDataSchema.Fields.EmployeeData].Keys)
                {
                    baseCost.employeeData[itemKey] = float.Parse(gameDataJson[0][GameDataSchema.Fields.EmployeeData][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0][GameDataSchema.Fields.ObjectData].Keys)
                {
                    baseCost.objectData[itemKey] = int.Parse(gameDataJson[0][GameDataSchema.Fields.ObjectData][itemKey].ToString());
                }
                foreach (string itemKey in gameDataJson[0][GameDataSchema.Fields.GameProgress].Keys)
                {
                    baseCost.gameProgressBool[itemKey] = bool.Parse(gameDataJson[0][GameDataSchema.Fields.GameProgress][itemKey].ToString());
                }

                foreach (LitJson.JsonData equip in gameDataJson[0][GameDataSchema.Fields.EmployeeList])
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
            bro = Backend.GameData.Update(GameDataSchema.TableName, new Where(), param);
        }
        else
        {
            bro = Backend.GameData.UpdateV2(GameDataSchema.TableName, gameDataRowInDate, Backend.UserInDate, param);
        }
    }

    // 데이터 삭제 함수
    public void DeleteData()
    {
        BackendReturnObject bro = Backend.GameData.DeleteV2(GameDataSchema.TableName, gameDataRowInDate, Backend.UserInDate);
        
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



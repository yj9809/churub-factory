using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

// 이건 내 작품
// 이번엔 큐로 작업한게 아니라 딕셔너리를 활용해 봤음
// 딕셔너리로 쓰니 더 깔끔해진 느낌 추후 풀링 작업할 일 생기면 딕셔너리로 작업하셈
public class PoolingManager : Singleton<PoolingManager>
{
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    // 이건 누가봐도 함수 이름 때문에 오브젝트 만드는 부분이다 하고 이해 가능
    private GameObject CreateObj(GameObject ObjPrefab)
    {
        if(ObjPrefab == null)
        {
            return null;
        }
        GameObject newObj = Instantiate(ObjPrefab, transform);
        newObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
        newObj.transform.localScale = Vector3.one;

        // 여기서 꼭 똑같은 이름으로 해줘야 딕셔너리가 여러개 안생기니 주의 바람.
        // 막 오류로 오브젝트 이름(clone) 하고 해당 큐가 없습니다 나오면
        // 여기서 이름 설정해주는 부분이 문제니까 확인 필요함
        newObj.gameObject.name = ObjPrefab.name;
        newObj.SetActive(false);
        return newObj;
    }

    // 여긴 누가봐도 오브젝트 가져오는 함수임
    public GameObject GetObj(GameObject prefab)
    {
        if(prefab == null)
        {
            return null;
        }
        if(!poolDictionary.ContainsKey(prefab.name))
        {
            // 키가 없으면 큐를 생성해주는데 솔직히 이게 맞나 싶음
            // 그래서 해당 키가 없으면 밑에서 만들도록 해뒀는데
            // 차라리 여기서 새로 poolDictionary.Add로 만들어주는게 좋은거 아닐까 싶음
            poolDictionary[prefab.name] = new Queue<GameObject>();
        }

        Queue<GameObject> objPool = poolDictionary[prefab.name];

        if(objPool.Count <= 0)
        {
            GameObject newObj = CreateObj(prefab);
            objPool.Enqueue(newObj);
        }

        GameObject objToReturn = objPool.Dequeue();
        objToReturn.SetActive(true);
        return objToReturn;
    }

    // 이건 오브젝트 다시 돌려받을 때 불러오는 함수임
    public void ReturnObjecte(GameObject returnPrefab)
    {
        if(returnPrefab == null)
        {
            return;
        }

        if (returnPrefab.TryGetComponent<Item>(out var item))
        {
            if (item.IsStored)
            {
                Debug.LogError("Remove the Item from its buffer before returning it to the pool.", returnPrefab);
                return;
            }
            returnPrefab.transform.DOKill();
        }
        returnPrefab.SetActive(false);

        // 여기 if 문에서 해당 키가 있는지 확인을 함
        if(poolDictionary.ContainsKey(returnPrefab.name))
        {
            if(returnPrefab.GetComponent<Rigidbody>())
                Destroy(returnPrefab.GetComponent<Rigidbody>());

            returnPrefab.transform.SetParent(transform);
            returnPrefab.transform.localScale = Vector3.one;
            poolDictionary[returnPrefab.name].Enqueue(returnPrefab);
        }
        else
        {
            // 만약 잘 바꿀 자신 있다 하면 이 부분을 위쪽에서 처리해보는것도 좋을꺼 같음
            poolDictionary.Add($"{returnPrefab.name}", new Queue<GameObject>());
            
            // 해당 부분은 키가 없어서 Add 를 통해 다시 만들었는데 굳이 삭제 처리해야하나 싶지만 
            // 삭제 처리할게 많지 않을꺼 같아서 그냥 냅둬봤음
            Destroy(returnPrefab);
        }
    }
}

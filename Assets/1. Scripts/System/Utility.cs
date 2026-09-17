using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using DG.Tweening;

// 여기는 재료 타입 나눠서 움직임 결정해줄려고 만든 enum임
// create는 재료 생산할 때, Drop은 물건 가져갈 때, Array는 박스 스토리지에 옮길 때
// Car는 트럭에 박스 옮길 때, Box는 말 그대로 박스를 플레이어 카트에 옮길 때 
// 어느 부분 오브젝트 움직임이 이상하다 싶으면 여기 확인해서 밑에 움직임 처리하는 부분에 해당 enum 번호 찾아서 확인하면 됨
public enum CheckType
{
    Create,
    Drop,
    Array,
    Car,
    Box
}
#region 진동이었던 친구...
public static class Vibrations
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass AndroidPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject AndroidcurrentActivity = AndroidPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject AndroidVibrator = AndroidcurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#endif
    public static void Vibrate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidVibrator.Call("vibrate");
#endif
    }

    public static void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
         AndroidVibrator.Call("vibrate", milliseconds);
#endif
    }

    public static void Cancel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidVibrator.Call("cancel");
#endif
    }

}
#endregion
public static class Utility
{
    public static float ObjRendererCheck(GameObject obj)
    {
        BoxCollider ren = obj.GetComponent<BoxCollider>();

        return ren.size.y;
    }
    // parentPos 이동 시킬 곳, churu 만들 오브젝트(처음 재료 만들어주는 곳에서만 사용하면 될꺼 같아서 나머지는 다 Null)
    // getChuruStack 가져올 스택(a에서 b로 옮길 때 a를 말함), setChuruStack 받을 스택(마찬가지로 b를 말함), num 타입 구분을 위한 인트
    // 막 재료들 이상하게 크게나 적게 나올 때나 재료들이 이상하게 움직일 때 대부분 여기서 확인하면 됨
    /// <summary>
    /// 오브젝트 이동 시킬 함수.
    /// </summary>
    /// <param name="parentPos">이동 시킬 곳</param>
    /// <param name="churu">만들 오브젝트</param>
    /// <param name="getChuruStack">가져올 스택</param>
    /// <param name="setChuruStack">받을 스택</param>
    /// <param name="num">타입을 구분하는 인트</param>
    public static void ObjectDrop(Transform parentPos, GameObject churu, Stack<GameObject> getChuruStack, Stack<GameObject> setChuruStack, int num)
    {
        GameObject newChuru;

        if (num == (int)CheckType.Create)
        {
            newChuru = churu;
            newChuru.name = churu.name;
            newChuru.transform.SetParent(parentPos);
            newChuru.transform.DOLocalMove(new Vector3(0, 0 + (ObjRendererCheck(newChuru) * setChuruStack.Count), 0), 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => 
                {
                    newChuru.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    newChuru.transform.localScale = Vector3.one;
                } );
        }
        else
        {
            if (num == (int)CheckType.Drop)
            {
                newChuru = getChuruStack.Pop();
                newChuru.transform.DOLocalMove(new Vector3(0, 0 + (ObjRendererCheck(newChuru) * setChuruStack.Count), 0), 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    newChuru.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    newChuru.transform.localScale = Vector3.one;
                });
            }
            else if (num == (int) CheckType.Array)
            {
                newChuru = churu;
                newChuru.transform.DOLocalMove(new Vector3(0, 0 + (ObjRendererCheck(newChuru) * (setChuruStack.Count % 10)), 0), 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    newChuru.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    newChuru.transform.localScale = Vector3.one;
                });
            }
            else if(num == (int) CheckType.Car)
            {
                newChuru = getChuruStack.Pop();
                newChuru.transform.DOMove(parentPos.position, 0.2f).SetEase(Ease.InBack);
            }
            else
            {
                newChuru = getChuruStack.Pop();
                newChuru.transform.DOLocalMove(new Vector3(0, 0 + (ObjRendererCheck(newChuru) * setChuruStack.Count), 0), 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    newChuru.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    newChuru.transform.localScale = Vector3.one;
                });
            }
        }
        newChuru.transform.SetParent(parentPos);

        if(setChuruStack != null)
            setChuruStack.Push(newChuru);
    }
}

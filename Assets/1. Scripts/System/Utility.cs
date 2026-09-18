using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

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
}

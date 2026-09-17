using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOSGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;

    void Awake()
    {
        InitializeAds();
    }

    public void InitializeAds()
    {
#if UNITY_IOS
        _gameId = _iOSGameId; // iOS 플랫폼용 게임 ID 설정
#elif UNITY_ANDROID
        _gameId = _androidGameId; // Android 플랫폼용 게임 ID 설정
#elif UNITY_EDITOR
        _gameId = _androidGameId; // 에디터에서 기능 테스트를 위한 설정
#endif
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, false, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads 초기화 완료.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity Ads 초기화 실패: {error.ToString()} - {message}");
    }
}

using UnityEngine;
using UnityEngine.Advertisements;

public class InterstitialAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOsAdUnitId = "Interstitial_iOS";
    string _adUnitId;

    void Awake()
    {
        // 현재 플랫폼에 대한 광고 단위 ID 가져오기
        _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsAdUnitId
            : _androidAdUnitId;
    }

    void Start()
    {
        LoadAd();
        // 5분(300초)마다 광고 표시 시작
        InvokeRepeating("ShowAd", 300f, 300f);
    }

    // 광고 단위에 콘텐츠 로드:
    public void LoadAd()
    {
        // 중요! 초기화 이후에만 콘텐츠를 로드하세요 (이 예제에서는 초기화가 다른 스크립트에서 처리됨).
        Advertisement.Load(_adUnitId, this);
    }

    // 광고 단위에서 로드된 콘텐츠 표시:
    public void ShowAd()
    {
        // 광고 콘텐츠가 이전에 로드되지 않은 경우 이 메서드는 실패합니다.
        Advertisement.Show(_adUnitId, this);
    }

    // Load Listener 및 Show Listener 인터페이스 메서드 구현:
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        // 광고 단위가 콘텐츠를 성공적으로 로드했을 때 실행할 코드를 선택적으로 작성할 수 있습니다.
    }

    public void OnUnityAdsFailedToLoad(string _adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"광고 단위 로드 오류: {_adUnitId} - {error.ToString()} - {message}");
        // 광고 단위가 로드 실패할 경우 선택적으로 다시 시도하는 등의 코드를 실행할 수 있습니다.
    }

    public void OnUnityAdsShowFailure(string _adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"광고 단위 표시 오류 {_adUnitId}: {error.ToString()} - {message}");
        // 광고 단위가 표시 실패할 경우 선택적으로 다른 광고를 로드하는 등의 코드를 실행할 수 있습니다.
    }

    public void OnUnityAdsShowStart(string _adUnitId) { }
    public void OnUnityAdsShowClick(string _adUnitId) { }
    public void OnUnityAdsShowComplete(string _adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        LoadAd();
    }
}

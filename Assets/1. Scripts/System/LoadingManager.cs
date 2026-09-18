using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private Image loadingBar;
    [SerializeField] private TMP_Text statusText;
    private float target;
    private float displayed;
    public bool IsComplete => displayed >= 1f;
    public void Show(float progress, string message)
    {
        target = Mathf.Clamp01(progress);
        if (displayed > target) displayed = target;
        if (statusText != null) statusText.text = message;
    }
    private void Update()
    {
        displayed = Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime * .6f);
        if (loadingBar != null) loadingBar.fillAmount = displayed;
    }
    // Keep old event bindings from bypassing startup validation.
    public void StartCoroutine() { Debug.LogWarning("Scene entry is owned by StartupCoordinator."); }
}
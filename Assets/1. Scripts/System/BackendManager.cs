using UnityEngine;
using UnityEngine.UI;

// Scene/UnityEvent compatibility bridge. Existing script GUID and serialized fields stay intact.
public class BackendManager : MonoBehaviour
{
    [SerializeField] private LoadingManager loadingManager;
    [SerializeField] private Image image;
    private StartupCoordinator coordinator;
    private StartupCoordinator Coordinator
    {
        get
        {
            if (coordinator == null)
            {
                coordinator = GetComponent<StartupCoordinator>();
                if (coordinator == null) coordinator = gameObject.AddComponent<StartupCoordinator>();
                coordinator.Configure(loadingManager, image, FindObjectOfType<InAppUpdate>());
            }
            return coordinator;
        }
    }
    public void GuestLogin() => Coordinator.Begin(false);
    public void StartGoogleLogin() => Coordinator.Begin(true);
    public void GetAccessCode() => StartGoogleLogin();
}
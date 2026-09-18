using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 이건 권오석?? 작품 일꺼임 아마도 그럼
// 그러니까 문제 생기면 권오석 쪽으로 문의 부탁
public class MainCamera : MonoBehaviour
{
    [SerializeField] private Player p;
    private Vector3 inCameraPosition = new Vector3(-6.7f, 9f, -6.2f);
    private Vector3 targetCameraPosition;
    private bool isZoom;

    private Tween cameraTween;

    void Start()
    {
        p = GameManager.Instance.P;
        targetCameraPosition = inCameraPosition;
        transform.position = GetTargetPosition(targetCameraPosition);
    }

    void Update()
    {
        if (cameraTween == null || !cameraTween.IsActive() || !cameraTween.IsPlaying())
        {
            Vector3 targetPosition = GetTargetPosition(targetCameraPosition);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }

    public void ZoomScreen()
    {
        if (!isZoom)
        {
            transform.GetComponent<Camera>().DOOrthoSize(15, 1f);
            isZoom = true;
        }
        else
        {
            transform.GetComponent<Camera>().DOOrthoSize(9, 1f);
            isZoom = false;
        }

        if (cameraTween != null)
        {
            cameraTween.Kill(true);
        }
    }

    private Vector3 GetTargetPosition(Vector3 cameraPosition)
    {
        return new Vector3(
            p.transform.position.x + cameraPosition.x,
            cameraPosition.y,
            p.transform.position.z + cameraPosition.z
        );
    }
}

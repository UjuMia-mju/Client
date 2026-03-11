using UnityEngine;
using System.Collections;

public class StageCameraController : MonoBehaviour
{
    [Header("Camera Move Settings")] 
    [Tooltip("카메라가 이동하는 데 걸리는 시간(초)")] 
    [SerializeField] private float cameraMoveDuration = 0.5f;

    [Tooltip("줌인 시 카메라의 고정 회전 각도")] 
    [SerializeField] private Vector3 zoomEulerAngles = new Vector3(45f, 0f, 0f);

    [Tooltip("행성 중심으로부터 카메라가 떨어질 거리")] 
    [SerializeField] private float zoomDistance = 10f;

    private Vector3 originPos = new Vector3(0.2f, -13.3f, 6.2f);
    private Quaternion originRot = new Quaternion(-0.3888f, 0.0016f, 0.003f, 0.92f);
    
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void ResetToOrigin()
    {
        if (_mainCamera != null)
        {
            _mainCamera.transform.position = originPos;
            _mainCamera.transform.rotation = originRot;
        }
    }

    public IEnumerator ZoomIn(Transform targetTransform)
    {
        if (_mainCamera == null) yield break;
        
        Quaternion dynamicTargetRot = Quaternion.Euler(zoomEulerAngles);
        Vector3 dynamicTargetPos = targetTransform.position - (dynamicTargetRot * Vector3.forward * zoomDistance);

        yield return StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, dynamicTargetPos, dynamicTargetRot));
    }

    public IEnumerator ZoomOut()
    {
        if (_mainCamera == null) yield break;
        yield return StartCoroutine(MoveCamera(_mainCamera.transform.position, _mainCamera.transform.rotation, originPos, originRot));
    }

    private IEnumerator MoveCamera(Vector3 startP, Quaternion startR, Vector3 endP, Quaternion endR)
    {
        float elapsed = 0f;
        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / cameraMoveDuration);
            
            _mainCamera.transform.position = Vector3.Lerp(startP, endP, t);
            _mainCamera.transform.rotation = Quaternion.Slerp(startR, endR, t);
            
            yield return null;
        }
        
        _mainCamera.transform.position = endP;
        _mainCamera.transform.rotation = endR;
    }
}
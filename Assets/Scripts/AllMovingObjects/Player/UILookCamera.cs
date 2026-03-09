using UnityEngine;

public class UILookCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            Debug.Log("카메라 초기화 : World Space");
            canvas.worldCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }
}
using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; }

    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private bool _hasTarget = false;

    void Start()
    {
        _targetPos = transform.position;
        _targetRot = transform.rotation;
    }

    void Update()
    {
        if (_hasTarget)
        {
            // 부드러운 보간 이동
            transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Time.deltaTime * lerpSpeed);
        }
    }

    public void SetTargetPosition(Vector3 position, Quaternion rotation)
    {
        _targetPos = position;
        _targetRot = rotation;
        _hasTarget = true;
    }
}

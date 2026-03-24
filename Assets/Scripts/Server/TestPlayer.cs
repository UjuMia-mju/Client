using Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sendInterval = 0.05f; // 20fps로 위치 전송 (네트워크 부하 고려)
    private float _lastSendTime = 0f;
    private Vector3 _lastSendPos;
    private Quaternion _lastSendRot;

    void Start()
    {
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;

        // 게임 입장 패킷 전송
        PacketDispatcher.Instance.SendEnterGame(0);
    }

    void Update()
    {
        HandleMovement();
        SendPositionToServer();
    }

    private void HandleMovement()
    {
        // WASD 입력
        Vector2 input = new Vector2(
            Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
            Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0
        ).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 moveDir = new Vector3(input.x, 0f, input.y);
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // 이동 방향으로 회전
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }

    private void SendPositionToServer()
    {
        // 일정 간격으로만 전송 (네트워크 최적화)
        if (Time.time - _lastSendTime < sendInterval)
            return;

        // 위치나 회전이 변경되었을 때만 전송
        bool posChanged = Vector3.Distance(transform.position, _lastSendPos) > 0.01f;
        bool rotChanged = Quaternion.Angle(transform.rotation, _lastSendRot) > 0.5f;

        if (posChanged || rotChanged)
        {
            PacketDispatcher.Instance.SendMove(transform.position, transform.rotation);

            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;
            _lastSendTime = Time.time;
        }
    }
}
using UnityEngine;

public class Items : MovingObject
{
    // 내가 이 아이템을 들고 있는지 여부
    private bool IsOwnedByMe;
    private const string SOCKET = "Socket";

    // 서버 관련 변수들
    public float sendInterval = 0.05f; // 20fps로 위치 전송 (네트워크 부하 고려)
    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    private void LateUpdate()
    {
        // 부모 이름이 "Socket"이면 내가 손에 들고 있는 상태라고 판단
        if (transform.parent != null && transform.parent.name == SOCKET)
        {
            IsOwnedByMe = true;
        }
        else
        {
            IsOwnedByMe = false;
        }

        // 내가 들고 있지 않을 때만 위치 패킷을 서버로 전송
        if (!IsOwnedByMe)
        {
            SendPositionToServer();
        }
    }

    // 서버로 위치 정보 패킷전송
    protected void SendPositionToServer()
    {
        // 일정 간격으로만 전송 (네트워크 최적화)
        if (Time.time - _lastSendTime < sendInterval)
            return;

        // 위치나 회전이 변경되었을 때만 전송
        bool posChanged = Vector3.Distance(transform.position, _lastSendPos) > 0.01f;
        bool rotChanged = Quaternion.Angle(transform.rotation, _lastSendRot) > 0.5f;

        if (posChanged || rotChanged)
        {
            NetManager.Instance.SendItemMove(transform.position, transform.rotation);

            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;


            _lastSendTime = Time.time;
        }
    }

    // 서버에서 받은 위치와 회전을 적용
    public void SetPos(Vector3 pos, Quaternion rot)
    {
        // 내가 들고 있는 아이템이면 서버 위치를 무시하고,
        // 다른 플레이어가 들고 있는 아이템만 위치를 갱신
        if (!IsOwnedByMe)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

}
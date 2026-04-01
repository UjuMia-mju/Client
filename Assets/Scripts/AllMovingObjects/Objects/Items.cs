using UnityEngine;
using UnityEngine.Animations;
using static UnityEngine.Rendering.ReloadAttribute;

public class Items : MovingObject
{
    // 내가 이 아이템을 들고 있는지 여부
    private bool IsOwnedByMe;
    protected const string SOCKET = "Socket";

    // 서버 관련 변수들
    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    // 이제 ItemManager에서 자동부여하며 이전에 테스트했던 방식대로 인스펙터에서 특정 값 줘서 작동하지 않습니다.
    [HideInInspector] public int itemId; // 아이템 고유 ID

    public string itemStringKey;

    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 _targetPos;
    private Quaternion _targetRot = Quaternion.identity;

    private PlanetGravity _planet;

    private void Start()
    {
        ItemManager.Instance.RegisterItem(this);
        _planet = FindFirstObjectByType<PlanetGravity>();
        _targetPos = transform.position;
        _targetRot = transform.rotation;
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;
        _lastSendTime = Time.time;
    }

    private void FixedUpdate()
    {
        // 바닥에 닿으면 목표 위치를 현재 위치로 갱신 (이전 공중 위치로 되돌아가는 현상 방지)
        if (_planet != null)
        {
            Vector3 gravityDir = (transform.position - _planet.transform.position).normalized;
            LayerMask groundMask = LayerMask.GetMask(Define.Layer.GROUND, Define.Layer.WALKABLE_COLLIDER);
            if (Physics.Raycast(transform.position, -gravityDir, 2.0f, groundMask))
            {
                _targetPos = transform.position;
                _targetRot = transform.rotation;
            }
        }

        // 서버에서 목표 위치를 받았을 때만 보간 이동
        if (Vector3.Distance(transform.position, _targetPos) > 0.01f ||
            Quaternion.Angle(transform.rotation, _targetRot) > 0.5f)
        {
            Moving(Vector3.zero);
        }
    }

    private void LateUpdate()
    {
        // 부모 이름이 "Socket"이면 내가 손에 들고 있는 상태라고 판단
        if (transform.parent != null && transform.parent.name == SOCKET)
        {
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
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
    private void SendPositionToServer()
    {
        Vector3 gravityDir = (transform.position - _planet.transform.position).normalized;
        Vector3 horizontalVelocity = rb.linearVelocity - Vector3.Project(rb.linearVelocity, gravityDir);
        float interval = horizontalVelocity.sqrMagnitude > 0.1f ? 0.05f : 0.5f;

        if (Time.time - _lastSendTime < interval)
            return;

        bool posChanged = Vector3.Distance(transform.position, _lastSendPos) > 0.1f;
        bool rotChanged = Quaternion.Angle(transform.rotation, _lastSendRot) > 5f;

        if (posChanged || rotChanged)
        {
            Debug.Log($"위치 패킷전송 | posChanged={posChanged} ({Vector3.Distance(transform.position, _lastSendPos):F4}) | rotChanged={rotChanged} ({Quaternion.Angle(transform.rotation, _lastSendRot):F4}) | hVel={horizontalVelocity.sqrMagnitude:F4}");
            PacketDispatcher.Instance.SendItemOrToolMove(this, transform.position, transform.rotation);
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
            _targetPos = pos;
            _targetRot = rot;
        }
    }

    protected override void Moving(Vector3 movDir)
    {
        if (IsOwnedByMe) return; // 들고 있는 중이면 보간 이동 불필요

        // AddForce로 움직이는 중이면 보간 이동을 막는다
        if (rb.linearVelocity.sqrMagnitude < 0.0001f) // 거의 정지 상태일 때만 보간
        {
            rb.MovePosition(Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed));
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed));
        }
    }

}
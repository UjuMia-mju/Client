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

    // 씬에 직접 배치된 아이템인지 여부 (런타임 Instantiate가 아닌 경우)
    [Tooltip("씬에 미리 배치된 아이템이면 체크. 호스트가 피어에게 초기 ID를 동기화합니다.")]
    [SerializeField] private bool isScenePlacedItem = false;

    private void Start()
    {
        ItemManager.Instance.RegisterItem(this);
        _planet = FindFirstObjectByType<PlanetGravity>();
        _targetPos = transform.position;
        _targetRot = transform.rotation;
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;
        _lastSendTime = Time.time;

        // 씬에 직접 배치된 아이템은 호스트가 피어에게 ID 및 위치를 동기화
        if (isScenePlacedItem && ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            StartCoroutine(BroadcastSpawnNextFrame());
    }

    private System.Collections.IEnumerator BroadcastSpawnNextFrame()
    {
        yield return null; // RegisterItem() 완료 후 itemId 확정 대기
        PacketSender.Instance.SendObjectSpawn(this, transform.position, transform.rotation);
        Debug.Log($"[Items] 씬 배치 아이템 동기화: itemId={itemId}, key={itemStringKey}");
    }

    private void FixedUpdate()
    {
        if (_planet != null)
        {
            Vector3 gravityDir = (transform.position - _planet.transform.position).normalized;
            LayerMask groundMask = LayerMask.GetMask(Define.Layer.GROUND, Define.Layer.WALKABLE_COLLIDER);

            // 거리를 2.0f → 0.6f로 줄여서 완전히 바닥에 닿았을 때만 위치 고정
            if (Physics.Raycast(transform.position, -gravityDir, 0.6f, groundMask))
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
        if (transform.parent != null && transform.parent.name == SOCKET)
        {
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;

            // 부모 계층에서 OtherPlayers 또는 Player를 찾아 로컬 플레이어 소유 여부 판단
            // OtherPlayers가 부모이면 다른 플레이어가 든 것 → 내 소유 아님
            OtherPlayers otherPlayer = transform.parent.GetComponentInParent<OtherPlayers>();
            if (otherPlayer != null)
            {
                IsOwnedByMe = false;
            }
            else
            {
                // OtherPlayers가 없으면 로컬 Player가 든 것
                Player localPlayer = transform.parent.GetComponentInParent<Player>();
                IsOwnedByMe = localPlayer != null;
            }
        }
        else
        {
            IsOwnedByMe = false;
        }

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
            PacketSender.Instance.SendItemMove(itemId, transform.position, transform.rotation);
            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;
            _lastSendTime = Time.time;
        }
    }

    // 서버에서 받은 위치와 회전을 적용
    public void SetPos(Vector3 pos, Quaternion rot)
    {
        if (!IsOwnedByMe)
        {
            _targetPos = pos;
            _targetRot = rot;
        }
    }

    // 아이템을 놓을 때 즉시 위치 동기화 강제 전송
    public void OnDetached()
    {
        _lastSendPos = Vector3.zero; // 강제로 변화 감지되게 초기화
        _lastSendRot = Quaternion.identity;
        _lastSendTime = 0f;
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
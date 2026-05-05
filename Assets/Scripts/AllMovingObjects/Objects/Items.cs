using UnityEngine;
using UnityEngine.Animations;
using static UnityEngine.Rendering.ReloadAttribute;

public class Items : MovingObject
{
    private bool IsOwnedByMe;
    protected const string SOCKET = "Socket";

    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    [HideInInspector] public int itemId;
    public string itemStringKey;

    [SerializeField] private float lerpSpeed = 25f; // [수정] 10 → 25: 패킷 간격(0.05s) 내 ~95% 도달

    private Vector3 _targetPos;
    private Quaternion _targetRot = Quaternion.identity;
    private Vector3 _targetVelocity = Vector3.zero; // [추가] Dead-reckoning용 속도

    private PlanetGravity _planet;

    [Tooltip("씬에 미리 배치된 아이템이면 체크. 호스트가 피어에게 초기 ID를 동기화합니다.")]
    [SerializeField] private bool isScenePlacedItem = false;
    public bool IsScenePlacedItem => isScenePlacedItem;

    private void Start()
    {
        ItemManager.Instance.RegisterItem(this);
        _planet = FindFirstObjectByType<PlanetGravity>();
        _targetPos = transform.position;
        _targetRot = transform.rotation;
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;
        _lastSendTime = Time.time;

        // 피어 클라이언트에서는 물리 시뮬레이션 비활성화
        // 호스트 위치를 받아서 따라가기만 하면 되므로 물리가 필요 없음
        if (ConnectManager.Instance != null && !ConnectManager.Instance.isHost)
        {
            rb.isKinematic = true;

            ObjectsGravityController gravController = GetComponent<ObjectsGravityController>();
            if (gravController != null)
                gravController.enabled = false;
        }

        // OtherPlayers와 물리 충돌 무시 (밀림 방지)
        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            foreach (var rp in FindObjectsByType<OtherPlayers>(FindObjectsSortMode.None))
            {
                Collider rpCol = rp.GetComponent<Collider>();
                if (rpCol != null)
                    Physics.IgnoreCollision(myCol, rpCol, true);
            }
        }

        if (isScenePlacedItem && ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            StartCoroutine(BroadcastSpawnNextFrame());
    }

    private System.Collections.IEnumerator BroadcastSpawnNextFrame()
    {
        yield return null;
        PacketSender.Instance.SendObjectSpawn(this, transform.position, transform.rotation);
        Debug.Log($"[Items] 씬 배치 아이템 동기화: itemId={itemId}, key={itemStringKey}");
    }

    private void FixedUpdate()
    {
        bool isPeer = ConnectManager.Instance != null && !ConnectManager.Instance.isHost;

        // [수정] 호스트는 Rigidbody 물리가 단일 진실의 원천이므로
        //       _targetPos 기반 보정을 일절 수행하지 않는다.
        //       기존 코드는 raycast miss(콜라이더가 두껍거나 경사면 등)로
        //       _targetPos가 stale 상태에서 velocity가 순간적으로 0에 근접할 때
        //       MovePosition(Lerp(pos, stale, 0.5))이 발사되어 지면 침투 →
        //       물리 솔버가 위로 튕김(팝콘 현상)을 유발했다.
        if (!isPeer) return;

        // ↓ 이하 피어 전용 로직 ↓

        // Dead-reckoning: 패킷 사이 구간을 마지막 수신 속도로 예측
        if (rb.isKinematic)
        {
            _targetPos += _targetVelocity * Time.fixedDeltaTime;
        }

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

            OtherPlayers otherPlayer = transform.parent.GetComponentInParent<OtherPlayers>();
            if (otherPlayer != null)
                IsOwnedByMe = false;
            else
            {
                Player localPlayer = transform.parent.GetComponentInParent<Player>();
                IsOwnedByMe = localPlayer != null;
            }
        }
        else
        {
            IsOwnedByMe = false;
        }

        if (!IsOwnedByMe)
            SendPositionToServer();
    }

    private void SendPositionToServer()
    {
        // 아이템 위치 송신 권한은 호스트에게만 있음
        if (ConnectManager.Instance != null && !ConnectManager.Instance.isHost) return;

        Vector3 gravityDir = (transform.position - _planet.transform.position).normalized;
        Vector3 horizontalVelocity = rb.linearVelocity - Vector3.Project(rb.linearVelocity, gravityDir);

        // [수정] sqrMagnitude 0.1f(=속도 0.316m/s) → 0.01f(=속도 0.1m/s)로 정지 판정 엄격화
        float interval = horizontalVelocity.sqrMagnitude > 0.01f ? 0.05f : 0.2f;

        if (Time.time - _lastSendTime < interval) return;

        // [수정] 임계값 완화: 위치 0.1f→0.03f, 회전 5f→2f
        bool posChanged = Vector3.Distance(transform.position, _lastSendPos) > 0.03f;
        bool rotChanged = Quaternion.Angle(transform.rotation, _lastSendRot) > 2f;

        if (posChanged || rotChanged)
        {
            PacketSender.Instance.SendItemMove(itemId, transform.position, transform.rotation);
            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;
            _lastSendTime = Time.time;
        }
    }

    // [수정] velocity 파라미터 추가하여 Dead-reckoning 지원
    public void SetPos(Vector3 pos, Quaternion rot, Vector3 velocity = default)
    {
        if (!IsOwnedByMe)
        {
            _targetPos = pos;
            _targetRot = rot;
            _targetVelocity = velocity;
        }
    }

    public void OnDetached(bool ownedByMeAfterDetach)
    {
        IsOwnedByMe = ownedByMeAfterDetach;
        _lastSendPos = Vector3.zero;
        _lastSendRot = Quaternion.identity;
        _lastSendTime = 0f;
    }

    public void SetOwnedByMe(bool ownedByMe)
    {
        IsOwnedByMe = ownedByMe;
    }

    protected override void Moving(Vector3 movDir)
    {
        if (IsOwnedByMe) return;

        if (rb.isKinematic)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed);
        }
        else if (rb.linearVelocity.sqrMagnitude < 0.0001f)
        {
            rb.MovePosition(Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed));
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed));
        }
    }
}
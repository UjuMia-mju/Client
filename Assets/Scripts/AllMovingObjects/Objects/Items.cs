using UnityEngine;


public class Items : MovingObject
{
    private bool IsOwnedByMe;
    protected const string SOCKET = "Socket";

    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    [HideInInspector] public int itemId;

    [SerializeField, ItemKey] private string itemKey;
    public string itemStringKey => itemKey;

    [SerializeField] private float lerpSpeed = 25f;

    private Vector3 _targetPos;
    private Quaternion _targetRot = Quaternion.identity;
    private Vector3 _targetVelocity = Vector3.zero;

    private PlanetGravity _planet;

    [Tooltip("씬에 미리 배치된 아이템이면 체크. 호스트가 피어에게 초기 ID를 동기화합니다.")]
    [SerializeField] private bool isScenePlacedItem = false;
    public bool IsScenePlacedItem => isScenePlacedItem;

    private void Start()
    {
        // [수정] 씬 배치 아이템은 ItemManager.Awake/Start 에서 일괄 사전 등록한다.
        //       여기서 RegisterItem 을 호출하면 피어 측에서 itemId 가 호스트와 다르게 부여되어
        //       PICKUP/DROP/MOVE 패킷 ID 매칭이 어긋남(야구배트 2개, 삽이 따라다님 등 증상).
        if (!isScenePlacedItem)
        {
            ItemManager.Instance.RegisterItem(this);
        }

        _planet = FindFirstObjectByType<PlanetGravity>();
        _targetPos = transform.position;
        _targetRot = transform.rotation;
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;
        _lastSendTime = Time.time;

        if (ConnectManager.Instance != null && !ConnectManager.Instance.isHost)
        {
            rb.isKinematic = true;

            ObjectsGravityController gravController = GetComponent<ObjectsGravityController>();
            if (gravController != null)
                gravController.enabled = false;
        }

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

        // [삭제] 씬 배치 아이템 BroadcastSpawn 도 ItemManager 가 담당하도록 이동.
        //       (런타임 스폰은 SpawnItemAndBroadcast 경로로 처리)
    }

    private void FixedUpdate()
    {
        bool isPeer = ConnectManager.Instance != null && !ConnectManager.Instance.isHost;
        if (!isPeer) return;

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
        if (ConnectManager.Instance != null && !ConnectManager.Instance.isHost) return;

        Vector3 gravityDir = (transform.position - _planet.transform.position).normalized;
        Vector3 horizontalVelocity = rb.linearVelocity - Vector3.Project(rb.linearVelocity, gravityDir);

        float interval = horizontalVelocity.sqrMagnitude > 0.01f ? 0.05f : 0.2f;

        if (Time.time - _lastSendTime < interval) return;

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
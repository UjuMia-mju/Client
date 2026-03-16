using UnityEngine;

public class Player : MovingObject
{
    // 입력 동결 플래그 (충돌 시 입력 무시용이며 최종 판단을 내리는 클래스라 판단해 이곳에 선언했습니다.)
    private bool inputFreeze = false;

    // 컴포넌트 참조 변수
    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    private PlayerTPCamera playerTPCamera;
    public PlayerItemSystem playerItemSystem { get; private set; }

    public GameObject nearestObject { get; private set; } // 플레이어에게서 가장 가까운 오브젝트

    private const float DETECT_RADIUS = 5.5f; // 구형 트리거 반지름 

    public bool isPlayerGetSomething { get; private set; } = false;
    public bool isMining { get; private set; } = false;

    public GameObject playerBoneModel;

    // TODO : 기초 플레이어 능력치 시스템 구현 완료 - 실제로 표시되는 방식은 UI담당과 상의 필요
    private PlayerStat playerStat;


    // 서버 관련 변수들
    public float sendInterval = 0.05f; // 20fps로 위치 전송 (네트워크 부하 고려)
    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    private AnimState lastAnimState;
    private int lastHP;
    private float lastOxygen;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerItemSystem = GetComponent<PlayerItemSystem>();
        playerTPCamera = Camera.main.GetComponent<PlayerTPCamera>();
        playerStat = GetComponent<PlayerStat>();

        playerAnimator.Initialize();
        lastAnimState = AnimState.Idle;
    }

    private void Start()
    {

        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;

        lastHP = playerStat.GetHp();
        lastOxygen = playerStat.GetOxygen();

        // 게임 입장 패킷 전송
        NetManager.Instance.SendEnterGame(0);

        SendEnterPosToServer();

        // 산소가 줄어들기 시작함
        StartCoroutine(playerStat.OxygenDecrease());
    }

    // 플레이어 인풋, 레이캐스트, 애니메이션 업데이트
    private void Update()
    {
        playerInput.InputProcess(); // 인풋, 충돌 감지는 Input이 되지 않으면 레이캐스트가 멈추므로 가장 먼저 처리합니다.

        // 충돌 감지
        inputFreeze = CollisionDetectWithRaycast(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir), wallMask);

        if (!inputFreeze)
        {
            GroundDetectingWithRaycast(groundMask | walkable);

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                isGrounded, 
                inputFreeze,
                isMining);

            KeyEInteract();
            KeyFInteract();
        }

        // 현재 땅을 밟았는지 안 밟았는지와는 무관하게 레이캐스트를 길게 펼쳐 해당 지면의 접지면 벡터를 구합니다.
        GetGroundNormal(groundMask);

        // 구형 트리거
        SphereTriggerFunc();
    }

    // 물리 작용 업데이트
    private void FixedUpdate()
    {
        if (!inputFreeze)
        {
            Moving(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));
            RotateToDirection(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));

            if (playerInput.GetIsJumping() && isGrounded && !isMining)
            {
                Jump();
                playerInput.SetIsJumping(false);
            }
            else if (!isGrounded)
            {
                // 공중에서 눌린 점프 입력은 그냥 버림(중요)
                playerInput.SetIsJumping(false);
            }
        }
    }

    private void LateUpdate()
    {
        // 서버로 패킷 전송
        SendPositionToServer();
        SendAnimationToServer(); 
        //SendPlayerStatToServer();
    }

    protected override void Moving(Vector3 movDir)
    {
        base.Moving(movDir);
        isMining = false; // 이동하면 광질이 멈춥니다.
    }

    // E키 상호작용
    private void KeyEInteract()
    {
        if (playerInput.GetIsInteract())
        {
            playerInput.MakeIsInteractFalse();
            // 플레이어가 아이템을 들고 있고, 그게 어떤 도구일 때
            if (playerItemSystem.GetItemTag() != null && playerItemSystem.GetItemTag().Equals(Define.Tag.PICKAXE) && isPlayerGetSomething)
            {
                isMining = true;
            }

            else if (nearestObject == null)
            {
                return;
            }

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 아이템이며, 빈 손일 때
            // 혹은 태그가 Tool인 것도 포함함.
            else if (nearestObject.CompareTag(Define.Tag.ITEM) && !isPlayerGetSomething || nearestObject.CompareTag(Define.Tag.PICKAXE) && !isPlayerGetSomething)
            {
                playerItemSystem.AttachItem(nearestObject);
                //SendItemAttachedToServer(nearestObject.GetComponent<Items>());
                SendItemAttachedToServer(playerItemSystem.GetCurrentEquipItemClass());
                isPlayerGetSomething = true;
            }

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 조합대이며, 아이템을 들고 있을 때
            // 또한 투입할 때 플레이어의 손에서 Detach
            else if (nearestObject.CompareTag(Define.Tag.CRAFT_TABLE) && isPlayerGetSomething)
            {
                Crafting craftTable = nearestObject.GetComponent<Crafting>();
                isPlayerGetSomething = false;
                craftTable.AddCraftItems(playerItemSystem.currentEquipItem);
                playerItemSystem.DetachItem();
            }

            // 그 외에는 처리하지 않음
        }
    }

    // F키 상호작용
    private void KeyFInteract()
    {
        if (playerInput.GetIsThrowOrCancel())
        {
            playerInput.MakeIsThrowOrCancelFalse();

            if (nearestObject == null || nearestObject != null && !nearestObject.CompareTag(Define.Tag.CRAFT_TABLE))
            {
                // 아이템을 던지고, 플레이어의 손에서 Detach
                if (isPlayerGetSomething)
                {
                    isPlayerGetSomething = false;
                    SendItemDetatchedToServer(playerItemSystem.GetCurrentEquipItemClass());
                    playerItemSystem.ThrowItem(GetMovingAmount());
                    playerItemSystem.DetachItem();
                }
                return;
            }

            else if (nearestObject.CompareTag(Define.Tag.CRAFT_TABLE))
            {
                Crafting craftTable = nearestObject.GetComponent<Crafting>();
                craftTable.RemoveAllItems();
            }
        }
    }

    // 구형 트리거를 생성해 감지합니다.
    // OnTrigger는 리스트 반환이 불가능해 OverlapSphere를 사용합니다.
    private void SphereTriggerFunc()
    {
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, DETECT_RADIUS);
        float nearestDist = Mathf.Infinity;
        GameObject foundObject = null;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag(Define.Tag.ITEM) || col.CompareTag(Define.Tag.CRAFT_TABLE) || col.CompareTag(Define.Tag.PICKAXE))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    foundObject = col.gameObject;
                }
            }
        }

        nearestObject = foundObject;
    }
    
    // 현재 콜라이더 탐지 범위를 시각화해 디버깅합니다.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DETECT_RADIUS);
    }

    // TODO : 처음 접속했을 때 위치가 초기화되어야 하는데 잘 안된다.
    private void SendEnterPosToServer()
    {
        NetManager.Instance.SendMove(transform.position, transform.rotation);

        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;
        _lastSendTime = Time.time;
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
            NetManager.Instance.SendMove(transform.position, transform.rotation);

            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;


            _lastSendTime = Time.time;
        }
    }

    // 애니메이션 상태 패킷 전송
    private void SendAnimationToServer()
    {
        AnimState currentState = playerAnimator.GetAnimState();

        // 상태가 바뀐 경우에만 전송
        if (currentState != lastAnimState)
        {
            NetManager.Instance.SendAnimation(currentState);
            lastAnimState = currentState;
        }
    }

    // 아이템을 들어올렸을 때 RemotePlayer의 소켓에 부착시키기 위해 패킷을 1회 전송
    private void SendItemAttachedToServer(Items data)
    {
        NetManager.Instance.SendItemAttached(data);
    }

    // 아이템을 내려놓을 때 RemotePlayer의 소켓에서 분리시키기 위해 패킷을 1회 전송
    private void SendItemDetatchedToServer(Items data)
    {
        NetManager.Instance.SendItemDetatched(data);
    }

    //private void SendPlayerStatToServer()
    //{
    //    float currentOxygen = playerStat.GetOxygen();
    //    int currentHp = playerStat.GetHp();

    //    if (currentOxygen != lastOxygen && currentHp != lastHP)
    //    {
    //        NetManager.Instance.SendPlayerStat(currentHp, currentOxygen);
    //        lastOxygen = currentOxygen;
    //        lastHP = currentHp;
    //    }
    //}
}
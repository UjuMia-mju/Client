using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Protocol;
using TMPro;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(50)]
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

    private const float DETECT_RADIUS = 2.2f; // 구형 트리거 반지름 
    private const float THROW_IGNORE_COLLISION_DURATION = 0.65f; // 던진 후 충돌 무시 시간
    private const string SOCKET = "Socket"; // 아이템이 플레이어 손에 들려있는 상태를 판단하기 위한 부모 이름 상수

    public bool isPlayerGetSomething = false;
    public bool isUsingTool { get; private set; } = false;
    private bool isPlayerThrowSomething = false;    // 무언가를 던지는 플래그
    
    private PlayerStat playerStat;
    private FootstepEmitter footstepEmitter;

    private GameObject playerMesh;
    
    // 서버 관련 변수들
    public float sendInterval = 0.05f; // 20fps로 위치 전송 (네트워크 부하 고려)
    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    private AnimState lastAnimState;
    private int lastHP;
    private float lastOxygen;

    [Header("Player UI")]
    [SerializeField] private HPUIController hpUIController;
    [SerializeField] private OxygenUIController oxygenUIController;
    [SerializeField] private PlayerDamageOverlayController damageOverlayController;
    [SerializeField] private TextMeshProUGUI NicknameText;
    [SerializeField] private ThrowTrajectoryPreview trajectoryPreview;
    [Header("Hit SFX")]
    [SerializeField] private string hitStunSfxName = "PlayerHitStun";
    [SerializeField, Range(0f, 1f)] private float hitStunSfxVolumeScale = 1f;
    [SerializeField, Tooltip("경직음 연속 재생 방지용 최소 간격(초)")]
    private float hitStunSfxCooldown = 0.1f;

    [Header("Oxygen Tuning")]
    [Tooltip("1초당 자연 감소량 (0~1 정규화)")]
    [SerializeField, Range(0f, 0.5f)] public float oxygenDecreasePerTick = 0.01f;

    [Tooltip("우주선 회복 영역에서 1초당 회복량")]
    [SerializeField, Range(0f, 0.5f)] public float oxygenIncreasePerTick = 0.02f;

    [Tooltip("감소/회복 코루틴 틱 간격(초)")]
    [SerializeField, Range(0.1f, 5f)] public float oxygenTickInterval = 1.0f;

    private float externalFreezeUntil = 0f;
    private bool isExternallyFrozen => Time.time < externalFreezeUntil;
    private float nextHitStunSfxTime = 0f;

    private static bool s_stageStatResetDone = false;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerItemSystem = GetComponent<PlayerItemSystem>();
        playerTPCamera = Camera.main.GetComponent<PlayerTPCamera>();
        footstepEmitter = GetComponent<FootstepEmitter>();

        if (trajectoryPreview == null)
            trajectoryPreview = GetComponent<ThrowTrajectoryPreview>();
        if (trajectoryPreview == null)
            trajectoryPreview = gameObject.AddComponent<ThrowTrajectoryPreview>();

        playerAnimator.Initialize();
        lastAnimState = AnimState.Idle;

        if (ConnectManager.Instance.isHost)
        {
            playerStat = gameObject.AddComponent<HostPlayerStat>();
            HostStatManager.Instance.RegisterPlayer((ulong)NetManager.Instance._playerId, playerStat);
        }
        else
        {
            playerStat = gameObject.AddComponent<PeerPlayerStat>();
            // 피어는 _playerId가 S_PLAYER_ENTER 수신 후에 확정되므로 확정된 시점에 등록한다.
            // 등록되어야 PeerStatManager.UpdateStat → PlayerStat.OnHpChanged 까지 전파되어 HP UI가 갱신된다.
            StartCoroutine(RegisterPeerStatWhenIdReady());
        }

        // ==== 임시 UI 초기화 ===
        if (damageOverlayController == null)
            damageOverlayController = GetComponent<PlayerDamageOverlayController>();
        if (damageOverlayController == null)
            damageOverlayController = gameObject.AddComponent<PlayerDamageOverlayController>();

        hpUIController.SetPlayerStat(playerStat);
        oxygenUIController.playerStat = playerStat;
        damageOverlayController?.SetPlayerStat(playerStat);

        hpUIController.gameObject.SetActive(true);
        oxygenUIController.gameObject.SetActive(true);
        // =====

        if (InputManager.IsGameplaySuppressed && playerInput != null)
            playerInput.SetInputEnabled(false);
    }

    private void Start()
    {
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;

        if (playerMesh == null)
        {
            Transform t = transform.Find("PlayerMesh");
            if (t != null)
                playerMesh = t.gameObject;
        }

        if (damageOverlayController != null && playerMesh != null)
            damageOverlayController.SetMeshRoot(playerMesh.transform);

        playerStat.OnHpChanged += HandleHpChanged;
        playerStat.OnOxygenChanged += HandleOxygenChanged;
        playerStat.OnPlayerDead += HandlePlayerDead;
        playerStat.OnPlayerRevive += HandlePlayerRevive;

        // 스테이지 선택 → 멀티 시작 동기화(ReadyToStartPanel) 후에 네트워크·산소 감소를 켭니다.
        GameplayReadyCoordinator.WhenGateReleased(OnNetworkReadyAfterReadyGate);
        InputManager.WhenBecameUnblocked(OnInputUnblockedForGameplay);

        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnPlayerHitEvent += OnPlayerHitReceived;
            HostPacketHandler.Instance.OnEnterGameEvent += OnEnterGameApplyLocalNickname;
        }

        // 씬 로드 후 서버/호스트에 입장을 알립니다.
        // (기존: ConnectManager.Start()에서 호출 → 자동 로그인 제거 때 함께 삭제됨)
        OnNetworkReady();
    }

    void OnInputUnblockedForGameplay()
    {
        if (this == null) return;
        if (playerStat != null && playerStat.statData.hp <= 0) return;
        if (playerInput != null)
            playerInput.SetInputEnabled(true);
    }

    // ===== 스테이지 stat 리셋 가드 =====
    // false → true : OnNetworkReadyAfterReadyGate 에서 호스트일 때 1회 (이 스테이지에서 적용 완료)
    // true  → false: 씬이 새로 로드될 때 OnSceneLoaded_ResetStageGuard 에서 명시적으로 되돌림
    // static + SceneManager.sceneLoaded 구독으로 양쪽 전이 지점이 코드에 명확히 드러나게 한다.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InstallSceneLoadHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_ResetStageGuard; // 도메인 리로드 후 중복 구독 방지
        SceneManager.sceneLoaded += OnSceneLoaded_ResetStageGuard;
        s_stageStatResetDone = false;
    }

    private static void OnSceneLoaded_ResetStageGuard(Scene scene, LoadSceneMode mode)
    {
        s_stageStatResetDone = false;
        Debug.Log($"[Player] s_stageStatResetDone = false (scene loaded: {scene.name})");
    }

    void OnNetworkReadyAfterReadyGate()
    {
        if (this == null) return;

        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnPlayerHitEvent += OnPlayerHitReceived;
        }

        // 새 스테이지 시작 시점에 호스트가 모든 플레이어 stat 을 풀 회복으로 초기화.
        // 매니저가 DontDestroyOnLoad 라 이전 스테이지의 hp/oxygen 이 새는 문제를 차단한다.
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost && !s_stageStatResetDone)
        {
            s_stageStatResetDone = true;
            HostStatManager.Instance?.ResetAndBroadcastAll();
            Debug.Log("[Player] s_stageStatResetDone = true (host reset broadcasted)");
        }

        OnNetworkReady();
    }

    // 이벤트 구독 해제 0324 (추가)
    private void OnDestroy()
    {
        if (playerStat != null)
        {
            playerStat.OnHpChanged -= HandleHpChanged;
            playerStat.OnOxygenChanged -= HandleOxygenChanged;
            playerStat.OnPlayerDead -= HandlePlayerDead;
            playerStat.OnPlayerRevive -= HandlePlayerRevive;
        }

        InputManager.CancelWhenBecameUnblocked(OnInputUnblockedForGameplay);
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnPlayerHitEvent -= OnPlayerHitReceived;
            HostPacketHandler.Instance.OnEnterGameEvent -= OnEnterGameApplyLocalNickname;
        }
    }


    private void HandlePlayerDead()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);

        PlayerOverheadUI.SetWorldCanvasActive(transform, false);

        if (playerInput != null)
            playerInput.SetInputEnabled(false);

        // [추가] 사망 시 들고 있던 아이템 강제 드롭.
        //   미처리 시 currentEquipItem 이 소켓 자식으로 남아 부활 후에도 손에 매달려 따라다니는 문제 발생.
        DropHeldItemForDeath();
    }

    /// <summary>
    /// 사망 시 들고 있던 아이템을 떨군다.
    /// 호스트: 권위 측이므로 detach 브로드캐스트 + 물리 throw.
    /// 피어:   호스트 OtherPlayers 대역이 권위 처리하므로 시각적 detach 만 수행.
    /// </summary>
    private void DropHeldItemForDeath()
    {
        if (playerItemSystem == null || playerItemSystem.currentEquipItem == null) return;

        Items heldItem = playerItemSystem.GetCurrentEquipItemClass();

        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
        {
            if (heldItem != null)
                SendItemDetatchedToServer(heldItem, false);
            playerItemSystem.ThrowItem(0f); // runningAmount=0 → 제자리 약한 낙하
        }
        else
        {
            playerItemSystem.DetachForRemoteSync();
        }

        isPlayerGetSomething = false;
    }

    private void HandlePlayerRevive()
    {
        if (playerMesh != null)
            playerMesh.SetActive(true);

        PlayerOverheadUI.SetWorldCanvasActive(transform, true);

        if (playerInput != null)
            playerInput.SetInputEnabled(true);
    }

    // 체력 변경 이벤트 핸들러 0324 (추가)
    private void HandleHpChanged(int newHp)
    {
        if (lastHP != newHp)
        {
            lastHP = newHp;
        }
    }

    // 산소 변경 이벤트 핸들러 0324 (추가)
    private void HandleOxygenChanged(float newOxygen)
    {
        // 서버 부하를 줄이기 위해 소수점 단위 변화가 클 때만 보낼 수도 있습니다.
        if (Mathf.Abs(lastOxygen - newOxygen) > 0.01f)
        {
            lastOxygen = newOxygen;
        }
    }

    public void OnNetworkReady()
    {
        Debug.Log($"[Player] OnNetworkReady. isHost={ConnectManager.Instance.isHost}");

        if (ConnectManager.Instance.isHost)
        {
            // 호스트: 자신의 입장을 피어들에게 즉시 알림 (S_PLAYER_ANIMATION보다 반드시 먼저 전송)
            PacketSender.Instance.BroadcastPlayerEnter((ulong)NetManager.Instance._playerId);
        }
        else
        {
            // 피어: C_ENTER_GAME 전송 → 호스트가 S_ENTER_GAME으로 전체 플레이어 목록 응답
            PacketSender.Instance.SendEnterGame();
        }

        SendEnterPosToServer();
        playerStat.StartOxygenDecrease();

        RefreshLocalNicknameFromRoomCache();
    }

    private void OnEnterGameApplyLocalNickname(S_ENTER_GAME packet)
    {
        if (packet == null || NicknameText == null || NetManager.Instance == null)
            return;

        ulong myId = (ulong)NetManager.Instance._playerId;
        foreach (var p in packet.Players)
        {
            if ((ulong)p.PlayerId != myId) continue;
            if (!string.IsNullOrWhiteSpace(p.Name))
                NicknameText.text = RoomMemberDisplayCache.WithoutDiscriminatorTag(p.Name.Trim());
            else
                RefreshLocalNicknameFromRoomCache();
            return;
        }

        RefreshLocalNicknameFromRoomCache();
    }

    private void RefreshLocalNicknameFromRoomCache()
    {
        if (NicknameText == null || NetManager.Instance == null) return;

        ulong id = (ulong)NetManager.Instance._playerId;
        if (id == 0) return;

        RoomMemberDisplayCache.Instance?.WarmUp();
        if (RoomMemberDisplayCache.Instance != null &&
            RoomMemberDisplayCache.Instance.TryGet(id, out var entry) &&
            !string.IsNullOrWhiteSpace(entry.DisplayName))
            NicknameText.text = RoomMemberDisplayCache.WithoutDiscriminatorTag(entry.DisplayName.Trim());
    }


    // 플레이어 인풋, 레이캐스트, 애니메이션 업데이트
    private void Update()
    {
        playerInput.InputProcess();

        inputFreeze = CollisionDetectWithRaycast(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir), wallMask, walkable);

        if (Time.time < externalFreezeUntil)
        {
            inputFreeze = true;
        }

        playerAnimator.SurpriseAnimation(isExternallyFrozen);

        if (!inputFreeze)
        {
            GroundDetectingWithRaycast(groundMask | walkable | hillMask);

            // 들고 있는 도구가 Shovel이면 Mining 대신 Digging 애니메이션을 재생
            bool isHoldingShovel = playerItemSystem != null
                && playerItemSystem.currentEquipItem != null
                && playerItemSystem.currentEquipItem.GetComponent<Shovel>() != null;

            bool isDigging = isUsingTool && isHoldingShovel;
            bool isMining = isUsingTool && !isHoldingShovel;

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                isGrounded,
                inputFreeze,
                isMining,
                IsHoldingThrowInput(),
                WasThrowReleasedThisFrame(),
                isDigging);

            KeyEInteract();
            KeyLeftClickInteract();
            TryChargedAimThrow();
            KeyFInteract();
            UpdateThrowAimPreview();
        }
        else
        {
            if (trajectoryPreview != null)
                trajectoryPreview.Hide();
            if (playerTPCamera != null)
                playerTPCamera.SetThrowAimZoom(false);
        }

        // 현재 땅을 밟았는지 안 밟았는지와는 무관하게 레이캐스트를 길게 펼쳐 해당 지면의 접지면 벡터를 구합니다.
        GetGroundNormal(groundMask);
        SphereTriggerFunc();
    }

    // E키 상호작용
    private void KeyEInteract()
    {
        if (playerInput.GetIsInteract())
        {
            playerInput.MakeIsInteractFalse();

            if (nearestObject == null)
            {
                return;
            }

            else if (nearestObject.CompareTag(Define.Tag.ITEM) && !isPlayerGetSomething && !isPlayerThrowSomething ||
                nearestObject.CompareTag(Define.Tag.TOOL) && !isPlayerGetSomething && !isPlayerThrowSomething)
            {
                isPlayerGetSomething = true;
                playerItemSystem.AttachItem(nearestObject);
                SendItemAttachedToServer(playerItemSystem.GetCurrentEquipItemClass());
            }

            else if (nearestObject.CompareTag(Define.Tag.CRAFT_TABLE) && isPlayerGetSomething)
            {
                Crafting craftTable = nearestObject.GetComponent<Crafting>();
                isPlayerGetSomething = false;
                craftTable.AddCraftItems(playerItemSystem.currentEquipItem);
                playerItemSystem.DetachItem();
            }

            else if (nearestObject.CompareTag(Define.Tag.FURNACE))
            {
                FurnaceObject furnace = nearestObject.GetComponent<FurnaceObject>();

                if (furnace != null)
                {
                    Debug.Log($"[Player] Furnace interact: furnaceId={furnace.furnaceId}, hasResult={furnace.hasResult}, isWorking={furnace.isWorking}, isPlayerGetSomething={isPlayerGetSomething}");

                    if (furnace.hasResult || (!furnace.isWorking && !isPlayerGetSomething))
                    {
                        furnace.RequestRetrieve();
                        Debug.Log("용광로 결과물 수거 요청!");
                    }
                    else if (!furnace.isWorking && isPlayerGetSomething)
                    {
                        Items currentItem = playerItemSystem.GetCurrentEquipItemClass();
                        if (currentItem == null) return;

                        if (furnace.RequestSmelt(currentItem.itemId))
                        {
                            if (ConnectManager.Instance.isHost)
                            {
                                // SendObjectDestroy는 FurnaceServerManager에서 처리
                                // 호스트는 로컬 아이템만 파괴
                                Destroy(playerItemSystem.currentEquipItem);
                                isPlayerGetSomething = false;
                                playerItemSystem.DetachItem();
                            }
                            // 피어: 로컬 처리 없음
                            // FurnaceServerManager -> SendObjectDestroy 브로드캐스트
                            // → OnHostObjectDestroy -> 아이템 파괴 + DetachItem
                        }
                    }
                    else
                    {
                        MessageManager.TryShowKey(MessageKeys.FurnaceStillProcessing);
                        Debug.Log("아직 용광로가 이전 작업을 처리 중입니다!");
                    }
                }
            }

            else if (nearestObject.CompareTag(Define.Tag.SPACESHIP) && isPlayerGetSomething)
            {
                SpaceshipAssembly spaceshipAssembly = nearestObject.GetComponent<SpaceshipAssembly>();

                Items currentItem = playerItemSystem.GetCurrentEquipItemClass();
                if (currentItem == null) return;

                if (ConnectManager.Instance.isHost)
                {
                    bool success = spaceshipAssembly.AddTargetItems(playerItemSystem.currentEquipItem);
                    if (success)
                    {
                        isPlayerGetSomething = false;
                        playerItemSystem.DetachItem();
                    }
                }
                else
                {
                    PacketSender.Instance.SendSpaceshipInsert(currentItem.itemStringKey, currentItem.itemId);
                }
            }
        }
    }

    // 좌클릭 상호작용
    private void KeyLeftClickInteract()
    {
        if (playerInput.GetIsLeftClick())
        {
            playerInput.MakeIsLeftClickFalse();

            // 도구
            if (playerItemSystem.GetItemTag() != null && playerItemSystem.GetItemTag().Equals(Define.Tag.TOOL) && isPlayerGetSomething && !isPlayerThrowSomething)
            {
                isUsingTool = true;
            }
        }
    }

    // 물리 작용 업데이트
    private void FixedUpdate()
    {
        RotateToDirection(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));

        if (!inputFreeze)
        {
            Moving(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));

            if (playerInput.GetIsJumping() && isGrounded && !isUsingTool)
            {
                Jump();
                playerInput.SetIsJumping(false);
                EndMining();
            }
            else if (!isGrounded)
            {
                playerInput.SetIsJumping(false);
            }
        }
        else
        {
            // [수정] freeze 중에는:
            //   - 수평 속도 0
            //   - 위 방향 속도도 0 (벽 비비며 위로 기어오르기 차단)
            //   - 아래 방향 속도(중력 낙하)만 유지
            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;
                float upDot = Vector3.Dot(v, transform.up);
                if (upDot > 0f) upDot = 0f; // 위로 가는 성분 제거, 낙사는 유지
                rb.linearVelocity = transform.up * upDot;
            }
        }
    }

    private void LateUpdate()
    {
        if (ConnectManager.Instance.isHost)
        {
            BroadcastPositionToPeers();
            BroadcastAnimationToPeers();
            return;
        }

        SendPositionToServer();
        SendAnimationToServer();
    }

    // 호스트 전용: 자신의 위치를 피어들에게 브로드캐스트
    private void BroadcastPositionToPeers()
    {
        if (Time.time - _lastSendTime < sendInterval)
            return;

        bool posChanged = Vector3.Distance(transform.position, _lastSendPos) > 0.01f;
        bool rotChanged = Quaternion.Angle(transform.rotation, _lastSendRot) > 0.5f;

        if (posChanged || rotChanged)
        {
            PacketSender.Instance.BroadcastMove(transform.position, transform.rotation);

            _lastSendPos = transform.position;
            _lastSendRot = transform.rotation;
            _lastSendTime = Time.time;
        }
    }

    // 호스트 전용: 자신의 애니메이션을 피어들에게 브로드캐스트
    private void BroadcastAnimationToPeers()
    {
        AnimState currentState = playerAnimator.GetAnimState();
        if (currentState != lastAnimState)
        {
            PacketSender.Instance.BroadcastAnimation(currentState);
            lastAnimState = currentState;
        }
    }

    protected override void Moving(Vector3 movDir)
    {
        if (movDir != Vector3.zero)
        {
            EndMining();
        }

        if (movDir == Vector3.zero || rb == null)
        {
            base.Moving(movDir);
            return;
        }

        movDir.Normalize();
        float dist = walkSpeed * Time.fixedDeltaTime;

        // 1) sweep으로 진행 경로에 충돌이 있는지 확인.
        if (rb.SweepTest(movDir, out RaycastHit hit, dist + 0.05f, QueryTriggerInteraction.Ignore))
        {
            // [변경] Hill 레이어인지 판별 → 위 성분 제거를 건너뛸지 결정.
            bool isHill = hit.collider != null
                && (hillMask.value & (1 << hit.collider.gameObject.layer)) != 0;

            Vector3 slideDir = Vector3.ProjectOnPlane(movDir, hit.normal);

            // Hill이 아니면(=벽) 위 방향 성분 제거. Hill이면 그대로 둬서 경사 등반 허용.
            if (!isHill)
            {
                float upDot = Vector3.Dot(slideDir, transform.up);
                if (upDot > 0f) slideDir -= transform.up * upDot;
            }

            if (slideDir.sqrMagnitude < 1e-4f) return;
            slideDir.Normalize();

            float slideDist = dist;
            if (rb.SweepTest(slideDir, out RaycastHit hit2, dist + 0.05f, QueryTriggerInteraction.Ignore))
            {
                slideDist = Mathf.Max(0f, hit2.distance - 0.02f);
            }
            rb.MovePosition(rb.position + slideDir * slideDist);
            return;
        }

        rb.MovePosition(rb.position + movDir * dist);
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
                    // 우클릭 조준 중에는 F로 약한 던지기 하지 않음(강한 던지기는 좌클릭)
                    if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                        return;

                    isPlayerGetSomething = false;
                    SendItemDetatchedToServer(playerItemSystem.GetCurrentEquipItemClass(), false);
                    playerItemSystem.ThrowItem(GetMovingAmount());
                    StartCoroutine(IgnoreItemCollisionAfterThrow(playerItemSystem.GetLastThrownItem()));
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
            if (col == null)
            {
                Debug.Log("콜라이더 null 감지");
                continue;
            }
            if (col.CompareTag(Define.Tag.ITEM) || col.CompareTag(Define.Tag.CRAFT_TABLE) || col.CompareTag(Define.Tag.TOOL) || col.CompareTag(Define.Tag.FURNACE)
                || col.CompareTag(Define.Tag.SPACESHIP))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    if (col.CompareTag(Define.Tag.ITEM) || col.CompareTag(Define.Tag.TOOL))
                    {
                        Items items = col.GetComponent<Items>();
                        if (items != null &&
                            items.transform.parent != null &&
                            items.gameObject.transform.parent.name.Contains(SOCKET))
                        {
                            continue;
                        }
                    }
                    nearestDist = dist;
                    foundObject = col.gameObject;
                }
            }
        }

        nearestObject = foundObject;
    }
    
    // 현재 콜라이더 탐지 범위를 시각화해 디버깅합니다(MovingObject 충돌/지면 레이 기즈모 포함).
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DETECT_RADIUS);
    }

    public void EndMining()
    {
        if (playerItemSystem.currentEquipItem != null && playerItemSystem.currentEquipItem.CompareTag(Define.Tag.TOOL))
        {
            Pickaxe tempP = playerItemSystem.currentEquipItem.GetComponent<Pickaxe>();
            if (tempP != null)
            {
                tempP.ResetHasMined();
            }
            else if (playerItemSystem.currentEquipItem.GetComponent<Axe>() != null)
            {
                Axe tempA = playerItemSystem.currentEquipItem.GetComponent<Axe>();
                tempA.ResetHasChopped();
            }
            else if (playerItemSystem.currentEquipItem.GetComponent<Shovel>() != null)
            {
                Shovel tempS = playerItemSystem.currentEquipItem.GetComponent<Shovel>();
                tempS.ResetHasDug();
            }
        }

        isUsingTool = false;
    }

    private IEnumerator IgnoreItemCollisionAfterThrow(GameObject thrownItem)
    {
        if (thrownItem == null) yield break;

        isPlayerThrowSomething = true;
        SetIgnoreCollisionWithPlayer(thrownItem, true);
        yield return new WaitForSeconds(THROW_IGNORE_COLLISION_DURATION);
        SetIgnoreCollisionWithPlayer(thrownItem, false);
        isPlayerThrowSomething = false;
    }

    /// <summary>
    /// 플레이어/아이템 계층의 모든 콜라이더 쌍에 IgnoreCollision을 적용합니다.
    /// 루트 콜라이더 하나만 무시하면 자식 콜라이더와 재충돌해 튕기는 문제가 발생할 수 있습니다.
    /// </summary>
    private void SetIgnoreCollisionWithPlayer(GameObject thrownItem, bool ignore)
    {
        if (thrownItem == null) return;

        Collider[] playerColliders = GetComponentsInChildren<Collider>(true);
        Collider[] itemColliders = thrownItem.GetComponentsInChildren<Collider>(true);
        if (playerColliders == null || itemColliders == null) return;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null) continue;

            for (int j = 0; j < itemColliders.Length; j++)
            {
                Collider itemCollider = itemColliders[j];
                if (itemCollider == null) continue;

                Physics.IgnoreCollision(playerCollider, itemCollider, ignore);
            }
        }
    }


    private void SendEnterPosToServer()
    {
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            PacketSender.Instance.BroadcastMove(transform.position, transform.rotation);
        else
            PacketSender.Instance.SendMove(transform.position, transform.rotation);

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
            PacketSender.Instance.SendMove(transform.position, transform.rotation);

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
            PacketSender.Instance.SendAnimation(currentState);
            lastAnimState = currentState;
        }
    }

    // 아이템을 들어올렸을 때 RemotePlayer의 소켓에 부착시키기 위해 패킷을 1회 전송
    private void SendItemAttachedToServer(Items data)
    {
        PacketSender.Instance.SendItemAttached(data);
    }

    // 아이템을 내려놓을 때 RemotePlayer의 소켓에서 분리시키기 위해 패킷을 1회 전송
    private void SendItemDetatchedToServer(Items data, bool charged)
    {
        PacketSender.Instance.SendItemDetatched(data, charged);
    }

    private bool IsHoldingThrowInput()
    {
        if (Mouse.current == null) return false;
        return isPlayerGetSomething &&
               playerItemSystem.currentEquipItem != null &&
               !isPlayerThrowSomething &&
               Mouse.current.rightButton.isPressed;
    }

    private bool WasThrowReleasedThisFrame()
    {
        if (Mouse.current == null) return false;
        return Mouse.current.rightButton.isPressed &&
               Mouse.current.leftButton.wasPressedThisFrame &&
               isPlayerGetSomething &&
               playerItemSystem.currentEquipItem != null &&
               !isPlayerThrowSomething &&
               (nearestObject == null || !nearestObject.CompareTag(Define.Tag.CRAFT_TABLE));
    }

    private void TryChargedAimThrow()
    {
        if (Mouse.current == null)
            return;
        if (!Mouse.current.rightButton.isPressed || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (!isPlayerGetSomething || playerItemSystem.currentEquipItem == null || isPlayerThrowSomething)
            return;
        if (nearestObject != null && nearestObject.CompareTag(Define.Tag.CRAFT_TABLE))
            return;



        isPlayerGetSomething = false;
        SendItemDetatchedToServer(playerItemSystem.GetCurrentEquipItemClass(), true);
        playerItemSystem.ThrowChargedAim(GetMovingAmount(), GetThrowAimDirection());
        if (trajectoryPreview != null)
            trajectoryPreview.Hide();
        StartCoroutine(IgnoreItemCollisionAfterThrow(playerItemSystem.GetLastThrownItem()));

    }


    private void UpdateThrowAimPreview()
    {
        bool aimZoom = false;

        if (trajectoryPreview != null && Mouse.current != null)
        {
            bool holdingItem = isPlayerGetSomething && playerItemSystem.currentEquipItem != null && !isPlayerThrowSomething;
            if (holdingItem && Mouse.current.rightButton.isPressed)
            {
                aimZoom = true;
                Vector3 aimDir = GetThrowAimDirection();
                Vector3 impulse = playerItemSystem.ComputeThrowImpulse(GetMovingAmount(), aimDir, chargedThrow: true);
                float mass = playerItemSystem.GetHeldItemMass();
                Vector3 v0 = impulse / mass;
                trajectoryPreview.ShowTrajectory(playerItemSystem.GetThrowStartPosition(), v0, mass);
            }
            else
                trajectoryPreview.Hide();
        }
        else if (trajectoryPreview != null)
            trajectoryPreview.Hide();

        if (playerTPCamera != null)
            playerTPCamera.SetThrowAimZoom(aimZoom);
    }

    private Vector3 GetThrowAimDirection()
    {
        Vector3 up = transform.up;
        Vector3 flat = Vector3.ProjectOnPlane(transform.forward, up);
        if (flat.sqrMagnitude < 1e-4f)
            flat = Vector3.ProjectOnPlane(transform.right, up);
        flat.Normalize();
        return flat;
    }

    public void FreezeFor(float seconds)
    {
        externalFreezeUntil = Mathf.Max(externalFreezeUntil, Time.time + seconds);
        if (!string.IsNullOrEmpty(hitStunSfxName) && Time.time >= nextHitStunSfxTime)
        {
            nextHitStunSfxTime = Time.time + Mathf.Max(0f, hitStunSfxCooldown);
            SoundManager.Instance?.PlaySFXAt(
                hitStunSfxName,
                transform.position,
                volumeScale: hitStunSfxVolumeScale,
                minPitch: 0.98f,
                maxPitch: 1.02f,
                minDistance: 2f,
                maxDistance: 14f);
        }
    }

    /// <summary>
    /// 일정 시간 동안 이동 입력을 랜덤 패턴으로 섞습니다.
    /// </summary>
    public void ApplyMoveInputScramble(float seconds)
    {
        playerInput?.ApplyRandomMoveScramble(seconds);
    }

    private void OnPlayerHitReceived(S_PLAYER_HIT packet)
    {
        if (packet.VictimPlayerId != (ulong)NetManager.Instance._playerId) return;
        FreezeFor(packet.FreezeSeconds);
    }

    /// <summary>
    /// Run 애니메이션 이벤트 수신 지점.
    /// FootstepEmitter가 연결되어 있으면 실제 파티클 재생을 위임한다.
    /// </summary>
    public void OnFootstep()
    {
        if (footstepEmitter == null)
            footstepEmitter = GetComponent<FootstepEmitter>();

        if (footstepEmitter != null)
            footstepEmitter.OnFootstep();
    }

    private IEnumerator RegisterPeerStatWhenIdReady()
    {
        while (NetManager.Instance == null || NetManager.Instance._playerId == 0)
            yield return null;

        if (PeerStatManager.Instance != null && playerStat != null)
            PeerStatManager.Instance.RegisterPlayer((ulong)NetManager.Instance._playerId, playerStat);
    }

    public void FindThrowingWeaponAndTriggerIt()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        Transform socketTransform = null;
        foreach (Transform child in children)
        {
            if (child.name == "SOCKET" || child.name == "Socket")
            {
                socketTransform = child;
                break;
            }
        }

        if (socketTransform == null) return;

        foreach (Transform child in socketTransform)
        {
            if (!child.name.StartsWith("Aqua"))
            {
                continue;
            }

            // Aqua의 바로 아래 자식 중에서 BossDamageTrigger를 찾음
            Transform boxTransform = child.Find("BossDamageTrigger");
            if (boxTransform == null)
            {
                continue;
            }

            Aquamarine aquamarine = boxTransform.GetComponent<Aquamarine>();
            if (aquamarine != null)
            {
                aquamarine.SetActiveAquaTrigger();
            }
        }
    }
}
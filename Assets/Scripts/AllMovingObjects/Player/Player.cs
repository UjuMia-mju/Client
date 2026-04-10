using UnityEngine;
using System.Collections;

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

    public bool isPlayerGetSomething { get; private set; } = false;
    public bool isMining { get; private set; } = false;
    private bool isPlayerThrowSomething = false;    // 무언가를 던지는 플래그
    
    private PlayerStat playerStat;

    private GameObject playerMesh;


    // 서버 관련 변수들
    public float sendInterval = 0.05f; // 20fps로 위치 전송 (네트워크 부하 고려)
    protected float _lastSendTime = 0f;
    protected Vector3 _lastSendPos;
    protected Quaternion _lastSendRot;

    private AnimState lastAnimState;
    private int lastHP;
    private float lastOxygen;

    // 임시 UI 객체
    [SerializeField] private HPUIController hpUIController;
    [SerializeField] private OxygenUIController oxygenUIController;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerItemSystem = GetComponent<PlayerItemSystem>();
        playerTPCamera = Camera.main.GetComponent<PlayerTPCamera>();

        playerAnimator.Initialize();
        lastAnimState = AnimState.Idle;

        if (ConnectManager.Instance.isHost)
        {
            playerStat = gameObject.AddComponent<HostPlayerStat>();
            HostStatManager.Instance.RegisterPlayer(playerStat.playerId, playerStat);
        }
        else
        {
            playerStat = gameObject.AddComponent<PeerPlayerStat>();
        }

        // ==== 임시 UI 초기화 ===
        hpUIController.playerStat = playerStat;
        oxygenUIController.playerStat = playerStat;

        hpUIController.gameObject.SetActive(true);
        oxygenUIController.gameObject.SetActive(true);
        // =====
    }

    private void Start()
    {
        _lastSendPos = transform.position;
        _lastSendRot = transform.rotation;

        // 자동으로 자식 중 PlayerMesh를 찾아 할당 (Inspector에 없으면)
        if (playerMesh == null)
        {
            Transform t = transform.Find("PlayerMesh");
            if (t != null)
                playerMesh = t.gameObject;
        }


        // 산소/HP 이벤트 기반 로직
        // lastHP = playerStat.GetHp();
        // lastOxygen = playerStat.GetOxygen();

        playerStat.OnHpChanged += HandleHpChanged;
        playerStat.OnOxygenChanged += HandleOxygenChanged;
        playerStat.OnPlayerDead += HandlePlayerDead;
        playerStat.OnPlayerRevive += HandlePlayerRevive;
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
    }


    private void HandlePlayerDead()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);

        if (playerInput != null)
            playerInput.SetInputEnabled(false);
    }

    private void HandlePlayerRevive()
    {
        if (playerMesh != null)
            playerMesh.SetActive(true);

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
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
        {
            PacketSender.Instance.SendEnterGame(0);
        }
        SendEnterPosToServer();

        // 네트워크 준비 후 산소 감소 시작 (EnterGame 패킷 이후에 산소 패킷이 전송되도록)
        playerStat.StartOxygenDecrease();
    }


    // 플레이어 인풋, 레이캐스트, 애니메이션 업데이트
    private void Update()
    {
        playerInput.InputProcess(); // 인풋, 충돌 감지는 Input이 되지 않으면 레이캐스트가 멈추므로 가장 먼저 처리합니다.

        // 충돌 감지
        inputFreeze = CollisionDetectWithRaycast(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir), wallMask, walkable);

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
        RotateToDirection(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));
        if (!inputFreeze)
        {
            Moving(playerTPCamera.GetPlayerMovingOffset().TransformDirection(playerInput.axisResultDir));

            if (playerInput.GetIsJumping() && isGrounded && !isMining)
            {
                Jump();
                playerInput.SetIsJumping(false);
                EndMining();
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
        base.Moving(movDir);
    }

    // E키 상호작용
    // NOTE : nearestObject는 항상 SphereTriggerFunc로 트리거 감지합니다. 뭔가 만들었는데 안된다 싶으면, 해당 함수에서 태그를 검사하고 있는지 확인해주세요!
    private void KeyEInteract()
    {
        if (playerInput.GetIsInteract())
        {
            playerInput.MakeIsInteractFalse();
            // 플레이어가 아이템을 들고 있고, 그게 어떤 도구일 때
            if (playerItemSystem.GetItemTag() != null && playerItemSystem.GetItemTag().Equals(Define.Tag.PICKAXE) && isPlayerGetSomething && !isPlayerThrowSomething)
            {
                isMining = true;
            }

            else if (nearestObject == null)
            {
                return;
            }

            // 플레이어에게서 가장.closest오브젝트의 태그가 아이템이며, 빈 손일 때
            // 혹은 태그가 Tool인 것도 포함함.
            else if (nearestObject.CompareTag(Define.Tag.ITEM) && !isPlayerGetSomething && !isPlayerThrowSomething ||
                nearestObject.CompareTag(Define.Tag.PICKAXE) && !isPlayerGetSomething && !isPlayerThrowSomething)
            {
                isPlayerGetSomething = true;
                playerItemSystem.AttachItem(nearestObject);
                //SendItemAttachedToServer(nearestObject.GetComponent<Items>());
                SendItemAttachedToServer(playerItemSystem.GetCurrentEquipItemClass());
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

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 용광로.
            else if (nearestObject.CompareTag(Define.Tag.FURNACE))
            {
                FurnaceObject furnace = nearestObject.GetComponent<FurnaceObject>();
            
                if (furnace != null)
                {
                    // 1. 용광로가 작업이 끝났으면 아이템 회수하기

                    if (furnace.hasResult)
                    {
                        // 클라이언트가 서버로 C_FURNACE_RETRIEVE 패킷을 보내도록 FurnaceObject에 함수(예: RequestRetrieve) 구현 필요
                        furnace.RequestRetrieve();
                        Debug.Log("용광로 결과물 수거 요청!");
                    }
                    else if (!furnace.isWorking && isPlayerGetSomething)
                    {
                        // 손에 든 아이템의 실제 itemId를 사용
                        Items currentItem = playerItemSystem.GetCurrentEquipItemClass();
                        if (currentItem == null) return;

                        int objectId = currentItem.itemId;

                        if (furnace.RequestSmelt(objectId))
                        {
                            Debug.Log("용광로에 아이템 투입 요청 성공!");
                            PacketSender.Instance.SendObjectDestroy(currentItem.itemId);
                            Destroy(playerItemSystem.currentEquipItem);
                            isPlayerGetSomething = false;
                            playerItemSystem.DetachItem();
                        }
                    }
                    else
                    {
                        Debug.Log("아직 용광로가 이전 작업을 처리 중입니다!");
                    }
                }
            }

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 우주선이며, 아이템을 들고 있을 때
            // 또한 투입할 때 플레이어의 손에서 Detach
            else if (nearestObject.CompareTag(Define.Tag.SPACESHIP) && isPlayerGetSomething)
            {
                SpaceshipAssembly spaceshipAssembly = nearestObject.GetComponent<SpaceshipAssembly>();

                Items currentItem = playerItemSystem.GetCurrentEquipItemClass();
                if (currentItem == null) return;

                if (ConnectManager.Instance.isHost)
                {
                    // 호스트: 직접 판정 후 브로드캐스트
                    spaceshipAssembly.AddTargetItems(playerItemSystem.currentEquipItem);
                    isPlayerGetSomething = false;
                    playerItemSystem.DetachItem();
                }
                else
                {
                    // 피어: 패킷 전송 후 로컬 정리
                    PacketSender.Instance.SendSpaceshipInsert(currentItem.itemStringKey, currentItem.itemId);
                    PacketSender.Instance.SendObjectDestroy(currentItem.itemId);
                    Destroy(playerItemSystem.currentEquipItem);
                    isPlayerGetSomething = false;
                    playerItemSystem.DetachItem();
                }

                //if (spaceshipAssembly != null)
                //{
                //    spaceshipAssembly.AddTargetItems(playerItemSystem.currentEquipItem);
                //    isPlayerGetSomething = false;
                //    playerItemSystem.DetachItem();
                //}
            }
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
            if (col.CompareTag(Define.Tag.ITEM) || col.CompareTag(Define.Tag.CRAFT_TABLE) || col.CompareTag(Define.Tag.PICKAXE) || col.CompareTag(Define.Tag.FURNACE)
                || col.CompareTag(Define.Tag.SPACESHIP))
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

    public void EndMining()
    {
        if (playerItemSystem.currentEquipItem != null && playerItemSystem.currentEquipItem.CompareTag(Define.Tag.PICKAXE))
        {
            Pickaxe tempP = playerItemSystem.currentEquipItem.GetComponent<Pickaxe>();
            if (tempP != null)
            {
                tempP.ResetHasMined();
            }
        }

        isMining = false;
    }

    private IEnumerator IgnoreItemCollisionAfterThrow(GameObject thrownItem)
    {
        if (thrownItem == null) yield break;

        Collider playerCollider = GetComponent<Collider>();
        Collider itemCollider = thrownItem.GetComponent<Collider>();

        if (playerCollider == null || itemCollider == null) yield break;

        isPlayerThrowSomething = true;
        Physics.IgnoreCollision(playerCollider, itemCollider, true);
        yield return new WaitForSeconds(THROW_IGNORE_COLLISION_DURATION);
        Physics.IgnoreCollision(playerCollider, itemCollider, false);
        isPlayerThrowSomething = false;
    }


    // TODO : 처음 접속했을 때 위치가 초기화되어야 하는데 잘 안된다.
    private void SendEnterPosToServer()
    {
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
    private void SendItemDetatchedToServer(Items data)
    {
        PacketSender.Instance.SendItemDetatched(data);
    }
}
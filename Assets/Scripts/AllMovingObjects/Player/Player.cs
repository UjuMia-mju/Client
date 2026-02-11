using UnityEngine;
using System.Collections;
using UnityEngine.Animations;

public class Player : MovingObject
{
    // 입력 동결 플래그 (충돌 시 입력 무시용이며 최종 판단을 내리는 클래스라 판단해 이곳에 선언했습니다.)
    private bool inputFreeze = false;

    // 컴포넌트 참조 변수
    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    public PlayerItemSystem playerItemSystem { get; private set; }

    public bool isGetItem { get; private set; } = false;

    // TODO : 기초 산소 시스템 구현
    //private OxygenSystem oxygenSystem;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerItemSystem = GetComponent<PlayerItemSystem>();
        //oxygenSystem = GetComponent<OxygenSystem>();

        playerAnimator.Initialize();
    }

    private void Start()
    {
        // 산소가 줄어들기 시작함
        //StartCoroutine(oxygenSystem.OxygenDecrease());
    }

    // 플레이어 인풋, 레이캐스트, 애니메이션 업데이트
    private void Update()
    {
        playerInput.InputProcess(); // 인풋, 충돌 감지는 Input이 되지 않으면 레이캐스트가 멈추므로 가장 먼저 처리합니다.

        // 충돌 감지
        inputFreeze = CollisionDetectWithRaycast(playerInput.axisResultDir, wallMask);

        if (!inputFreeze)
        {
            GroundDetectingWithRaycast(groundMask);

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                isGrounded, 
                inputFreeze);


        }

        // 현재 땅을 밟았는지 안 밟았는지와는 무관하게 레이캐스트를 길게 펼쳐 해당 지면의 접지면 벡터를 구합니다.
        GetGroundNormal(groundMask);
    }

    // 물리 작용 업데이트
    private void FixedUpdate()
    {
        if (!inputFreeze)
        {
            Moving(playerInput.axisResultDir);
            RotateToDirection(this.transform, playerInput.axisX, playerInput.axisY);

            if (playerInput.GetIsJumping())
            {
                Jump();
                playerInput.MakeIsJumpingFalse();
            }
        }

        else
        {
            rb.Sleep();
        }
    }

    // TODO : 이하 4개의 함수 모두 구현해야 함
    // 아이템 획득에 대한 함수
    public void GetItem(GameObject item)
    {
        if (playerInput.GetIsInteract())
        {
            playerInput.MakeIsInteractFalse();
            
            // 플레이어의 손에 잡힐 때 문제가 되는 요소들을 모두 비활성화
            Rigidbody rb = item.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            // 해당 오브젝트의 중력 제어 비활성화
            ObjectsGravityController objectGravityController = item.GetComponent<ObjectsGravityController>();
            objectGravityController.enabled = false;

            BoxCollider[] colliders = item.GetComponentsInChildren<BoxCollider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 아이템 시스템에 아이템 장착
            playerItemSystem.AttachItem(item);
            isGetItem = true;

            Debug.Log("아이템 획득! 이 아이템의 이름은 : " + item);
        }
    }

    // 아이템을 들고 있는지 검사하고, 들어 있다면 소켓에 있는 아이템을 Destroy 하고 isGetItem을 false로 변경
    public void Crafting(Crafting craftTable)
    {
        if (playerInput.GetIsInteract())
        {
            Debug.Log("상호작용 입력 받음");
            playerInput.MakeIsInteractFalse();

            if (isGetItem)
            {
                isGetItem = false;
                craftTable.AddCraftItems(playerItemSystem.item);
                Destroy(playerItemSystem.item);
                Debug.Log("아이템 투입 완료!");
                
            }
        }
    }

    // 미구현
    public void Throw()
    {
        if (playerInput.GetIsThrowOrCancel())
        {
            Debug.Log("던지기 또는 취소 입력 받음");
            playerInput.MakeIsThrowOrCancelFalse();
        }
    }

    // 미구현
    public void Cancel()
    {
        if (playerInput.GetIsThrowOrCancel())
        {
            Debug.Log("던지기 또는 취소 입력 받음");
            playerInput.MakeIsThrowOrCancelFalse();
        }
    }
}
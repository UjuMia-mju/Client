using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using static UnityEditor.Progress;

enum PlayerTriggetDetectedState
{
    Item,
    CraftTable,
    None
}

public class Player : MovingObject
{
    // 입력 동결 플래그 (충돌 시 입력 무시용이며 최종 판단을 내리는 클래스라 판단해 이곳에 선언했습니다.)
    private bool inputFreeze = false;

    // 컴포넌트 참조 변수
    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    public PlayerItemSystem playerItemSystem { get; private set; }

    public GameObject nearestObject { get; private set; } // 플레이어에게서 가장 가까운 오브젝트
    private PlayerTriggetDetectedState playerTriggetDetectedState = PlayerTriggetDetectedState.None; // 플레이어가 트리거로 무엇을 발견했는지 상태

    private const float DETECT_RADIUS = 5.5f; // 구형 트리거 반지름 


    public bool isGetItem { get; private set; } = false;

    // TODO : 기초 플레이어 능력치 시스템 구현 완료 - 실제로 표시되는 방식은 UI담당과 상의 필요
    //private PlayerStat playerStat;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerItemSystem = GetComponent<PlayerItemSystem>();
        //playerStat = GetComponent<PlayerStat>();

        playerAnimator.Initialize();
    }

    private void Start()
    {
        // 산소가 줄어들기 시작함
        //StartCoroutine(playerStat.OxygenDecrease());
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

    // E키 상호작용
    private void KeyEInteract()
    {
        if (playerInput.GetIsInteract())
        {
            playerInput.MakeIsInteractFalse();

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 아이템이며, 빈 손일 때
            if (nearestObject.CompareTag(Define.Tag.ITEM) && !isGetItem)
            {
                playerItemSystem.AttachItem(nearestObject);
                isGetItem = true;
            }

            // 플레이어에게서 가장 가까운 오브젝트의 태그가 조합대이며, 아이템을 들고 있을 때
            // 또한 투입할 때 플레이어의 손에서 Detach
            else if (nearestObject.CompareTag(Define.Tag.CRAFT_TABLE) && isGetItem)
            {
                Debug.Log("조합대 상호작용 입력받았습니다");
                Crafting craftTable = nearestObject.GetComponent<Crafting>();
                isGetItem = false;
                craftTable.AddCraftItems(playerItemSystem.currentEquipItem);
                playerItemSystem.DetachItem();
                Debug.Log("아이템 투입 완료!");
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

            if (nearestObject.CompareTag(Define.Tag.CRAFT_TABLE))
            {
                Crafting craftTable = nearestObject.GetComponent<Crafting>();
                craftTable.RemoveAllItems();
                Debug.Log("조합대에서 아이템 빼내기");
            }

            // 아이템을 던지고, 플레이어의 손에서 Detach
            else if (isGetItem)
            {
                isGetItem = false;
                playerItemSystem.ThrowItem();

                playerItemSystem.DetachItem();
                Debug.Log("아이템 던지기 완료!");
            }
        }
    }

    // 구형 트리거를 생성해 감지합니다.
    // OnTrigger는 리스트 반환이 불가능해 OverlapSphere를 사용합니다.
    private void SphereTriggerFunc()
    {
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, DETECT_RADIUS);
        float nearestDist = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag(Define.Tag.ITEM) || col.CompareTag(Define.Tag.CRAFT_TABLE))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestObject = col.gameObject;
                }
            }
        }
    }
    
    // 현재 콜라이더 탐지 범위를 시각화해 디버깅합니다.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DETECT_RADIUS);
    }

}
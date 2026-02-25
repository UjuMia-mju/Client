using UnityEngine;

public class OtherPlayers : MovingObject
{
    private bool inputFreeze = false;

    // 컴포넌트 참조 변수
    private PlayerAnimator playerAnimator;

    // 아이템 시스템은 잠시 주석화
    //public PlayerItemSystem playerItemSystem { get; private set; }

    public GameObject nearestObject { get; private set; } // 플레이어에게서 가장 가까운 오브젝트

    //private const float DETECT_RADIUS = 5.5f; // 구형 트리거 반지름 

    public bool isGetItem { get; private set; } = false;

    public GameObject playerBoneModel;

    // TODO : 기초 플레이어 능력치 시스템 구현 완료 - 실제로 표시되는 방식은 UI담당과 상의 필요
    //private PlayerStat playerStat;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerAnimator = GetComponent<PlayerAnimator>();
        //playerItemSystem = GetComponent<PlayerItemSystem>();
        //playerStat = GetComponent<PlayerStat>();

        playerAnimator.Initialize();
    }

    //private void Start()
    //{
    //    // 산소가 줄어들기 시작함
    //    //StartCoroutine(playerStat.OxygenDecrease());
    //}

    // Update is called once per frame
    //void Update()
    //{
        
    //}

    //private void FixedUpdate()
    //{
        
    //}
}

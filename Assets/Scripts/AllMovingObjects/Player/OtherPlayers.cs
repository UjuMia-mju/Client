using UnityEngine;
using UnityEngine.InputSystem;

public class OtherPlayers : MovingObject
{
    private PlayerAnimator playerAnimator;

    // TODO : 기초 플레이어 능력치 시스템 구현 완료 - 실제로 표시되는 방식은 UI담당과 상의 필요
    //private PlayerStat playerStat;

    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; }

    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private bool _hasTarget = false;

    // 초기화
    protected override void Awake()
    {
        base.Awake();

        playerAnimator = GetComponent<PlayerAnimator>();
        //playerItemSystem = GetComponent<PlayerItemSystem>();
        //playerStat = GetComponent<PlayerStat>();

        playerAnimator.Initialize();
    }

    private void Start()
    {
        _targetPos = transform.position;
        _targetRot = transform.rotation;
        // 산소가 줄어들기 시작함
        //StartCoroutine(playerStat.OxygenDecrease());
    }

    void Update()
    {
        transform.position = _targetPos;
        transform.rotation = _targetRot;
        //playerAnimator.PlayerAnimation(playerInput.axisResultDir,
        //        playerInput.GetIsJumping(),
        //        isGrounded,
        //        inputFreeze);
    }

    private void FixedUpdate()
    {
        // movDir은 일단 사용하지 않는 것으로 하고, 영벡터를 파라메터로 넣었습니다. 의미는 없습니다.
        //Moving(Vector3.zero);
    }

    //protected override void Moving(Vector3 movDir)
    //{
    //    if (_hasTarget)
    //    {
    //        // 부드러운 보간 이동
    //        rb.MovePosition(Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed));
    //        rb.MoveRotation(Quaternion.Slerp(transform.rotation, _targetRot, Time.f * lerpSpeed));
    //    }
    //}

    public void SetTargetPosition(Vector3 position, Quaternion rotation)
    {
        _targetPos = position;
        _targetRot = rotation;
        _hasTarget = true;
    }
}

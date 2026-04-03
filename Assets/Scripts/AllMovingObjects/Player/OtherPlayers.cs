using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class OtherPlayers : MovingObject
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; }

    [SerializeField] private float lerpSpeed = 100f;

    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private bool _hasTarget = false;

    private Animator otherPlayerAnimator;
    private OtherPlayerStats otherPlayerStats;  // UI 담당과 상의필요함.
    private PlayerItemSystem otherPlayerItemSystem;

    // 초기화
    protected override void Awake()
    {
        base.Awake();
        otherPlayerAnimator = GetComponent<Animator>();
        otherPlayerStats = GetComponent<OtherPlayerStats>();
        otherPlayerItemSystem = GetComponent<PlayerItemSystem>();

        // 물리 충돌로 밀려 떨리는 현상 방지
        rb.isKinematic = true;

    }

    private void FixedUpdate()
    {
        // movDir은 일단 사용하지 않는 것으로 하고, 영벡터를 파라메터로 넣었습니다. 의미는 없습니다.
        Moving(Vector3.zero);
    }

    protected override void Moving(Vector3 movDir)
    {
        if (_hasTarget)
        {
            // 부드러운 보간 이동
            rb.MovePosition(Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed));
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed));
        }
    }

    public void SetTargetPosition(Vector3 position, Quaternion rotation)
    {
        _targetPos = position;
        _targetRot = rotation;
        _hasTarget = true;
    }

    public void SetAnimState(int data)
    {
        otherPlayerAnimator.SetInteger("AnimationPar", data);
    }

    public void SetEquipItem(Items itemData)
    {
        otherPlayerItemSystem.AttachItem(itemData.gameObject);
    }

    public void DetachEquipItem()
    {
        otherPlayerItemSystem.ThrowItem(GetMovingAmount());
    }

    public void SetStat(int hpData, float oxygenData)
    {
        otherPlayerStats.SetStat(hpData, oxygenData);
    }
}
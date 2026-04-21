using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class OtherPlayers : MovingObject
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; }

    [SerializeField] private float lerpSpeed = 10f;

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
        Moving(Vector3.zero);
    }

    protected override void Moving(Vector3 movDir)
    {
        // NOTE : rb 사용을 주석화합니다. 20260403 
        // 문제 상황 : 몸으로 다른 플레이어를 밀다 보면 다른 쪽에서 플레이어가 부들부들 떨리는 문제
        // 이를 해결하기 위해 isKinematic을 설정해 물리 충돌로 인한 위치 변화가 일어나지 않게끔 했습니다.
        // 그런데 이렇게 설정하니 다른 플레이어를 몸으로 밀면 그 객체가 멀리 밀려나는 문제가 생겼습니다.
        // 도저히 모르겠어서 코파일럿한테 물어봤는데, 아마 유니티 내부 물리엔진에서 자체적으로 위치를 보정시키는거 같습니다.
        // 이를 해결하기 위해 Rigidbody를 사용한 이동을 포기하고, 직접 위치와 회전을 넣는 방식으로 변경하였습니다.
        // 이것은 서버에서 보낸 위치로 강제로 텔레포트 시키는 방식에 가까운데, 아마 로컬에서는 이런 식으로 이동 구현을 시키면 안 된다는걸 다들 아실 겁니다.
        // 다만 물리엔진을 계속 사용을 하면 문제가 해결이 안 되기도 하고, 어차피 서버에서 보낸 위치로 이동을 시키고 있기 때문에 그냥 transform에 대입시키도록 변경했습니다.
        if (_hasTarget)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed);

            //Vector3 newPos = Vector3.Lerp(transform.position, _targetPos, Time.fixedDeltaTime * lerpSpeed);
            //rb.MovePosition(newPos);
            //rb.MoveRotation(Quaternion.Slerp(transform.rotation, _targetRot, Time.fixedDeltaTime * lerpSpeed));
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


    public void EndMining()
    {
        //empty
    }

    // 특정 아이템을 들고 있는지 확인 후 분리
    public bool TryDetachItem(GameObject item)
    {
        if (otherPlayerItemSystem.currentEquipItem == item)
        {
            otherPlayerItemSystem.DetachItem();
            return true;
        }
        return false;
    }
}
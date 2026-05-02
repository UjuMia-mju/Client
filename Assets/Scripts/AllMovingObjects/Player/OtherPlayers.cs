using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class OtherPlayers : MovingObject
{
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; }

    [SerializeField] private float lerpSpeed = 10f;

    private GameObject[] toggleOnDeath;

    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private bool _hasTarget = false;
    private bool _isDead = false;

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
        if (_isDead) return;
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
        itemData.SetOwnedByMe(false);
    }

    public void DetachEquipItem(bool charged)
    {
        // 호스트 측: 이 OtherPlayers는 "피어 대역"이므로, 호스트가 권위 물리로 던지기를 시뮬해야 함
        // 피어 측: 호스트가 broadcast하는 S_OBJECT_MOVE로 알아서 위치 동기화되므로 시각적 detach만
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
        {
            float runningAmount = GetMovingAmount();
            if (charged)
                otherPlayerItemSystem.ThrowChargedAim(runningAmount, transform.forward);
            else
                otherPlayerItemSystem.ThrowItem(runningAmount);
        }
        else
        {
            otherPlayerItemSystem.DetachForRemoteSync();
        }
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

    public void ApplyDeath()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[OtherPlayers] ApplyDeath 호출됨. playerId={PlayerId}, toggleOnDeath count={(toggleOnDeath != null ? toggleOnDeath.Length : 0)}");

        SetVisible(false);
    }

    public void ApplyRevive(Vector3 pos, Quaternion rot)
    {
        _isDead = false;

        // 위치/회전 즉시 이동 + 보간 타깃도 동기화 (안 그러면 직후 lerp가 옛 위치로 잡아당김)
        transform.SetPositionAndRotation(pos, rot);
        _targetPos = pos;
        _targetRot = rot;
        _hasTarget = true;

        SetVisible(true);
        Debug.Log($"[OtherPlayers] ApplyRevive. playerId={PlayerId}, pos={pos}");
    }

    private void SetVisible(bool visible)
    {
        if (toggleOnDeath != null && toggleOnDeath.Length > 0)
        {
            foreach (var go in toggleOnDeath)
                if (go != null) go.SetActive(visible);
        }
        else
        {
            // 인스펙터에 안 넣었으면 fallback으로 렌더러/콜라이더 토글
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
        }
    }
}
using UnityEngine;

public class FurnaceObject : MonoBehaviour
{
    public int furnaceId; // 이 용광로의 고유 ID

    [Header("Visuals & Effects")]
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private AudioSource workingSound;
    private Coroutine visualTimerCoroutine; // 시각적 타이머를 관리할 코루틴
    private bool isWorking = false;
    private void Start()
    {
        // 매니저가 씬에 존재한다면 이 용광로를 통신망(Dictionary)에 등록
        if (FurnaceClientManager.Instance != null)
        {
            FurnaceClientManager.Instance.RegisterFurnace(furnaceId, this);
        }
    }

    private void OnDestroy()
    {
        // 씬 전환이나 파괴 시, 매니저의 관리 목록에서 제거하여 에러(NullReference) 방지
        if (FurnaceClientManager.Instance != null)
        {
            FurnaceClientManager.Instance.UnregisterFurnace(furnaceId);
        }
    }

    // 유저가 용광로에 아이템을 넣으려 할 때 호출 (상호작용 키 등)
    public bool RequestSmelt(int objectId)
    {
        if (isWorking)
        {
            Debug.LogWarning($"[Client] 용광로({furnaceId})는 이미 작동 중입니다.");
            return false;
        }

        if (ConnectManager.Instance.isHost)
        {
            // 호스트는 직접 서버 매니저에 smelt 요청
            FurnaceServerManager.Instance.OnReceiveSmeltRequest(furnaceId, (ulong)objectId);
        }
        else
        {
            // 클라이언트는 패킷 송신을 통해 호스트에게 요청
            PacketSender.Instance.SendFurnanceSmeltRequest((ulong)objectId, furnaceId);
        }

        return true;
    }

    // 서버(통신 Manager)로부터 작동 시작 명령을 받았을 때 호출
    public void OnSmeltStarted(int meltTime)
    {
        if (isWorking)
        {
            Debug.LogWarning($"[Client] 용광로({furnaceId})는 이미 작동 중입니다.");
            return; // 이미 작동 중이라면 중복 실행 방지
        }

        isWorking = true; // 작동 상태로 전환

        // 1. 이펙트 및 사운드 재생
        if (fireEffect != null) fireEffect.Play();
        if (workingSound != null) workingSound.Play();

        // 2. 프로그레스 바 UI 등 시각적 타이머 설정 (클라이언트는 시각적 처리만)
        //if (visualTimerCoroutine != null) StopCoroutine(visualTimerCoroutine);
        //visualTimerCoroutine = StartCoroutine(VisualTimerRoutine(meltTime)); // 시각적 타이머 코루틴 시작 (추후 연결 부탁드립니다.)
    }

    // 서버(통신 Manager)로부터 작동 완료 명령을 받았을 때 호출
    public void OnSmeltCompleted(/* ItemType resultItem */)
    {
        isWorking = false; // 작동 상태 해제

        // 1. 진행 중이던 이펙트/사운드 정지
        if (fireEffect != null) fireEffect.Stop();
        if (workingSound != null) workingSound.Stop();
        // progressBar.Hide();

        // 필요 시 완성 알림음이나 완성 이펙트 재생 (아이템 생성은 서버의 몫)
        Debug.Log($"[Client] 용광로({furnaceId}) 완료! 결과물 드랍 대기중...");
    }
}
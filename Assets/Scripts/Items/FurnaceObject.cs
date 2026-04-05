using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FurnaceObject : MonoBehaviour
{
    public int furnaceId; // 이 용광로의 고유 ID

    [Header("Visuals & Effects")]
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private AudioSource workingSound;
    [SerializeField] private Image progressBar; // 시각적 타이머용 UI (인스펙터에서 연결)
    private Coroutine visualTimerCoroutine; // 시각적 타이머를 관리할 코루틴
    
    public bool isWorking {get; private set;} = false; // 현재 용광로가 작동 중인지 여부
    public bool hasResult {get; private set;} = false; // 제련이 완료되어 결과 아이템이 생성되었는지 여부 (완성된 아이템이 아직 수거되지 않은 상태)

    private float item_throw_height = 3.5f;
    private float item_throw_force = 200f;
    private void Start()
    {
        // 진행 이미지 초기 상태 설정 (투명하게 숨김)
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.gameObject.SetActive(false);
        }

        FurnaceClientManager.Instance?.RegisterFurnace(furnaceId, this);
    }

    private void OnDestroy()
    {
        FurnaceClientManager.Instance?.UnregisterFurnace(furnaceId);
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
        if (visualTimerCoroutine != null) StopCoroutine(visualTimerCoroutine);
        visualTimerCoroutine = StartCoroutine(VisualTimerRoutine(meltTime));
    }

    // [클라이언트 전용] 시각적 진행 게이지 코루틴
    private IEnumerator VisualTimerRoutine(float timerDuration)
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.fillAmount = 0f;
        }

        float remainingTime = timerDuration;

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            // 진행률 계산하여 이미지 채우기 (시간에 비례해서 점점 차오름)
            if (progressBar != null)
            {
                progressBar.fillAmount = (timerDuration - remainingTime) / timerDuration;
            }

            yield return null; // 프레임 단위 매번 갱신
        }
    }

    // 서버(통신 Manager)로부터 작동 완료 명령을 받았을 때 호출
    public void OnSmeltCompleted(/* ItemType resultItem */)
    {
        isWorking = false; // 작동 상태 해제
        hasResult = true; // 결과 아이템이 생성된 상태로 전환 (수거 대기)

        // 1. 진행 중이던 이펙트/사운드 정지
        if (fireEffect != null) fireEffect.Stop();
        if (workingSound != null) workingSound.Stop();

        // 2. 타이머 코루틴 중지 시키기 (메모리 낭비/버그 방지)
        if (visualTimerCoroutine != null)
        {
            StopCoroutine(visualTimerCoroutine);
            visualTimerCoroutine = null;
        }

        // 3. UI(Progress Bar) 숨기기
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f; // 다음 작업을 위해 0으로 초기화
            progressBar.gameObject.SetActive(false); // 게이지 끄기
        }

        // 필요 시 완성 알림음이나 완성 이펙트 재생 (아이템 생성은 서버의 몫)
        Debug.Log($"[Client] 용광로({furnaceId}) 완료! 결과물 드랍 대기중...");
    }

    // 서버로부터 아이템 회수 명령을 받았을 때 호출 (C_FURNACE_RETRIEVE 패킷 처리)
    public void RequestRetrieve()
    {
        if (!hasResult)
        {
            Debug.LogWarning($"[Client] 용광로({furnaceId})에는 아직 수거할 결과물이 없습니다.");
            return;
        }

        if (ConnectManager.Instance.isHost)
        {
            // 호스트는 직접 서버 매니저에 retrieve 요청
            FurnaceServerManager.Instance.OnReceiveFurnaceRetrieve(furnaceId);
        }
        else
        {
            // 클라이언트는 패킷 송신을 통해 호스트에게 요청
            PacketSender.Instance.SendFurnaceRetrieveRequest(furnaceId);
        }
    }

    public void ThrowSmeltedItem(GameObject resultPrefab)
    {
        // 1. 상태 완전 초기화 (이제 빈 용광로가 됨)
        hasResult = false;
        isWorking = false; // 혹시 몰라서 한번 더 함.

        // 2. 스폰 위치 지정 (현재 위치 + 윗방향 높이)
        Vector3 spawnPos = this.transform.position + this.transform.up * item_throw_height;

        // 3. 아이템 생성
        GameObject itemToThrow = Instantiate(resultPrefab, spawnPos, Quaternion.identity);

        // 4. Rigidbody 가져와서 던지는 물리력 적용
        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDir = (this.transform.up + this.transform.forward).normalized;
            rb.AddForce(throwDir * item_throw_force);
        }

        Debug.Log($"[Client] 용광로({furnaceId}) 배출 완료!");
    }
}
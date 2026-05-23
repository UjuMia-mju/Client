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
    [SerializeField] private Image finishImage; // 제련 완료 시 표시할 이미지 (인스펙터에서 연결)
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
        if (finishImage != null)
            finishImage.gameObject.SetActive(false);

        FurnaceClientManager.Instance?.RegisterFurnace(furnaceId, this);
    }

    private void OnDestroy()
    {
        FurnaceClientManager.Instance?.UnregisterFurnace(furnaceId);
    }

    // 유저가 용광로에 아이템을 넣으려 할 때 호출 (상호작용 키 등)
    public bool RequestSmelt(int objectId)
    {
        if (isWorking || hasResult)
        {
            Debug.LogWarning($"[Client] 용광로({furnaceId})는 이미 작동 중이거나 결과물이 있습니다.");
            return false;
        }

        if (ConnectManager.Instance.isHost)
        {
            Items item = ItemManager.Instance.GetItem(objectId);
            if (item == null) return false;

            // 호스트: 레시피 먼저 검증 후 요청
            if (!SmeltingRecipeManager.Instance.TryGetRecipe(item.itemStringKey, out _))
            {
                Debug.LogWarning($"[Furnace] '{item.itemStringKey}'은 제련할 수 없는 아이템입니다.");
                return false;
            }

            FurnaceServerManager.Instance.OnReceiveSmeltRequest(furnaceId, (ulong)objectId);
        }
        else
        {
            // 피어: 서버에 요청만, 검증은 호스트가 수행
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
        isWorking = false;
        hasResult = true;

        if (fireEffect != null) fireEffect.Stop();
        if (workingSound != null) workingSound.Stop();

        if (visualTimerCoroutine != null)
        {
            StopCoroutine(visualTimerCoroutine);
            visualTimerCoroutine = null;
        }

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.gameObject.SetActive(false);
        }
        if (finishImage != null)
            finishImage.gameObject.SetActive(true);

        Debug.Log($"[Client] 용광로({furnaceId}) 완료! 결과물 드랍 대기중...");
    }

    // 서버로부터 아이템 회수 명령을 받았을 때 호출 (C_FURNACE_RETRIEVE 패킷 처리)
    public void RequestRetrieve()
    {
        Debug.Log($"[FurnaceObject] 수거 요청 시도: furnaceId={furnaceId}, hasResult={hasResult}, isWorking={isWorking}");

        if (isWorking)
        {
            Debug.LogWarning($"[Client] 용광로({furnaceId})는 아직 작동 중입니다.");
            return;
        }

        if (ConnectManager.Instance.isHost)
        {
            // 최종 수거 가능 여부는 FurnaceServerManager.completedFurnaces 기준으로 판단
            FurnaceServerManager.Instance.OnReceiveFurnaceRetrieve(furnaceId);
        }
        else
        {
            // 피어도 로컬 hasResult에 의존하지 않고 호스트에게 요청
            PacketSender.Instance.SendFurnaceRetrieveRequest(furnaceId);
        }
    }

    // 호스트 전용: 아이템 생성 및 배출. 생성된 Items 컴포넌트를 반환
    public Items ThrowSmeltedItem(GameObject resultPrefab)
    {
        hasResult = false;
        isWorking = false;

        if (finishImage != null)
            finishImage.gameObject.SetActive(false);

        Vector3 spawnPos = this.transform.position + this.transform.up * item_throw_height;
        Quaternion spawnRot = Quaternion.LookRotation(this.transform.forward, this.transform.up);
        GameObject itemToThrow = Instantiate(resultPrefab, spawnPos, spawnRot);

        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDir = (this.transform.up + this.transform.forward).normalized;
            rb.AddForce(throwDir * item_throw_force);
        }

        Debug.Log($"[FurnaceObject] ({furnaceId}) 배출 완료!");
        return itemToThrow.GetComponent<Items>();
    }

    // 피어 전용: 아이템 생성 없이 용광로 상태 및 UI만 초기화
    public void OnItemRetrieved()
    {
        hasResult = false;
        isWorking = false;

        if (finishImage != null)
            finishImage.gameObject.SetActive(false);

        Debug.Log($"[FurnaceObject] ({furnaceId}) 피어 - 수거 상태 초기화 완료.");
    }
}
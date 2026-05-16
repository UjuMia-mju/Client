using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 비동기 로딩 진행도를 퍼센트 텍스트·진행바에 반영합니다.
/// 배경(barEmpty)은 항상 꽉 찬 트랙만 표시하고, 겹친(barFill · Filled Horizontal · Left origin) 채우기 이미지만 0→1로 늘어납니다.
/// </summary>
public class LoadingUIController : MonoBehaviour
{
    const float AsyncProgressCap = 0.9f;

    [Header("Loading Bar")]
    [SerializeField] Image barEmpty;
    [SerializeField] Image barFill;

    [Header("Labels")]
    [SerializeField] TextMeshProUGUI percentText;

    [Header("표시 진행도 (억지 연출 가능)")]
    [Tooltip(
        "실제 진행(ceiling)을 넘을 수 없게 하면서만 올립니다. 패널 표시 시작 후 이만큼 Realtime 초가 지나면 시간 진행바닥이 끝(1.0)까지 오릅니다.")]
    [SerializeField] float visualProgressRampRealtimeSeconds = 2.5f;

    [Tooltip(
        "Unity가 progress를 0 근처에 오래 두는 경우 표시만 0%로 고정되어 끝에 한 번에 차는 현상을 막습니다. 실제 완료 처리 전까지는 시간 기반 표시를 이 비율 이하로 깎습니다.")]
    [SerializeField, Range(0.5f, 0.99f)]
    float preCompleteDisplayClamp01 = 0.9f;

    [Tooltip(
        "시간 진행이 이 비율(0~1)에 닿았을 때, 지정 시간만큼 퍼센트·바를 그대로 멈춥니다. 0초면 비활성.")]
    [SerializeField] float midHoldDurationRealtimeSeconds = 0.6f;

    [Tooltip("중간 멈춤 구간 근처(이 값 이상이 되면 시작). 표시 진행보다 높진 않도록 자동 보정.")]
    [SerializeField, Range(0.08f, 0.92f)]
    float midHoldKickInNormalized = 0.48f;

    float _panelShownRealtime;
    float _displayShownNormalized;

    float _midHoldEndRealtime;
    float _midHoldFrozenNormalized;
    bool _midHoldPlayed;

    Coroutine _activeLoad;

    public bool IsLoading => _activeLoad != null;

    /// <summary>SceneLoader 등 외부에서 AsyncOperation 진행만 반영할 때 사용합니다.</summary>
    public void ShowAndResetProgress()
    {
        gameObject.SetActive(true);
        _panelShownRealtime = Time.realtimeSinceStartup;
        _displayShownNormalized = 0f;
        ResetMidHoldState();
        ApplyLoadingPercent(0f);
    }

    public void ApplyAsyncOperationProgress(AsyncOperation op)
    {
        if (op == null)
            return;

        float realCeil01 = Mathf.Clamp01(ComputeRealProgressNormalized(op));

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _panelShownRealtime);
        float ramp01 = Mathf.Max(0.05f, visualProgressRampRealtimeSeconds);
        float timeFloor01 = Mathf.Clamp01(elapsed / ramp01);

        // real이 거의 안 움직이는 동안에도 시간으로 바가 따라 올라가게 해 0→100 점프를 줄입니다.
        float cappedTime01 = realCeil01 >= 1f - Mathf.Epsilon
            ? timeFloor01
            : Mathf.Min(timeFloor01, Mathf.Clamp01(preCompleteDisplayClamp01));
        float blendedCap01 = Mathf.Max(realCeil01, cappedTime01);

        TryStartMidHold(blendedCap01);
        if (IsMidHoldActive())
        {
            _displayShownNormalized = Mathf.Clamp(_midHoldFrozenNormalized, 0f, blendedCap01);
            ApplyLoadingPercent(_displayShownNormalized);
            return;
        }

        EndMidHoldTimerIfPassed();

        float boosted01 = Mathf.Max(_displayShownNormalized, timeFloor01);
        _displayShownNormalized = Mathf.Min(boosted01, blendedCap01);
        ApplyLoadingPercent(_displayShownNormalized);
    }

    void ResetMidHoldState()
    {
        _midHoldEndRealtime = 0f;
        _midHoldFrozenNormalized = 0f;
        _midHoldPlayed = false;
    }

    /// <param name="displayCap01">표시 허용 상한(blended 실제·시간).</param>
    void TryStartMidHold(float displayCap01)
    {
        if (_midHoldPlayed || midHoldDurationRealtimeSeconds < 1e-3f || midHoldKickInNormalized <= 0f)
            return;

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _panelShownRealtime);
        float ramp01 = Mathf.Max(0.05f, visualProgressRampRealtimeSeconds);
        float timeFloor01 = Mathf.Clamp01(elapsed / ramp01);
        float boosted01 = Mathf.Max(_displayShownNormalized, timeFloor01);

        float kick = Mathf.Clamp(midHoldKickInNormalized, 0.05f, 0.95f);
        // displayCap도 같이 따라와야 같은 구간에서 멈춤이 걸림 (옛 순수 real ceil=0 조건 때문에 스킵되던 현상 수정)
        if (boosted01 < kick || displayCap01 + 1e-4f < Mathf.Min(boosted01, kick))
            return;

        _midHoldPlayed = true;
        _midHoldFrozenNormalized =
            Mathf.Clamp(Mathf.Max(_displayShownNormalized, Mathf.Min(kick, boosted01)), 0f,
                displayCap01);
        _midHoldEndRealtime = Time.realtimeSinceStartup + midHoldDurationRealtimeSeconds;
    }

    bool IsMidHoldActive()
    {
        return _midHoldPlayed && _midHoldEndRealtime > 0f
               && Time.realtimeSinceStartup < _midHoldEndRealtime;
    }

    void EndMidHoldTimerIfPassed()
    {
        if (_midHoldEndRealtime <= 0f || Time.realtimeSinceStartup < _midHoldEndRealtime)
            return;
        _midHoldEndRealtime = 0f;
    }

    static float ComputeRealProgressNormalized(AsyncOperation op)
    {
        return op.progress >= AsyncProgressCap
            ? 1f
            : Mathf.Clamp01(op.progress / AsyncProgressCap);
    }

    public void SnapToCompletedDisplay()
    {
        ResetMidHoldState();
        _displayShownNormalized = 1f;
        ApplyLoadingPercent(1f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void LoadSceneAsync(string sceneName)
    {
        RestartLoad(StartCoroutine(LoadSceneRoutine(sceneName)));
    }

    public void LoadSceneAsync(int buildIndex)
    {
        RestartLoad(StartCoroutine(LoadSceneRoutine(buildIndex)));
    }

    void RestartLoad(Coroutine routine)
    {
        if (_activeLoad != null)
            StopCoroutine(_activeLoad);
        _activeLoad = routine;
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        try
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
                yield break;

            _panelShownRealtime = Time.realtimeSinceStartup;

            op.allowSceneActivation = false;
            yield return RunProgressLoop(op);

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }
        finally
        {
            _activeLoad = null;
        }
    }

    IEnumerator LoadSceneRoutine(int buildIndex)
    {
        try
        {
            var op = SceneManager.LoadSceneAsync(buildIndex);
            if (op == null)
                yield break;

            _panelShownRealtime = Time.realtimeSinceStartup;

            op.allowSceneActivation = false;
            yield return RunProgressLoop(op);

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }
        finally
        {
            _activeLoad = null;
        }
    }

    IEnumerator RunProgressLoop(AsyncOperation op)
    {
        while (op.progress < AsyncProgressCap)
        {
            ApplyAsyncOperationProgress(op);
            yield return null;
        }

        ApplyAsyncOperationProgress(op);
        yield return null;
    }

    void ApplyLoadingPercent(float normalized01)
    {
        normalized01 = Mathf.Clamp01(normalized01);
        int pct;
        // 실제 진행 중 Round는 49.9→50 같이 과하게 올려 보일 수 있어 Floor 우선 (완료 1.f는 Snap 등에서 처리)
        if (normalized01 >= 1f - Mathf.Epsilon)
            pct = 100;
        else
            pct = Mathf.Clamp(Mathf.FloorToInt(normalized01 * 100f), 0, 99);

        ApplyBarImages(normalized01);

        if (percentText != null)
            percentText.text = $"{pct}%";
    }

    void ApplyBarImages(float normalized01)
    {
        KeepFullBarTrack(barEmpty);
        SetBarFillAmount(barFill, normalized01);
    }

    /// <summary>어두운/빈 느낌의 바 배경. 진행량과 무관하게 항상 전체 폭 트랙만 표시합니다.</summary>
    static void KeepFullBarTrack(Image track)
    {
        if (track == null)
            return;

        if (track.type == Image.Type.Filled)
        {
            track.fillAmount = 1f;
            return;
        }

        var c = track.color;
        c.a = 1f;
        track.color = c;
    }

    /// <summary>진행 색 채우기: Filled 가로 좌측 또는 Simple 레거시 anchor 스트레치.</summary>
    static void SetBarFillAmount(Image fill, float t)
    {
        if (fill == null)
            return;
        t = Mathf.Clamp01(t);

        if (fill.type == Image.Type.Filled)
        {
            fill.fillAmount = t;
            fill.SetVerticesDirty();
            return;
        }

        var rt = fill.rectTransform;
        rt.anchorMax = new Vector2(t, rt.anchorMax.y);
    }
}

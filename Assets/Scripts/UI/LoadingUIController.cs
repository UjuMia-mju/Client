using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 표시 진행이 특정 퍼센트에 닿았을 때 잠깐 멈추는 연출 비트.
/// </summary>
[Serializable]
public struct LoadingPercentHoldBeat
{
    [Tooltip("이 표시 퍼센트(1~99)까지 올라온 직후 멈춥니다.")]
    [Range(1, 99)]
    public int pauseAtPercent;

    [Tooltip("멈춤 시간(Realtime 초). 0이면 이 비트는 적용하지 않습니다.")]
    [Min(0f)]
    public float holdRealtimeSeconds;
}

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

    [Tooltip("중간 멈춤: Pause At %, Hold 초. 비우면 기본 30·70% / 0.5초. Size를 0으로 두면 멈춤 없음.")]
    [SerializeField]
    LoadingPercentHoldBeat[] percentHoldBeats;

    static readonly LoadingPercentHoldBeat[] DefaultPercentHoldBeats =
    {
        new LoadingPercentHoldBeat { pauseAtPercent = 30, holdRealtimeSeconds = 0.5f },
        new LoadingPercentHoldBeat { pauseAtPercent = 70, holdRealtimeSeconds = 0.5f }
    };

    float _panelShownRealtime;
    float _displayShownNormalized;

    LoadingPercentHoldBeat[] _sortedPercentHoldBeats;
    int _nextPercentHoldBeatIndex;
    float _percentHoldEndRealtime;
    float _percentHoldFrozenNormalized;

    Coroutine _activeLoad;

    public bool IsLoading => _activeLoad != null;

    void Awake()
    {
        RebuildSortedPercentHoldBeats();
    }

    /// <summary>SceneLoader 등 외부에서 AsyncOperation 진행만 반영할 때 사용합니다.</summary>
    public void ShowAndResetProgress()
    {
        gameObject.SetActive(true);
        _panelShownRealtime = Time.realtimeSinceStartup;
        _displayShownNormalized = 0f;
        ResetPercentHoldState();
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

        EndPercentHoldTimerIfPassed();

        if (IsPercentHoldActive())
        {
            _displayShownNormalized = Mathf.Clamp(_percentHoldFrozenNormalized, 0f, blendedCap01);
            ApplyLoadingPercent(_displayShownNormalized);
            return;
        }

        float boosted01 = Mathf.Max(_displayShownNormalized, timeFloor01);
        float desired01 = Mathf.Min(boosted01, blendedCap01);

        if (TryStartPercentHoldBeat(desired01, blendedCap01))
            return;

        _displayShownNormalized = desired01;
        ApplyLoadingPercent(_displayShownNormalized);
    }

    void RebuildSortedPercentHoldBeats()
    {
        if (percentHoldBeats != null && percentHoldBeats.Length == 0)
        {
            _sortedPercentHoldBeats = Array.Empty<LoadingPercentHoldBeat>();
            return;
        }

        LoadingPercentHoldBeat[] source = percentHoldBeats;
        if (source == null || source.Length == 0)
            source = DefaultPercentHoldBeats;

        var copy = new LoadingPercentHoldBeat[source.Length];
        Array.Copy(source, copy, source.Length);
        Array.Sort(copy, (a, b) => a.pauseAtPercent.CompareTo(b.pauseAtPercent));
        _sortedPercentHoldBeats = copy;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RebuildSortedPercentHoldBeats();
    }
#endif

    void ResetPercentHoldState()
    {
        _nextPercentHoldBeatIndex = 0;
        _percentHoldEndRealtime = 0f;
        _percentHoldFrozenNormalized = 0f;
    }

    void EndPercentHoldTimerIfPassed()
    {
        if (_percentHoldEndRealtime <= 0f || Time.realtimeSinceStartup < _percentHoldEndRealtime)
            return;
        _percentHoldEndRealtime = 0f;
        _nextPercentHoldBeatIndex++;
    }

    bool IsPercentHoldActive()
    {
        return _percentHoldEndRealtime > 0f && Time.realtimeSinceStartup < _percentHoldEndRealtime;
    }

    /// <summary>
    /// <paramref name="desired01"/>까지 이번에 채울 수 있을 때, 다음 비트 퍼센트를 지나가면 그 지점에서 멈춤.
    /// </summary>
    /// <returns>이번 프레임에서 멈춤을 시작해 더 이상 진행하지 않으면 true.</returns>
    bool TryStartPercentHoldBeat(float desired01, float blendedCap01)
    {
        if (_sortedPercentHoldBeats == null || _sortedPercentHoldBeats.Length == 0)
            return false;

        const float eps = 1e-3f;

        while (_nextPercentHoldBeatIndex < _sortedPercentHoldBeats.Length)
        {
            var beat = _sortedPercentHoldBeats[_nextPercentHoldBeatIndex];
            if (beat.holdRealtimeSeconds < 1e-3f)
            {
                _nextPercentHoldBeatIndex++;
                continue;
            }

            float t = Mathf.Clamp(beat.pauseAtPercent * 0.01f, 0.02f, 0.98f);

            if (desired01 < t - eps)
                return false;

            // desired가 이 비트를 통과할 수 있을 때만 멈춤. 멈춤이 끝나면 EndPercentHoldTimerIfPassed에서 인덱스만 증가.
            _percentHoldFrozenNormalized = Mathf.Clamp(t, 0f, blendedCap01);
            _percentHoldEndRealtime = Time.realtimeSinceStartup + beat.holdRealtimeSeconds;
            _displayShownNormalized = _percentHoldFrozenNormalized;
            ApplyLoadingPercent(_displayShownNormalized);
            return true;
        }

        return false;
    }

    static float ComputeRealProgressNormalized(AsyncOperation op)
    {
        return op.progress >= AsyncProgressCap
            ? 1f
            : Mathf.Clamp01(op.progress / AsyncProgressCap);
    }

    public void SnapToCompletedDisplay()
    {
        ResetPercentHoldState();
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

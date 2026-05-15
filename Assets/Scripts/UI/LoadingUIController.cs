using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 비동기 씬 로딩과 로딩 바·퍼센트 텍스트를 동기화합니다.
/// Fill은 Image Type이 Filled(가로)이거나, Simple일 때 좌측 기준으로 anchorMax.x가 0~1로 늘어나는 레이아웃을 사용하세요.
/// </summary>
public class LoadingUIController : MonoBehaviour
{
    const float AsyncProgressCap = 0.9f;

    [Header("Loading Bar")]
    [SerializeField] Image barEmpty;
    [SerializeField] Image barFill;

    [Header("Labels")]
    [SerializeField] TextMeshProUGUI percentText;

    Coroutine _activeLoad;

    public bool IsLoading => _activeLoad != null;

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
            var displayed = Mathf.Clamp01(op.progress / AsyncProgressCap);
            ApplyLoadingPercent(displayed);
            yield return null;
        }

        ApplyLoadingPercent(1f);
        yield return null;
    }

    void ApplyLoadingPercent(float normalized01)
    {
        normalized01 = Mathf.Clamp01(normalized01);
        SetBarFill(normalized01);

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(normalized01 * 100f)}%";
    }

    void SetBarFill(float t)
    {
        if (barFill == null)
            return;

        if (barFill.type == Image.Type.Filled)
        {
            barFill.fillAmount = t;
            return;
        }

        var rt = barFill.rectTransform;
        rt.anchorMax = new Vector2(t, rt.anchorMax.y);
    }
}

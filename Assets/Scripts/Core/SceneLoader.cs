using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    private float fadeDuration = 1.0f;

    /// <summary>
    /// 외부 호출용: 특정 CanvasGroup을 페이드하며 씬 전환
    /// </summary>
    public void LoadScene(string sceneName, CanvasGroup targetCanvas)
    {
        StartCoroutine(LoadAsyncSequence(sceneName, targetCanvas));
    }

    private IEnumerator LoadAsyncSequence(string sceneName, CanvasGroup targetCanvas)
    {
        // 1. 전달받은 캔버스를 Fade Out
        yield return StartCoroutine(Fade(targetCanvas, 1.0f));

        // 2. 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; 
        
        while (!op.isDone)
        {
            if (op.progress >= 0.9f)
            {
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        // 3. 새 씬 로드 후 다시 Fade In
        yield return StartCoroutine(Fade(targetCanvas, 0.0f));
    }

    /// <summary>
    /// 인자로 받은 CanvasGroup의 알파값을 조절
    /// </summary>
    public IEnumerator Fade(CanvasGroup targetCanvas, float targetAlpha)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("Fade를 수행할 CanvasGroup이 할당되지 않았습니다.");
            yield break;
        }

        float startAlpha = targetCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            targetCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        targetCanvas.alpha = targetAlpha;
    }
}
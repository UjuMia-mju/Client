using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 비동기 씬 로딩 및 전역 페이드 UI의 생명주기를 전담하는 매니저
/// </summary>
public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    [Header("페이드 설정")]
    public GameObject fadePrefab; 
    public float fadeDuration = 1.0f;

    private CanvasGroup fadeCanvasGroup;
    private GameObject fadeInstance;

    private void InitFadeCanvas()
    {
        if (fadeInstance == null && fadePrefab != null)
        {
            fadeInstance = Instantiate(fadePrefab);
            DontDestroyOnLoad(fadeInstance);
            fadeCanvasGroup = fadeInstance.GetComponent<CanvasGroup>();
            fadeInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 씬 전환의 모든 과정(페이드 인/로드/페이드 아웃/비활성화)을 관리
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (fadeInstance == null) InitFadeCanvas();
        StartCoroutine(LoadAsyncSequence(sceneName));
    }

    private IEnumerator LoadAsyncSequence(string sceneName)
    {
        // 1. 페이드 오브젝트 활성화 및 암전(Fade Out)
        fadeInstance.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        yield return StartCoroutine(Fade(1.0f));

        // 2. 비동기 씬 로딩
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

        // 3. 새로운 씬에서 화면 밝아짐(Fade In)
        yield return StartCoroutine(Fade(0.0f));

        // 4. 연출이 끝났으므로 스스로 비활성화
        fadeCanvasGroup.blocksRaycasts = false;
        fadeInstance.SetActive(false);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
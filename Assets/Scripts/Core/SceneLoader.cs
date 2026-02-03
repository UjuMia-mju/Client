using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 비동기 씬 로딩을 관리하는 싱글톤 클래스
/// </summary>
public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    /// <summary>
    /// 외부 호출용 씬 전환 메서드
    /// </summary>
    /// <param name="sceneName">대상 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAsyncSequence(sceneName));
    }

    /// <summary>
    /// 비동기 로딩 및 활성화 제어 코루틴
    /// </summary>
    /// <param name="sceneName">대상 씬 이름</param>
    private IEnumerator LoadAsyncSequence(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; 
        
        while (!op.isDone)
        {
            // 0.9f는 유니티 엔진의 로딩 완료 시점
            if (op.progress >= 0.9f)
            {
                // TODO: 서버 로딩
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
using System.Collections;
using UnityEngine;

/// <summary>
/// 싱글플레이: 플레이어에게는 스테이지 선택으로 가는 것처럼 보이게,
/// 메인에서 방 생성·레디·시작을 로딩 UI 뒤에서 처리합니다.
/// </summary>
public class SinglePlaySilentBootstrap : MonoBehaviour
{
    public static SinglePlaySilentBootstrap Instance { get; private set; }

    [SerializeField] private float bootstrapTimeoutSeconds = 12f;

    MenuPanelController _menu;
    Coroutine _timeoutRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <returns>부트스트랩 시작 여부(이미 진행 중이면 false)</returns>
    public bool TryBegin(MenuPanelController menu)
    {
        if (SinglePlaySession.IsAwaitingRoomBootstrap)
            return false;

        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
        {
            MessageManager.Instance?.ShowKey(MessageKeys.MultiplayLoginRequired);
            return false;
        }

        _menu = menu;
        _menu?.SetMainMenuInputBlocked(true);
        SceneLoader.Instance?.ShowLoadingOverlay();

        SinglePlaySession.BeginSoloMultiplayer();
        PacketDispatcher.Instance.SendCreateRoom();

        CancelTimeout();
        _timeoutRoutine = StartCoroutine(CoBootstrapTimeout());
        return true;
    }

    public static void NotifyFailed(string logReason)
    {
        if (!SinglePlaySession.IsAwaitingRoomBootstrap && !SinglePlaySession.IsActive)
            return;

        Debug.LogWarning($"[SinglePlaySilentBootstrap] {logReason}");
        Instance?.FinishWithError();
    }

    public static void NotifyEnteringStageSelect()
    {
        Instance?.CancelTimeout();
        // 씬 로드 연출이 로딩 패널을 이어받음 — 여기서는 메뉴만 잠금 유지.
    }

    void FinishWithError()
    {
        CancelTimeout();
        SinglePlaySession.End();
        SceneLoader.Instance?.HideLoadingOverlay();
        _menu?.SetMainMenuInputBlocked(false);
        _menu = null;
    }

    void CancelTimeout()
    {
        if (_timeoutRoutine == null) return;
        StopCoroutine(_timeoutRoutine);
        _timeoutRoutine = null;
    }

    IEnumerator CoBootstrapTimeout()
    {
        yield return new WaitForSecondsRealtime(bootstrapTimeoutSeconds);
        _timeoutRoutine = null;

        if (!SinglePlaySession.IsAwaitingRoomBootstrap)
            yield break;

        MessageManager.Instance?.ShowKey(MessageKeys.StartRoomFailed);
        FinishWithError();
    }
}

using Protocol;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StageSelect 씬에서 행선지 결정 버튼을 처리하고,
/// S_GAME_READY_TO_START 수신 시 id_order[0] 기준으로 호스트/피어 역할을 확정한 뒤
/// 인게임 씬으로 이동합니다.
/// </summary>
public class StageSelectHandler : SceneSingleton<StageSelectHandler>
{
    [SerializeField] private Button confirmButton;

    private int _selectedMapId;
    private int _selectedChapter;
    private int _selectedStageIndex;

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmButton);
    }

    private void OnEnable()
    {
        PacketHandler.Instance.OnGameReadyToStartEvent += OnGameReadyToStart;
    }

    private void OnDisable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnGameReadyToStartEvent -= OnGameReadyToStart;
    }

    /// <summary>
    /// 스테이지 패널에서 스테이지를 선택했을 때 호출합니다.
    /// </summary>
    public void SetSelectedStage(int mapId, int chapter, int stageIndex)
    {
        _selectedMapId = mapId;
        _selectedChapter = chapter;
        _selectedStageIndex = stageIndex;
        Debug.Log($"[StageSelectHandler] 스테이지 선택: map={mapId}, chapter={chapter}, stage={stageIndex}");
    }

    private void OnConfirmButton()
    {
        if (_selectedStageIndex == 0)
        {
            Debug.LogWarning("[StageSelectHandler] 스테이지를 먼저 선택해주세요.");
            return;
        }

        PacketDispatcher.Instance.SendStartStage(_selectedMapId, _selectedChapter, _selectedStageIndex);
        confirmButton.interactable = false;
    }

    private void OnGameReadyToStart(S_GAME_READY_TO_START packet)
    {
        // id_order[0]이 본인이면 호스트, 아니면 피어
        bool isHost = packet.IdOrder.Count > 0
                      && packet.IdOrder[0] == (ulong)NetManager.Instance._playerId;

        ConnectManager.Instance.SetHostRole(isHost);

        if (!Define.Scene.TryGetGameplayScene(_selectedMapId, _selectedChapter, _selectedStageIndex, out string sceneName))
        {
            Debug.LogError($"[StageSelectHandler] 씬 이름 조회 실패: map={_selectedMapId}, ch={_selectedChapter}, stage={_selectedStageIndex}");
            confirmButton.interactable = true;
            return;
        }

        Debug.Log($"[StageSelectHandler] 인게임 진입. isHost={isHost}, 씬={sceneName}");
        SceneLoader.Instance.LoadScene(sceneName);
    }
}
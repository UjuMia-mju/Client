using UnityEngine;
using UnityEngine.UI;
using Protocol;

/// <summary>
/// 로비의 "시작" 버튼 동작.
/// - 버튼 클릭 시 서버에 C_START_ROOM 전송
/// - S_START_ROOM 응답을 받아 성공/실패 로그 처리 및 인게임 씬 전환
/// </summary>
public class LobbyStartButton : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private LobbyManager lobbyManager; // 우주선 연출용

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
            startButton.interactable = true;
        }

        if (PacketManager.Instance != null)
            PacketManager.Instance.OnStartRoomEvent += OnStartRoomResult;
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnClickStart);

        if (PacketManager.Instance != null)
            PacketManager.Instance.OnStartRoomEvent -= OnStartRoomResult;
    }

    private void OnClickStart()
    {
        if (startButton != null)
            startButton.interactable = false;

        PacketDispatcher.Instance.SendStartRoom();
    }

    private void OnStartRoomResult(S_START_ROOM packet)
    {
        if (startButton != null)
            startButton.interactable = true;

        if (!packet.Success)
        {
            Debug.LogWarning($"[LobbyStartButton] 시작 실패: {packet.ErrorMsg}");
            return;
        }

        Debug.Log("[LobbyStartButton] 시작 성공. 인게임으로 전환합니다.");

        // LobbyManager: 전원 레디 검사 후에만 우주선 연출 → 연출 끝에 씬 전환
        if (lobbyManager != null)
        {
            // 서버가 이미 시작 조건을 통과시킨 응답이므로 여기서는 레디 재검사 생략
            lobbyManager.OnAllPlayersReady(skipReadyValidation: true);
            return;
        }

        SceneLoader.Instance.LoadScene(Define.Scene.GAME_1_1);
    }
}


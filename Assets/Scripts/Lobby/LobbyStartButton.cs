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
    [SerializeField] private LobbyManager lobbyManager; // 우주선 연출용 (옵션)

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

        NetManager.Instance.SendStartRoom();
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

        // (옵션) 로비 연출부터 실행하고, 연출이 끝나면 Scene 전환되도록 LobbyManager를 수정해 둔 경우에만 사용.
        // 현재 LobbyManager가 연출만 담당하고 씬 전환을 하지 않으면 아래 씬 전환이 즉시 수행됩니다.
        if (lobbyManager != null)
            lobbyManager.OnAllPlayersReady();

        // 로비 연출과 무관하게 일단 인게임 씬으로 전환.
        // (우주선 연출이 끝난 후 전환 로직을 넣고 싶으면 LobbyManager 쪽에서 처리하세요.)
        SceneLoader.Instance.LoadScene(Define.Scene.GAME);
    }
}


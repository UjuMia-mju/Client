using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Protocol;


// 로비에서 초대 보내기 UI.
// 플레이어 이름 + 태그를 입력하고 초대 버튼을 누르면 NetManager.SendInvitePlayer를 호출하여 C_INVITE_PLAYER 패킷을 서버에 전송한다.
public class LobbyInviteUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panelRoot;   // InvitePanel 루트 (초기 비활성, 초대 버튼으로만 열림)

    [Header("열기/닫기")]
    [SerializeField] private Button openInvitePanelButton;  // 로비에 둘 "초대" 버튼 → 누르면 패널 활성화
    [SerializeField] private Button closeButton;           // 패널 안 "닫기" 버튼 (선택)

    [Header("초대 입력")]
    [SerializeField] private TMP_InputField targetPlayerNameInput;
    [SerializeField] private TMP_InputField targetPlayerTagInput;

    [Header("버튼")]
    [SerializeField] private Button inviteButton;   // 초대 전송 버튼

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (openInvitePanelButton != null)
            openInvitePanelButton.onClick.AddListener(OnClickOpenPanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClosePanel);
        if (inviteButton != null)
            inviteButton.onClick.AddListener(OnClickInvite);
    }

    private void OnClickOpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void OnClickClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        PacketManager.Instance.OnInvitePlayerResultEvent += OnInvitePlayerResult;
    }

    private void OnDisable()
    {
        if (PacketManager.Instance != null)
            PacketManager.Instance.OnInvitePlayerResultEvent -= OnInvitePlayerResult;
    }

    // 초대 버튼 클릭 시 호출. 입력 검증 후 NetManager.SendInvitePlayer로 C_INVITE_PLAYER 패킷 전송.
    private void OnClickInvite()
    {
        // 입력값 추출 (null 체크)
        string playerName = targetPlayerNameInput != null ? targetPlayerNameInput.text.Trim() : "";
        string tagStr = targetPlayerTagInput != null ? targetPlayerTagInput.text.Trim() : "";

        // 유저 이름 검증
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("[LobbyInviteUI] 초대할 유저 이름을 입력하세요.");
            return;
        }

        // 태그는 숫자여야 함
        if (!int.TryParse(tagStr, out int playerTag))
        {
            Debug.LogWarning("[LobbyInviteUI] 유저 태그(숫자)를 입력하세요. 예: 1234");
            return;
        }

        // 서버 연결 여부 확인
        if (!NetManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LobbyInviteUI] 서버에 연결되지 않았습니다.");
            return;
        }

        // C_INVITE_PLAYER 패킷 전송 → 서버가 S_INVITE_PLAYER(보낸 사람), S_INVITE_NOTIFICATION(받는 사람) 처리
        NetManager.Instance.SendInvitePlayer(playerName, playerTag);
    }

    private void OnInvitePlayerResult(S_INVITE_PLAYER packet)
    {
        if (packet.Success)
            Debug.Log($"[LobbyInviteUI] 초대 전송 성공: {packet.PlayerName}#{packet.PlayerTag}");
        else
            Debug.LogWarning($"[LobbyInviteUI] 초대 전송 실패: {packet.ErrorMsg}");
    }
}

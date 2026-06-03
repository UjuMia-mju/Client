using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Protocol;

// 로비에서 초대 보내기 UI.
// 이 스크립트는 "항상 활성"인 오브젝트(Canvas, 로비 빈 오브젝트 등)에 붙여야 함.
// InvitePanel에 붙이면 패널이 비활성일 때 Start()가 안 돌아서 "초대" 버튼 리스너가 등록되지 않음.
public class LobbyInviteUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panelRoot;   // InvitePanel 루트 (초기 비활성, 초대 버튼으로만 열림)

    [Header("열기/닫기")]
    [SerializeField] private Button openInvitePanelButton;  // 로비에 둘 "초대" 버튼 → 누르면 패널 활성화
    //[SerializeField] private Button closeButton;           // 패널 안 "닫기" 버튼 (선택)

    [Header("초대 입력")]
    [SerializeField] private TMP_InputField targetPlayerNameInput;
    [SerializeField] private TMP_InputField targetPlayerTagInput;

    [Header("버튼")]
    [SerializeField] private Button inviteButton;   // 초대 전송 버튼

    private bool _inRoom = false;

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (openInvitePanelButton != null)
        {
            UiButtonHoverSfx.Register(openInvitePanelButton);
            openInvitePanelButton.onClick.AddListener(OnClickOpenPanel);
        }
        else
        {
            Debug.LogWarning("[LobbyInviteUI] Open Invite Panel Button이 할당되지 않았습니다. 인스펙터에서 연결하세요.");
        }

        if (panelRoot == null)
            Debug.LogWarning("[LobbyInviteUI] Panel Root가 할당되지 않았습니다. InvitePanel을 연결하세요.");
            
        // 닫기 버튼을 UI에서 다시 사용하려면 아래 주석을 해제하세요.
        // if (closeButton != null)
        //     closeButton.onClick.AddListener(OnClickClosePanel);
        
        if (inviteButton != null)
        {
            UiButtonHoverSfx.Register(inviteButton);
            inviteButton.onClick.AddListener(OnClickInvite);
        }

        // 방 입장(S_ENTER_ROOM) 성공 전에는 초대 불가
        // if (inviteButton != null)
        //     inviteButton.interactable = false;
    }

    public bool IsInvitePanelOpen => panelRoot != null && panelRoot.activeSelf;

    /// <summary><see cref="ScenePauseMenuController"/> ESC 처리 전에 초대 패널만 닫을 때 사용.</summary>
    public bool TryCloseInvitePanelFromEscape()
    {
        if (!IsInvitePanelOpen)
            return false;

        OnClickClosePanel();
        return true;
    }

    private void OnClickOpenPanel()
    {
        SoundManager.Instance.PlaySFX("Click2");

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    private void OnClickClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        PacketHandler.Instance.OnInvitePlayerResultEvent += OnInvitePlayerResult;
        PacketHandler.Instance.OnEnterRoomEvent += OnEnterRoom;
    }

    private void OnDisable()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnInvitePlayerResultEvent -= OnInvitePlayerResult;
            PacketHandler.Instance.OnEnterRoomEvent -= OnEnterRoom;
        }
    }

    private void OnEnterRoom(S_ENTER_ROOM packet)
    {
        _inRoom = packet.Success;
        if (inviteButton != null)
            inviteButton.interactable = _inRoom;
    }

    // 초대 버튼 클릭 시 호출. 입력 검증 후 NetManager.SendInvitePlayer로 C_INVITE_PLAYER 패킷 전송.
    private void OnClickInvite()
    {
        SoundManager.Instance.PlaySFX("Click2");

        // if (!_inRoom)
        // {
        //     Debug.LogWarning("[LobbyInviteUI] 아직 방 입장이 완료되지 않았습니다. 잠시 후 다시 시도하세요.");
        //     return;
        // }

        // 입력값 추출 (null 체크)
        string playerName = targetPlayerNameInput != null ? targetPlayerNameInput.text.Trim() : "";
        string tagStr = targetPlayerTagInput != null ? targetPlayerTagInput.text.Trim() : "";

        // 유저 이름 검증
        if (string.IsNullOrEmpty(playerName))
        {
            MessageManager.Instance?.ShowKey(MessageKeys.InviteTargetNameRequired);
            return;
        }

        // 태그는 숫자여야 함
        if (!int.TryParse(tagStr, out int playerTag))
        {
            MessageManager.Instance?.ShowKey(MessageKeys.InviteTargetTagInvalid);
            return;
        }

        // 서버 연결 여부 확인
        if (!NetManager.Instance.IsConnected)
        {
            MessageManager.Instance?.ShowKey(MessageKeys.NotConnected);
            return;
        }

        if (IsSelfInvite(playerName, playerTag))
        {
            MessageManager.Instance?.ShowKey(MessageKeys.InviteSelfNotAllowed);
            return;
        }

        // C_INVITE_PLAYER 패킷 전송 → 서버가 S_INVITE_PLAYER(보낸 사람), S_INVITE_NOTIFICATION(받는 사람) 처리
        PacketDispatcher.Instance.SendInvitePlayer(playerName, playerTag);
    }

    private static bool IsSelfInvite(string targetName, int targetTag)
    {
        var nm = NetManager.Instance;
        if (nm == null) return false;

        // 닉네임#태그 조합으로 식별 — 태그만 같아도 다른 유저일 수 있음
        string myName = RoomMemberDisplayCache.WithoutDiscriminatorTag(nm.PlayerName);
        string theirName = RoomMemberDisplayCache.WithoutDiscriminatorTag(targetName);
        if (string.IsNullOrEmpty(myName) || string.IsNullOrEmpty(theirName))
            return false;

        return targetTag == nm.PlayerTag
            && string.Equals(myName, theirName, StringComparison.OrdinalIgnoreCase);
    }

    private void OnInvitePlayerResult(S_INVITE_PLAYER packet)
    {
        if (packet.Success)
        {
            Debug.Log($"[LobbyInviteUI] 초대 전송 성공: {packet.PlayerName}#{packet.PlayerTag}");
            MessageManager.Instance?.ShowKey(MessageKeys.InviteSentSuccess);

            // 초대 요청 후 패널 닫기
            OnClickClosePanel();
        }
        else
        {
            if (IsSelfInviteServerError(packet.ErrorMsg))
                MessageManager.Instance.ShowKey(MessageKeys.InviteSelfNotAllowed);
            else
                MessageManager.Instance.ShowInvitePlayerFailed(packet.ErrorMsg);
        }
    }

    private static bool IsSelfInviteServerError(string errorMsg)
    {
        if (string.IsNullOrWhiteSpace(errorMsg)) return false;
        string msg = errorMsg.Trim();
        return msg.IndexOf("self", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("yourself", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.Contains("자기")
            || msg.Contains("본인");
    }
}
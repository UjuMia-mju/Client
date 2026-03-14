using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Protocol;

// 초대 알림 팝업.
// PacketManager.OnInviteNotificationEvent를 구독하여 S_INVITE_NOTIFICATION 패킷 수신 시 팝업 표시.
// 수락/거절 버튼 클릭 시 NetManager.SendInviteResponse로 C_INVITE_RESPONSE 패킷을 서버에 전송한다.
public class LobbyInvitePopup : MonoBehaviour
{
    [Header("팝업")]
    [SerializeField] private GameObject popupRoot;   // 팝업 Panel 오브젝트 (초기에는 비활성화)
    [SerializeField] private TextMeshProUGUI messageText;   // 초대 메시지 표시용 Text

    [Header("버튼")]
    [SerializeField] private Button acceptButton;    // 수락 버튼
    [SerializeField] private Button declineButton;   // 거절 버튼

    // 현재 초대의 invite_id (C_INVITE_RESPONSE 전송 시 사용)
    private ulong _inviteId;
    // 초대된 방 ID (수락 시 C_ENTER_ROOM 전송에 사용 가능)
    private ulong _roomId;

    // 활성화 시 이벤트 구독. S_INVITE_NOTIFICATION 수신 시 OnInviteNotification 호출됨.
    private void OnEnable()
    {
        PacketManager.Instance.OnInviteNotificationEvent += OnInviteNotification;
        PacketManager.Instance.OnInviteResponseResultEvent += OnInviteResponseResult;
    }

    // 비활성화 시 이벤트 구독 해제
    private void OnDisable()
    {
        if (PacketManager.Instance != null)
        {
            PacketManager.Instance.OnInviteNotificationEvent -= OnInviteNotification;
            PacketManager.Instance.OnInviteResponseResultEvent -= OnInviteResponseResult;
        }
    }

    // 초기화: 팝업 비활성화, 수락/거절 버튼 클릭 리스너 등록
    private void Start()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnClickAccept);
        if (declineButton != null)
            declineButton.onClick.AddListener(OnClickDecline);
    }

    // S_INVITE_NOTIFICATION 패킷 수신 시 호출. invite_id 저장 후 팝업에 메시지 표시하고 팝업 활성화.
    private void OnInviteNotification(S_INVITE_NOTIFICATION packet)
    {
        _inviteId = packet.InviteId;  // 수락/거절 시 C_INVITE_RESPONSE에 전달할 ID
        _roomId = packet.RoomId;

        if (messageText != null)
            messageText.text = $"{packet.InviterName} 님이 '{packet.RoomName}' 방으로 초대했습니다.";

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    // 수락 버튼 클릭 시 C_INVITE_RESPONSE(inviteId, accept: true) 전송 후 팝업 숨김
    private void OnClickAccept()
    {
        NetManager.Instance.SendInviteResponse(_inviteId, true);

        // 서버 구현에 따라 '수락'만 보내면 자동으로 S_ENTER_ROOM을 내려줄 수도 있지만,
        // 클라가 명시적으로 입장을 요청하는 구조라면 아래 호출이 필요하다.
        NetManager.Instance.SendEnterRoom(_roomId);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    // 거절 버튼 클릭 시 C_INVITE_RESPONSE(inviteId, accept: false) 전송 후 팝업 숨김
    private void OnClickDecline()
    {
        NetManager.Instance.SendInviteResponse(_inviteId, false);
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnInviteResponseResult(S_INVITE_RESPONSE packet)
    {
        if (!packet.Success)
            Debug.LogWarning($"[LobbyInvitePopup] 초대 응답 처리 실패: {packet.ErrorMsg}");
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Protocol;

// 초대 알림 팝업.
// PacketManager.OnInviteNotificationEvent를 구독하여 S_INVITE_NOTIFICATION 패킷 수신 시 팝업 표시.
// 팝업 패널을 "비활성"으로 두면 이 스크립트의 OnEnable이 안 돌아서 이벤트 구독이 안 됨.
// "항상 활성" 오브젝트(Canvas, 빈 InvitePopupManager 등)에 붙여야합니다
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

    // 수락 버튼 클릭 시 C_INVITE_RESPONSE(inviteId, accept: true) 전송 후 방 입장 요청, 메인에 있으면 로비 씬으로 이동
    private void OnClickAccept()
    {
        NetManager.Instance.SendInviteResponse(_inviteId, true);
        NetManager.Instance.SendEnterRoom(_roomId);

        if (popupRoot != null)
            popupRoot.SetActive(false);

        // 메인(또는 로비가 아닌 씬)에 있으면 로비 씬으로 이동 → 방 입장 화면으로
        if (SceneManager.GetActiveScene().name != Define.Scene.LOBBY)
            SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
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

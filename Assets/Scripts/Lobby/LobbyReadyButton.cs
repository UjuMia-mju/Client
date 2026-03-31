using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Protocol;

/// <summary>
/// 로비에서 준비 버튼. 클릭 시 C_READY(is_ready)를 서버로 보내고,
/// S_READY 수신 시 본인 상태를 UI에 맞춥니다.
/// </summary>
public class LobbyReadyButton : MonoBehaviour
{
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text labelText; // 선택: "준비" / "준비 해제" 등

    /// <summary>클라이언트가 알고 있는 본인 레디 상태 (S_READY로 동기화)</summary>
    private bool _localReady;

    private void OnEnable()
    {
        _localReady = false;
        UpdateLabel(false);

        if (readyButton != null)
            readyButton.onClick.AddListener(OnClickReady);

        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnReadyEvent += OnReadyPacket;
        }
            
    }

    private void OnDisable()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnClickReady);

        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnReadyEvent -= OnReadyPacket;
    }

    private void OnClickReady()
    {
        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LobbyReadyButton] 서버에 연결되지 않았습니다.");
            return;
        }

        bool next = !_localReady;
        PacketDispatcher.Instance.SendReady(next);
        _localReady = next;
        UpdateLabel(next);
    }

    private void OnReadyPacket(S_READY packet)
    {
        if (NetManager.Instance == null)
            return;
        if ((int)packet.PlayerId != NetManager.Instance._playerId)
            return;

        _localReady = packet.IsReady;
        UpdateLabel(packet.IsReady);
    }

    private void UpdateLabel(bool ready)
    {
        if (labelText != null)
            // 한글이 네모로 보여서 임시로 Ready로 변경
            // labelText.text = ready ? "준비 해제" : "준비";
            labelText.text = "Ready";
    }
}


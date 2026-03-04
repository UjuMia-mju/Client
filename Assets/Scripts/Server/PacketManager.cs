using UnityEngine;
using Google.Protobuf;
using Protocol;
using UnityEngine.SceneManagement;

public class PacketManager : Singleton<PacketManager>
{
    // 이벤트
    public event System.Action<S_LOGIN> OnLoginResultEvent;
    public event System.Action<S_ENTER_GAME> OnEnterGameResultEvent;
    public event System.Action<S_PLAYER_LIST> OnPlayerListEvent;
    public event System.Action<S_PLAYER_ENTER> OnPlayerEnterEvent;
    public event System.Action<S_PLAYER_LEAVE> OnPlayerLeaveEvent;
    public event System.Action<S_MOVE> OnMoveEvent;
    public event System.Action<S_CHAT> OnChatEvent;
    public event System.Action<S_ANIMATION> OnAnimationEvent;
    public void HandlePacket(PacketId packetId, byte[] data)
    {
        Debug.Log($"Received packet with ID: {packetId}, Size: {data.Length} bytes");
        switch (packetId)
        {
            case PacketId.PKT_S_LOGIN: 
                HandleLoginResult(data);
                break;
            case PacketId.PKT_S_ENTER_GAME: 
                HandleEnterGameResult(data);
                break;
            case PacketId.PKT_S_CHAT: 
                HandleChat(data);
                break;
            case PacketId.PKT_S_PLAYER_LIST: 
                HandlePlayerList(data);
                break;
            case PacketId.PKT_S_PLAYER_ENTER: 
                HandlePlayerEnter(data);
                break;
            case PacketId.PKT_S_PLAYER_LEAVE: 
                HandlePlayerLeave(data);
                break;
            case PacketId.PKT_S_MOVE: 
                HandleMove(data);
                break;
            case PacketId.PKT_S_ANIMATION:
                HandleAnimation(data);
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }


    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data);  // ← S_LOGIN 사용
        
        if (result.Success)
        {
            Debug.Log($"✓ Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}");
            Debug.Log($"  Player Name: {result.Player.Name}");

            OnLoginResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data);  // ← S_ENTER_GAME 사용

        if (result.Success)
        {
            Debug.Log("✓ Entered Game Successfully!");
            OnEnterGameResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Failed to Enter Game!");
        }
    }

    private void HandlePlayerList(byte[] payloadData)
    {
        S_PLAYER_LIST packet = S_PLAYER_LIST.Parser.ParseFrom(payloadData);
        OnPlayerListEvent?.Invoke(packet);
    }

    private void HandlePlayerEnter(byte[] payloadData)
    {
        S_PLAYER_ENTER packet = S_PLAYER_ENTER.Parser.ParseFrom(payloadData);
        OnPlayerEnterEvent?.Invoke(packet);
    }

    private void HandlePlayerLeave(byte[] payloadData)
    {
        S_PLAYER_LEAVE packet = S_PLAYER_LEAVE.Parser.ParseFrom(payloadData);
        OnPlayerLeaveEvent?.Invoke(packet);
    }

    private void HandleMove(byte[] payloadData)
    {
        S_MOVE packet = S_MOVE.Parser.ParseFrom(payloadData);
        OnMoveEvent?.Invoke(packet);
    }

    private void HandleChat(byte[] payloadData)
    {
        S_CHAT packet = S_CHAT.Parser.ParseFrom(payloadData);
        OnChatEvent?.Invoke(packet);
    }

    private void HandleAnimation(byte[] payloadData)
    {
        S_ANIMATION packet = S_ANIMATION.Parser.ParseFrom(payloadData);
        OnAnimationEvent?.Invoke(packet);
    }
}

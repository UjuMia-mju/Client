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
    public event System.Action<S_SHOW_STAGE> OnShowStageEvent;

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
            case PacketId.PKT_S_SHOW_STAGE:
                HandleShowStage(data);
                break;
            default:
                Debug.LogWarning($"Unhandled packet ID: {packetId}");
                break;
        }
    }


    private void HandleLoginResult(byte[] data)
    {
        S_LOGIN result = S_LOGIN.Parser.ParseFrom(data); 

        if (result.Success)
        {
            Debug.Log($"✓ Login Success!");
            Debug.Log($"  Player ID: {result.Player.Id}, Name: {result.Player.Name}");
            if (result.PlayerInfo != null)
                Debug.Log($"  PlayerInfo: coin={result.PlayerInfo.Coin}, gem={result.PlayerInfo.Gem}, owned_skins={result.PlayerInfo.OwnedSkins.Count}");

            OnLoginResultEvent?.Invoke(result);
        }
        else
        {
            Debug.LogError("✗ Login Failed!");
        }
    }

    private void HandleEnterGameResult(byte[] data)
    {
        S_ENTER_GAME result = S_ENTER_GAME.Parser.ParseFrom(data); 

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
    
    private void HandleShowStage(byte[] payloadData)
    {
        S_SHOW_STAGE packet = S_SHOW_STAGE.Parser.ParseFrom(payloadData);
        
        OnShowStageEvent?.Invoke(packet);
    }
}
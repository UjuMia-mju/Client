using UnityEngine;

public class TestConnectManager : SceneSingleton<TestConnectManager>
{
    void Start()
    {
        NetManager.Instance.Connect("127.0.0.1", 7777);
    }

    public void SendLogin(string userId, string password)
    {
        NetManager.Instance.SendLogin(userId, password);
    }
}

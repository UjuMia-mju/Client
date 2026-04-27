using System.Net.Sockets;
using UnityEngine;

// 아이템의 곡괭이로써의 기능을 담당하는 클래스입니다.
public class Pickaxe : Items
{
    private bool hasMined = false; // 이미 채굴했는지 여부

    private void Update()
    {

    }

    private void OnTriggerStay(Collider other)
    {
        // 부모 이름이 "Socket"이면 내가 손에 들고 있는 상태라고 판단
        if (transform.parent != null && transform.parent.name == SOCKET)
        {
            Player player = GetComponentInParent<Player>();
            if (player != null)
            {
                if (!hasMined && player.isUsingTool && other.CompareTag(Define.Tag.ORE))
                {
                    Ore o = other.GetComponent<Ore>();
                    if (o != null)
                    {
                        o.Mine();
                        hasMined = true;
                    }
                }
            }
        }
    }

    public void ResetHasMined()
    {
        hasMined = false;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGravityController : MonoBehaviour
{
    public PlanetGravity planet;

    private Player player;

    private void Start()
    {
        //planet = GameObject.FindGameObjectWithTag("Planet").GetComponent<PlanetGravity>();
        player = GetComponent<Player>();
        player.rb.useGravity = false;  // 리지드바디의 기본 중력은 필요 없으므로 비활성화
        player.rb.constraints = RigidbodyConstraints.FreezeRotation;	// 꼿꼿히 세울 것이므로 회전을 비활성화함
    }

    private void FixedUpdate()
    {
        planet.AttractPlayer(transform);
        player.rb.useGravity = false;
    }
}
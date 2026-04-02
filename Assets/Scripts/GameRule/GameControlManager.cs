using System.Collections;
using UnityEngine;

public class GameControlManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject remotePlayerPrefab;
    [SerializeField] private PlanetGravity planetGravity;
    [SerializeField] private PlayerTPCamera playerTPCamera;
    [SerializeField] private Vector3[] spawnPoints;
    void Start()
    {
        //StartCoroutine(StartGameCo());
    }

    IEnumerator StartGameCo()
    {
        yield return new WaitForSecondsRealtime(1f);
        GameObject player = Instantiate(playerPrefab, spawnPoints[0], Quaternion.identity);
        player.GetComponent<PlayerGravityController>().planet = planetGravity;
        playerTPCamera.cameraOffset = player.transform.GetChild(2); // 카메라 오프셋 설정 (위험한 코드임 나중에 수정요함.)
    }
}

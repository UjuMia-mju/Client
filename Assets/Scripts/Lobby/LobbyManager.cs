using System.Collections;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Vector3 spawnCenter; // 스폰될 왼쪽 구역의 중심 좌표
    public float spawnRadius = 4f; // 스폰 허용 반경

    [Header("우주선 세팅")]
    public Transform spaceshipDoor; // 우주선 문(빨려 들어갈 목표 지점)
    public float suckDuration = 2.0f; // 빨려 들어가는 데 걸리는 시간
    // public AnimationCurve suckCurve; // 빨려 들어가는 속도 조절을 위한 애니메이션 커브

    // 임시 테스트용
    public void OnAllPlayersReady()
    {
        StartCoroutine(StartSpaceshipSequence());
    }

    IEnumerator StartSpaceshipSequence()
    {
        Debug.Log("모두 레디 완료");

        // // 플레이어들의 초기 위치와 크기를 기억할 배열 준비
        // Vector3[] startPositions = new Vector3[players.Length];
        // Vector3[] startScales = new Vector3[players.Length];

        // 1. 씬에 있는 모든 플레이어 찾기 (Player태그)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // 2. 빨려 들어가기 전 물리 효과 끄기
        foreach (GameObject player in players)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 더 이상 튕기지 않고 물리 연산을 멈춤.
                rb.isKinematic = true; 
            }
            
            Collider col = player.GetComponent<Collider>();
            if (col != null)
            {
                // 서로 부딪히거나 우주선 벽에 막히지 않도록 충돌체도 비활성화.
                col.enabled = false;
            }
        }

        float elapsedTime = 0f;

        // 3. 우주선으로 빨려 들어가는 연출 (위치 이동 + 크기 축소)
        while (elapsedTime < suckDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 0에서 1로 변하는 진행률
            float t = elapsedTime / suckDuration; 
            
            // 점점 빠르게 빨려 들어가는 느낌을 주려면 가속도 곡선 적용
            float curve = t * t; 

            foreach (GameObject player in players)
            {
                if (player != null)
                {
                    // 위치를 우주선 문 쪽으로 부드럽게 이동
                    player.transform.position = Vector3.Lerp(player.transform.position, spaceshipDoor.position, curve);
                    
                    // 크기를 점점 0으로 줄여서 쏙 들어가는 느낌 극대화
                    player.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, curve);
                }
            }

            yield return null;
        }

        // 4. 탑승 완료 후 대기
        Debug.Log("탑승 완료! 우주선 출발!");
        
        // (선택) 여기서 우주선이 화면 밖으로 날아가는 애니메이션을 실행.
        yield return new WaitForSeconds(1.5f); 

        Debug.Log("인게임 씬으로 넘어갑니다.");
    }

    // 누군가 방에 들어왔을 때 호출할 함수
    public void SpawnNewPlayer()
    {
        Vector3 safePosition = GetSafeSpawnPosition();
        Instantiate(playerPrefab, safePosition, Quaternion.identity);
    }

    // 빈 공간을 찾는 함수
    Vector3 GetSafeSpawnPosition()
    {
        int maxAttempts = 10; // 최대 10번 빈 공간을 찾음
        float playerRadius = 1f; // 플레이어의 대략적인 충돌체 반지름

        for (int i = 0; i < maxAttempts; i++)
        {
            // 중심점 주변 랜덤 위치 생성
            Vector3 randomPos = spawnCenter + Random.insideUnitSphere * spawnRadius;
            randomPos.z = spawnCenter.z; // Z축 고정

            // 해당 위치에 겹치는 다른 콜라이더(플레이어)가 없는지 확인
            if (!Physics.CheckSphere(randomPos, playerRadius))
            {
                return randomPos; // 안전한 빈 공간 발견 시 해당 위치 반환
            }
        }
        
        // 10번 다 실패했으면 그냥 중심에 스폰 (가장자리에 다 몰려있을 경우를 대비)
        return spawnCenter; 
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 씬에서 플레이어 캐릭터(LobbyAstronut) 스폰/디스폰, 우주선 탑승 연출을 담당합니다.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("로비 플레이어 스폰")]
    public GameObject playerPrefab;           // LobbyAstronut 프리팹
    public Vector3 spawnCenter;               // 스폰될 구역의 중심 좌표
    public float spawnRadius = 4f;            // 스폰 허용 반경 (중복 방지용)

    /// <summary>현재 스폰된 로비 캐릭터들. key = 플레이어 ID (퇴장 시 디스폰/레디 토글에 사용)</summary>
    private readonly Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();

    [Header("우주선 세팅")]
    public Transform spaceshipDoor;   // 우주선 문(빨려 들어갈 목표 지점)
    public float suckDuration = 2.0f; // 빨려 들어가는 데 걸리는 시간(초)

    /// <summary>전원 레디 시 호출. 우주선 빨려 들어가는 연출 후 인게임 씬 전환용.</summary>
    /// <param name="skipReadyValidation">true면 클라이언트 레디 검사 생략 (서버가 S_START_ROOM 성공을 준 경우 등)</param>
    public void OnAllPlayersReady(bool skipReadyValidation = false)
    {
        if (!skipReadyValidation && !AreAllSpawnedPlayersReady())
        {
            Debug.Log("[LobbyManager] 전원 준비가 아니어서 우주선 연출(StartSpaceshipSequence)을 실행하지 않습니다.");
            return;
        }

        StartCoroutine(StartSpaceshipSequence());
    }

    /// <summary>스폰된 로비 멤버가 1명 이상이고, 모두 레디인지 여부.</summary>
    public bool AreAllSpawnedPlayersReady()
    {
        if (spawnedPlayers.Count == 0)
            return false;

        foreach (var kv in spawnedPlayers)
        {
            GameObject go = kv.Value;
            if (go == null)
                continue;

            var slot = go.GetComponentInChildren<LobbyPlayerSlot>();
            if (slot == null || !slot.IsReady)
                return false;
        }

        return true;
    }

    /// <summary>우주선 문으로 플레이어들이 빨려 들어가는 연출 코루틴.</summary>
    IEnumerator StartSpaceshipSequence()
    {
        Debug.Log("모두 레디 완료");


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
        // 우주선 연출이 끝난 뒤 인게임 씬으로 전환.
        SceneLoader.Instance.LoadScene(Define.Scene.GAME_1_1);
    }

    /// <summary>기존에 스폰된 로비 플레이어를 모두 제거합니다. (방 입장 시 S_ENTER_ROOM 처리 전 초기화용)</summary>
    public void ClearSpawnedPlayers()
    {
        foreach (var go in spawnedPlayers.Values)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedPlayers.Clear();
    }

    /// <summary>해당 플레이어 ID의 로비 캐릭터를 제거합니다. (플레이어 퇴장 시 호출)</summary>
    public void DespawnPlayer(int playerId)
    {
        if (spawnedPlayers.TryGetValue(playerId, out GameObject go))
        {
            spawnedPlayers.Remove(playerId);
            if (go != null)
                Destroy(go);
        }
    }

    /// <summary>S_READY 등으로 받은 레디 상태를 해당 플레이어 슬롯에 반영합니다.</summary>
    public void SetPlayerReadyState(int playerId, bool isReady)
    {
        if (!spawnedPlayers.TryGetValue(playerId, out GameObject go) || go == null)
            return;
        var slot = go.GetComponentInChildren<LobbyPlayerSlot>();
        if (slot != null)
            slot.SetReady(isReady);
    }

    /// <summary>플레이어 이름을 가진 LobbyAstronut을 스폰합니다. (로비 입장/멤버 입장 시 이름 표시)</summary>
    /// <param name="playerName">표시할 플레이어 이름 (Name 라벨에 출력)</param>
    /// <param name="playerId">플레이어 ID (퇴장 시 DespawnPlayer에 사용)</param>
    /// <param name="isReady">입장 시점 서버 레디 여부 (RoomMemberInfo.is_ready)</param>
    public void SpawnNewPlayer(string playerName, int playerId, bool isReady = false)
    {
        Vector3 safePosition = GetSafeSpawnPosition();
        GameObject newPlayer = Instantiate(playerPrefab, safePosition, Quaternion.Euler(0, 180f, 0));
        spawnedPlayers[playerId] = newPlayer;

        // LobbyPlayerSlot이 없으면 루트에 추가 (프리팹에 미리 붙여두지 않아도 동작)
        var slot = newPlayer.GetComponentInChildren<LobbyPlayerSlot>();
        if (slot == null)
            slot = newPlayer.AddComponent<LobbyPlayerSlot>();
        if (slot != null)
        {
            slot.SetPlayerName(playerName);
            slot.SetReady(isReady);
        }

        Animator anim = newPlayer.GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("bIsInLobby");
    }

    /// <summary>spawnCenter 주변에서 다른 플레이어와 겹치지 않는 위치를 반환합니다.</summary>
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
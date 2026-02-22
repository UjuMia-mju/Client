using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    [Header("Stage Nodes")]
    // 관리할 모든 스테이지 노드들
    public List<StageNode> stageNodes = new List<StageNode>();

    [Header("Global Settings")]
    [Tooltip("true가 되면 모든 행성의 자전과 공전이 멈춥니다.")]
    public bool isMovementPaused = false; 

    private void Start()
    {
        // 씬이 시작될 때 씬에 있는 모든 StageNode를 자동으로 찾아서 리스트에 넣음
        // (원한다면 Inspector 창에서 수동으로 드래그해서 넣어도 돼)
        if (stageNodes.Count == 0)
        {
            stageNodes = new List<StageNode>(Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None));
        }
    }

    private void Update()
    {
        // 매 프레임마다 리스트에 있는 모든 구체를 순회하며 업데이트
        foreach (var node in stageNodes)
        {
            // Hover로 인한 크기 변환은 UI 애니메이션이므로 일시정지와 무관하게 항상 실행
            node.UpdateScale(Time.deltaTime);

            // 움직임이 일시정지 상태가 아닐 때만 자전/공전 실행
            if (!isMovementPaused)
            {
                node.UpdateMovement(Time.deltaTime);
            }
        }
    }

    // MenuManager에서 패널을 열고 닫을 때 이 함수를 호출하여 움직임을 제어할 수 있음
    public void SetMovementPause(bool pause)
    {
        isMovementPaused = pause;
    }
}
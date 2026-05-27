using UnityEditor;
using UnityEngine;

public static class PlanetScatterToolEditor
{
    [MenuItem("Tools/Planet Scatter/Scatter Active Scene")]
    static void ScatterActiveScene()
    {
        var tools = Object.FindObjectsByType<PlanetScatterTool>(FindObjectsSortMode.None);
        if (tools.Length == 0)
        {
            Debug.LogWarning("씬에 PlanetScatterTool이 없습니다.");
            return;
        }

        Undo.SetCurrentGroupName("Planet Scatter");
        int group = Undo.GetCurrentGroup();
        foreach (var tool in tools)
        {
            Undo.RegisterFullObjectHierarchyUndo(tool.gameObject, "Planet Scatter");
            tool.Scatter();
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"PlanetScatterTool Scatter 완료 ({tools.Length}개).");
    }

    [MenuItem("Tools/Planet Scatter/Clear Active Scene")]
    static void ClearActiveScene()
    {
        var tools = Object.FindObjectsByType<PlanetScatterTool>(FindObjectsSortMode.None);
        if (tools.Length == 0)
        {
            Debug.LogWarning("씬에 PlanetScatterTool이 없습니다.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Planet Scatter", "활성 씬의 Scatter 자식을 모두 지울까요?", "Clear", "Cancel"))
            return;

        Undo.SetCurrentGroupName("Planet Scatter Clear");
        int group = Undo.GetCurrentGroup();
        foreach (var tool in tools)
        {
            Undo.RegisterFullObjectHierarchyUndo(tool.gameObject, "Planet Scatter Clear");
            tool.ClearChildren();
        }

        Undo.CollapseUndoOperations(group);
    }
}

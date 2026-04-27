using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 유니티의 고유 시스템(Scene, Tag, Layer 등) 이름 관리
/// </summary>
public static class Define
{
    public static class Scene
    {
        public const string SPLASH = "Splash";
        public const string MAIN   = "Main";
        public const string GACHA  = "Gacha";
        public const string LOBBY  = "Lobby";
        public const string STAGE_SELECT = "StageSelect";
        public const string LOGIN = "Login";
        public const string GAME_1_1   = "Stage01Level01";
        public const string GAME_1_2   = "Stage01Level02";
        public const string GAME_1_3   = "Stage01Level03";
        public const string GAME_1_4   = "Stage01Level04";
        public const string GAME_1_5   = "Stage01Level05";

        /// <summary>
        /// DB/서버 스테이지 식별자(map, chapter, stage)에 대응하는 인게임 씬 이름.
        /// StageSelect → 실제 플레이 씬 전환 시 사용.
        /// </summary>
        public static bool TryGetGameplayScene(int mapId, int chapter, int stage, out string sceneName)
        {
            sceneName = null;
            if (mapId == 1 && chapter == 1)
            {
                switch (stage)
                {
                    case 1:
                        sceneName = GAME_1_1;
                        return true;
                    case 2:
                        sceneName = GAME_1_2;
                        return true;
                    case 3:
                        sceneName = GAME_1_3;
                        return true;
                    case 4:
                        sceneName = GAME_1_4;
                        return true;
                    case 5:
                        sceneName = GAME_1_5;
                        return true;
                }
            }

            return false;
        }
    }

    public static class Tag
    {
        public const string PLAYER = "Player";
        public const string ITEM = "Item";
        public const string CRAFT_TABLE = "CraftTable";
        public const string MAINBUTTON = "MainButton";
        public const string TOOL = "Tool";
        public const string ORE = "Ore";
        public const string FURNACE = "Furnace";
        public const string CLICKOFF = "ClickOff";
        public const string SPACESHIP = "Spaceship";
        public const string RESPAWN_SPOT = "RespawnSpot";    
        public const string PLANET = "Planet";
        public const string TREE = "Tree";
    }

    public static class Layer
    {
        public const string GROUND = "Ground";
        public const string WALL = "Wall";
        public const string WALKABLE_COLLIDER = "WalkableCollider";
    }

    public static readonly List<Vector2Int> Resolution = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080), // Index 0 (FHD)
        new Vector2Int(2560, 1440), // Index 1 (QHD)
        new Vector2Int(3840, 2160)  // Index 2 (UHD)
    };
    
    public static class KeyName
    {
        public const string up = "위쪽 이동";
        public const string down = "아래쪽 이동";
        public const string left = "왼쪽 이동";
        public const string right = "오른쪽 이동";
        public const string move = "이동";
        
        public const string jump = "점프";
        public const string @throw = "던지기"; // throw는 C# 예약어이므로 @를 붙여서 사용
        public const string interact = "상호작용";
    }
}
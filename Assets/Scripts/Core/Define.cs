using System.Collections.Generic;
using UnityEngine;

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
        public const string GAME_1_1   = "Stage01Level01";
    }

    public static class Tag
    {
        public const string PLAYER = "Player";
        public const string ITEM = "Item";
        public const string CRAFT_TABLE = "CraftTable";
        public const string MAINBUTTON = "MainButton";
        public const string PICKAXE = "Pickaxe";
        public const string ORE = "Ore";
        public const string CLICKOFF = "ClickOff";
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
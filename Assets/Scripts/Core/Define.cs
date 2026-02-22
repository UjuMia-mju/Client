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
        public const string GAME   = "Stage01Level01";
        public const string StageSelect  = "StageSelect";
    }

    public static class Tag
    {
        public const string PLAYER = "Player";
        public const string ITEM = "Item";
        public const string CRAFT_TABLE = "CraftTable";
        public const string MAINBUTTON = "MainButton";
    }

    public static class Layer
    {
        public const string GROUND = "Ground";
        public const string WALL = "Wall";
    }

    public static readonly List<Vector2Int> Resolution = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080), // Index 0 (FHD)
        new Vector2Int(2560, 1440), // Index 1 (QHD)
        new Vector2Int(3840, 2160)  // Index 2 (UHD)
    };
}
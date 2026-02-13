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
        public const string GAME   = "Game";
    }

    public static class Tag
    {
        public const string PLAYER = "Player";
        public const string ITEM = "Item";
        public const string CRAFT_TABLE = "CraftTable";
    }

    public static class Layer
    {
        public const string GROUND = "Ground";
        public const string WALL = "Wall";
    }
}
namespace Gameplay.Grid {
    /// <summary>
    /// Different types of <see cref="GameplayTile"/>s. Multiple <see cref="GameplayTile"/>s can share the same types, like
    /// the different forest tiles that are all type forest. 
    /// </summary>
    public enum GameplayTileType {
        Grass,
        Forest,
        Mountain,
        AmberForge,
        WheatField,
        AmberDeposit,
        Road
    }
}
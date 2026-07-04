namespace HillbillyAlienShooter.Utils
{
    /// <summary>
    /// Central home for the physics layers the game relies on. We use a numeric
    /// user layer (3) for ground so no manual TagManager setup is ever required —
    /// unnamed layers work fine in code, and the scene builder assigns them.
    /// </summary>
    public static class GameLayers
    {
        /// <summary>Walkable terrain: the ground plane and hills.</summary>
        public const int Ground = 3;

        /// <summary>Raycast mask that hits only terrain.</summary>
        public const int GroundMask = 1 << Ground;
    }
}

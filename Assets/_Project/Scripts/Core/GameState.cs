namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// High-level state of a single play session.
    /// Kept intentionally small for Packet 1.1 (single wave). More states
    /// (Boarding, Homeworld, BossFight, etc.) will be added in later phases.
    /// </summary>
    public enum GameState
    {
        Boot,          // Systems initialising
        Playing,       // A wave is active
        WaveComplete,  // Wave cleared, brief breather (used more in Phase 3)
        Won,           // Player defended the farm
        Lost,          // Player died OR all cattle were rustled
        Paused
    }
}

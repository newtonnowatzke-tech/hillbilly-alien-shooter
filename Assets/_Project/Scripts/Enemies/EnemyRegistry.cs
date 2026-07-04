using UnityEngine;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.Enemies
{
    /// <summary>
    /// Shared alive-enemy counter. Introduced in Packet 2.1 when UFOs joined the
    /// ground aliens: the WaveSpawner shouldn't care HOW MANY enemy classes
    /// exist, only whether anything hostile is still breathing. Every enemy type
    /// registers in OnEnable / unregisters in OnDisable.
    /// </summary>
    public static class EnemyRegistry
    {
        public static int Count { get; private set; }

        public static void Register()
        {
            Count++;
            GameEvents.RaiseEnemyCountChanged(Count);
        }

        public static void Unregister()
        {
            Count = Mathf.Max(0, Count - 1);
            GameEvents.RaiseEnemyCountChanged(Count);
        }

        /// <summary>Reset on scene load (GameManager) so fast play-mode restarts stay correct.</summary>
        public static void Reset()
        {
            Count = 0;
        }
    }
}

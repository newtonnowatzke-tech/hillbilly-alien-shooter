using System.Collections.Generic;
using UnityEngine;

namespace HillbillyAlienShooter.Data
{
    /// <summary>
    /// Describes a single wave as a list of "spawn entries" (which alien, how
    /// many). Packet 1.1 uses one wave; Packet 3.1 will feed the spawner a whole
    /// sequence of these to build escalating farm waves.
    /// </summary>
    [CreateAssetMenu(menuName = "Hillbilly/Wave Data", fileName = "WaveData_Wave1")]
    public class WaveData : ScriptableObject
    {
        [System.Serializable]
        public class SpawnEntry
        {
            public EnemyData enemy;
            [Min(1)] public int count = 8;
        }

        public string waveName = "Wave 1";
        [Tooltip("Seconds to wait after the scene loads before the first spawn.")]
        public float startDelay = 2.5f;
        [Tooltip("Seconds between individual enemy spawns.")]
        public float spawnInterval = 1.1f;

        public List<SpawnEntry> spawns = new List<SpawnEntry>();

        /// <summary>Total enemies across every entry — used to detect wave completion.</summary>
        public int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var s in spawns)
                    if (s != null) total += Mathf.Max(0, s.count);
                return total;
            }
        }

        /// <summary>Runtime fallback: one squad of default aliens.</summary>
        public static WaveData CreateDefault(EnemyData enemyData = null)
        {
            var w = CreateInstance<WaveData>();
            w.name = "WaveData_DefaultWave1 (runtime)";
            w.spawns = new List<SpawnEntry>
            {
                new SpawnEntry { enemy = enemyData != null ? enemyData : EnemyData.CreateDefault(), count = 8 }
            };
            return w;
        }
    }
}

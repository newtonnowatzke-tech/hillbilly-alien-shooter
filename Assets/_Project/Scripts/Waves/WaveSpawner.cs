using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.Enemies;
using HillbillyAlienShooter.Utils;

namespace HillbillyAlienShooter.Waves
{
    /// <summary>
    /// Spawns a single wave of aliens for Packet 1.1. Reads a <see cref="WaveData"/>
    /// SO (or falls back to a default), drips enemies in on an interval from either
    /// designer-placed spawn points or an auto-generated ring around the farm, then
    /// waits for the wave to be cleared and raises <c>WaveCompleted</c>.
    ///
    /// Structured so Packet 3.1 can hand it a LIST of waves and loop.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Wave definition (optional — falls back to a default)")]
        [SerializeField] private WaveData wave;

        [Header("Spawning")]
        [Tooltip("Optional spawn points. If empty, a ring is generated around the origin.")]
        [SerializeField] private Transform[] spawnPoints;
        [Tooltip("Radius of the auto-generated spawn ring when no spawn points are set.")]
        [SerializeField] private float spawnRingRadius = 22f;
        [Tooltip("Optional custom enemy prefab. If null, a low-poly primitive alien is built.")]
        [SerializeField] private GameObject enemyPrefab;

        [Tooltip("1-based wave number reported to the HUD/GameManager.")]
        [SerializeField] private int waveNumber = 1;

        private int _spawnedCount;
        private int _totalToSpawn;
        private bool _waveRunning;

        private IEnumerator Start()
        {
            if (wave == null)
                wave = WaveData.CreateDefault();

            _totalToSpawn = wave.TotalEnemies;

            // Small delay so the player can get their bearings (and read the banner).
            yield return new WaitForSeconds(wave.startDelay);

            GameEvents.RaiseWaveStarted(waveNumber);
            _waveRunning = true;

            yield return StartCoroutine(SpawnAll());

            // Wait until every spawned enemy — ground alien or saucer — is gone.
            while (EnemyRegistry.Count > 0)
                yield return null;

            _waveRunning = false;
            GameEvents.RaiseWaveCompleted(waveNumber);
        }

        private IEnumerator SpawnAll()
        {
            foreach (var entry in wave.spawns)
            {
                if (entry == null) continue;
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnOne(entry.enemy);
                    _spawnedCount++;
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
        }

        private void SpawnOne(EnemyData enemyData)
        {
            Vector3 pos = GetSpawnPosition();

            if (enemyPrefab != null)
            {
                var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
                var alien = go.GetComponent<AlienEnemy>();
                if (alien != null && enemyData != null) alien.Configure(enemyData);
            }
            else
            {
                // Role-aware: saucers spawn at altitude, ground aliens at the ring.
                LowPolyFactory.BuildEnemy(enemyData, pos);
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var t = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (t != null) return t.position;
            }

            // Auto ring: random point on a circle around the farm centre.
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 origin = transform.position;
            return new Vector3(
                origin.x + Mathf.Cos(angle) * spawnRingRadius,
                0f,
                origin.z + Mathf.Sin(angle) * spawnRingRadius);
        }

        /// <summary>Used by the editor scene builder to configure the wave.</summary>
        public void EditorConfigure(WaveData waveData, int number)
        {
            wave = waveData;
            waveNumber = number;
        }

#if UNITY_EDITOR
        // Visualise the spawn ring in the Scene view.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            const int segs = 32;
            Vector3 prev = transform.position + Vector3.right * spawnRingRadius;
            for (int i = 1; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRingRadius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}

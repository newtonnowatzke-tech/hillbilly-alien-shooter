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
    /// Runs the farm CAMPAIGN (Packet 3.1): a sequence of escalating
    /// <see cref="WaveData"/> assets with brief rest periods in between.
    ///
    /// Per wave: announce → drip-spawn every entry → wait for the field to
    /// clear (via <see cref="EnemyRegistry"/>) → announce the clear → rest
    /// (heal/restock/spend-tech window, handled by listeners) → next wave.
    /// After the final wave it raises <c>CampaignCompleted</c>; the GameManager
    /// owns what that means (win + the cattle-count mothership gate).
    ///
    /// Spawning stops cold if the session ends mid-wave (no aliens piling
    /// onto the lose screen).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Campaign (falls back to one default wave if empty)")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        [Header("Rest between waves")]
        [Tooltip("Breather length in seconds — patch up, restock, jury-rig.")]
        [SerializeField] private float restDuration = 12f;

        [Header("Spawning")]
        [Tooltip("Optional spawn points. If empty, a ring is generated around the origin.")]
        [SerializeField] private Transform[] spawnPoints;
        [Tooltip("Radius of the auto-generated spawn ring when no spawn points are set.")]
        [SerializeField] private float spawnRingRadius = 22f;
        [Tooltip("Optional custom enemy prefab. If null, low-poly primitive enemies are built.")]
        [SerializeField] private GameObject enemyPrefab;

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        private void OnGameStateChanged(GameState state)
        {
            // Session over (win or lose) → stop feeding the meat grinder.
            if (state == GameState.Won || state == GameState.Lost)
                StopAllCoroutines();
        }

        private IEnumerator Start()
        {
            if (waves == null || waves.Count == 0)
                waves = new List<WaveData> { WaveData.CreateDefault() };

            int total = waves.Count;

            for (int i = 0; i < total; i++)
            {
                WaveData wave = waves[i];
                if (wave == null) continue;

                // The opening delay only applies to the very first wave — later
                // waves are paced by the rest period instead.
                if (i == 0)
                    yield return new WaitForSeconds(wave.startDelay);

                GameEvents.RaiseWaveStarted(i + 1, total);

                yield return StartCoroutine(SpawnAll(wave));

                // Wait until every enemy — ground alien or saucer — is gone.
                while (EnemyRegistry.Count > 0)
                    yield return null;

                GameEvents.RaiseWaveCompleted(i + 1, total);

                if (i < total - 1)
                {
                    GameEvents.RaiseRestStarted(restDuration);
                    yield return new WaitForSeconds(restDuration);
                }
            }

            GameEvents.RaiseCampaignCompleted();
        }

        private IEnumerator SpawnAll(WaveData wave)
        {
            foreach (var entry in wave.spawns)
            {
                if (entry == null) continue;
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnOne(entry.enemy);
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

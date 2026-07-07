using UnityEngine;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Player;

namespace HillbillyAlienShooter.Pickups
{
    /// <summary>
    /// A shard of glowing alien tech dropped by dead invaders (Packet 2.1).
    /// Bobs and spins where it fell; when the hillbilly gets close it magnets to
    /// him and adds to <see cref="TechInventory"/> (spent by the upgrade system
    /// in Packet 2.3).
    ///
    /// Collection is distance-based (no physics/trigger plumbing): a couple of
    /// float compares per frame per pickup, and it works identically on foot,
    /// on horseback, and at a full gallop drive-by.
    /// </summary>
    public class TechPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 1;

        [Header("Feel")]
        [Tooltip("Start flying toward the player within this distance.")]
        [SerializeField] private float magnetRadius = 3.2f;
        [Tooltip("Counts as collected within this distance.")]
        [SerializeField] private float collectRadius = 1.0f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float bobAmplitude = 0.22f;
        [SerializeField] private float bobFrequency = 1.6f;
        [SerializeField] private float spinSpeed = 120f;

        private Transform _player;
        private Vector3 _restPos;
        private float _phase;
        private bool _magnetized;

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;
            _restPos = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void Configure(int techAmount) => amount = Mathf.Max(1, techAmount);

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (_player == null)
            {
                Bob();
                return;
            }

            // Chase the player's chest so it flies to the rider, not the hooves.
            Vector3 target = _player.position + Vector3.up * 0.5f;
            float dist = Vector3.Distance(transform.position, target);

            if (dist <= collectRadius)
            {
                Collect();
            }
            else if (_magnetized || dist <= magnetRadius)
            {
                _magnetized = true; // once it starts flying, it commits
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
            }
            else
            {
                Bob();
            }
        }

        private void Bob()
        {
            Vector3 pos = _restPos;
            pos.y += Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + _phase) * bobAmplitude;
            transform.position = pos;
        }

        private void Collect()
        {
            TechInventory.Add(amount);
            // TODO (Packet 4.2/4.3): collect chime + sparkle burst.
            Destroy(gameObject);
        }
    }
}

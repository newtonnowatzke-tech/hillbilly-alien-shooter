using UnityEngine;
using HillbillyAlienShooter.Combat;

namespace HillbillyAlienShooter.Enemies
{
    /// <summary>
    /// UFO support fire (Packet 2.2): a slow, glowing plasma bolt that flies in a
    /// straight line. Deliberately dodgeable — the threat is area denial while
    /// the saucer beams your cows, not sniping. Distance-based hit detection
    /// against the player keeps it physics-free like the rest of the codebase.
    /// </summary>
    public class PlasmaBolt : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _damage;
        private float _life = 5f;
        private Transform _player;
        private IDamageable _playerDamageable;

        private const float HitRadius = 0.75f;

        public void Configure(Vector3 direction, float speed, float damage, GameObject playerTarget)
        {
            _velocity = direction.normalized * speed;
            _damage = damage;
            if (playerTarget != null)
            {
                _player = playerTarget.transform;
                _playerDamageable = playerTarget.GetComponentInChildren<IDamageable>();
            }
        }

        private void Update()
        {
            transform.position += _velocity * Time.deltaTime;

            _life -= Time.deltaTime;
            if (_life <= 0f || transform.position.y <= 0.05f)
            {
                Destroy(gameObject); // fizzles on the dirt (impact VFX in 4.3)
                return;
            }

            if (_player == null || _playerDamageable == null || !_playerDamageable.IsAlive) return;

            // Player root sits at capsule centre, so this is a chest-height check.
            if (Vector3.Distance(transform.position, _player.position) <= HitRadius)
            {
                _playerDamageable.TakeDamage(new DamageInfo(
                    _damage, transform.position, _velocity.normalized, source: gameObject));
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace HillbillyAlienShooter.Effects
{
    /// <summary>
    /// The Brute's slam ring (Packet 2.2): a flattened disc that expands to the
    /// smash radius and fades out, marking the AoE the player needed to dodge.
    /// Self-destructs when done. Cheap placeholder until real particles in 4.3.
    /// </summary>
    public class ShockwaveFx : MonoBehaviour
    {
        private float _radius = 3f;
        private float _duration = 0.45f;
        private float _elapsed;
        private Material _material;
        private Color _startColor;

        public void Configure(float radius, float duration, Material material, Color color)
        {
            _radius = radius;
            _duration = Mathf.Max(0.05f, duration);
            _material = material;
            _startColor = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Ease-out expansion: fast pop, gentle settle.
            float eased = 1f - (1f - t) * (1f - t);
            float diameter = Mathf.Lerp(0.6f, _radius * 2f, eased);
            transform.localScale = new Vector3(diameter, 0.08f, diameter);

            if (_material != null)
            {
                Color c = _startColor;
                c.a = (1f - t) * _startColor.a;
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", c);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", c);
            }

            if (t >= 1f) Destroy(gameObject);
        }
    }
}

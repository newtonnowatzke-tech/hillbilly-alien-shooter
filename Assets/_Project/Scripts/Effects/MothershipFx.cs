using UnityEngine;

namespace HillbillyAlienShooter.Effects
{
    /// <summary>
    /// The mothership's arrival (Packet 3.1): a pure set piece that descends
    /// from the stars to loom over the farm, slowly rotating with a heavy bob.
    /// No collider, no health — you don't fight it, you BOARD it (Packet 3.2).
    /// Runs on unscaled-ish scaled time; it appears on the win screen where
    /// timeScale is still 1, so plain deltaTime is fine.
    /// </summary>
    public class MothershipFx : MonoBehaviour
    {
        [SerializeField] private float descendFrom = 60f;
        [SerializeField] private float hoverAt = 24f;
        [SerializeField] private float descendSpeed = 9f;
        [SerializeField] private float spinSpeed = 8f;
        [SerializeField] private float bobAmplitude = 0.6f;

        private float _bobPhase;
        private bool _arrived;

        private void Start()
        {
            var pos = transform.position;
            pos.y = descendFrom;
            transform.position = pos;
        }

        private void Update()
        {
            Vector3 pos = transform.position;

            if (!_arrived)
            {
                pos.y = Mathf.MoveTowards(pos.y, hoverAt, descendSpeed * Time.deltaTime);
                if (Mathf.Approximately(pos.y, hoverAt)) _arrived = true;
            }
            else
            {
                _bobPhase += Time.deltaTime;
                pos.y = hoverAt + Mathf.Sin(_bobPhase * 0.8f) * bobAmplitude;
            }

            transform.position = pos;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}

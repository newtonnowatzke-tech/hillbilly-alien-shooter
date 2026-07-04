using UnityEngine;

namespace HillbillyAlienShooter.Utils
{
    /// <summary>
    /// Cheap terrain following for kinematically-moved AI (aliens for now).
    /// Every LateUpdate — after the AI has done its flat XZ movement — we raycast
    /// down against the Ground layer and ease the transform's Y to the surface.
    /// This keeps shamblers hugging the new hills without needing a NavMesh yet.
    /// </summary>
    public class GroundSnap : MonoBehaviour
    {
        [Tooltip("How fast (m/s) the object's height chases the terrain height.")]
        [SerializeField] private float verticalFollowSpeed = 9f;

        [Tooltip("Layers considered walkable terrain.")]
        [SerializeField] private LayerMask groundMask = 1 << GameLayers.Ground;

        private const float ProbeHeight = 4f;  // start the ray this far above us
        private const float ProbeDepth = 12f;  // total ray length

        private void LateUpdate()
        {
            Vector3 pos = transform.position;
            Vector3 origin = pos + Vector3.up * ProbeHeight;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, ProbeDepth,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                pos.y = Mathf.MoveTowards(pos.y, hit.point.y, verticalFollowSpeed * Time.deltaTime);
                transform.position = pos;
            }
        }
    }
}

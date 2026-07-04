using UnityEngine;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.Player
{
    /// <summary>
    /// Finds the nearest <see cref="IInteractable"/> around the player, surfaces
    /// its prompt on the HUD (via GameEvents) and triggers it on the Interact key.
    ///
    /// Proximity (overlap sphere) rather than aim-raycast on purpose: walking up
    /// to a horse should "just work" without pixel-hunting it with the crosshair.
    /// The scan runs on a small timer, not every frame — interaction targets
    /// don't move fast enough to justify per-frame overlap queries.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("How close an interactable must be (metres).")]
        [SerializeField] private float interactRadius = 2.8f;
        [Tooltip("Seconds between proximity scans.")]
        [SerializeField] private float scanInterval = 0.15f;

        private PlayerInputHandler _input;
        private IInteractable _current;
        private string _lastPrompt;
        private float _nextScanTime;
        private bool _active = true;
        private readonly Collider[] _hits = new Collider[16]; // non-alloc buffer

        private void Awake() => _input = GetComponent<PlayerInputHandler>();

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        private void OnGameStateChanged(GameState state)
        {
            _active = state == GameState.Playing;
            if (!_active && !string.IsNullOrEmpty(_lastPrompt))
            {
                // Hide any lingering prompt on win/lose/pause screens.
                _lastPrompt = null;
                _current = null;
                GameEvents.RaiseInteractPromptChanged(null);
            }
        }

        private void Update()
        {
            if (!_active) return;

            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + scanInterval;
                Scan();
            }

            if (_input.InteractPressedThisFrame && _current != null && _current.CanInteract(gameObject))
            {
                _current.Interact(gameObject);
                _nextScanTime = 0f; // re-scan immediately so the prompt flips (Ride -> Hop off)
            }
        }

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, interactRadius, _hits, ~0, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                // Skip our own colliders. NOTE: compare against the player transform,
                // NOT transform.root — while riding, the player is parented to the
                // horse, and a root comparison would wrongly skip the horse itself.
                if (col == null || col.transform.IsChildOf(transform)) continue;

                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(gameObject)) continue;

                float sqr = (col.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = interactable;
                }
            }

            _current = best;

            // Only broadcast when the prompt text actually changes.
            string prompt = _current?.Prompt;
            if (prompt != _lastPrompt)
            {
                _lastPrompt = prompt;
                GameEvents.RaiseInteractPromptChanged(prompt);
            }
        }
    }
}

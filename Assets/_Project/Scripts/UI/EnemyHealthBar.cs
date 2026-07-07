using UnityEngine;
using HillbillyAlienShooter.Combat;

namespace HillbillyAlienShooter.UI
{
    /// <summary>
    /// Tiny world-space health bar (Packet 2.2). Two unlit quads — dark track +
    /// coloured fill — floating above the enemy, billboarded to the camera every
    /// LateUpdate. Hidden until first damage (pristine enemies stay clean), fill
    /// shades green → red as health drops, gone on death.
    ///
    /// Built from primitives at runtime; no canvas, no raycast targets, and the
    /// quads' colliders are stripped so they can never eat shotgun pellets.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Tooltip("Metres above the enemy origin.")]
        [SerializeField] private float height = 2.2f;
        [SerializeField] private float width = 1.1f;

        private Health _health;
        private Transform _root;      // bar assembly (billboarded)
        private Transform _fillPivot; // left-anchored: scaling X shrinks toward the left edge
        private Material _fillMat;
        private bool _visible;

        public void Configure(float barHeight, float barWidth = 1.1f)
        {
            height = barHeight;
            width = barWidth;
        }

        private void Start()
        {
            _health = GetComponentInParent<Health>();
            if (_health == null) { enabled = false; return; }

            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            BuildBar();
            _root.gameObject.SetActive(false); // clean until first hit
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo _)
        {
            if (_root == null || !_health.IsAlive) return;
            if (!_visible)
            {
                _visible = true;
                _root.gameObject.SetActive(true);
            }
            UpdateFill();
        }

        private void OnDied(Health _)
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void UpdateFill()
        {
            float frac = _health.Normalized;
            Vector3 s = _fillPivot.localScale;
            s.x = Mathf.Clamp01(frac);
            _fillPivot.localScale = s;

            // Green at full → amber → red near death.
            Color c = frac > 0.5f
                ? Color.Lerp(new Color(1f, 0.75f, 0.2f), new Color(0.35f, 0.9f, 0.3f), (frac - 0.5f) * 2f)
                : Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(1f, 0.75f, 0.2f), frac * 2f);
            if (_fillMat.HasProperty("_Color")) _fillMat.SetColor("_Color", c);
        }

        private void LateUpdate()
        {
            if (_root == null || !_visible) return;

            _root.position = transform.position + Vector3.up * height;

            // Billboard: mirror the camera's orientation so the quad faces it flat.
            var cam = Camera.main;
            if (cam != null) _root.rotation = cam.transform.rotation;
        }

        // -------------------------------------------------------------------
        // Construction
        // -------------------------------------------------------------------
        private void BuildBar()
        {
            _root = new GameObject("HealthBar").transform;
            // Deliberately NOT parented to the enemy: squash/stretch reactions
            // scale the enemy root, and a parented bar would squash with it.
            _root.position = transform.position + Vector3.up * height;

            var track = MakeQuad(_root, "Track", new Color(0.05f, 0.05f, 0.08f, 0.8f));
            track.localScale = new Vector3(width, 0.14f, 1f);

            // Fill sits under a pivot whose origin is the bar's left edge.
            _fillPivot = new GameObject("FillPivot").transform;
            _fillPivot.SetParent(_root, false);
            _fillPivot.localPosition = new Vector3(-width * 0.5f, 0f, -0.001f); // a hair in front

            var fill = MakeQuad(_fillPivot, "Fill", new Color(0.35f, 0.9f, 0.3f, 0.95f));
            fill.localPosition = new Vector3(width * 0.5f, 0f, 0f);
            fill.localScale = new Vector3(width, 0.1f, 1f);
            _fillMat = fill.GetComponent<Renderer>().material;
        }

        private static Transform MakeQuad(Transform parent, string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>()); // must never block pellets
            go.transform.SetParent(parent, false);

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;
            return go.transform;
        }

        private void OnDisable()
        {
            // The bar lives outside our hierarchy, so clean it up explicitly.
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}

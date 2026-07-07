using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HillbillyAlienShooter.Combat
{
    /// <summary>
    /// Classic low-poly hit feedback: every renderer under this object flashes
    /// white for a few frames, then restores its original tint. Pairs with the
    /// squash/dip reactions so hits read instantly even at night. Works with the
    /// factory's per-instance materials (URP _BaseColor or Standard _Color).
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.07f;
        [SerializeField] private Color flashColor = Color.white;

        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Color> _originals = new List<Color>();
        private Coroutine _routine;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            // Cache every tintable material once. Factory materials are already
            // per-instance, so tinting them can't bleed onto other enemies.
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                // Skip LineRenderers (beams/tracers) — flashing those looks wrong.
                if (renderer is LineRenderer) continue;

                var mat = renderer.material;
                if (mat == null) continue;
                if (!mat.HasProperty(BaseColorId) && !mat.HasProperty(ColorId)) continue;

                _materials.Add(mat);
                _originals.Add(GetColor(mat));
            }
        }

        public void Flash()
        {
            if (_materials.Count == 0 || !isActiveAndEnabled) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetAll(flashColor);
            yield return new WaitForSeconds(flashDuration);
            RestoreAll();
            _routine = null;
        }

        private void OnDisable() => RestoreAll(); // never leave a corpse frozen white

        private void SetAll(Color color)
        {
            for (int i = 0; i < _materials.Count; i++)
                SetColor(_materials[i], color);
        }

        private void RestoreAll()
        {
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null)
                    SetColor(_materials[i], _originals[i]);
        }

        private static Color GetColor(Material mat) =>
            mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : mat.GetColor(ColorId);

        private static void SetColor(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
            if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
        }
    }
}

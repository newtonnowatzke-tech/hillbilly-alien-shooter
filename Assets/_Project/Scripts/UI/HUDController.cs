using UnityEngine;
using UnityEngine.UI;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.UI
{
    /// <summary>
    /// Minimal but complete heads-up display for Packet 1.1. Builds its own Canvas
    /// and widgets at runtime (using the built-in legacy font so there's no
    /// TextMeshPro import step) and updates purely by listening to
    /// <see cref="GameEvents"/> — it never reaches into gameplay systems.
    ///
    /// This gets replaced with a polished TMP/uGUI layout in Packet 4.3; keeping
    /// it event-driven means that swap won't touch any gameplay code.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Text _healthText;
        private Text _cattleText;
        private Text _ammoText;
        private Text _reloadText;
        private Text _bannerText;
        private Text _endText;
        private Text _endHint;

        private Font _font;
        private float _bannerHideTime;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            BuildUI();
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.CattleCountsChanged += OnCattle;
            GameEvents.AmmoChanged += OnAmmo;
            GameEvents.ReloadStateChanged += OnReload;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.GameStateChanged += OnGameState;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.CattleCountsChanged -= OnCattle;
            GameEvents.AmmoChanged -= OnAmmo;
            GameEvents.ReloadStateChanged -= OnReload;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.GameStateChanged -= OnGameState;
        }

        private void Update()
        {
            if (_bannerText.enabled && Time.time >= _bannerHideTime)
                _bannerText.enabled = false;
        }

        // ---------------------------------------------------------------
        // Event handlers → text updates
        // ---------------------------------------------------------------
        private void OnHealth(float current, float max) =>
            _healthText.text = $"HP  {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

        private void OnCattle(int saved, int taken, int total) =>
            _cattleText.text = $"CATTLE SAVED {saved}     RUSTLED {taken}";

        private void OnAmmo(int magazine, int reserve) =>
            _ammoText.text = $"SHELLS  {magazine} / {reserve}";

        private void OnReload(bool reloading)
        {
            _reloadText.enabled = reloading;
            _reloadText.text = "RELOADIN'...";
        }

        private void OnWaveStarted(int waveNumber)
        {
            _bannerText.text = $"WAVE {waveNumber} — YEE-HAW!";
            _bannerText.enabled = true;
            _bannerHideTime = Time.time + 2.5f;
        }

        private void OnGameState(GameState state)
        {
            bool over = state == GameState.Won || state == GameState.Lost;
            _endText.enabled = over;
            _endHint.enabled = over;

            if (state == GameState.Won)
            {
                _endText.text = "FARM DEFENDED!";
                _endText.color = new Color(0.6f, 1f, 0.5f);
            }
            else if (state == GameState.Lost)
            {
                _endText.text = "THEM ALIENS GOT YER CATTLE...";
                _endText.color = new Color(1f, 0.5f, 0.4f);
            }
        }

        // ---------------------------------------------------------------
        // Runtime UI construction
        // ---------------------------------------------------------------
        private void BuildUI()
        {
            // Canvas
            var canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            // Crosshair (a small white dot in the centre).
            var cross = new GameObject("Crosshair").AddComponent<Image>();
            cross.transform.SetParent(root, false);
            cross.color = new Color(1f, 1f, 1f, 0.8f);
            var cr = cross.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(6f, 6f);
            cr.anchoredPosition = Vector2.zero;

            // Corner readouts.
            _healthText = MakeText(root, "HealthText", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -24f), new Vector2(500f, 50f), 32, TextAnchor.UpperLeft, Color.white);

            _cattleText = MakeText(root, "CattleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(900f, 50f), 30, TextAnchor.UpperCenter, new Color(1f, 0.95f, 0.7f));

            _ammoText = MakeText(root, "AmmoText", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-30f, 26f), new Vector2(500f, 50f), 34, TextAnchor.LowerRight, Color.white);

            _reloadText = MakeText(root, "ReloadText", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-30f, 74f), new Vector2(500f, 40f), 26, TextAnchor.LowerRight, new Color(1f, 0.8f, 0.3f));
            _reloadText.enabled = false;

            // Wave banner + end-of-round overlay.
            _bannerText = MakeText(root, "BannerText", new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1400f, 120f), 64, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.enabled = false;

            _endText = MakeText(root, "EndText", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1600f, 160f), 80, TextAnchor.MiddleCenter, Color.white);
            _endText.fontStyle = FontStyle.Bold;
            _endText.enabled = false;

            _endHint = MakeText(root, "EndHint", new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1200f, 60f), 34, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f));
            _endHint.text = "Press R to saddle up again";
            _endHint.enabled = false;
        }

        private Text MakeText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            // Cheap readability outline so bright text reads over any background.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return text;
        }
    }
}

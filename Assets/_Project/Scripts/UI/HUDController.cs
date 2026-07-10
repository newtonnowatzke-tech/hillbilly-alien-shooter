using System.Collections.Generic;
using System.Text;
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
        private Text _promptText;   // "[E] Ride Buttercup"
        private Text _horseText;    // "Buttercup: followin' you"
        private Text _techText;     // "ALIEN TECH: 3"
        private Text _upgradeText;  // active upgrade slots + countdowns
        private Text _toastText;    // "BOOMSTICK ROUNDS! Now THAT'S a boomstick!"

        private Text _restText;     // "NEXT WAVE IN 9s..."

        private Font _font;
        private float _bannerHideTime;
        private float _toastHideTime;
        private float _restEndTime = -1f;
        private bool _mothershipComing;
        private int _lastSaved;
        private int _lastTaken;

        // Local mirror of active upgrades: name -> (expiry time, stacks).
        // The HUD counts down between UpgradeChanged events on its own.
        private readonly List<(string name, float expiry, int stacks)> _upgrades =
            new List<(string, float, int)>();

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
            GameEvents.WaveCompleted += OnWaveCompleted;
            GameEvents.GameStateChanged += OnGameState;
            GameEvents.InteractPromptChanged += OnPrompt;
            GameEvents.HorseStateChanged += OnHorseState;
            GameEvents.TechChanged += OnTech;
            GameEvents.UpgradeToast += OnUpgradeToast;
            GameEvents.UpgradeChanged += OnUpgradeChanged;
            GameEvents.UpgradeExpired += OnUpgradeExpired;
            GameEvents.RestStarted += OnRestStarted;
            GameEvents.MothershipSummoned += OnMothershipSummoned;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.CattleCountsChanged -= OnCattle;
            GameEvents.AmmoChanged -= OnAmmo;
            GameEvents.ReloadStateChanged -= OnReload;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.WaveCompleted -= OnWaveCompleted;
            GameEvents.GameStateChanged -= OnGameState;
            GameEvents.InteractPromptChanged -= OnPrompt;
            GameEvents.HorseStateChanged -= OnHorseState;
            GameEvents.TechChanged -= OnTech;
            GameEvents.UpgradeToast -= OnUpgradeToast;
            GameEvents.UpgradeChanged -= OnUpgradeChanged;
            GameEvents.UpgradeExpired -= OnUpgradeExpired;
            GameEvents.RestStarted -= OnRestStarted;
            GameEvents.MothershipSummoned -= OnMothershipSummoned;
        }

        private void Update()
        {
            if (_bannerText.enabled && Time.time >= _bannerHideTime)
                _bannerText.enabled = false;

            if (_toastText.enabled && Time.time >= _toastHideTime)
                _toastText.enabled = false;

            RefreshRestCountdown();
            RefreshUpgradeList();
        }

        private void RefreshRestCountdown()
        {
            if (_restEndTime < 0f) return;

            float remaining = _restEndTime - Time.time;
            if (remaining <= 0f)
            {
                _restEndTime = -1f;
                _restText.enabled = false;
                return;
            }

            _restText.text = $"NEXT WAVE IN {Mathf.CeilToInt(remaining)}s — PATCH UP & JURY-RIG [Q]";
            _restText.enabled = true;
        }

        /// <summary>Redraws the active-upgrade slots with locally-ticked countdowns.</summary>
        private void RefreshUpgradeList()
        {
            // Drop anything that ran out (safety net alongside UpgradeExpired).
            _upgrades.RemoveAll(u => Time.time >= u.expiry);

            if (_upgrades.Count == 0)
            {
                if (_upgradeText.enabled) _upgradeText.enabled = false;
                return;
            }

            var sb = new StringBuilder();
            foreach (var (name, expiry, stacks) in _upgrades)
            {
                sb.Append(name.ToUpperInvariant());
                if (stacks > 1) sb.Append(" x").Append(stacks);
                sb.Append("  ").Append(Mathf.CeilToInt(expiry - Time.time)).Append("s\n");
            }
            _upgradeText.text = sb.ToString();
            _upgradeText.enabled = true;
        }

        // ---------------------------------------------------------------
        // Event handlers → text updates
        // ---------------------------------------------------------------
        private void OnHealth(float current, float max) =>
            _healthText.text = $"HP  {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

        private void OnCattle(int saved, int taken, int total)
        {
            _lastSaved = saved;
            _lastTaken = taken;
            _cattleText.text = $"CATTLE SAVED {saved}     RUSTLED {taken}";
        }

        private void OnRestStarted(float duration) => _restEndTime = Time.time + duration;

        private void OnMothershipSummoned() => _mothershipComing = true;

        private void OnAmmo(int magazine, int reserve) =>
            _ammoText.text = $"SHELLS  {magazine} / {reserve}";

        private void OnReload(bool reloading)
        {
            _reloadText.enabled = reloading;
            _reloadText.text = "RELOADIN'...";
        }

        private void OnPrompt(string prompt)
        {
            bool has = !string.IsNullOrEmpty(prompt);
            _promptText.enabled = has;
            if (has) _promptText.text = prompt;
        }

        private void OnHorseState(string status)
        {
            bool has = !string.IsNullOrEmpty(status);
            _horseText.enabled = has;
            if (has) _horseText.text = status;
        }

        private void OnTech(int total) => _techText.text = $"ALIEN TECH  {total}";

        private void OnUpgradeToast(string message)
        {
            _toastText.text = message;
            _toastText.enabled = true;
            _toastHideTime = Time.time + 2.6f;
        }

        private void OnUpgradeChanged(string name, float remaining, int stacks)
        {
            float expiry = Time.time + remaining;
            for (int i = 0; i < _upgrades.Count; i++)
            {
                if (_upgrades[i].name == name)
                {
                    _upgrades[i] = (name, expiry, stacks);
                    return;
                }
            }
            _upgrades.Add((name, expiry, stacks));
        }

        private void OnUpgradeExpired(string name) =>
            _upgrades.RemoveAll(u => u.name == name);

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            _bannerText.text = $"WAVE {waveNumber} OF {totalWaves} — YEE-HAW!";
            _bannerText.enabled = true;
            _bannerHideTime = Time.time + 2.5f;
        }

        private void OnWaveCompleted(int waveNumber, int totalWaves)
        {
            if (waveNumber >= totalWaves) return; // final clear → the end screen handles it
            _bannerText.text = "WAVE CLEARED!";
            _bannerText.enabled = true;
            _bannerHideTime = Time.time + 2f;
        }

        private void OnGameState(GameState state)
        {
            bool over = state == GameState.Won || state == GameState.Lost;
            _endText.enabled = over;
            _endHint.enabled = over;
            if (over)
            {
                _restEndTime = -1f;
                _restText.enabled = false;
            }

            string stats = $"Cattle saved {_lastSaved} — rustled {_lastTaken}";

            if (state == GameState.Won)
            {
                if (_mothershipComing)
                {
                    _endText.text = "THE MOTHERSHIP DESCENDS...";
                    _endText.color = new Color(0.6f, 1f, 0.8f);
                    _endHint.text = $"{stats}\nYou saved enough of the herd to lure 'em in. TO BE CONTINUED (Packet 3.2) — R to replay";
                }
                else
                {
                    _endText.text = "FARM DEFENDED!";
                    _endText.color = new Color(0.6f, 1f, 0.5f);
                    _endHint.text = $"{stats}\nThe varmints got away clean... save more cows to lure the mothership! R to try again";
                }
            }
            else if (state == GameState.Lost)
            {
                _endText.text = "THEM ALIENS GOT YER CATTLE...";
                _endText.color = new Color(1f, 0.5f, 0.4f);
                _endHint.text = $"{stats}\nPress R to saddle up again";
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

            // Interaction prompt, low-centre near the crosshair's eyeline.
            _promptText = MakeText(root, "PromptText", new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 50f), 30, TextAnchor.MiddleCenter, new Color(0.85f, 1f, 0.85f));
            _promptText.enabled = false;

            // Horse status, tucked under the health readout.
            _horseText = MakeText(root, "HorseText", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -70f), new Vector2(600f, 40f), 24, TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.6f));
            _horseText.enabled = false;

            // Alien tech tally, top-right (currency for Packet 2.3 upgrades).
            _techText = MakeText(root, "TechText", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-30f, -24f), new Vector2(500f, 50f), 30, TextAnchor.UpperRight, new Color(0.5f, 1f, 1f));
            _techText.text = "ALIEN TECH  0";

            // Active upgrade slots + countdowns, stacked under the tech tally.
            _upgradeText = MakeText(root, "UpgradeText", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-30f, -68f), new Vector2(520f, 220f), 24, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.35f));
            _upgradeText.enabled = false;

            // Upgrade toast: acquisition flavor / "need more tech", centre-low.
            _toastText = MakeText(root, "ToastText", new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1400f, 60f), 32, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.6f));
            _toastText.fontStyle = FontStyle.Bold;
            _toastText.enabled = false;

            // Between-waves rest countdown, just under the wave banner spot.
            _restText = MakeText(root, "RestText", new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1400f, 50f), 30, TextAnchor.MiddleCenter, new Color(0.75f, 0.95f, 1f));
            _restText.enabled = false;
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

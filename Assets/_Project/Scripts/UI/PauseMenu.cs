using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Player;

namespace HillbillyAlienShooter.UI
{
    /// <summary>
    /// Pause menu + settings stub (Packet 1.3). Self-building like the HUD:
    /// creates its own canvas, buttons, sliders and an EventSystem if the scene
    /// lacks one — zero manual wiring.
    ///
    /// Settings persist via PlayerPrefs and apply live:
    ///   • Mouse sensitivity  → PlayerInputHandler.MouseSensitivity
    ///   • Invert Y           → PlayerInputHandler.InvertY
    ///   • Master volume     → AudioListener.volume (future-proofing for 4.2)
    ///
    /// WebGL notes: Esc is reserved by browsers to exit pointer lock and may not
    /// reach the game, so P also pauses — and losing pointer lock mid-play
    /// auto-pauses, which turns the browser Esc into a natural pause key anyway.
    /// The Quit button is hidden on WebGL (Application.Quit is a no-op there).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private const string PrefSensitivity = "hb_mouse_sensitivity";
        private const string PrefInvertY = "hb_invert_y";
        private const string PrefVolume = "hb_master_volume";

        private InputAction _pauseAction;
        private PlayerInputHandler _playerInput;

        private GameObject _root;          // whole overlay
        private GameObject _mainPanel;     // Resume / Settings / Restart / Quit
        private GameObject _settingsPanel; // sliders + toggles
        private Text _invertLabel;
        private Font _font;
        private bool _hadPointerLock;

        // -------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------
        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // Esc + P + gamepad Start all toggle pause (P matters on WebGL where
            // the browser eats Esc for pointer-lock release).
            _pauseAction = new InputAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Keyboard>/p");
            _pauseAction.AddBinding("<Gamepad>/start");

            EnsureEventSystem();
            BuildUI();
        }

        private void OnEnable()
        {
            _pauseAction.Enable();
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            _pauseAction.Disable();
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnDestroy() => _pauseAction?.Dispose();

        private void Start()
        {
            _playerInput = FindFirstObjectByType<PlayerInputHandler>();
            LoadAndApplySettings();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (_pauseAction.WasPressedThisFrame())
                gm.TogglePause();

            // WebGL/desktop nicety: if the OS/browser stole our pointer lock while
            // playing (Esc in browser, alt-tab...), pause instead of letting the
            // player flail with a visible cursor. Only after lock was first
            // acquired, so the initial "click to focus" doesn't insta-pause.
            if (gm.State == GameState.Playing)
            {
                if (Cursor.lockState == CursorLockMode.Locked) _hadPointerLock = true;
                else if (_hadPointerLock) gm.Pause();
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            bool paused = state == GameState.Paused;
            _root.SetActive(paused);
            if (paused)
            {
                _hadPointerLock = false;
                ShowSettings(false); // always land on the main panel
            }
        }

        // -------------------------------------------------------------------
        // Button handlers
        // -------------------------------------------------------------------
        private void OnResume() => GameManager.Instance?.Resume();
        private void OnRestart() => GameManager.Instance?.Restart();
        private void OnQuit() => Application.Quit();

        private void ShowSettings(bool show)
        {
            _settingsPanel.SetActive(show);
            _mainPanel.SetActive(!show);
        }

        // -------------------------------------------------------------------
        // Settings plumbing
        // -------------------------------------------------------------------
        private void LoadAndApplySettings()
        {
            float sens = PlayerPrefs.GetFloat(PrefSensitivity, 0.08f);
            bool invert = PlayerPrefs.GetInt(PrefInvertY, 0) == 1;
            float volume = PlayerPrefs.GetFloat(PrefVolume, 1f);

            ApplySensitivity(sens);
            ApplyInvertY(invert);
            ApplyVolume(volume);
        }

        private void ApplySensitivity(float value)
        {
            if (_playerInput != null) _playerInput.MouseSensitivity = value;
            PlayerPrefs.SetFloat(PrefSensitivity, value);
        }

        private void ApplyInvertY(bool value)
        {
            if (_playerInput != null) _playerInput.InvertY = value;
            PlayerPrefs.SetInt(PrefInvertY, value ? 1 : 0);
            if (_invertLabel != null) _invertLabel.text = value ? "INVERT Y: ON" : "INVERT Y: OFF";
        }

        private void ApplyVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(PrefVolume, value);
        }

        // -------------------------------------------------------------------
        // UI construction
        // -------------------------------------------------------------------
        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // New Input System UI driver
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("PauseCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the HUD
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen dim behind the menu.
            _root = new GameObject("PauseRoot");
            _root.transform.SetParent(canvasGo.transform, false);
            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.08f, 0.82f);
            Stretch(dim.rectTransform);

            _mainPanel = BuildMainPanel(_root.transform);
            _settingsPanel = BuildSettingsPanel(_root.transform);
            _settingsPanel.SetActive(false);
            _root.SetActive(false);
        }

        private GameObject BuildMainPanel(Transform parent)
        {
            var panel = new GameObject("MainPanel");
            panel.transform.SetParent(parent, false);
            Stretch(panel.AddComponent<RectTransform>());

            MakeText(panel.transform, "Title", "GAME PAUSED", 66, new Vector2(0.5f, 0.78f),
                new Vector2(1400f, 100f), new Color(1f, 0.85f, 0.3f), FontStyle.Bold);
            MakeText(panel.transform, "Subtitle", "(catchin' yer breath)", 28, new Vector2(0.5f, 0.71f),
                new Vector2(800f, 40f), new Color(0.85f, 0.85f, 0.85f), FontStyle.Italic);

            MakeButton(panel.transform, "Resume", "RESUME", new Vector2(0.5f, 0.56f), OnResume);
            MakeButton(panel.transform, "Settings", "SETTINGS", new Vector2(0.5f, 0.46f), () => ShowSettings(true));
            MakeButton(panel.transform, "Restart", "RESTART WAVE", new Vector2(0.5f, 0.36f), OnRestart);

            // Application.Quit does nothing inside a browser tab — hide it there.
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                MakeButton(panel.transform, "Quit", "QUIT GAME", new Vector2(0.5f, 0.26f), OnQuit);

            return panel;
        }

        private GameObject BuildSettingsPanel(Transform parent)
        {
            var panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(parent, false);
            Stretch(panel.AddComponent<RectTransform>());

            MakeText(panel.transform, "Title", "SETTINGS", 58, new Vector2(0.5f, 0.76f),
                new Vector2(900f, 90f), new Color(1f, 0.85f, 0.3f), FontStyle.Bold);

            // Mouse sensitivity.
            MakeText(panel.transform, "SensLabel", "MOUSE SENSITIVITY", 26, new Vector2(0.5f, 0.63f),
                new Vector2(700f, 36f), Color.white, FontStyle.Normal);
            MakeSlider(panel.transform, "SensSlider", new Vector2(0.5f, 0.58f), 0.02f, 0.2f,
                PlayerPrefs.GetFloat(PrefSensitivity, 0.08f), ApplySensitivity);

            // Invert Y as a cycling button (simpler + chunkier than a checkbox).
            var invertBtn = MakeButton(panel.transform, "InvertY", "INVERT Y: OFF", new Vector2(0.5f, 0.47f), () =>
            {
                bool now = PlayerPrefs.GetInt(PrefInvertY, 0) == 1;
                ApplyInvertY(!now);
            });
            _invertLabel = invertBtn.GetComponentInChildren<Text>();

            // Master volume (drives AudioListener now; real mixer arrives with audio in 4.2).
            MakeText(panel.transform, "VolLabel", "MASTER VOLUME", 26, new Vector2(0.5f, 0.37f),
                new Vector2(700f, 36f), Color.white, FontStyle.Normal);
            MakeSlider(panel.transform, "VolSlider", new Vector2(0.5f, 0.32f), 0f, 1f,
                PlayerPrefs.GetFloat(PrefVolume, 1f), ApplyVolume);

            MakeButton(panel.transform, "Back", "BACK", new Vector2(0.5f, 0.2f), () => ShowSettings(false));
            return panel;
        }

        // ---- widget helpers -------------------------------------------------

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Text MakeText(Transform parent, string name, string content, int size,
            Vector2 anchor, Vector2 dims, Color color, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;

            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dims;
            return text;
        }

        private GameObject MakeButton(Transform parent, string name, string label,
            Vector2 anchor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name + "Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.16f, 0.95f); // dark barn-wood green

            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 64f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.45f, 0.28f);
            colors.pressedColor = new Color(0.45f, 0.6f, 0.35f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = MakeText(go.transform, "Label", label, 30, new Vector2(0.5f, 0.5f),
                new Vector2(420f, 64f), new Color(0.95f, 0.95f, 0.9f), FontStyle.Bold);
            text.raycastTarget = false;
            return go;
        }

        private void MakeSlider(Transform parent, string name, Vector2 anchor,
            float min, float max, float initial, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480f, 28f);

            // Track.
            var bg = new GameObject("Background").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0.1f, 0.12f, 0.1f, 0.9f);
            Stretch(bg.rectTransform);

            // Fill.
            var fillArea = new GameObject("FillArea").AddComponent<RectTransform>();
            fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea);
            var fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(fillArea, false);
            fill.color = new Color(0.55f, 0.8f, 0.4f); // fresh-grass green
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Handle.
            var handleArea = new GameObject("HandleArea").AddComponent<RectTransform>();
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea);
            var handle = new GameObject("Handle").AddComponent<Image>();
            handle.transform.SetParent(handleArea, false);
            handle.color = new Color(0.95f, 0.9f, 0.75f);
            handle.rectTransform.sizeDelta = new Vector2(26f, 40f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fillRt;
            slider.handleRect = handle.rectTransform;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initial;
            slider.onValueChanged.AddListener(onChanged);
        }
    }
}

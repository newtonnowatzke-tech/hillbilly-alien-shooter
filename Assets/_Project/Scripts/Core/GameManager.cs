using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// Owns the session state machine and the campaign rules (Packet 3.1):
    ///   WIN  — every wave cleared with the herd not wiped out.
    ///   LOSE — the hillbilly dies, OR every cow gets rustled.
    ///   GATE — win with ≥ <see cref="cattleToSummonMothership"/> cows saved and
    ///          the MOTHERSHIP descends over the farm (the Packet 3.2 hook);
    ///          save fewer and the varmints get away clean.
    ///
    /// Also does defensive static cleanup on Awake so restarts behave correctly
    /// even with fast (no-domain-reload) play mode.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Progression gate")]
        [Tooltip("Cows that must survive the campaign to lure the mothership in.")]
        [SerializeField] private int cattleToSummonMothership = 3;

        public GameState State { get; private set; } = GameState.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // A previous session may have ended (or been reloaded) while paused.
            Time.timeScale = 1f;

            // Runs before any OnEnable this scene, so we start from a clean slate.
            GameEvents.ResetAll();
            HillbillyAlienShooter.Livestock.Cattle.ResetTallies();
            HillbillyAlienShooter.Enemies.EnemyRegistry.Reset();
            TechInventory.Reset();
        }

        private void OnEnable()
        {
            GameEvents.CampaignCompleted += OnCampaignCompleted;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.CattleCountsChanged += OnCattleCountsChanged;
        }

        private void OnDisable()
        {
            GameEvents.CampaignCompleted -= OnCampaignCompleted;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.CattleCountsChanged -= OnCattleCountsChanged;
        }

        private void Start()
        {
            // Cows register in their OnEnable, whose order relative to the HUD's
            // OnEnable is undefined. Re-broadcasting here (after every OnEnable has
            // run) guarantees the HUD shows the correct starting tally.
            GameEvents.RaiseCattleCountsChanged(
                HillbillyAlienShooter.Livestock.Cattle.SavedCount,
                HillbillyAlienShooter.Livestock.Cattle.TakenCount,
                HillbillyAlienShooter.Livestock.Cattle.TotalCount);

            SetState(GameState.Playing);
        }

        private void Update()
        {
            // Restart on R / Enter once the round is over.
            if (State == GameState.Won || State == GameState.Lost)
            {
                var kb = Keyboard.current;
                if (kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    Restart();
            }
        }

        private void OnCampaignCompleted()
        {
            if (State != GameState.Playing) return;

            // Surviving every wave IS the win (a wiped herd already lost above).
            SetState(GameState.Won);

            // The gate: enough cattle saved lures the mothership down —
            // Packet 3.2 boards it. Otherwise the rustlers slip away clean.
            if (HillbillyAlienShooter.Livestock.Cattle.SavedCount >= cattleToSummonMothership)
            {
                GameEvents.RaiseMothershipSummoned();
                HillbillyAlienShooter.Utils.LowPolyFactory.BuildMothership(new Vector3(0f, 0f, 10f));
            }
        }

        private void OnPlayerDied()
        {
            if (State == GameState.Playing)
                SetState(GameState.Lost);
        }

        private void OnCattleCountsChanged(int saved, int taken, int total)
        {
            // Every cow rustled → instant loss.
            if (State == GameState.Playing && total > 0 && saved <= 0)
                SetState(GameState.Lost);
        }

        private void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            GameEvents.RaiseGameStateChanged(next);
        }

        // -------------------------------------------------------------------
        // Pause (driven by the PauseMenu UI)
        // -------------------------------------------------------------------
        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f; // never carry a paused clock into the fresh scene
            // Statics get reset in Awake of the freshly-loaded scene.
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// Owns the session state machine and the single-wave win/lose rules for
    /// Packet 1.1:
    ///   WIN  — the wave is cleared with at least <see cref="cattleNeededToWin"/> cows saved.
    ///   LOSE — the hillbilly dies, OR every cow gets rustled.
    ///
    /// Also does defensive static cleanup on Awake so restarts behave correctly
    /// even with fast (no-domain-reload) play mode.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Win condition")]
        [Tooltip("Minimum cows that must survive the wave to count as a win.")]
        [SerializeField] private int cattleNeededToWin = 1;

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
            GameEvents.WaveCompleted += OnWaveCompleted;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.CattleCountsChanged += OnCattleCountsChanged;
        }

        private void OnDisable()
        {
            GameEvents.WaveCompleted -= OnWaveCompleted;
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

        private void OnWaveCompleted(int waveNumber)
        {
            if (State != GameState.Playing) return;

            // Cleared the wave — did enough cattle survive?
            if (HillbillyAlienShooter.Livestock.Cattle.SavedCount >= cattleNeededToWin)
                SetState(GameState.Won);
            else
                SetState(GameState.Lost);
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

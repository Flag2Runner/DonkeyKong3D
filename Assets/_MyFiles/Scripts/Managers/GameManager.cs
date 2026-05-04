using _MyFiles.Scripts.Environment;
using _MyFiles.Scripts.Puppet;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace _MyFiles.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        // The Singleton Instance
        public static GameManager Instance { get; private set; }

       //Enums to manage the game state of DK arcade Cabniet
        public enum ArcadeState { AttractMode, CreditLoaded, Starting, Playing, Respawning, GameOver, Paused, HitStop }
        public ArcadeState CurrentState { get; private set; } = ArcadeState.AttractMode;

        // The specific DK Pause State
        public bool IsDKPaused { get; private set; }

        [Header("DK Game UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject pauseTextPrompt;

        [Header("HUD Text Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI livesText;

        [Header("Main Menu Text Elements")]
        [SerializeField] private TextMeshProUGUI insertCoinPromptText;

        [Header("Diorama Sync Settings")]
        public Transform masterLevelCenter;
        public Transform dioramaCenter;
        public float puppetScaleFactor = 0.1f;

        [Header("Game Stats")]
        public int currentLives = 3;
        public int currentScore = 0;

        [Header("Settings")]
        [Tooltip("How long the 'Get Ready' pause lasts before you can move.")]
        public float startDelay = 3f;

        [Header("Respawn Settings")]
        [SerializeField] private GameObject jumpman;
        [SerializeField] private Transform jumpmanStartNode;
        [Tooltip("Drag your Master Level Hammer Pickups here so they can respawn.")]
        [SerializeField] private GameObject[] mapHammers;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (pauseTextPrompt != null) pauseTextPrompt.SetActive(false);
            // Start the cabinet in Attract Mode ie (Waiting for coin)
            ResetToAttractMode();
        }

        // --- Arcade Flow  ---

        // Handles the pause logic triggered by the Input Manager
        public void TogglePause()
        {
            // This 'if' statement is what guarantees you can only pause during actual gameplay!
            if (CurrentState == ArcadeState.Playing)
            {
                CurrentState = ArcadeState.Paused;
                DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxPause);
                DKAudioManager.Instance.StopMusic();
                if (pauseTextPrompt != null) pauseTextPrompt.SetActive(true);
            }
            else if (CurrentState == ArcadeState.Paused)
            {
                CurrentState = ArcadeState.Playing;
                DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxPause);
                DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicLevel1);
                if (pauseTextPrompt != null) pauseTextPrompt.SetActive(false);
            }
        }

        public void InsertCoin()
        {
            // Only accept coins if we are waiting for one
            if (CurrentState == ArcadeState.AttractMode)
            {
                CurrentState = ArcadeState.CreditLoaded;

                // Change the UI text from "Insert Coin" to "Press Start"
                if (insertCoinPromptText != null)
                    insertCoinPromptText.text = "CREDIT 1\nPRESS START";

                DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxScore); // Quick feedback beep
                Debug.Log("Coin Inserted! Waiting for Start.");
            }
        }

        public void PressStart()
        {
            // Only start if a coin is loaded
            if (CurrentState == ArcadeState.CreditLoaded)
            {
                //Immediately change state so this can't be spammed
                CurrentState = ArcadeState.Starting;
                StartCoroutine(StartGameSequence());
            }
        }

        private IEnumerator StartGameSequence()
        {
            //Show the HUD, Hide the Main Menu
            currentLives = 3;
            currentScore = 0;
            UpdateHUD();

            DKAudioManager.Instance.StopMusic();
            DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxIntro);

            // Wait for intro length
            if (DKAudioManager.Instance.sfxIntro != null)
                yield return new WaitForSeconds(DKAudioManager.Instance.sfxIntro.length);

            ShowPanel(hudPanel);

            //Destroy all barrels on the screen
            GameObject[] activeBarrels = GameObject.FindGameObjectsWithTag("Barrel");
            foreach (GameObject barrel in activeBarrels)
            {
                Destroy(barrel);
            }

            //Teleport Jumpman back to the start and kill any weird physics momentum
            jumpman.transform.position = jumpmanStartNode.position;
            if (jumpman.TryGetComponent(out BasicGiantMover jumpmanScript))
            {
                jumpmanScript.ResetPlayerState();
            }

            //Turn all the hammers back on
            foreach (GameObject hammer in mapHammers)
            {
                if (hammer != null) hammer.SetActive(true);
            }

            // Play Start Level Audio
            DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxStartLevel);
            if (DKAudioManager.Instance.sfxStartLevel != null)
                yield return new WaitForSeconds(DKAudioManager.Instance.sfxStartLevel.length);

            // Give the player control and start the game!
            CurrentState = ArcadeState.Playing;
            DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicLevel1);
            Debug.Log("Game Started! Jumpman can move.");
        }

        public void ResetToAttractMode()
        {
            CurrentState = ArcadeState.AttractMode;
            if (insertCoinPromptText != null)
                insertCoinPromptText.text = "INSERT COIN";

            ShowPanel(mainMenuPanel);
            DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicAttract);
        }

        //-- Gameplay Logic --
        public void WinGame()
        {
            if (CurrentState != ArcadeState.Playing) return;

            CurrentState = ArcadeState.GameOver;
            ShowPanel(winPanel);

            DKAudioManager.Instance.StopMusic();
            DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxWin);

            // Go back to the title screen after 5 seconds
            Invoke(nameof(ResetToAttractMode), 5f);
        }

        public void LoseLife()
        {
            if (CurrentState != ArcadeState.Playing) return;

            currentLives--;
            UpdateHUD();

            DKAudioManager.Instance.StopMusic();
            DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxDeath);

            if (currentLives <= 0)
            {
                CurrentState = ArcadeState.GameOver;
                ShowPanel(gameOverPanel);
                Invoke(nameof(ResetToAttractMode), 5f);
            }
            else
            {
                // Start the arcade death pause
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            CurrentState = ArcadeState.Respawning;
            Debug.Log("Jumpman died! Freezing game...");

            //Wait a moment so the player sees their death

            //Destroy all barrels on the screen
            GameObject[] activeBarrels = GameObject.FindGameObjectsWithTag("Barrel");
            foreach (GameObject barrel in activeBarrels)
            {
                Destroy(barrel);
            }

            //Teleport Jumpman back to the start and kill any weird physics momentum
            jumpman.transform.position = jumpmanStartNode.position;
            if (jumpman.TryGetComponent(out BasicGiantMover jumpmanScript))
            {
                jumpmanScript.ResetPlayerState();
            }

            //Turn all the hammers back on
            foreach (GameObject hammer in mapHammers)
            {
                if (hammer != null) hammer.SetActive(true);
            }

            yield return new WaitForSeconds(2f);

            //Play Start Level again when respawning
            DKAudioManager.Instance.PlaySFX(DKAudioManager.Instance.sfxStartLevel);
            if (DKAudioManager.Instance.sfxStartLevel != null)
                yield return new WaitForSeconds(DKAudioManager.Instance.sfxStartLevel.length);

            //Resume gameplay
            CurrentState = ArcadeState.Playing;
            DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicLevel1);
            Debug.Log("Board reset! Resuming Gameplay...");
        }

        public void AddScore(int pointsToAdd)
        {
            currentScore += pointsToAdd;
            UpdateHUD();
        }

        // --- Helper Function ---

        private void UpdateHUD()
        {
            // The "D6" formats the integer to always show 6 digits (e.g., 000500)
            if (scoreText != null) scoreText.text = "SCORE\n" + currentScore.ToString("D6");

            // Just slap the lives number on the end of the string
            if (livesText != null) livesText.text = "LIVES\n" + currentLives.ToString();
        }

        // This instantly turns off all panels and only turns on the one you want
        private void ShowPanel(GameObject panelToShow)
        {
            mainMenuPanel.SetActive(false);
            hudPanel.SetActive(false);
            winPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            if (panelToShow != null) panelToShow.SetActive(true);
        }

        public void SetDKPaused(bool isPaused)
        {
            IsDKPaused = isPaused;
        }
        public void TriggerHitStop(float duration = 0.1f)
        {
            // Only allow hit stop if the game is actively playing
            if (CurrentState == ArcadeState.Playing)
            {
                StartCoroutine(HitStopRoutine(duration));
            }
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            CurrentState = ArcadeState.HitStop;

            // Use realtime so it ignores any other time-scale weirdness
            yield return new WaitForSecondsRealtime(duration);

            // Ensure we didn't die or hit Pause during the freeze before giving control back
            if (CurrentState == ArcadeState.HitStop)
            {
                CurrentState = ArcadeState.Playing;
            }
        }
    }
}
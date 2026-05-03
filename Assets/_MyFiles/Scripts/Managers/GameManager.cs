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

        //Game State Machine
        public enum ArcadeState { AttractMode, CreditLoaded, Playing, Respawning, GameOver }
        public ArcadeState CurrentState { get; private set; } = ArcadeState.AttractMode;

        // The specific DK Pause State
        public bool IsDKPaused { get; private set; }

        [Header("DK Game UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;

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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void Start()
        {
            // Start the cabinet in Attract Mode ie (Waiting for coin)
            ResetToAttractMode();
        }

        // --- Arcade Flow  ---

        public void InsertCoin()
        {
            // Only accept coins if we are waiting for one
            if (CurrentState == ArcadeState.AttractMode)
            {
                CurrentState = ArcadeState.CreditLoaded;

                // Change the UI text from "Insert Coin" to "Press Start"
                if (insertCoinPromptText != null)
                    insertCoinPromptText.text = "CREDIT 1\nPRESS START";

                Debug.Log("Coin Inserted! Waiting for Start.");
            }
        }

        public void PressStart()
        {
            // Only start if a coin is loaded
            if (CurrentState == ArcadeState.CreditLoaded)
            {
                StartCoroutine(StartGameSequence());
            }
        }

        private IEnumerator StartGameSequence()
        {
            // 1. Show the HUD, Hide the Main Menu
            currentLives = 3;
            currentScore = 0;
            UpdateHUD();
            ShowPanel(hudPanel);

            // 2. Play the classic DK Start Jingle here!
            Debug.Log("Playing Start Jingle...");

            // 3. Wait for the jingle to finish
            yield return new WaitForSeconds(startDelay);

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

            // 4. Give the player control and start the game!
            CurrentState = ArcadeState.Playing;
            Debug.Log("Game Started! Jumpman can move.");

            // Tell the Donkey Kong AI to start throwing barrels
        }

        public void ResetToAttractMode()
        {
            CurrentState = ArcadeState.AttractMode;
            if (insertCoinPromptText != null)
                insertCoinPromptText.text = "INSERT COIN";

            ShowPanel(mainMenuPanel);

            //Reset Jumpman and Donkey Kong to their starting positions
        }

        //-- Gameplay Logic --
        public void WinGame()
        {
            if (CurrentState != ArcadeState.Playing) return;

            CurrentState = ArcadeState.GameOver;
            ShowPanel(winPanel);

            // Go back to the title screen after 5 seconds
            Invoke(nameof(ResetToAttractMode), 5f);
        }

        public void LoseLife()
        {
            if (CurrentState != ArcadeState.Playing) return;

            currentLives--;
            UpdateHUD();

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
            yield return new WaitForSeconds(2f);

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

            //Resume gameplay
            CurrentState = ArcadeState.Playing;
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
    }
}

using System;
using UnityEngine;

namespace _MyFiles.Scripts.Managers
{ 
    public class GameManager : MonoBehaviour
    {
        // The Singleton Instance
        public static GameManager Instance { get; private set; }

        // The specific DK Pause State
        public bool IsDKPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
           
            Instance = this;
           
            //Keep it alive even if you switch scenes
            //Well I don't think there will be any other scene so it's probably fine to have this off?
            //DontDestroyOnLoad(gameObject); 
        }

        public void SetDKPaused(bool isPaused)
        {
            IsDKPaused = isPaused;
            Debug.Log("DK Game Paused State: " + isPaused);

            // will add logic here to open/close the canvas menu
        }
    }
}

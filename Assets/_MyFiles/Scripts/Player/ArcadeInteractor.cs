using System.Collections;
using _MyFiles.Scripts.Managers;
using _MyFiles.Scripts.Environment; // To access the ArcadeCabinet script
using UnityEngine;
using UnityEngine.InputSystem;

namespace _MyFiles.Scripts.Player
{ 
    public class ArcadeInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera PlayerCamera;
        [SerializeField] private Transform CameraHeadBone;
        [SerializeField] private GameInputManager InputManager;

        [Header("Settings")]
        [SerializeField] private float TransitionSpeed = 2f;

        [Header("Private")]
        [SerializeField]private bool bIsTransitioning = false;
        [SerializeField] private bool bIsPlayingArcade = false;

        //Tracks the cabinet we are currently standing in front of
        [SerializeField] private ArcadeCabinet NearbyCabinet = null;
        [SerializeField] private Transform ActiveScreenAnchor = null;

        private void Start()
        {
            InputManager.InputActions.PlayerFreeRoaming.Interact.performed += TryInteract;
            InputManager.InputActions.PlayerLockedIn.Exit_Cabinet.performed += ExitArcade;
        }

        // --- TRIGGER LOGIC ---
        private void OnTriggerEnter(Collider other)
        {
            // If the trigger we walked into has an ArcadeCabinet script, remember it!
            if (other.TryGetComponent(out ArcadeCabinet cabinet))
            {
                NearbyCabinet = cabinet;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // If we walk away from the cabinet we remembered, forget it!
            if (other.TryGetComponent(out ArcadeCabinet cabinet) && cabinet == NearbyCabinet)
            {
                NearbyCabinet = null;
            }
        }

        // --- INTERACTION LOGIC ---
        private void TryInteract(InputAction.CallbackContext context)
        {
            Debug.Log("Player Interacted");
            if (bIsTransitioning || bIsPlayingArcade) return;

            // If we press E and we are standing near a cabinet, start the transition!
            if (NearbyCabinet != null)
            {
                ActiveScreenAnchor = NearbyCabinet.ScreenAnchor;
                StartCoroutine(TransitionToArcade());
            }
        }

        private void ExitArcade(InputAction.CallbackContext context)
        {
            Debug.Log("Player pressed ESC");
            if (bIsTransitioning || !bIsPlayingArcade) return;
            StartCoroutine(TransitionToFreeRoam());
        }

        // --- COROUTINES ---
        private IEnumerator TransitionToArcade()
        {
            bIsTransitioning = true;
            InputManager.InputActions.PlayerFreeRoaming.Disable();

            // Grab the exact starting position of the camera right now
            Vector3 startPos = PlayerCamera.transform.position;
            Quaternion startRot = PlayerCamera.transform.rotation;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * TransitionSpeed;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                // Lerp from where we actually are, to the anchor
                PlayerCamera.transform.position = Vector3.Lerp(startPos, ActiveScreenAnchor.position, smoothT);
                PlayerCamera.transform.rotation = Quaternion.Lerp(startRot, ActiveScreenAnchor.rotation, smoothT);
                yield return null;
            }

            InputManager.EnableCabinetPlay();
            bIsPlayingArcade = true;
            bIsTransitioning = false;
        }

        private IEnumerator TransitionToFreeRoam()
        {
            bIsTransitioning = true;
            InputManager.InputActions.PlayerLockedIn.Disable();

            // Grab the exact starting position of the camera right now
            Vector3 startPos = PlayerCamera.transform.position;
            Quaternion startRot = PlayerCamera.transform.rotation;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * TransitionSpeed;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                // Lerp from where we actually are, to the head bone
                PlayerCamera.transform.position = Vector3.Lerp(startPos, CameraHeadBone.position, smoothT);
                PlayerCamera.transform.rotation = Quaternion.Lerp(startRot, CameraHeadBone.rotation, smoothT);
                yield return null;
            }

            // Clear the anchor since we left
            ActiveScreenAnchor = null;

            InputManager.EnableFreeRoam();
            bIsPlayingArcade = false;
            bIsTransitioning = false;
        }

        private void OnDestroy()
        {
            if (InputManager != null && InputManager.InputActions != null)
            {
                InputManager.InputActions.PlayerFreeRoaming.Interact.performed -= TryInteract;
                InputManager.InputActions.PlayerLockedIn.Exit_Cabinet.performed -= ExitArcade;
            }
        }
    }
}

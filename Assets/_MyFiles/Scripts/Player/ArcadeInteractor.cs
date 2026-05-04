using System.Collections;
using _MyFiles.Scripts.Managers;
using _MyFiles.Scripts.Environment;
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
        [Tooltip("How fast the camera snaps left and right when leaning.")]
        [SerializeField] private float LeanSpeed = 8f;

        [Header("Private States")]
        [SerializeField] private bool bIsTransitioning = false;
        [SerializeField] private bool bIsPlayingArcade = false;

        //Tracks the cabinet we are currently standing in front of
        [SerializeField] private ArcadeCabinet NearbyCabinet = null;

        // Lean Anchors
        private Transform centerAnchor = null;
        private Transform leftAnchor = null;
        private Transform rightAnchor = null;
        private Transform currentTargetAnchor = null;

        // Toggle Lock
        private bool wasLeaningInputActiveLastFrame = false;

        private void Start()
        {
            InputManager.InputActions.PlayerFreeRoaming.Interact.performed += TryInteract;
            InputManager.InputActions.PlayerLockedIn.Exit_Cabinet.performed += ExitArcade;
        }

        private void Update()
        {
            // Only allow leaning if we are actively playing and NOT currently sitting down/standing up
            if (bIsPlayingArcade && !bIsTransitioning && centerAnchor != null)
            {
                HandleLeaning();
            }
        }

        private void HandleLeaning()
        {
            // Read the 1D Axis. Q is Negative (-1), E is Positive (1). Nothing pressed is (0).
            float leanInput = InputManager.InputActions.PlayerLockedIn.DK_Lean.ReadValue<float>();
            bool isLeaningInputActive = Mathf.Abs(leanInput) > 0.1f;

            // Only trigger the logic on the exact frame they push the button down
            if (isLeaningInputActive && !wasLeaningInputActiveLastFrame)
            {
                if (leanInput < -0.1f && leftAnchor != null) // Pressed Q (Left)
                {
                    // If we are already left, toggle back to center. Otherwise, go left!
                    currentTargetAnchor = (currentTargetAnchor == leftAnchor) ? centerAnchor : leftAnchor;
                }
                else if (leanInput > 0.1f && rightAnchor != null) // Pressed E (Right)
                {
                    // If we are already right, toggle back to center. Otherwise, go right!
                    currentTargetAnchor = (currentTargetAnchor == rightAnchor) ? centerAnchor : rightAnchor;
                }
            }

            // Save this frame's input state so we don't rapid-fire the toggle next frame
            wasLeaningInputActiveLastFrame = isLeaningInputActive;

            // Smoothly glide the camera towards whichever anchor is currently targeted
            PlayerCamera.transform.position = Vector3.Lerp(PlayerCamera.transform.position, currentTargetAnchor.position, Time.deltaTime * LeanSpeed);
            PlayerCamera.transform.rotation = Quaternion.Lerp(PlayerCamera.transform.rotation, currentTargetAnchor.rotation, Time.deltaTime * LeanSpeed);
        }

        // --- Trigger Logic ---
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out ArcadeCabinet cabinet))
            {
                NearbyCabinet = cabinet;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out ArcadeCabinet cabinet) && cabinet == NearbyCabinet)
            {
                NearbyCabinet = null;
            }
        }

        // --- Interaction Logic ---
        private void TryInteract(InputAction.CallbackContext context)
        {
            Debug.Log("Player Interacted");
            if (bIsTransitioning || bIsPlayingArcade) return;

            if (NearbyCabinet != null)
            {
                // Grab all 3 anchors from the cabinet
                centerAnchor = NearbyCabinet.CenterScreenAnchor;
                leftAnchor = NearbyCabinet.LeftScreenAnchor;
                rightAnchor = NearbyCabinet.RightScreenAnchor;

                // Default to the middle
                currentTargetAnchor = centerAnchor;
                wasLeaningInputActiveLastFrame = false; // Reset the toggle lock

                StartCoroutine(TransitionToArcade());
            }
        }

        private void ExitArcade(InputAction.CallbackContext context)
        {
            Debug.Log("Player pressed ESC");
            if (bIsTransitioning || !bIsPlayingArcade) return;
            StartCoroutine(TransitionToFreeRoam());
        }

        // --- Coroutines ---
        private IEnumerator TransitionToArcade()
        {
            bIsTransitioning = true;
            InputManager.InputActions.PlayerFreeRoaming.Disable();

            Vector3 startPos = PlayerCamera.transform.position;
            Quaternion startRot = PlayerCamera.transform.rotation;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * TransitionSpeed;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                // Lerp smoothly to the CENTER anchor when sitting down
                PlayerCamera.transform.position = Vector3.Lerp(startPos, centerAnchor.position, smoothT);
                PlayerCamera.transform.rotation = Quaternion.Lerp(startRot, centerAnchor.rotation, smoothT);
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

            // Grab exactly where the camera is right now (even if they were mid-lean!)
            Vector3 startPos = PlayerCamera.transform.position;
            Quaternion startRot = PlayerCamera.transform.rotation;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * TransitionSpeed;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                PlayerCamera.transform.position = Vector3.Lerp(startPos, CameraHeadBone.position, smoothT);
                PlayerCamera.transform.rotation = Quaternion.Lerp(startRot, CameraHeadBone.rotation, smoothT);
                yield return null;
            }

            // Clear the anchors since we left
            centerAnchor = null;
            leftAnchor = null;
            rightAnchor = null;
            currentTargetAnchor = null;

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
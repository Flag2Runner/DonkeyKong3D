using _MyFiles.Scripts.Managers;
using UnityEngine;

namespace _MyFiles.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameInputManager inputManager;
        [SerializeField] private Camera playerCamera;

        [Header("Settings")]
        public float walkSpeed = 5f;
        public float lookSensitivity = 0.5f;

        private CharacterController controller;
        private float verticalRotation = 0f;

        void Start()
        {
            controller = GetComponent<CharacterController>();

            // Lock the mouse cursor to the center of the screen and hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // ONLY move and look if the Free Roaming map is active
            if (!inputManager.InputActions.PlayerFreeRoaming.enabled) return;

            HandleMovement();
            HandleLook();
        }

        private void HandleMovement()
        {
            Vector2 moveInput = inputManager.InputActions.PlayerFreeRoaming.Move.ReadValue<Vector2>();

            // Move relative to where the player is currently facing
            Vector3 moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);

            // Simple gravity application
            moveDirection.y = -9.81f;

            controller.Move(moveDirection * walkSpeed * Time.deltaTime);
        }

        private void HandleLook()
        {
            Vector2 lookInput = inputManager.InputActions.PlayerFreeRoaming.Look.ReadValue<Vector2>();

            // Left/Right rotation (Turns the whole player body)
            transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

            // Up/Down rotation (Tilts the camera only)
            verticalRotation -= lookInput.y * lookSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f); // Prevent doing backflips
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }
    }
}

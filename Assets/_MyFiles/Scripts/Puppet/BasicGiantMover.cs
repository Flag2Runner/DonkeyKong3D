using System;
using _MyFiles.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _MyFiles.Scripts.Puppet
{
    public class BasicGiantMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float jumpForce = 8f;
        
        [SerializeField] private Rigidbody jumpManRigidbody;
        [SerializeField] private GameInputManager inputManager;
        [SerializeField] private float moveInput;

        private void Start()
        {
            jumpManRigidbody = GetComponent<Rigidbody>();
            
            //find the manager in the scene why not just set it before hand :/
            inputManager = FindFirstObjectByType<GameInputManager>();

            inputManager.InputActions.PlayerLockedIn.DK_Jump.performed += Jump;
        }

        private void Update()
        {
            //Read the 1D Axis (A/D keys) for left/right movement
            //Only works if PlayerLockedIn Map is active
            moveInput = inputManager.InputActions.PlayerLockedIn.DK_Move.ReadValue<float>();
        }

        private void FixedUpdate()
        {
            //Apply physics movement on the X Axis
            jumpManRigidbody.linearVelocity = new Vector3(moveInput * moveSpeed, jumpManRigidbody.linearVelocity.y , 0);
        }

        private void Jump(InputAction.CallbackContext context)
        {
            //simple physics jump
            jumpManRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void OnDestroy()
        {
            //clean up the event sub if it's destroyed
            if (inputManager)
            {
                inputManager.InputActions.PlayerLockedIn.DK_Jump.performed -= Jump;
            }
        }
    }
}

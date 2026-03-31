using System;
using _MyFiles.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _MyFiles.Scripts.Puppet
{
    public class BasicGiantMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float climbingSpeed = 5f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("How far down to check. Increase this slightly if the green line doesn't reach the floor!")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [Tooltip("How far UP to start the raycast to prevent starting inside the floor.")]
        [SerializeField] private float groundCheckOffset = 0.1f;

        [Header("Climbing Adjustments")]
        [Tooltip("A tiny nudge upward when dismounting a ladder to prevent spawning inside the floor.")]
        [SerializeField] private float dismountVerticalOffset = 0.05f;

        [Header("References")]
        [SerializeField] private Rigidbody jumpManRigidbody;
        [SerializeField] private GameInputManager inputManager;

        [Header("Debug States")]
        [SerializeField] private float moveInput;
        [SerializeField] private float climbingInput;

        [SerializeField] private bool bIsClimbing = false;
        [SerializeField] private bool bIsInLadderZone = false;
        [SerializeField] private bool bIsGrounded = false;
        [SerializeField] private bool bHasHammer = false;

        [SerializeField] private LadderData currentLadder;

        private void Start()
        {
            if (!jumpManRigidbody) jumpManRigidbody = GetComponent<Rigidbody>();
            if (!inputManager) return;

            inputManager.InputActions.PlayerLockedIn.DK_Jump.performed += Jump;
        }

        private void Update()
        {
            if (!inputManager) return;

            Vector3 rayStart = transform.position + (Vector3.up * groundCheckOffset);
            bIsGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
            Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, bIsGrounded ? Color.green : Color.red);

            Vector2 moveVector = inputManager.InputActions.PlayerLockedIn.DK_Move.ReadValue<Vector2>();
            moveInput = moveVector.x;
            climbingInput = moveVector.y;

            if (bIsInLadderZone && currentLadder != null && !bIsClimbing && bIsGrounded && !bHasHammer)
            {
                float distToTop = Vector3.Distance(transform.position, currentLadder.topNode.position);
                float distToBottom = Vector3.Distance(transform.position, currentLadder.bottomNode.position);

                // If we are closer to the BOTTOM node, ONLY allow climbing UP
                if (distToBottom < distToTop && climbingInput > 0.1f)
                {
                    StartClimbing(currentLadder.bottomNode);
                }
                // If we are closer to the TOP node, ONLY allow climbing DOWN
                else if (distToTop < distToBottom && climbingInput < -0.1f)
                {
                    StartClimbing(currentLadder.topNode);
                }
            }
        }

        private void FixedUpdate()
        {
            if (bIsClimbing && currentLadder != null)
            {
                if (climbingInput > 0)
                {
                    // Move towards the top node
                    transform.position = Vector3.MoveTowards(transform.position, currentLadder.topNode.position, climbingSpeed * Time.fixedDeltaTime);

                    // Check distance to top node
                    if (Vector3.Distance(transform.position, currentLadder.topNode.position) < 0.01f)
                    {
                        if (currentLadder.isTopBroken)
                        {
                            transform.position = currentLadder.topNode.position;
                        }
                        else
                        {
                            StopClimbing(true); // Pass 'true' to indicate we reached the top
                        }
                    }
                }
                else if (climbingInput < 0)
                {
                    // Move towards the bottom node
                    transform.position = Vector3.MoveTowards(transform.position, currentLadder.bottomNode.position, climbingSpeed * Time.fixedDeltaTime);

                    // Check distance to bottom node
                    if (Vector3.Distance(transform.position, currentLadder.bottomNode.position) < 0.01f)
                    {
                        if (currentLadder.isBottomBroken)
                        {
                            transform.position = currentLadder.bottomNode.position;
                        }
                        else
                        {
                            StopClimbing(false); // Pass 'false' to indicate we reached the bottom
                        }
                    }
                }
            }
            else if (!bIsClimbing)
            {
                jumpManRigidbody.linearVelocity = new Vector3(moveInput * moveSpeed, jumpManRigidbody.linearVelocity.y, 0);
            }
        }

        private void StartClimbing(Transform startNode)
        {
            bIsClimbing = true;
            jumpManRigidbody.isKinematic = true;
            jumpManRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Snap to the exact X and Z of the ladder, but maintain our current Y height (standing on the floor)
            transform.position = new Vector3(startNode.position.x, transform.position.y, startNode.position.z);
        }

        private void StopClimbing(bool reachedTop)
        {
            bIsClimbing = false;

            // Apply a microscopic vertical nudge before physics reactivates to prevent floor clipping
            if (reachedTop)
            {
                transform.position += Vector3.up * dismountVerticalOffset;
            }

            jumpManRigidbody.isKinematic = false;
            jumpManRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }

        private void Jump(InputAction.CallbackContext context)
        {
            if (!bIsClimbing && bIsGrounded && !bHasHammer)
            {
                jumpManRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            LadderData ladder = other.GetComponent<LadderData>();
            if (ladder != null)
            {
                // Remember the ladder data, and tell the script we are allowed to climb
                currentLadder = ladder;
                bIsInLadderZone = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            LadderData ladder = other.GetComponent<LadderData>();
            if (ladder != null && ladder == currentLadder)
            {
                // Tell the script we are no longer allowed to START a new climb.
                // BUT DO NOT nullify currentLadder! 
                // If they are currently mid-climb, let them finish using the remembered data!
                bIsInLadderZone = false;
            }
        }

        private void OnDestroy()
        {
            if (inputManager) inputManager.InputActions.PlayerLockedIn.DK_Jump.performed -= Jump;
        }
    }
}
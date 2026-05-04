using System;
using System.Collections;
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
        public bool IsGrounded => bIsGrounded;

        [Header("Climbing Adjustments")]
        [Tooltip("A tiny nudge upward when dismounting a ladder to prevent spawning inside the floor.")]
        [SerializeField] private float dismountVerticalOffset = 0.05f;

        [Header("References")]
        [SerializeField] private Rigidbody jumpManRigidbody;
        [SerializeField] private GameInputManager inputManager;
        [SerializeField] private Animator puppetAnimator;

        [Tooltip("Drag the child GameObject containing the Hammer collider here.")]
        [SerializeField] private GameObject hammerHitboxObject;

        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource; 
        [SerializeField] private AudioClip walkClimbClip;
        [SerializeField] private AudioClip jumpClip;

        [Header("Debug States")]
        [SerializeField] private float moveInput;
        [SerializeField] private float climbingInput;

        [SerializeField] private bool bIsClimbing = false;
        [SerializeField] private bool bIsInLadderZone = false;
        [SerializeField] private bool bIsGrounded = false;
        [SerializeField] private bool bHasHammer = false;

        [SerializeField] private LadderData currentLadder;
        [SerializeField] private bool isFacingRight = false;

        private Vector3 savedVelocity;
        private bool wasPaused = false;

        private void Start()
        {
            if (!jumpManRigidbody) jumpManRigidbody = GetComponent<Rigidbody>();
            if (!inputManager) return;

            //Turn off the hammer
            if (hammerHitboxObject != null) hammerHitboxObject.SetActive(false);

            inputManager.InputActions.PlayerLockedIn.DK_Jump.performed += Jump;
        }

        private void Update()
        {

            //Pause the animator if the game is paused!
            if (puppetAnimator != null)
            {
                // Pause the animation if the game is Paused OR in HitStop!
                puppetAnimator.speed = (GameManager.Instance.CurrentState == GameManager.ArcadeState.Paused ||
                                        GameManager.Instance.CurrentState == GameManager.ArcadeState.HitStop) ? 0f : 1f;
            }

            // if the input manager is null or If the arcade machine isn't actively playing a game just ignore all inputs and physics
            if (!inputManager || GameManager.Instance.CurrentState != GameManager.ArcadeState.Playing) 
            {
                // Ensure looping audio stops if paused or dead
                if (movementAudioSource != null && movementAudioSource.isPlaying) movementAudioSource.Stop();
                return;
            }

            Vector3 rayStart = transform.position + (Vector3.up * groundCheckOffset);
            bIsGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
            Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, bIsGrounded ? Color.green : Color.red);

            Vector2 moveVector = inputManager.InputActions.PlayerLockedIn.DK_Move.ReadValue<Vector2>();
            if (bIsGrounded) 
            {
                moveInput = moveVector.x;
            }

            climbingInput = moveVector.y;

            // Handle the looping audio
            HandleMovementAudio();

            //Feed data to the Animator
            if (puppetAnimator != null)
            {
                puppetAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
                puppetAnimator.SetBool("IsGrounded", bIsGrounded);
                puppetAnimator.SetFloat("VerticalVelocity", jumpManRigidbody.linearVelocity.y);
                puppetAnimator.SetBool("IsClimbing", bIsClimbing);
                puppetAnimator.SetFloat("ClimbSpeed", bIsClimbing ? climbingInput : 0f);
                puppetAnimator.SetBool("HasHammer", bHasHammer);
            }

            //Flip the character model left or right
            if (moveInput > 0.1f && isFacingRight && !bIsClimbing)
            {
                isFacingRight = false;
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (moveInput < -0.1f && !isFacingRight && !bIsClimbing)
            {
                isFacingRight = true;
                transform.rotation = Quaternion.Euler(0, 180, 0); // Flips the Master, and PuppetSync will copy it
            }

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
            // --- Pause Logic ---
            if (GameManager.Instance.CurrentState != GameManager.ArcadeState.Playing)
            {
                if (!wasPaused)
                {
                    // Only save momentum and freeze if we aren't already kinematic (like when climbing)
                    if (!jumpManRigidbody.isKinematic)
                    {
                        savedVelocity = jumpManRigidbody.linearVelocity;
                        jumpManRigidbody.isKinematic = true;
                    }
                    wasPaused = true;
                }
                return; // Stop the rest of the movement code
            }
            else if (wasPaused)
            {
                // We are unpausing!
                wasPaused = false;

                // Only restore physics if we aren't currently on a ladder
                if (!bIsClimbing)
                {
                    jumpManRigidbody.isKinematic = false;
                    jumpManRigidbody.WakeUp();
                    jumpManRigidbody.linearVelocity = savedVelocity;
                }
            }
            // -----------------------

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

        private void HandleMovementAudio()
        {
            if (movementAudioSource == null || walkClimbClip == null) return;

            bool isWalking = bIsGrounded && Mathf.Abs(moveInput) > 0.1f && !bIsClimbing;
            bool isClimbingActive = bIsClimbing && Mathf.Abs(climbingInput) > 0.1f;

            if (isWalking || isClimbingActive)
            {
                if (!movementAudioSource.isPlaying)
                {
                    movementAudioSource.clip = walkClimbClip;
                    movementAudioSource.loop = true;
                    movementAudioSource.Play();
                }

                movementAudioSource.pitch = isClimbingActive ? 0.8f : 1.0f;
            }
            else
            {
                movementAudioSource.Stop();
            }
        }

        private void StartClimbing(Transform startNode)
        {
            bIsClimbing = true;
            puppetAnimator.SetBool("IsClimbing", bIsClimbing);
            jumpManRigidbody.isKinematic = true;
            jumpManRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            //Force him to face forward while on a ladder
            transform.rotation = Quaternion.Euler(0, -180, 0);

            // Snap to the exact X and Z of the ladder, but maintain our current Y height (standing on the floor)
            transform.position = new Vector3(startNode.position.x, transform.position.y, startNode.position.z);
        }

        private void StopClimbing(bool reachedTop)
        {
            bIsClimbing = false;
            puppetAnimator.SetBool("IsClimbing", bIsClimbing);
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
                DKAudioManager.Instance.PlaySFX(jumpClip); 
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            LadderData ladder = other.GetComponent<LadderData>();
            if (ladder != null)
            {
                currentLadder = ladder;
                bIsInLadderZone = true;
            }

            // Pick up the Hammer!
            if (other.CompareTag("HammerPickup"))
            {
                other.gameObject.SetActive(false);

                //Tell the Animator about the hammer IMMEDIATELY 
                bHasHammer = true;
                if (puppetAnimator != null)
                {
                    puppetAnimator.SetBool("HasHammer", true);
                    // Force the animator to instantly process the new boolean before we freeze it!
                    puppetAnimator.Update(0f);
                }

                // Freeze the game for 0.15 seconds to emphasize the power-up!
                GameManager.Instance.TriggerHitStop(0.15f);

                StartCoroutine(HammerTimerRoutine());
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

        private void OnCollisionEnter(Collision collision)
        {
            // If a barrel hits the player lose a life
            if (collision.gameObject.CompareTag("Barrel"))
            {
                KillPlayer();
            }
        }

        private void OnDestroy()
        {
            if (inputManager) inputManager.InputActions.PlayerLockedIn.DK_Jump.performed -= Jump;
        }

        public void ResetPlayerState()
        {
            bIsClimbing = false;
            bIsInLadderZone = false;
            currentLadder = null;
            bHasHammer = false;
            isFacingRight = true;

            wasPaused = false;

            if (hammerHitboxObject != null) hammerHitboxObject.SetActive(false);
            StopAllCoroutines(); // Stops the hammer timer if it was running

            if (jumpManRigidbody != null)
            {
                // Force physics back on and reset the constraints
                jumpManRigidbody.isKinematic = false;
                jumpManRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
                jumpManRigidbody.linearVelocity = Vector3.zero;
            }

            //Wipe the animator memory so he doesn't stay dead
            if (puppetAnimator != null)
            {
                puppetAnimator.ResetTrigger("Die");
                puppetAnimator.Play("Idle");
                puppetAnimator.SetBool("HasHammer", false);
            }
        }
        private IEnumerator HammerTimerRoutine()
        {
            bHasHammer = true;
            if (hammerHitboxObject != null) hammerHitboxObject.SetActive(true);

            DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicHammer);

            // Classic arcade hammer lasts about 10 seconds
            yield return new WaitForSeconds(10f);

            bHasHammer = false;
            if (hammerHitboxObject != null) hammerHitboxObject.SetActive(false);

            if (GameManager.Instance.CurrentState == GameManager.ArcadeState.Playing)
            {
                DKAudioManager.Instance.PlayMusic(DKAudioManager.Instance.musicLevel1);
            }
        }
        public void KillPlayer()
        {
            // Trigger the death animation
            if (puppetAnimator != null) puppetAnimator.SetTrigger("Die");

            // Tell the GameManager to handle the sequence
            GameManager.Instance.LoseLife();
        }
    }
}
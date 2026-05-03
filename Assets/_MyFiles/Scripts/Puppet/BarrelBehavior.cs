using UnityEngine;
using _MyFiles.Scripts.Managers;
using _MyFiles.Scripts.Puppet.Environment;

namespace _MyFiles.Scripts.Puppet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BarrelBehavior : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float dropSpeed = 5f;
        [SerializeField] private float initialPushForce = 2f;
        [SerializeField] private float ladderDropChance = 0.25f;

        [Header("Targeted Throw Settings")]

        private Rigidbody barrelRigidBody;
        [SerializeField] private bool isDropping = false;

        private Vector3 targetDropPosition;
        [SerializeField] private float nextRollDirection = 1f;

        private Vector3 calculatedHoverPoint;
        [SerializeField] private bool isBeingThrownToTarget = false;
        private bool needsLandingPush = false;

        private void Start()
        {
            barrelRigidBody = GetComponent<Rigidbody>();
            barrelRigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.CurrentState != GameManager.ArcadeState.Playing)
            {
                barrelRigidBody.isKinematic = true;
                return;
            }

            if (!isDropping && !isBeingThrownToTarget && barrelRigidBody.isKinematic)
            {
                barrelRigidBody.isKinematic = false;
            }

            if (isBeingThrownToTarget)
            {
                transform.position = Vector3.MoveTowards(transform.position, calculatedHoverPoint, dropSpeed * Time.fixedDeltaTime);

                if (transform.position == calculatedHoverPoint)
                {
                    isBeingThrownToTarget = false;
                    barrelRigidBody.isKinematic = false;
                    barrelRigidBody.WakeUp();
                }
                return;
            }

            if (isDropping)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetDropPosition, dropSpeed * Time.fixedDeltaTime);

                if (Vector3.Distance(transform.position, targetDropPosition) < 0.05f)
                {
                    FinishDrop();
                }
            }
        }

        // --- Blackmagic stuff Downhill Math --- My head hurts reading this stuff everytime....
        private void OnCollisionEnter(Collision collision)
        {
            if (needsLandingPush && !collision.gameObject.CompareTag("Barrel"))
            {
                // Grab the angle of whatever we just hit
                Vector3 impactNormal = collision.contacts[0].normal;

                // Safety Check: Did it scrape a vertical wall? (y will be close to 0)
                // We only want to trigger the landing push if we hit a FLOOR (y > 0.5)
                if (impactNormal.y < 0.5f)
                {
                    Debug.Log("Scraped a wall! Waiting to hit the actual floor...");
                    return; // Ignore this collision and wait for the next one!
                }

                // We officially hit the floor! Consume the push.
                needsLandingPush = false;

                // Racast Check: Shoot a raycast straight down from the center of the barrel.
                // This ignores any weird corners it might be touching and gets the pure slope of the floor.
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
                {
                    Vector3 floorNormal = hit.normal;

                    // Math: If normal.x is positive, the floor slopes down to the Right (1f).
                    // If normal.x is negative, the floor slopes down to the Left (-1f).
                    float calculatedDownhillDirection = floorNormal.x > 0 ? 1f : -1f;

                    // Flat floor fallback: If it's a perfectly flat floor (x is basically 0), default to rolling right.
                    if (Mathf.Abs(floorNormal.x) < 0.01f) calculatedDownhillDirection = 1f;

                    InitialPush(calculatedDownhillDirection);
                    Debug.Log("Hit the floor safely! Raycast normal: " + floorNormal.x + " | Rolling: " + calculatedDownhillDirection);
                }
                else
                {
                    // Absolute fallback if the raycast somehow misses
                    InitialPush(1f);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDropping || isBeingThrownToTarget) return;

            if (other.TryGetComponent(out BarrelDropZone dropZone))
            {
                targetDropPosition = dropZone.landingNode.position;
                nextRollDirection = dropZone.rollDirectionAfterLanding;
                StartDrop();
            }
            else if (other.TryGetComponent(out LadderData ladder))
            {
                float distToTop = Vector3.Distance(transform.position, ladder.topNode.position);
                if (ladder.isTopBroken == false && ladder.isBottomBroken == false && distToTop < 1.0f)
                {
                    if (Random.value <= ladderDropChance)
                    {
                        targetDropPosition = ladder.bottomNode.position;
                        nextRollDirection = ladder.barrelRollDirectionOut;
                        StartDrop();
                    }
                }
            }

            if (other.CompareTag("OilDrum"))
            {
                Destroy(gameObject);
            }
        }

        private void StartDrop()
        {
            isDropping = true;
            barrelRigidBody.isKinematic = true;
        }

        private void FinishDrop()
        {
            isDropping = false;
            barrelRigidBody.isKinematic = false;

            barrelRigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
            barrelRigidBody.linearVelocity = new Vector3(nextRollDirection * initialPushForce, 0, 0);
        }

        public void InitialPush(float direction)
        {
            if (barrelRigidBody == null) barrelRigidBody = GetComponent<Rigidbody>();
            barrelRigidBody.isKinematic = false;

            // If direction is 0 (Igniter), we push slightly down so it falls into the drum
            if (direction == 0f)
            {
                barrelRigidBody.AddForce(new Vector3(0f, -2f, 0), ForceMode.Impulse);
            }
            else
            {
                barrelRigidBody.AddForce(new Vector3(direction * initialPushForce, 0, 0), ForceMode.Impulse);
            }
        }

        // We removed the direction parameter. The barrel figures it out when it lands!
        public void ThrowAtTarget(Vector3 groundTargetPosition, float zOffset, bool isIgniter)
        {
            if (barrelRigidBody == null) barrelRigidBody = GetComponent<Rigidbody>();

            calculatedHoverPoint = new Vector3(groundTargetPosition.x, transform.position.y, groundTargetPosition.z + zOffset);

            // Only prime the landing push if it's a Sniper. 
            // Igniters just drop straight into the drum and die, they don't roll!
            needsLandingPush = !isIgniter;

            isBeingThrownToTarget = true;
            barrelRigidBody.isKinematic = true;
        }

        private void OnDestroy()
        {
            PuppetSync[] allPuppets = FindObjectsByType<PuppetSync>(FindObjectsSortMode.None);
            foreach (PuppetSync puppet in allPuppets)
            {
                if (puppet.masterTarget == this.transform)
                {
                    Destroy(puppet.gameObject);
                }
            }
        }
    }
}
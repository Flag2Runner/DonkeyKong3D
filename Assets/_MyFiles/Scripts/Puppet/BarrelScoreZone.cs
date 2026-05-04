using UnityEngine;
using _MyFiles.Scripts.Managers;

namespace _MyFiles.Scripts.Puppet
{
    public class BarrelScoreZone : MonoBehaviour
    {
        [Tooltip("How high above the center of the barrel should the trigger sit?")]
        [SerializeField] private float verticalOffset = 1.0f;

        private bool hasScored = false;
        private Transform parentBarrel;

        private void Start()
        {
            // Cache the parent barrel so we can track its exact center
            parentBarrel = transform.parent;
        }

        // --- Gimbal Lock ---
        private void LateUpdate()
        {
            if (parentBarrel == null) return;

            //Lock the rotation upright so it doesn't spin
            transform.rotation = Quaternion.identity;
 
            // Force the box to sit exactly straight up from the barrel's center, ignoring its rotation.
            transform.position = parentBarrel.position + (Vector3.up * verticalOffset);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasScored) return;

            // Did Jumpman just pass through this trigger?
            if (other.TryGetComponent(out BasicGiantMover jumpman))
            {
                // Check the public getter! Is he in the air?
                if (!jumpman.IsGrounded)
                {
                    GameManager.Instance.AddScore(100);
                    hasScored = true;
                    Debug.Log("Jumped over barrel! +100 Points");
                }
            }
        }
    }
}
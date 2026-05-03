using UnityEngine;

namespace _MyFiles.Scripts.Puppet.Environment
{
    public class BarrelDropZone : MonoBehaviour
    {
        [Tooltip("Place an Empty GameObject exactly where the barrel should land on the floor below.")]
        public Transform landingNode;

        [Tooltip("Which direction should the barrel roll after landing? (1 for Right, -1 for Left)")]
        public float rollDirectionAfterLanding = 1f;
    }
}
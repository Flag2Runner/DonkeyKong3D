using UnityEngine;
using _MyFiles.Scripts.Puppet;

namespace _MyFiles.Scripts.Environment
{
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // 1. Did Jumpman fall out of bounds?
            if (other.TryGetComponent(out BasicGiantMover jumpman))
            {
                Debug.Log("Jumpman fell off the map!");
                jumpman.KillPlayer();
            }
            // 2. Did a rogue barrel fall out of bounds?
            else if (other.CompareTag("Barrel"))
            {   
                Debug.Log("Garbage collecting lost barrel.");
                Destroy(other.gameObject);
            }
        }
    }
}
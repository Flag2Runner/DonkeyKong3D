using UnityEngine;
using _MyFiles.Scripts.Managers;
using _MyFiles.Scripts.Puppet; // To access BasicGiantMover Probably

namespace _MyFiles.Scripts.Puppet.Enviorment
{
    public class DKWinZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // Check if the thing that entered the trigger is Jumpman
            if (other.GetComponent<BasicGiantMover>() != null)
            {
                GameManager.Instance.AddScore(1500);
                GameManager.Instance.WinGame();
            }
        }
    }
}
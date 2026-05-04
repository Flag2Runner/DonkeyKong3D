using UnityEngine;

namespace _MyFiles.Scripts.Puppet
{
    public class DKPuppetEventReceiver : MonoBehaviour
    {
        [Tooltip("Drag the Master Donkey Kong GameObject here so the Puppet can talk to it.")]
        [SerializeField] private DonkeyKongAI masterAI; 

        // The Animation Event calls this on the Puppet...
        public void GrabBarrelEvent()
        {
            if (masterAI != null) masterAI.GrabBarrelEvent();
        }

        // The Animation Event calls this on the Puppet...
        public void ReleaseBarrelEvent()
        {
            if (masterAI != null) masterAI.ReleaseBarrelEvent();
        }
    }
}
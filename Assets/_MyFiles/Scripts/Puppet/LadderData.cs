using UnityEngine;

namespace _MyFiles.Scripts.Puppet
{
    public class LadderData : MonoBehaviour
    {
        public Transform bottomNode;
        [Tooltip("Check this if the bottom of the ladder is broken/floating.")]
        public bool isBottomBroken = false;

        public Transform topNode;
        [Tooltip("Check this if the top of the ladder is broken/floating.")]
        public bool isTopBroken = false;
    }
}
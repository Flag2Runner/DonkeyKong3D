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

        [Header("Barrel Settings")]
        [Tooltip("When a barrel drops down this ladder, which way should it roll? (1 for Right, -1 for Left)")]
        public float barrelRollDirectionOut = 1f;
    }
}
using UnityEngine;

namespace _MyFiles.Scripts.Environment
{
    public class ArcadeCabinet : MonoBehaviour
    {

        [Tooltip("The exact spot the camera should sit for this specific machine.")]
        [Header("Camera Anchors")]
        public Transform CenterScreenAnchor;
        public Transform LeftScreenAnchor;
        public Transform RightScreenAnchor;
    }
}

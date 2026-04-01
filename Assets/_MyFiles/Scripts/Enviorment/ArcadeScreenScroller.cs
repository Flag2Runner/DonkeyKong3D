using UnityEngine;

namespace _MyFiles.Scripts.Environment
{
    [RequireComponent(typeof(Renderer))]
    public class ArcadeScreenScroller : MonoBehaviour
    {
        [Header("Scroll Speeds")]
        [Tooltip("Speed on the X axis. Make this faster for horizontal scanline movement.")]
        public float scrollSpeedX = 0.25f;

        [Tooltip("Speed on the Y axis. Keep this slow for a subtle vertical drift.")]
        public float scrollSpeedY = 0.05f;

        // In the URP Lit Shader, the base texture is internally named "_BaseMap"
        private string targetProperty = "_BaseMap";

        private Renderer screenRenderer;
        private Material screenMaterial;

        void Start()
        {
            screenRenderer = GetComponent<Renderer>();

            // By calling .material (instead of .sharedMaterial), Unity creates a unique 
            // instance of the material for this specific cabinet. This ensures that if you 
            // have 10 cabinets, their screens don't all scroll in perfect, robotic unison.
            screenMaterial = screenRenderer.material;
        }

        void Update()
        {
            // Calculate the current offset based on time and speed
            float offsetX = Mathf.Repeat(Time.time * scrollSpeedX, 1f);
            float offsetY = Mathf.Repeat(Time.time * scrollSpeedY, 1f);

            // Apply the offset
            screenMaterial.SetTextureOffset(targetProperty, new Vector2(offsetX, offsetY));
        }
    }
}
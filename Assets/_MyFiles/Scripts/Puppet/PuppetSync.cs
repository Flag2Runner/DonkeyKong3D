using UnityEngine;

public class PuppetSync : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Transform masterTarget;
    [SerializeField] private Transform masterLevelCenter;
    [SerializeField] private Transform dioramaCenter;

    [Header("Settings")]
    [Tooltip("How much smaller the puppet is. (e.g, 0.1 for 1/10th scale)")]
    [SerializeField] private float scaleDownFactor = 0.024f;

    [SerializeField] private Vector3 SavedScale = Vector3.zero;

    private void Start()
    {
        SavedScale = transform.localScale;
    }

    private void Update()
    {
        if (!masterTarget || !masterLevelCenter || !dioramaCenter) return;

        //If the Master is turned off, shrink the Puppet to nothing so it "disappears"
        if (!masterTarget.gameObject.activeInHierarchy)
        {
            transform.localScale = Vector3.zero;
            return; // Skip the rest of the math until it turns back on
        }
        else
        {
            // Restore its normal scale when the master comes back
            transform.localScale = SavedScale;
        }

        // Find exactly where the master object is relative to its own floor
        Vector3 offsetFromMasterCenter = masterTarget.position - masterLevelCenter.position;

        // Shrink the movement distance down to the diorama scale
        Vector3 scaleOffset = offsetFromMasterCenter * scaleDownFactor;

        // Apply the shrunken movement to the center of the arcade cabinet
        transform.position = dioramaCenter.position + scaleOffset;

        // Match the rotation exactly
        transform.rotation = masterTarget.rotation;
    }

    //Allow dynamic objects (like barrels) to set themselves up via code
    public void InitializeDynamicPuppet(Transform master, Transform masterCenter, Transform miniCenter, float scale)
    {
        masterTarget = master;
        masterLevelCenter = masterCenter;
        dioramaCenter = miniCenter;
        scaleDownFactor = scale;
    }
}
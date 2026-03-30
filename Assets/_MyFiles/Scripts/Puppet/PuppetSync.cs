using System;
using UnityEngine;

public class PuppetSync : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform masterTarget; // The actual player character with collisions
    [SerializeField] private Transform masterLevelCenter; // An empty GameObject at the center of the massive level
    [SerializeField] private Transform dioramaCenter;  //An empty GameObject inside the arcade cabinet
    
    [Header("Settings")]
    [Tooltip("How much smaller the puppet is. (e.g, 0.1 for 1/10th scale)")]
    [SerializeField] private float scaleDownFactor =  0.1f;

    private void Update()
    {
        if(!masterTarget || !masterLevelCenter || !dioramaCenter)
            return;
        
        //Find exactly where the master object is relative to it's own floor
        Vector3 offsetFromMasterCenter = masterTarget.position - masterLevelCenter.position;
        
        //Shrink the movement distance down to the diorama scale
        Vector3 scaleOffset = offsetFromMasterCenter * scaleDownFactor;
        
        //Apply the shrunken movement to the center of the arcade cabinet
        transform.position = dioramaCenter.position + scaleOffset;
        
        //Match the rotation exactly
        transform.rotation = masterTarget.rotation;
        
    }
}

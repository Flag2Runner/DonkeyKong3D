using System.Collections;
using UnityEngine;
using _MyFiles.Scripts.Managers;
using static _MyFiles.Scripts.Managers.GameManager;

namespace _MyFiles.Scripts.Puppet
{
    public class DonkeyKongAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject barrelPrefab;
        [SerializeField] private GameObject puppetBarrelPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform jumpmanStartNode;
        [SerializeField] private Transform jumpmanTransform;

        [Header("Throw Timings")]
        [SerializeField] private float minTimeBetweenThrows = 3f;
        [SerializeField] private float maxTimeBetweenThrows = 6f;
        [SerializeField] private float throwDirection = 1f;

        [Header("Targeted Offsets")]
        [Tooltip("Z Offset for the Igniter barrel so it perfectly hits the Oil Drum.")]
        [SerializeField] private float igniterZOffset = -0.5f;
        [Tooltip("Z Offset for Sniper barrels so they perfectly align with Jumpman's depth.")]
        [SerializeField] private float sniperZOffset = -0.7f;

        [Header("Sniper Settings")]
        [SerializeField] private int barrelsBeforeSnipeChance = 5;
        [SerializeField] private float snipeChance = 0.3f;

        [Header("Private Values")]
        [SerializeField] private bool isThrowingRoutineRunning = false;
        [SerializeField] private int totalBarrelsThrown = 0;
        [SerializeField] private int barrelsSinceLastSnipe = 0;

        private void Update()
        {
            if (GameManager.Instance.CurrentState == ArcadeState.Playing)
            {
                if (!isThrowingRoutineRunning) StartCoroutine(ThrowBarrelRoutine());
            }

            if (isThrowingRoutineRunning == true && GameManager.Instance.CurrentState != ArcadeState.Playing)
            {
                StopAllCoroutines();
                isThrowingRoutineRunning = false;
                totalBarrelsThrown = 0;
                barrelsSinceLastSnipe = 0;
            }
        }

        private IEnumerator ThrowBarrelRoutine()
        {
            isThrowingRoutineRunning = true;

            float waitTime = Random.Range(minTimeBetweenThrows, maxTimeBetweenThrows);
            yield return new WaitForSeconds(waitTime);

            GameObject newBarrel = Instantiate(barrelPrefab, spawnPoint.position, spawnPoint.rotation);
            GameObject miniBarrel = Instantiate(puppetBarrelPrefab, Vector3.zero, Quaternion.identity);

            PuppetSync syncScript = miniBarrel.AddComponent<PuppetSync>();
            syncScript.InitializeDynamicPuppet(
                newBarrel.transform,
                GameManager.Instance.masterLevelCenter,
                GameManager.Instance.dioramaCenter,
                GameManager.Instance.puppetScaleFactor
            );

            if (newBarrel.TryGetComponent(out BarrelBehavior barrelScript))
            {
                if (totalBarrelsThrown == 0 && jumpmanStartNode != null)
                {
                    // Igniter: Pass 'true' because this is the igniter barrel.
                    barrelScript.ThrowAtTarget(jumpmanStartNode.position, igniterZOffset, true);
                    barrelsSinceLastSnipe++;
                }
                else if (barrelsSinceLastSnipe >= barrelsBeforeSnipeChance && jumpmanTransform != null)
                {
                    if (Random.value <= snipeChance)
                    {
                        Debug.Log("DK is Sniping the Player!");

                        // Sniper: Pass 'false' because this is not the igniter barrel.
                        // It will calculate its own downhill roll direction when it hits the floor
                        barrelScript.ThrowAtTarget(jumpmanTransform.position, sniperZOffset, false);
                        barrelsSinceLastSnipe = 0;
                    }
                    else
                    {
                        barrelScript.InitialPush(throwDirection);
                        barrelsSinceLastSnipe++;
                    }
                }
                else
                {
                    // Normal Throw
                    barrelScript.InitialPush(throwDirection);
                    barrelsSinceLastSnipe++;
                }
            }

            totalBarrelsThrown++;
            isThrowingRoutineRunning = false;
        }
    }
}
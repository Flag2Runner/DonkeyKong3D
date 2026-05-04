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

        //Split spawn points for different throw types
        [SerializeField] private Transform underhandSpawn;
        [SerializeField] private Transform overhandSpawn;

        [SerializeField] private Transform jumpmanStartNode;
        [SerializeField] private Transform jumpmanTransform;

        //The Puppet Animator and the visual dummy barrel in DK's hand
        [Header("Animation Setup")]
        [SerializeField] private Animator puppetAnimator;
        [Tooltip("The visual barrel attached to the Puppet DK's hand bone.")]
        [SerializeField] private GameObject handBarrelDummy;

        [Header("Throw Timings")]
        [SerializeField] private float minTimeBetweenThrows = 3f;
        [SerializeField] private float maxTimeBetweenThrows = 6f;
        [SerializeField] private float throwDirection = 1f;

        [Header("Targeted Offsets")]
        [SerializeField] private float igniterZOffset = -0.5f;
        [SerializeField] private float sniperZOffset = -0.7f;

        [Header("Sniper Settings")]
        [SerializeField] private int barrelsBeforeSnipeChance = 5;
        [SerializeField] private float snipeChance = 0.3f;

        [Header("Private Values")]
        [SerializeField] private bool isThrowingRoutineRunning = false;
        [SerializeField] private int totalBarrelsThrown = 0;
        [SerializeField] private int barrelsSinceLastSnipe = 0;

        //Flag to pause the code while the animation plays
        private bool isWaitingForAnimationToFinish = false;

        private void Start()
        {
            // Make sure the hand barrel is hidden by default
            if (handBarrelDummy != null) handBarrelDummy.SetActive(false);
        }

        private void Update()
        {
            if (puppetAnimator != null)
            {
                // Pause the animation if the game is Paused OR in HitStop!
                puppetAnimator.speed = (GameManager.Instance.CurrentState == ArcadeState.Paused ||
                                        GameManager.Instance.CurrentState == ArcadeState.HitStop) ? 0f : 1f;
            }

            if (GameManager.Instance.CurrentState == ArcadeState.Playing)
            {
                if (!isThrowingRoutineRunning) StartCoroutine(ThrowBarrelRoutine());
            }

            //ONLY stop the routine if we actually died or the game ended.
            // DO NOT stop it if we just Paused!
            if (isThrowingRoutineRunning == true &&
               (GameManager.Instance.CurrentState == ArcadeState.Respawning ||
                GameManager.Instance.CurrentState == ArcadeState.GameOver ||
                GameManager.Instance.CurrentState == ArcadeState.AttractMode))
            {
                StopAllCoroutines();
                isThrowingRoutineRunning = false;
                isWaitingForAnimationToFinish = false;
                totalBarrelsThrown = 0;
                barrelsSinceLastSnipe = 0;

                if (handBarrelDummy != null) handBarrelDummy.SetActive(false);
            }
        }

        private IEnumerator ThrowBarrelRoutine()
        {
            isThrowingRoutineRunning = true;

            float waitTime = Random.Range(minTimeBetweenThrows, maxTimeBetweenThrows);
            float timer = 0f;

            //A custom timer that freezes completely when paused!
            while (timer < waitTime)
            {
                if (GameManager.Instance.CurrentState == ArcadeState.Playing)
                {
                    timer += Time.deltaTime;
                }
                yield return null;
            }

            // --- Sync Logic ---
            // Tell the animator what kind of throw this will be
            bool willBeSniperOrIgniter = (totalBarrelsThrown == 0) || (barrelsSinceLastSnipe >= barrelsBeforeSnipeChance && Random.value <= snipeChance);

            Transform activeSpawn = willBeSniperOrIgniter ? overhandSpawn : underhandSpawn;

            if (puppetAnimator != null)
            {
                puppetAnimator.SetInteger("ThrowType", willBeSniperOrIgniter ? 1 : 0);
                puppetAnimator.SetTrigger("Throw");

                isWaitingForAnimationToFinish = true;
                while (isWaitingForAnimationToFinish)
                {
                    yield return null;
                }
            }
            // ----------------------------------

            // Instantiate at the chosen point
            GameObject newBarrel = Instantiate(barrelPrefab, activeSpawn.position, activeSpawn.rotation);
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
                    barrelScript.ThrowAtTarget(jumpmanStartNode.position, igniterZOffset, true);
                    barrelsSinceLastSnipe++;
                }
                else if (willBeSniperOrIgniter && jumpmanTransform != null)
                {
                    Debug.Log("DK is Sniping the Player!");
                    barrelScript.ThrowAtTarget(jumpmanTransform.position, sniperZOffset, false);
                    barrelsSinceLastSnipe = 0;
                }
                else
                {
                    barrelScript.InitialPush(throwDirection);
                    barrelsSinceLastSnipe++;
                }
            }

            totalBarrelsThrown++;
            isThrowingRoutineRunning = false;
        }

        // --- Event Functions ---

        //Animator will place an Animation Event at the start of the swing calling this
        public void GrabBarrelEvent()
        {
            if (handBarrelDummy != null) handBarrelDummy.SetActive(true);
        }

        //Animator will place an Animation Event at the exact frame the hand lets go calling this
        public void ReleaseBarrelEvent()
        {
            if (handBarrelDummy != null) handBarrelDummy.SetActive(false);

            // This unpauses the Coroutine, causing the real physics barrel to instantly spawn
            isWaitingForAnimationToFinish = false;
        }
    }
}
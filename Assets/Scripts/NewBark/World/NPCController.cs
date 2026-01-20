using System.Collections;
using UnityEngine;
using NewBark.Data;

namespace NewBark.World
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class NPCController : MonoBehaviour
    {
        [Header("Settings")]
        public string trainerId = "trainer_0";
        public string targetTag = "Player"; // Tag check is crucial
        public float viewDistance = 5f;
        public float moveSpeed = 3f;
        public float stopDistance = 1.5f;

        [Header("State")]
        public bool isTrainer = true;
        public bool hasBattled = false;
        public bool isChasing = false;

        private Rigidbody2D rb;
        private Transform playerTarget;
        private TrainerData myData;
        private Vector2 lookDirection = Vector2.down;
        private Animator anim;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            // Load Trainer Data
            if (GameDatabase.Instance != null && GameDatabase.Instance.Trainers.ContainsKey(trainerId))
            {
                myData = GameDatabase.Instance.Trainers[trainerId];
            }

            // Check Persistence
            if (GameManager.Data != null && GameManager.Data.beatenTrainers.Contains(trainerId))
            {
                hasBattled = true;
            }
        }

        private void Update()
        {
            if (hasBattled) return;

            if (isChasing && playerTarget != null)
            {
                MoveToPlayer();
            }
            else if (isTrainer)
            {
                CheckForPlayer();
            }
        }

        private void CheckForPlayer()
        {
            // Simple Raycast in look direction
            // Using RaycastAll to avoid blocking view with own collider

            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, lookDirection, viewDistance);

            // Sort by distance to ensure we check the closest object first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Ignore myself
                if (hit.collider.gameObject == gameObject) continue;

                // Stop at first obstacle or player
                if (hit.collider.CompareTag(targetTag))
                {
                    Debug.Log("NPC detected player!");
                    playerTarget = hit.transform;
                    isChasing = true;
                    return; // Found target
                }
                else
                {
                    // It hit something else.
                    // If it's a trigger, we might want to see through it.
                    // If it's solid, vision is blocked.
                    if (!hit.collider.isTrigger)
                    {
                        // Vision Blocked by " + hit.collider.name
                        return;
                    }
                }
            }

            Debug.DrawRay(transform.position, lookDirection * viewDistance, Color.red);
        }

        private void MoveToPlayer()
        {
            float distance = Vector2.Distance(transform.position, playerTarget.position);

            if (distance > stopDistance)
            {
                Vector2 dir = (playerTarget.position - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;

                // Update Animation
                if (anim != null)
                {
                    anim.SetFloat("InputX", dir.x);
                    anim.SetFloat("InputY", dir.y);
                    anim.SetBool("IsMoving", true);
                    lookDirection = dir; // imprecise, but works for now
                }
            }
            else
            {
                // Stop and Battle
                rb.linearVelocity = Vector2.zero;
                if (anim != null) anim.SetBool("IsMoving", false);

                isChasing = false;
                hasBattled = true; // Set flag immediately to prevent loop

                StartCoroutine(StartBattleRoutine());
            }
        }

        private IEnumerator StartBattleRoutine()
        {
            // Dialog/Intro delay
            yield return new WaitForSeconds(0.5f);

            // Trigger Encounter
            if (EncounterManager.instance != null && myData != null)
            {
                EncounterManager.instance.StartTrainerBattle(myData);
            }
            else
            {
                Debug.LogError("Cannot start battle: Missing Manager or Data");
            }
        }

        // Gizmos for checking view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, lookDirection * viewDistance);
        }
    }
}

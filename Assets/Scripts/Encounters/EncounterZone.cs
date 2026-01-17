using UnityEngine;

public class EncounterZone : MonoBehaviour
{
    [SerializeField] EncounterData zoneData;

    [Header("Settings")]
    [SerializeField] float stepDistanceThreshold = 1.5f;
    [SerializeField] bool alwaysTriggerForTesting = true;

    private Vector3 lastStepPosition;
    private float currentStepDistance;
    private bool isPlayerInZone;
    private Transform playerTransform;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerTransform = collision.transform;
            lastStepPosition = playerTransform.position;
            isPlayerInZone = true;
            Debug.Log($"Player entered {zoneData.zoneName}. Tracking steps Enter...");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerTransform = null;
        }
    }

    private void Update()
    {
        if (isPlayerInZone && playerTransform != null)
        {
            float distanceMoved = Vector3.Distance(playerTransform.position, lastStepPosition);

            // Only count significant movement to avoid jitter
            if (distanceMoved > 0.01f)
            {
                currentStepDistance += distanceMoved;
                lastStepPosition = playerTransform.position;
            }

            if (currentStepDistance >= stepDistanceThreshold)
            {
                currentStepDistance = 0f;
                TryRandomEncounter();
            }
        }
    }


    public void TryRandomEncounter()
    {
        if (zoneData == null) return;

        float _dice = Random.Range(0f, 100f);

        Debug.Log($"Step taken! Dice: {_dice} | Chance: {zoneData.encounterChance}");

        if (alwaysTriggerForTesting || _dice <= zoneData.encounterChance)
        {
            Debug.Log($"Encounter triggered in {zoneData.zoneName}!");

            // Reset state to avoid re-triggering immediately or issues after battle
            isPlayerInZone = false;

            EncounterManager.instance.StartEncounter(zoneData);
        }
    }
}

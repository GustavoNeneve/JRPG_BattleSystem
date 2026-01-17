using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEncounterZone", menuName = "Encounter/EncounterZoneData")]
public class EncounterData : ScriptableObject
{
    [Header("Zone Info")]
    public string zoneName;
    public Sprite backgroundImage;
    public AudioClip battleMusic;

    [Header("Encounter Settings")]
    [Range(0, 100)] public float encounterChance = 10f;
    [Range(1, 4)] public int minEnemies = 1;
    [Range(1, 4)] public int maxEnemies = 3;

    [Header("Enemies Table")]
    public List<WeightedEnemy> enemyList;
}

[Serializable]
public struct WeightedEnemy
{
    public EnemyBehaviour enemyIdentifier;
    [Tooltip("Higher value = Higher chance to appear")]
    [Range(1, 100)] public int spawnWeight;
}

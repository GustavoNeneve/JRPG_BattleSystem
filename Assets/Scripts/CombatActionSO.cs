using UnityEngine;

public enum ActionType
{
    NORMAL_ATTACK,
    SKILL,
    ITEM,
    RECHARGING
}

public enum DamageType
{
    HARMFUL,
    HEALING,
    MANA,
    CAPTURE,
    UNDEFINED
}

[CreateAssetMenu(fileName = "New CombatAction", menuName = "Combat Actions/New CombatAction")]
[System.Serializable]
public class CombatActionSO : ScriptableObject
{
    public ActionType actionType;
    public DamageType damageType;
    public string actionName;
    public string description;
    public int mpCost;
    public GameObject projectile;
    public bool goToTarget;
    public bool isAreaOfEffect;
    public float damageMultiplier;
    public AnimationCycle animationCycle;
    public AudioClip actionSound; // Added for Audio integration

    public bool IsHarmful => this.damageType == DamageType.HARMFUL;

    public void InitializeFromData(NewBark.Data.MoveData data)
    {
        this.actionName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.dbSymbol.Replace("_", " "));
        this.mpCost = data.pp;

        switch (data.category)
        {
            case "special":
                this.actionType = ActionType.SKILL;
                this.damageType = data.isHeal ? DamageType.HEALING : DamageType.HARMFUL;
                break;
            case "physical":
                this.actionType = ActionType.NORMAL_ATTACK;
                this.damageType = DamageType.HARMFUL;
                break;
            case "status":
                this.actionType = ActionType.SKILL;
                this.damageType = data.isHeal ? DamageType.HEALING : DamageType.UNDEFINED;
                break;
        }

        if (data.isHeal) this.damageType = DamageType.HEALING;

        this.description = $"Power: {data.power} | Acc: {data.accuracy} | Type: {data.type}";
        this.damageMultiplier = data.power;

        // Load Audio Asynchronously
        if (NewBark.Data.GameDatabase.Instance != null)
        {
            NewBark.Data.GameDatabase.Instance.LoadAudioAsync(data.dbSymbol, (clip) =>
            {
                this.actionSound = clip;
            });
        }
    }
}

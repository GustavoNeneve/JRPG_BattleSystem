using System.Collections.Generic;
using NewBark.Data;

public static class TypeChart
{
    public static float GetEffectiveness(string attackType, string defenderType1, string defenderType2 = null)
    {
        float multiplier = GetSingleMultiplier(attackType, defenderType1);
        
        if (!string.IsNullOrEmpty(defenderType2) && defenderType2 != "none")
        {
            multiplier *= GetSingleMultiplier(attackType, defenderType2);
        }
        
        return multiplier;
    }

    private static float GetSingleMultiplier(string attackType, string defendType)
    {
        if (string.IsNullOrEmpty(attackType) || string.IsNullOrEmpty(defendType)) return 1f;

        // Retrieve from GameDatabase
        var types = GameDatabase.Instance.Types;
        if (types.TryGetValue(attackType, out var typeData))
        {
            if (typeData.damageTo != null)
            {
                foreach (var dt in typeData.damageTo)
                {
                    if (dt.defensiveType == defendType)
                    {
                        return dt.factor;
                    }
                }
            }
        }
        
        return 1f; // Neutral
    }
}

using System.Collections.Generic;
using UnityEngine;
using NewBark.Data;

namespace NewBark.Runtime
{
    public static class TypeChart
    {
        // Simple cache
        private static Dictionary<string, TypeData> _typeCache;

        private static void EnsureCache()
        {
            if (_typeCache == null)
            {
                _typeCache = GameDatabase.Instance.Types;
            }
        }

        public static float GetEffectiveness(string attackType, string defenseType1, string defenseType2 = null)
        {
            EnsureCache();
            float factor = 1.0f;

            if (string.IsNullOrEmpty(attackType)) return 1.0f;

            factor *= GetSingleTypeEffectiveness(attackType, defenseType1);

            if (!string.IsNullOrEmpty(defenseType2) && defenseType2 != defenseType1)
            {
                factor *= GetSingleTypeEffectiveness(attackType, defenseType2);
            }

            return factor;
        }

        private static float GetSingleTypeEffectiveness(string attackType, string defenseType)
        {
            if (string.IsNullOrEmpty(defenseType)) return 1.0f;
            EnsureCache();

            // Find the attacking type's data
            // The JSON structure is: TypeData has "damageTo" list.
            // So if AttackType is "fire", look at "fire.json", find "damageTo" entry for defenseType.

            if (_typeCache.TryGetValue(attackType, out var typeData))
            {
                if (typeData.damageTo != null)
                {
                    foreach (var dt in typeData.damageTo)
                    {
                        if (dt.defensiveType == defenseType)
                        {
                            return dt.factor;
                        }
                    }
                }
            }
            // Default 1.0 if not found in list (neutral)
            return 1.0f;
        }
    }
}

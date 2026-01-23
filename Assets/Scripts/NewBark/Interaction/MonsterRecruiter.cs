using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using NewBark.Input;
using NewBark.Runtime;
using Sirenix.OdinInspector;

namespace NewBark.Interaction
{
    public class MonsterRecruiter : MonoBehaviour
    {
        [Title("Monster Configuration")]
        [ValueDropdown("GetSpeciesIds")]
        [ValidateInput("IsValidSpecies", "Species ID not found in database!")]
        public string speciesID;

        [Range(1, 100)]
        public int level = 5;

        [Title("Interaction Settings")]
        [Tooltip("If true, the object will disable interaction after recruiting.")]
        public bool oneTimeOnly = true;

        [Tooltip("Optional: Message to show when recruited.")]
        public string recruitmentMessage = "You recruited a {0}!";

        private bool _recruited = false;

        // Called by InteractionController via SendMessage
        public void OnPlayerInteract(GameButton button)
        {
            if (_recruited) return;

            if (string.IsNullOrEmpty(speciesID))
            {
                Debug.LogError("[MonsterRecruiter] Species ID is empty!");
                return;
            }

            Debug.Log($"[MonsterRecruiter] Attempting to recruit {speciesID} (Lvl {level})...");

            // Create the monster instance
            PokemonInstance newMonster = new PokemonInstance(speciesID, level);

            // Add to Party
            bool added = PlayerParty.Instance.AddMember(newMonster);

            if (added)
            {
                Debug.Log($"[MonsterRecruiter] Successfully added {newMonster.Nickname} to party!");
                
                // Show feedback (Optional: You can integrate with your Dialog system here)
                // DialogManager.Show(string.Format(recruitmentMessage, newMonster.Nickname));

                if (oneTimeOnly)
                {
                    _recruited = true;
                    // Disable collider or visual to prevent re-interaction
                    var col = GetComponent<Collider2D>();
                    if (col) col.enabled = false;
                    
                    // Optional: Destroy object or play Disappear animation
                    // Destroy(gameObject, 1f); 
                }
            }
            else
            {
                Debug.Log("[MonsterRecruiter] Party full. Monster sent to Storage.");
                 if (oneTimeOnly)
                {
                    _recruited = true;
                    var col = GetComponent<Collider2D>();
                    if (col) col.enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        // Editor-only method to populate Dropdown
        private IEnumerable<string> GetSpeciesIds()
        {
            // Path to JSON files
            string path = Path.Combine(Application.dataPath, "novo projeto/Data/Studio/pokemon");
            
            if (!Directory.Exists(path))
            {
                return new string[] { "Error: Path not found" };
            }

            var ids = new List<string>();
            var files = Directory.GetFiles(path, "*.json");

            foreach (var file in files)
            {
                try
                {
                    // Simple parse to find dbSymbol without loading full object
                    // Or just use the filename if it matches the ID, but inside JSON is safer.
                    string content = File.ReadAllText(file);
                    // Extremely basic search to avoid heavy JSON parsing for every file in Editor
                    // Assuming "dbSymbol": "bulbasaur" format
                    
                    // Let's use simple string search for speed in editor
                    int idx = content.IndexOf("\"dbSymbol\"");
                    if (idx != -1)
                    {
                        int startQuote = content.IndexOf("\"", idx + 11); // skip "dbSymbol":
                        if (startQuote != -1)
                        {
                            int endQuote = content.IndexOf("\"", startQuote + 1);
                            if (endQuote != -1)
                            {
                                string id = content.Substring(startQuote + 1, endQuote - startQuote - 1);
                                ids.Add(id);
                            }
                        }
                    }
                }
                catch {}
            }
            
            ids.Sort();
            return ids;
        }

        private bool IsValidSpecies(string id)
        {
            // Just a check for the Odin Validator
            if (string.IsNullOrEmpty(id)) return false;
            return GetSpeciesIds().Contains(id);
        }
#endif
    }
}

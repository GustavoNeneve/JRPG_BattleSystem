import json
import os

file_path = r"c:\Users\gusta\Downloads\Project\JRPG_BattleSystem\Packages\packages-lock.json"

try:
    with open(file_path, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    # Writing it back will eliminate duplicates (keeping the last one loaded)
    with open(file_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        
    print(f"Successfully cleaned {file_path}")
except Exception as e:
    print(f"Error: {e}")

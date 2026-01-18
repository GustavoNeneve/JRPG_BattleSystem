using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SpriteSlicerTool : EditorWindow
{
    [MenuItem("Tools/Fatiar Sprites de Personagens")]
    public static void SliceSprites()
    {
        string targetFolder = "Assets/novo projeto/graphics/characters";
        
        // Verifica se a pasta existe
        if (!Directory.Exists(targetFolder))
        {
            Debug.LogError("Pasta não encontrada: " + targetFolder);
            return;
        }

        // Encontra todas as texturas na pasta
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { targetFolder });

        if (guids.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada para fatiar em: " + targetFolder);
            return;
        }

        int processedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null) continue;

            // Configura para Multiple
            importer.spriteImportMode = SpriteImportMode.Multiple;

            // Obtém dimensões originais da textura
            int width, height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            int sliceWidth = 32;
            int sliceHeight = 32;

            // Calcula colunas e linhas
            int colCount = width / sliceWidth;
            int rowCount = height / sliceHeight;
            int count = 0;

            // Cria os metadados dos sprites (Ordem: Topo-Esquerda -> Direita -> Baixo)
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    
                    // Unity usa sistema de coordenadas com Y crescendo para cima.
                    // Para começar do topo, calculamos Y a partir da altura total.
                    // Linha 0 (Topo): Y = height - 32
                    float y = height - (r + 1) * sliceHeight;
                    float x = c * sliceWidth;

                    meta.rect = new Rect(x, y, sliceWidth, sliceHeight);
                    // Nome do sprite segue o padrão: NomeArquivo_Indice
                    meta.name = Path.GetFileNameWithoutExtension(path) + "_" + count;
                    meta.alignment = (int)SpriteAlignment.Center; // Pivô no centro
                    meta.pivot = new Vector2(0.5f, 0.5f);

                    metas.Add(meta);
                    count++;
                }
            }

            // Aplica os sprites criados
            importer.spritesheet = metas.ToArray();
            
            // Marca como sujo e salva
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            
            processedCount++;
        }

        Debug.Log($"Concluído! {processedCount} texturas foram fatiadas em blocos de 32x32.");
    }
}

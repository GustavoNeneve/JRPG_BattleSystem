using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        int specialCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null) continue;

            string filenameNoExt = Path.GetFileNameWithoutExtension(path);

            // Configura para Multiple
            importer.spriteImportMode = SpriteImportMode.Multiple;

            // Obtém dimensões originais da textura
            int width, height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);

            List<SpriteMetaData> metas = new List<SpriteMetaData>();

            // Lógica para determinar o tamanho do slice
            int sliceWidth, sliceHeight;
            bool isSpecial = IsSpecialCase(filenameNoExt);

            if (isSpecial)
            {
                // Divide em 4 colunas e 4 linhas (grade 4x4)
                sliceWidth = width / 4;
                sliceHeight = height / 4;
                specialCount++;
            }
            else
            {
                // Padrão: 32x32
                sliceWidth = 32;
                sliceHeight = 32;
            }

            // Evita divisão por zero ou tamanhos inválidos
            if (sliceWidth <= 0) sliceWidth = width > 0 ? width : 32;
            if (sliceHeight <= 0) sliceHeight = height > 0 ? height : 32;

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
                    float y = height - (r + 1) * sliceHeight;
                    float x = c * sliceWidth;

                    meta.rect = new Rect(x, y, sliceWidth, sliceHeight);
                    // Nome do sprite segue o padrão: NomeArquivo_Indice
                    meta.name = filenameNoExt + "_" + count;
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

        Debug.Log($"Concluído! {processedCount} texturas processadas. ({specialCount} especiais detectadas)");
    }

    private static bool IsSpecialCase(string filename)
    {
        // Verifica palavras-chave
        if (filename.Contains("_rockclimb") || filename.Contains("_surf"))
            return true;

        // Verifica intervalo z_b149 a z_b212
        if (filename.StartsWith("z_b"))
        {
            // Regex para capturar o número após z_b
            // Padrão esperado: z_b123...
            Match match = Regex.Match(filename, @"z_b(\d+)");
            if (match.Success)
            {
                int number;
                if (int.TryParse(match.Groups[1].Value, out number))
                {
                    if (number >= 149 && number <= 212)
                        return true;
                }
            }
        }

        return false;
    }
}

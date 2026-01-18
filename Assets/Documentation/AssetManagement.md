# Gerenciamento de Assets - Sprites

## Ferramentas de Automação

### Fatiador de Sprites de Personagens (Sprite Slicer)
**Script:** `Assets/Editor/SpriteSlicerTool.cs`
**Menu:** `Tools > Fatiar Sprites de Personagens`

Esta ferramenta automatiza o processo de "Slicing" (recorte) dos spritesheets de personagens.

**Regras de Fatiamento:**

1.  **Regra Especial (4x4):**
    *   Aplica-se a arquivos cujos nomes:
        *   Estejam no intervalo `z_b149` a `z_b212`.
        *   Contenham `_rockclimb` ou `_surf`.
    *   A imagem é dividida em uma grade exata de **4 colunas por 4 linhas**. O tamanho do sprite é calculado dinamicamente (`Largura/4` x `Altura/4`).

2.  **Regra Padrão (32x32):**
    *   Aplica-se a todos os outros arquivos na pasta.
    *   A imagem é dividida em blocos fixos de **32x32 pixels**.

**Funcionalidade Geral:**
- Varre a pasta `Assets/novo projeto/graphics/characters`.
- Altera o `Sprite Mode` para `Multiple`.
- Define o pivô para **Center** (Centro).
- Nomeia os sprites sequencialmente (ex: `Nome_0`, `Nome_1`).

**Como usar:**
1. Na Unity, clique no menu `Tools`.
2. Selecione `Fatiar Sprites de Personagens`.
3. Verifique o Console. Ele informará quantos arquivos foram processados e quantos foram detectados como "Especiais".

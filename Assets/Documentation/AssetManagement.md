# Gerenciamento de Assets - Sprites

## Ferramentas de Automação

### Fatiador de Sprites de Personagens (Sprite Slicer)
**Script:** `Assets/Editor/SpriteSlicerTool.cs`
**Menu:** `Tools > Fatiar Sprites de Personagens`

Esta ferramenta automatiza o processo de "Slicing" (recorte) dos spritesheets de personagens.

**Funcionalidade:**
- Varre a pasta `Assets/novo projeto/graphics/characters`.
- Altera o `Sprite Mode` de todas as texturas para `Multiple`.
- Calcula e aplica recortes em grade (Grid) de **32x32 pixels**.
- Nomeia os sprites sequencialmente (ex: `NomeArquivo_0`, `NomeArquivo_1`, etc.).
- Define o pivô para **Center** (Centro).

**Como usar:**
1. Na Unity, clique no menu `Tools`.
2. Selecione `Fatiar Sprites de Personagens`.
3. Aguarde o processamento (verifique o Console para log de conclusão).

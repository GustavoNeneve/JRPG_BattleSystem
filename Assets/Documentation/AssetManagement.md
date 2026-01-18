# Gerenciamento de Assets - Sprites

## Ferramentas de Automação

### Fatiador de Sprites de Personagens (Python Script)
**Script:** `slice_sprites.py` (na raiz do projeto, pode ser removido após uso)

Este script Python automatiza o processo de "Slicing" (recorte) dos spritesheets de personagens modificando diretamente os arquivos `.meta`.

**Funcionalidade:**
- Varre a pasta `Assets/novo projeto/graphics/characters`.
- Lê as dimensões de cada arquivo PNG.
- Altera o `spriteMode` para 2 (Multiple) nos arquivos `.meta`.
- Substitui a seção `sprites:` com definições de grade de **32x32 pixels**.
- Nomeia os sprites sequencialmente (ex: `NomeArquivo_0`, `NomeArquivo_1`, etc.).

**Motivo do uso de Python:**
Uma tentativa anterior usando `AssetDatabase` via script de Editor apresentou problemas. A edição direta dos metadados via Python provou-se mais robusta para este lote específico de arquivos.

**Como usar:**
1. Tenha o Python instalado no sistema.
2. Execute o script na raiz do projeto: `python slice_sprites.py`
3. A Unity detectará as alterações nos arquivos meta e reimportará os assets automaticamente.

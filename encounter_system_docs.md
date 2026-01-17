# Documentação do Sistema de Encontros Aleatórios e Batalha

Este documento explica como configurar e utilizar o novo Sistema de Encontros e a Integração de Batalha no projeto JRPG.

## 1. Visão Geral
O sistema permite que o jogador encontre batalhas aleatórias enquanto caminha em zonas perigosas (como florestas ou cavernas).
Ao entrar em batalha, o jogo transita para uma cena dedicada (`Main_Offline`), carrega os inimigos corretos, toca a música de batalha e, ao finalizar, retorna o jogador para a posição original no mundo.

---

## 2. Componentes Principais

### A. EncounterData (Scriptable Object)
Define os dados de uma zona de encontro.
- **Zone Name:** Nome da região (ex: "Floresta Sombria").
- **Background Image:** Imagem de fundo que aparecerá na batalha.
- **Battle Music:** Música que tocará durante a batalha.
- **Encounter Chance:** Chance (%) de ocorrer uma batalha a cada passo.
- **Min/Max Enemies:** Quantidade mínima e máxima de inimigos por batalha.
- **Enemy Create List:** Lista de inimigos possíveis e seus pesos (raridade).
  - **Enemy Identifier:** Arraste um objeto que tenha o **mesmo nome** do inimigo que você cadastrou no `dex_prefab`.

### B. EncounterZone (MonoBehaviour)
Script que deve ser colocado em objetos da cena "World" que possuem um **Collider2D (Trigger)**.
- **Zone Data:** Arraste o arquivo `EncounterData` correspondente aqui.
- **Step Distance Threshold:** Distância que o jogador deve percorrer para o jogo rolar os dados de encontro (Padrão: 1.5).
- **Always Trigger For Testing:** Se marcado, a batalha acontece a cada passo (para testes).

### C. EncounterManager (Singleton)
Gerencia a transição entre cenas e a persistência de dados.
- Deve existir na cena inicial (ex: Splash ou StartMenu).
- **Hospital Scene Name:** Nome da cena para onde o jogador vai se perder a batalha (Game Over).
- **Hospital Position:** Coordenadas de respawn em caso de derrota.
- Ele salva automaticamente a posição do jogador e a música do mapa antes de carregar a batalha.

### D. dex_prefab (Banco de Dados de Inimigos)
Centraliza todos os prefabs de inimigos reais do jogo.
- Deve ser um objeto na cena inicial com o script `dex_prefab` anexado.
- **Enemy Prefabs:** Lista onde você deve arrastar **todos** os prefabs de inimigos (Goblins, Orcs, Slimes, etc.).
- O sistema usa o nome do `Enemy Identifier` (do EncounterData) para buscar o prefab real aqui. Isso evita ter que atualizar todos os arquivos de zona se você mudar um prefab.

### E. CombatManager (Gerenciador de Batalha)
Responsável por criar a batalha na cena `Main_Offline`.
- **Enemy Spawn Spots:** Uma lista de objetos `Transform` onde os inimigos serão criados.
  - O script tenta encontrar automaticamente objetos filhos chamados `"Spot 1"`, `"Spot 2"`, `"Spot 3"` dentro de `EnemiesParent`.
  - Se não encontrar, você pode criar Empty GameObjects na cena de batalha, posicioná-los onde quer que os inimigos fiquem, e arrastá-los para essa lista manualmente no Inspector.

---

## 3. Passo a Passo de Configuração

### Passo 1: Configurar a "Dex"
1. Crie um objeto vazio na sua cena principal/inicial e chame-o de `DexSystem`.
2. Adicione o componente `dex_prefab`.
3. Na lista `Enemy Prefabs`, arraste todos os seus prefabs de monstros.

### Passo 2: Criar uma Zona de Encontro (Dados)
1. Na janela Project, clique com botão direito -> `Create` -> `Encounter` -> `EncounterZoneData`.
2. Dê um nome (ex: `ForestZone`).
3. Configure o nome, imagem de fundo e música.
4. Em `Enemy List`, adicione os inimigos. No campo `Enemy Identifier`, você pode colocar o próprio prefab do inimigo (apenas para pegar o nome) ou qualquer objeto com o mesmo nome.

### Passo 3: Criar a Zona no Mundo 
1. Na cena do mapa ("World"), crie um objeto vazio (ex: `Trigger_Forest`).
2. Adicione um `BoxCollider2D` e marque **Is Trigger**.
3. Adicione o script `EncounterZone`.
4. Arraste o `EncounterData` criado no passo 2 para o campo `Zone Data`.

### Passo 4: Configurar a Cena de Batalha
1. Abra a cena `Main_Offline`.
2. No objeto `CombatManager`, verifique se o campo `Enemy Spawn Spots` está preenchido ou vazio.
   - **Opção A (Automática):** Certifique-se de que dentro de `EnemiesParent` existam objetos chamados exatamente `Spot 1`, `Spot 2`, `Spot 3`.
   - **Opção B (Manual):** Crie objetos onde quiser na cena, e arraste-os para a lista `Enemy Spawn Spots` no `CombatManager`.

---

## 4. Solução de Problemas Comuns

**Q: Os inimigos não aparecem nos spots certos!**
R: Verifique se o `CombatManager` encontrou os spots. Se a lista `Enemy Spawn Spots` estiver vazia no Inspector, o script tentou achar `Spot 1`, `Spot 2`... dentro de `EnemiesParent`. Se seus spots tiverem outros nomes ou estiverem em outro lugar, arraste-os manualmente para a lista.

**Q: O jogo trava ou dá erro ao iniciar a batalha.**
R: Verifique se o nome do objeto que você colocou no `EncounterData` (Enemy Identifier) existe exatamente igual na lista do `dex_prefab`. O sistema busca pelo **nome**.

**Q: O jogador volta da batalha mas não consegue andar.**
R: O `EncounterManager` tentou reativar o jogador (`SetActive(true)`). Verifique se o jogador tem a tag `Player` correta e se não há outro script bloqueando o input.

**Q: A música não volta ao normal depois da batalha.**
R: O `EncounterManager` precisa de um `AudioController` na cena (World) para salvar e restaurar a música. Verifique se ele existe.

# Documentação do Sistema de Batalha e NPC

## Visão Geral
Este documento descreve a implementação do sistema de NPC Treinador e a integração com dados JSON externos, conforme implementado na atualização recente.

## Estrutura de Dados
Os dados do jogo (Pokémon, Moves, Treinadores) são carregados de arquivos JSON localizados em `Assets/novo projeto/Data/Studio`.

### Principais Classes de Dados (`NewBark.Data`)
- **MoveData**: Define propriedades de ataques (Power, Accuracy, Type, Category, PP).
- **SpecieData**: Define dados base de um Pokémon (Base Stats, Tipos, MoveSet por nível).
- **TrainerData**: Define a equipe (Party) de um treinador e seu nível de inteligência artificial.

## Runtime (Execução)
- **`GameDatabase`**: Gerenciador Singleton responsible por carregar os JSONs na inicialização e fornecer acesso rápido via Dicionários. Também gerencia o carregamento assíncrono de Áudio.
- **`PokemonInstance`**: Classe C# (não MonoBehaviour) que representa um Pokémon individual. Calcula stats reais baseados em Level, Natures, IVs e EVs.

## NPCs e Interação
- **`NPCController`**:
    - Script adicionado aos NPCs no mundo.
    - Utiliza **Raycast** para detectar o jogador.
    - Ao detectar, move-se até o jogador e inicia a batalha chamando `EncounterManager.StartTrainerBattle`.

## Fluxo de Batalha
1. **Início**: `EncounterManager` recebe os dados do Treinador e cria instances (`PokemonInstance`) para cada membro da party inimiga.
2. **Setup**: Na cena de batalha (`CombatManager`), os inimigos são instanciados e recebem seus dados via método `EnemyBehaviour.Setup()`.
3. **IA (`BattleAIController`)**:
    - Cada inimigo possui um componente de IA que decide o melhor movimento.
    - **Níveis de Dificuldade**:
        - *Easy/Beginner*: Escolha aleatória.
        - *Medium*: Escolha baseada em dano simples.
        - *Hard/Inferno*: Considera eficácia de tipo (Super Effective) e Status (futuro).
4. **Áudio**:
    - Quando um inimigo ataca, o sistema tenta carregar e tocar o arquivo de áudio correspondente ao nome do movimento (ex: `tackle.wav` em `audio/se/moves`).

## Como Adicionar Novos Treinadores
1. Crie um arquivo JSON em `Data/Studio/trainers/`.
2. Defina o `party` com os IDs dos Pokémon (ex: `charmander`).
3. Adicione um GameObject no mundo com `NPCController` e configure o `Trainer ID` para corresponder ao JSON.

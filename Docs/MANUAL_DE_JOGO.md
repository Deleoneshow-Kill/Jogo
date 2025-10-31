# Manual do Jogo

## Objetivo Geral
Monte um esquadrão de Cavaleiros e vença batalhas táticas por turno. Administre a energia da equipe, escolha habilidades no melhor momento e utilize o modo automático quando desejar assistir à luta.

## Menu e Fluxo Inicial
- **Cena inicial:** `Assets/Scenes/Main.unity`.
- **Teclas rápidas globais:**
  - `F2` abre a seleção de time (troca integrantes rapidamente).
  - `F3` mostra a roleta de Gacha para desbloquear mais heróis.
  - `F4` abre a Arena PvP local com fluxo de pick/ban.
  - `F5` abre o Replay (grava e reproduz batalhas recentes).
- Utilize a UI lateral direita (Team Select) para montar o time do jogador e do inimigo e então pressione **Start** para carregar a batalha.

## Interface de Batalha (HUD)
- **Barra superior:**
  - `Round` indica o turno atual do combate.
  - `Energia Jogador / Inimigo` (agora posicionadas no topo) mostram o estoque atual e o limite máximo.
  - Anéis radiais exibem visualmente a energia de cada lado.
- **Fila de turnos:** ícones logo abaixo indicam a ordem dos próximos 8 turnos (verde = jogador, azul = inimigo).
- **Painel lateral esquerdo:**
  - Botões `1`, `2`, `3` acionam habilidades (básica, especial, ultimate).
  - `Auto` alterna o modo automático da batalha.
  - `Velocidade` alterna entre 1.0×, 1.5× e 2.0×.
  - Informações sobre o cavaleiro ativo aparecem logo abaixo (vida, escudo, cosmos equipados e sinergias ativas).
- **Tooltip inferior:** ao pairar sobre qualquer habilidade, o texto descreve efeitos e custos.

## Energia e Habilidades
- Cada rodada aumenta a energia base disponível (até o máximo mostrado na HUD).
- O custo (`⛁`) exige energia suficiente para lançar habilidades. A energia é compartilhada entre os membros do mesmo time.
- Habilidades básicas normalmente custam 0 ou 1 ponto e são seguras para manter o fluxo de dano.
- Especiais/ultimates consomem mais energia, mas causam efeitos mais fortes.
- Se não houver energia suficiente, a habilidade fica bloqueada até a recarga.

## Sistema de Turnos
1. A ordem inicial é definida pelo atributo **SPD** de cada cavaleiro.
2. A cada turno você pode:
   - Escolher uma habilidade (`1/2/3`).
   - Trocar de alvo (`Tab`).
   - Passar o turno (`Espaço`).
3. Após agir, o personagem entra em recarga (cooldown) e volta para a fila conforme a velocidade efetiva.
4. Estados como **Stun**, **Bleed** ou **Shield** são aplicados conforme as habilidades e duram a quantidade de rodadas indicada.

## Modo Automático
- Ative o botão **Auto** para deixar a IA controlar o time do jogador; o time inimigo já usa IA continuamente, então toda a partida flui sozinha.
- O jogo aguarda ~0,35s, escolhe uma habilidade válida (priorizando a de maior custo disponível) e executa o turno.
- Se nenhuma habilidade estiver disponível, o sistema passa o turno automaticamente.
- Desative o Auto para retomar o controle manual instantaneamente para o seu time (os inimigos continuam automáticos).
- A velocidade definida (1×, 1.5×, 2×) permanece válida mesmo no modo automático.

## Dicas de Estratégia
- Sinergias entre cavaleiros liberam bônus extras (visualize no painel à direita do HUD).
- Use habilidades de suporte antes das ultimates para maximizar dano.
- Controle o alvo inimigo com `Tab` para focar unidades frágeis ou aplicar debuffs importantes.
- Mantenha a energia acima do baseline para ter opções quando surgir a ultimate.
- Experimente o modo automático para assistir combos e testar composições sem microgerenciar cada turno.

## Problemas Comuns
- **Energia zerada:** use mais ataques básicos para recarregar ao baseline da rodada seguinte.
- **Habilidades apagadas:** verifique se o cavaleiro está atordoado ou se o cooldown ainda não terminou.
- **Modo automático parado:** confirme se há energia/habilidades utilizáveis; a IA passa o turno quando nenhuma opção está liberada.

Bom jogo! Ajuste os times, teste sinergias e domine o Coliseu dos Cavaleiros.

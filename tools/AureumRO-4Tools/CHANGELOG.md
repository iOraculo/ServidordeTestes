# Changelogs

## [v2.39.5] - 2026-11-25 — Correção de Falsa Pausa em Campos, Scanner de Mapa Ampliado (.rsw/.gat + Heap) e Diagnóstico Completo no `Ver Status (IDs)`
### Fixed
- **Fim da falsa pausa em campos no `Pausar em cidades` (`Model/Client.cs`)**: candidatos estáticos de mapa não confirmados não são mais tratados como cidade ativa (evitando pausar automações em campos como `prt_fild08` quando o endereço ainda não foi confirmado).
- **Scanner de mapa ampliado (`Model/Client.cs`)**: filtragem de tabelas Lua/estáticas agora roda **por região** (impedindo que listas de mapas como `mp3nametable`/`fogparametertable` esgotem o limite de varredura), suporte a `.rsw` e `.gat` em todo o espaço gravável (estático + heap, com teto de tempo seguro) e aceite de bytes binários de struct C++ antes do nome do mapa.
- **Diagnóstico completo de mapa no `Ver Status (IDs)` (`Model/Client.cs`)**: exibe sempre `HP`, `Nome`, `Status`, `MapaAddr` com origem (`config` / `auto` / `pendente`), valor bruto `Raw`, candidatos `.rsw`/`.gat` isolados ao vivo e nomes de mapa sem extensão próximos de `HP` (`±0x80000`).

## [v2.39.4] - 2026-11-24 — Prioridade Oficial do AureumRO sobre `supported_servers.json` Antigo, Scanner de Mapa Aprimorado e `EFST_RIDING (27)`
### Fixed
- **`supported_servers.json` antigo na pasta não sobrescreve mais o AureumRO (`Forms/ClientUpdaterForm.cs`)**: alterada a ordem de carregamento para priorizar a configuração oficial remota/embutida do AureumRO, evitando que arquivos `supported_servers.json` antigos com `mapAddress` incorreto (ex.: `0x00D2A3BC` preso em `prontera`) travem o `Pausar em cidades`.
- **Detecção automática de mapa aprimorada (`Model/Client.cs`)**: ampliada a varredura em páginas graváveis (`0x00400000..hp+0x400000`), suporte a fronteiras binárias de struct C++ antes do nome `.rsw`, filtragem de tabelas estáticas/Lua com múltiplos mapas agrupados (`FilterClusteredMapTableCandidates`), travamento somente quando o candidato atualiza ao trocar de mapa e exibição dos candidatos `.rsw` no **`Ver Status (IDs)`**.
- **Identificação do Peco Peco no inspetor de status (`Utils/EffectStatusIDs.cs`)**: adicionado `EFST_RIDING = 27` para exibir o nome correto em vez de `(desconhecido)`.

## [v2.39.3] - 2026-11-24 — Correções de Autopot com Chat, Hold-to-Pause sem Interferência do Alt, Anti-Spam de Gospel, Montaria Automática ao Iniciar e Detecção de Mapa para Pausar em Cidades
### Fixed
- **Autopot & Auto-Fuga com Chat Aberto (`Model/Autopot.cs`, `Model/AutoEscape.cs`, `Utils/ChatPauseState.cs`, `Utils/SessionContext.cs`)**: quando o modo `Chat` está ativo e o HP/SP cai abaixo da % configurada usando teclas alfanuméricas (ex.: `Q`, `1`, `E`), o AureumTools agora fecha automaticamente o chat (`Enter` via transição atômica `TryConsumeChatting()`) e dispara a poção/fuga em seguida (enquanto teclas `F1–F24` continuam potando sem fechar o chat).
- **Tecla `Alt` interferindo no `Segurar pausa` (`Model/Autopot.cs`, `Model/Autobuff.cs`, `Model/AHK.cs`, `Model/ATKDEFMode.cs`, `Model/Macro.cs`, `Model/AutoMount.cs`, `Model/AutoEscape.cs`, `Model/AutoRefreshSpammer.cs`, `Model/AutoRefreshCommand.cs`, `Model/StatusEffectRecoveryBase.cs`)**: removidos os bloqueios fixos de `KeyHelper.IsAltPressed()` / `KeyboardHook.Alt` dos loops de automação — agora apenas a tecla configurada em **`Segurar pausa`** pausa as automações (incluindo nos loops internos do Spammer e ATK x DEF), e `Alt` só pausa se o próprio usuário escolhê-lo.
- **Skill `Canto de Batalha / Gospel` (`EFST_GOSPEL = 109`) spamando sem parar (`Model/Autobuff.cs`)**: adicionada a mesma proteção anti-spam da Basílica (`GOSPEL_NO_ICON_RECAST_MS = 60000ms` quando o servidor Pré-Renewal não emite ícone EFST para o canalizador, reduzindo para `1200ms` caso emita), além de priorizar Gospel por último para não travar outros buffs.
- **`Ativar Montaria` não montava ao iniciar (`Model/AutoMount.cs`, `Forms/MountForm.cs`)**: ao marcar `Ativar Montaria` com uma tecla válida e ligar o AureumTools (ou ativar a opção já ligado), o personagem agora monta automaticamente na Rédea se estiver desmontado (verificando `EFST_ALL_RIDING` para nunca desmontar quem já está montado, respeitando sentado/recarga e funcionando também em cidades).
- **`Pausar em cidades` sem detecção de mapa no AureumRO (`Model/Client.cs`, `Utils/ProcessMemoryReader.cs`, `Forms/Container.cs`)**: implementada descoberta automática segura do endereço de mapa atual (`.rsw`) em páginas graváveis (`PAGE_READWRITE`) da memória do cliente quando `mapAddress` não está fixado, com travamento dinâmico ao trocar de mapa, atualização em tempo real do status `Pausado (Cidade)` na barra lateral e exibição do mapa + endereço detectado no botão `Ver Status (IDs)`.

## [v2.39.2] - 2026-11-24 — Correção de Proporção em Botões (`25px`), `Perfis` Sem Sobreposição e `AutoScaleMode.None` em Todos os Formulários
### Fixed
- **Sobreposição em `Perfis, Backup e Diagnóstico` (`Forms/ProfileForm.cs`, `Forms/*.Designer.cs`)**: desativado `AutoScaleMode.Font` em todos os 12 formulários embutidos (`.Designer.cs`) para eliminar a divergência de escala entre controles do Designer e controles criados em código em telas com escala do Windows >100%, e redesenhado o layout de `ProfileForm` aproveitando toda a largura (`616×289`) sem nenhuma sobreposição.
- **Texto cortado na parte inferior de botões (`Utils/FontManager.cs`, `Utils/ThemeManager.cs`, `Forms/SkillAutoBuffForm.cs`, `Forms/StuffAutoBuffForm.cs`, `Forms/MacroSwitchForm.cs`, `Forms/MacroSongForm.cs`, `Forms/ATKDEFForm.cs`, `Forms/MacrosHubForm.cs`, `Forms/SkillTimerForm.cs`, `Forms/MountForm.cs`, `Forms/UtilitiesForm.cs`, `Forms/GuideForm.cs`)**:
  - Aumentada a altura dos botões de ação (`Limpar`, `Limpar Skills`, `Limpar Itens`, `+ Adicionar campo`, `Limpar DEF/ATK`, etc.) para **`25px`–`27px`** com proteção global no `FontManager` (`8.5pt` em botões ≤24px, `UseCompatibleTextRendering = false` no `ThemeManager`), evitando qualquer corte de letras com haste inferior (`p`, `g`, `q`, `y`).
  - Cabeçalhos de `Auto-skill` e `Consumíveis` espaçados na largura total (`592px`) com folga limpa entre `Classe:`, `Filtrar:`, `SP%:`, `CD:` e o botão de limpar.

## [v2.39.1] - 2026-11-24 — Alinhamento Determinístico da Grade de Teclas do `Spammer` no Layout Ampliado
### Fixed
- **Aba `Spammer` (`Forms/AHKForm.cs`)**: posicionamento determinístico das 9 colunas de teclas (`F1..F9`, `1..9`, `Q..O`, `A..L`, `Z..M`) e expansão dos blocos `Legenda`, `Limpar Teclas`, `Configuração do Spammer` e `Prioridade de Ações` para aproveitar a nova largura (`616px`), eliminando qualquer encosto da coluna `F9 / 9 / O / L` na caixa `Legenda` com a fonte `Google Sans` ampliada.
- **Subtítulo da aba `Guia / Ajuda` (`Utils/LocalizationManager.cs`)**: ajustado para caber completo em uma única linha sem reticências.

## [v2.39.0] - 2026-11-24 — Cores Clássicas **Obsidiana & Ouro Real** Restauradas, Layout Ampliado com Fonte `Google Sans` Maior e Rodapé `Desenvolvido por Oráculo`
### Changed
- **Paleta Clássica **Obsidiana & Ouro Real** Restaurada (`Utils/ThemeManager.cs`, `Utils/ModernControls.cs`, `Utils/VitalBar.cs`, `Forms/MultiAccountShellForm.cs`, `Forms/GuideForm.cs`, `Forms/AureumHubForm.cs`, `Forms/MiniHudForm.cs`)**:
  - Mantido o visual moderno estilo **Antigravity Settings** (cards arredondados `10px`, pílulas suaves, barras vitais arredondadas e fonte **Google Sans** embutida), restaurando as cores quentes oficiais do AureumRO tanto no **Modo Escuro** (`#0B0701` / `#120C03` / `#1E1506` / `#594214` / `#F4D97A`) quanto no **Modo Claro** (`#F2E8CC` / `#FAF4E0` / `#FDF8E8` / `#A08123`).
- **Layout Ampliado & Tipografia `Google Sans` Maior (`Forms/Container.cs`, `Forms/MultiAccountShellForm.cs`, `Forms/AutopotForm.cs`, `Forms/ToggleApplicationStateForm.cs`, `Forms/GuideForm.cs`, `Utils/FontManager.cs`)**:
  - Janela principal ampliada de `824×644` para **`868×676`** (shell `868×716`) com cards mais espaçosos (`Autopot` `346×198`, `Status` `276×198`, `Seção` `632×352`) e fonte **Google Sans** maior e ainda mais legível em todas as abas (`+0.5pt` automático nos controles internos, títulos até `12pt`).
- **Crédito no Rodapé Restaurado (`Utils/LocalizationManager.cs`)**:
  - Topo sob o logo permanece limpo (`v2.39.0` + seletor `PT-BR`), enquanto o rodapé inferior volta a exibir **`✦ AureumTools · Desenvolvido por Oráculo  •  Minimize para enviar à bandeja`**.

## [v2.38.3] - 2026-11-24 — Espaçamento Fino das Teclas do `Spammer` com `Google Sans`
### Fixed
- **Aba `Spammer` (`Forms/AHKForm.cs`)**: ajustado o espaçamento vertical das fileiras de teclas (`F1..F9`, `1..9`, `Q..O`, `A..L`, `Z..M`) garantindo folga limpa acima do card `Configuração do Spammer`, além de migrar `priorityOrderFont` para o cache global do `FontManager`.

## [v2.38.2] - 2026-11-24 — Métricas GDI (`OS/2 winAscent/winDescent`) da Fonte `Google Sans` Normalizadas
### Fixed
- **Alinhamento perfeito em Spammer, ComboBoxes, TextBoxes e Guia (`Resources/Fonts/GoogleSans-*.ttf`, `Utils/FontManager.cs`)**: normalizadas as métricas `OS/2 usWinAscent/usWinDescent` das fontes `Google Sans` embutidas para o padrão latino (`1020 / 300`, altura idêntica ao `Segoe UI` no GDI do Windows), eliminando ComboBoxes altos e qualquer sobreposição de checkboxes na aba `Spammer`.

## [v2.38.1] - 2026-11-24 — Correção de Escala de Layout WinForms (`AutoScaleMode.Font`) com Google Sans
### Fixed
- **Layout 100% proporcional com Google Sans (`Utils/FontManager.cs`)**: desativado `AutoScaleMode.Font` nos containers WinForms antes de aplicar a tipografia `Google Sans`, impedindo o esticamento vertical automático que empurrava os cards para baixo, e ajustada a escala em caixas de texto/seletores para alinhamento perfeito.

## [v2.38.0] - 2026-11-24 — Fonte **Google Sans** Embutida, Novo Visual **Antigravity Obsidian + Ouro Real** e 4 Otimizações de Desempenho
### Changed
- **Tipografia Oficial `Google Sans` Embutida (`Utils/FontManager.cs`, `Resources/Fonts/GoogleSans-Regular.ttf`, `Resources/Fonts/GoogleSans-Bold.ttf`)**:
  - Fontes oficiais **Google Sans Regular & Bold** embutidas diretamente no executável via `PrivateFontCollection` + `AddFontMemResourceEx` (funciona em 100% dos controles WinForms sem precisar instalar nenhuma fonte no Windows), com cache global de instâncias `Font` eliminando vazamentos GDI.
- **Novo Visual Estilo Antigravity Settings + Destaque Dourado AureumRO (`Utils/ThemeManager.cs`, `Utils/ModernControls.cs`, `Utils/VitalBar.cs`, `Forms/MultiAccountShellForm.cs`, `Forms/Container.cs`, `Forms/GuideForm.cs`, `Forms/AureumHubForm.cs`, `Forms/MiniHudForm.cs`)**:
  - Paleta **Antigravity Obsidian** (`#09090B` / `#0E0E11` / `#141418` com bordas finas `#282831`, texto branco nítido `#F4F4F6` e secundário `#9A9AA6`) combinada ao destaque **Ouro Real** (`#E8C35A`), além de modo claro limpo (`#F5F5F7` / `#FFFFFF` + Ouro Antigo `#9E7A19`).
  - **Cards com cantos arredondados anti-aliased (`10px`)**, abas em pílula (`SegmentedTabButton`, `SidebarNavButton`, `AccountTabButton`), barras vitais arredondadas (`VitalBar`) e botão Sol/Lua minimalista.

### Performance & Stability
- **Zero alocações nos loops quentes (`Model/AHK.cs`, `Model/Macro.cs`, `Model/Autobuff.cs`)**:
  - `AHK` agora usa snapshot imutável cacheado (reconstruído apenas ao alterar teclas), eliminando `.Values.ToArray()` a cada tick (~200 alocações/s).
  - `Macro` avalia gatilhos sem clonar dicionários/listas quando ocioso (alocando apenas no instante em que uma cadeia dispara).
  - `AutoBuff` reutiliza `HashSet` e `Dictionary` pré-alocados entre ciclos.
- **Cache de 250ms para `HasExited` e `MainWindowHandle` (`Model/Client.cs`)**: reduz em >10× as chamadas de kernel Win32 feitas pelas múltiplas threads em segundo plano.
- **Eliminação de vazamentos de `Font` (`Utils/FontManager.cs`, `Forms/AutopotForm.cs`, `Forms/DebuffRecoveryForm.cs`, `Forms/MultiAccountShellForm.cs`, `Forms/MiniHudForm.cs`)**: todas as fontes compartilhadas são servidas pelo cache central do `FontManager`.

## [v2.37.1] - 2026-11-24 — Botão `Pausar em cidades` Centralizado (Marcado por Padrão)
### Changed
- **Barra superior (`Forms/Container.cs`, `Utils/LocalizationManager.cs`)**:
  - Renomeado `"Usar na cidade?"` para **`"Pausar em cidades"`** (`"Pause in towns"`), agora **marcado por padrão** (marcado = pausa Autopot, Auto-skill, Consumíveis, Temporizadores e Anti-debuffs em cidades; desmarcado = mantém tudo ativo).
  - Centralizado perfeitamente no espaço entre o seletor **`Perfil`** e o botão de tema (**Sol/Lua**), com alinhamento horizontal uniforme em toda a barra superior.

## [v2.37.0] - 2026-11-24 — Rebrand Completo para **AureumTools**, Painel Unificado Autopot/Yggdrasil, Filtro de Classe + Furtividade & Novo Guia Visual
### Changed
- **Rebrand Completo para `AureumTools`** (`Utils/AppConfig.cs`, `Properties/AssemblyInfo.cs`, `4RTools.csproj`, `Utils/LocalizationManager.cs`, `.github/workflows/`, `README.md`):
  - Projeto, repositório GitHub (`iOraculo/AureumTools`), executável (`AureumTools.exe`), pacote de release (`AureumTools-v2.37.0.zip`), janelas, textos e documentação atualizados de `AureumRO 4Tools` para **`AureumTools`**.
- **Barra Superior e Bloco `LIGAR / DESLIGAR` Mais Limpos** (`Forms/Container.cs`, `Forms/ToggleApplicationStateForm.cs`, `Utils/ModernControls.cs`):
  - Removido `· Oráculo` ao lado da versão sob o logo e movido o seletor de idioma (**`PT-BR` / `EN`**) diretamente para esse espaço.
  - Renomeado `"Usar buffs na cidade?"` para **`"Usar na cidade?"`** (`"Use in town?"`).
  - Substituída a caixa `"Modo Escuro"` por um **botão vetorial de Sol/Lua** (`ThemeToggleButton`) no canto superior direito.
  - No card **`LIGAR / DESLIGAR`**, todos os 4 toggles (`Som`, `Foco`, `Chat` e **`Mini HUD`**) foram alinhados à direita, movendo o `Mini HUD` da barra superior para esse espaço.
- **Autopot & Yggdrasil Unificados + Sprites Autênticos** (`Forms/AutopotForm.cs`, `Forms/Container.cs`, `Resources/4RTools/Icons.resx`):
  - Removidas as abas `Autopot | Yggdrasil` — agora ambos aparecem **simultaneamente no mesmo card** do dashboard!
  - Substituídos os ícones de HP/SP do Autopot pelos sprites oficiais de **Poção Branca (`504`)** e **Poção Azul (`505`)** do jogo.
- **Auditoria de Ícones de Consumíveis** (`assets/images/potions/g_med_life_potion.png`):
  - Auditados todos os 53 consumíveis Pré-Renewal — corrigido o sprite incorreto da **Poção Média de Vida (`True Medium Life Potion`)** com o ícone oficial do item `12419`.
- **Novo `Guia / Ajuda` Lúdico e Espaçoso** (`Forms/GuideForm.cs`):
  - Redesenhado completamente em **12 cards modernos** (incluindo o novo card dedicado ao **`Aureum Hub`**) com barra lateral colorida, ícone temático, subtítulo, tópicos separados com títulos destacados e caixa de **Dica rápida** ao final de cada card.
- **Revisão Geral de Tooltips (Pop-ups)** (`Forms/AureumHubForm.cs`, `Forms/Container.cs`, `Forms/AutopotForm.cs`, `Forms/ToggleApplicationStateForm.cs`, `Forms/SkillAutoBuffForm.cs`):
  - Adicionados pop-ups explicativos em todos os controles do **`Aureum Hub`**, barra superior, Autopot/Yggdrasil, Ligar/Desligar e Auto-skill.

### Added
- **Filtro por Classe & 10 Novas Skills Pré-Renewal no `Auto-skill`** (`Forms/SkillAutoBuffForm.cs`, `Model/BuffRenderer.cs`, `Model/Buff.cs`, `Utils/EffectStatusIDs.cs`, `Model/AutoBuff.cs`):
  - Adicionado seletor **`Classe:`** (`Todas as Classes` + cada classe visível no modo atual) trabalhando em conjunto com a busca bilíngue e o Modo Pré-Renewal.
  - Adicionadas 10 novas skills Pré-Renewal com sprites oficiais do jogo e tradução PT-BR / EN-US:
    - **Gatuno / Algoz / Desordeiro:** **Furtividade (`Cloaking`)** e **Túnel de Fuga (`Chase Walk`)**
    - **Noviço / Monge / Mestre:** **Corpo Fechado (`Steel Body`)** e **Revelação (`Ruwach`)**
    - **Mago / Bruxo / Sábio:** **Chama Reveladora (`Sight`)**
    - **Mercador / Criador:** **Proteção Química Total (`Full Chemical Protection`, com auto-clique em si)**
    - **Espiritualista (Soul Linker):** **`Kaahi`**, **`Kaupe`**, **`Kaite`** e **`Kaizel`** (com auto-clique em si)

## [v2.36.2] - 2026-11-23 — Horário Manual da Última Votação e Painel Completo de Itens/Drops na Coleção de Mapas
### Changed
- **Hora da Votação (`Forms/AureumHubForm.cs`, `Utils/AureumProgressStore.cs`)**:
  - Adicionado campo **`Últ. voto (HH:mm)` + botão `Definir`** em cada topsite: basta digitar o horário em que você votou (ex.: `14:30`, inclusive se foi ontem à noite) para o 4Tools calcular automaticamente o tempo restante e o próximo horário liberado!
  - Removido o botão `/navi Loja de Votos` da barra superior.
- **Coleção de Mapas (`Forms/AureumHubForm.cs`)**:
  - Removido o botão desnecessário **`Copiar código`**.
  - Expandido o painel inferior de detalhes para largura total (`572px`) com caixa multilinha rolável (`txtMapSelectedItems`), exibindo **todos os itens necessários, monstros e porcentagens de drop sem cortar nenhum texto**.

## [v2.36.1] - 2026-11-23 — Coleção de Mapas com Orientação por Setas (`← ↑ ↓ →`) e Código Limpo (sem `@warp`)
### Changed
- **Coleção de Mapas no `Aureum Hub` (`Forms/AureumHubForm.cs`, `Model/AureumCatalog.cs`, `Resources/AureumCatalog.json`)**:
  - Removido o prefixo `@warp` da coluna de código (agora exibe o código limpo, ex.: `prt_monk`, `tur_dun01`, `ice_dun03`) e do botão **`Copiar código`**.
  - Adicionada a coluna **`Como chegar`** com setas de orientação direcional (**`← ↑ ↓ →`**, `Barco`, `Trem`, `· 2F/3F`) a partir da cidade de referência (**`Ref.`**) para todos os **311 mapas** (ex.: `prt_monk` → `Ref: Prontera` · `→ ↑ → →`), além do trajeto detalhado passo a passo no rodapé/tooltip!

## [v2.36.0] - 2026-11-23 — Nova Aba `Aureum Hub` (Hora da Votação + Coleção de Cartas + Coleção de Mapas)
### Added
- **Nova aba `Aureum Hub` na barra lateral (`Forms/AureumHubForm.cs`, `Model/AureumCatalog.cs`, `Utils/AureumProgressStore.cs`, `Resources/AureumCatalog.json`)**:
  - **Sub-aba `Hora da Votação`**:
    - Lista os topsites oficiais do AureumRO (**Top Ragnarok BR** e **Top Ragnarok ORG**, 12h cada, +1 ponto cada, com sincronização opcional via `api.aureumro.com/vote/topsites`).
    - Contagem regressiva persistente de 12h ao vivo por topsite, botão **`Votar (+12h)`** (abre `https://aureumro.com/votar/` e inicia o timer), **`Marcar Todos (12h)`**, **`Zerar`**, atalho para copiar `/navi prontera 183/135` (NPC Loja de Votos) e **lembrete automático com som e notificação na bandeja** quando um novo voto ficar disponível.
  - **Sub-aba `Coleção` → `Coleção de Cartas (28 coleções / 140 cartas)`**:
    - Catálogo completo da Wiki separado por **Categoria de Bônus** (`Utilidade, Drop & EXP`, `Dano Físico & Crítico`, `Magia & Conjuração`, `Defesa, PvP & Reflexão`, `HP, SP & Cura`), com busca em tempo real, filtro (`Todos / Pendentes / Completos`), progresso geral (`X/140 cartas · Y/28 coleções`) e atalho `/navi prontera 153/177` (Colecionador de Cartas).
    - **Clique nas cartas já obtidas para marcá-las em tachado (`✓ Carta`)** (com isolamento por coleção para cartas repetidas como Carta Necromante), além de **`Marcar 5 / Desmarcar 5`** por coleção, **`Marcar visíveis / Desmarcar visíveis`** e **`Ocultar obtidas`**.
  - **Sub-aba `Coleção` → `Coleção de Mapas (311 mapas / 27 cidades)`**:
    - Alterna agrupamento **`Por Cidade`** (27 cidades oficiais) ou **`Por Tipo de Bônus`** (`Velocidade de Movimento`, `Crítico`, `Ataque, ASPD & Dano Físico`, `Dano Mágico & Elemental`, `Cast, Pós-Conj. & Recarga`, `Dano de Skills`, `Defesa, Resistências & PvP`, `HP, SP & Cura`, `Atributos, Precisão & Evasão`, `Utilidade, EXP, Drop & Peso`).
    - Exibe nome do mapa, comando **`@warp`** (clique na coluna Warp, duplo clique ou botão **`Copiar @warp`**), descrição exata do bônus, lista de itens de drop (`@mc`) do mapa selecionado e marcação persistente em tachado dos mapas concluídos.

## [v2.35.0] - 2026-11-22 — Modo Multi-Conta 3 em 1 (`Conta 1 | Conta 2 | Conta 3` em 1 Janela)
### Added
- **Janela única Multi-Conta 3 em 1 (`Forms/MultiAccountShellForm.cs`, `Utils/SessionContext.cs`, `Forms/Container.cs`, `Forms/ToggleApplicationStateForm.cs`, `Utils/_4RThread.cs`)**:
  - Agora você controla **3 contas completas e independentes** (`Conta 1`, `Conta 2`, `Conta 3`) dentro de **uma única janela** do AureumRO 4Tools — sem precisar abrir 3 executáveis diferentes nem duplicar hooks de teclado.
  - **Barra superior de abas ao vivo**: cada aba exibe uma bolinha de status (**Verde = ON**, **Laranja = CHAT**, **Cinza = OFF**) e o nome do personagem detectado em tempo real, além de botões rápidos **`Ligar Todas`** e **`Parar Todas`** e um único ícone na bandeja do sistema.
  - **Isolamento total por conta (`SessionContext`)**: cliente (`PID`), perfil, Autopot/Yggdrasil, Auto-skills, Auto-consumíveis, Anti-debuffs, Temporizadores, Auto-Fuga, `@refresh`, Montaria, estado `CHAT` e `Prioridade de Ações` / `Segurar pausa` rodam isolados por aba, inclusive em threads em background (`_4RThread`).
  - **Seleção inteligente de janelas e atalhos**: ao detectar vários clientes abertos, cada aba evita selecionar um `PID` já ocupado por outra aba; se 2+ contas estiverem ligadas juntas, **Autopot / Ygg / Auto-Fuga / Auto-buffs / Anti-debuffs** rodam simultaneamente em segundo plano em todas elas enquanto **Spammers / Macros manuais** respondem apenas na janela do RO em foco. A tecla de ligar/desligar (`End` ou customizada) alterna a janela do RO em foco se compartilhada, ou controla uma conta específica de qualquer lugar se configurada com tecla única.
  - **Tema, Idioma e Buffs em cidade sincronizados** automaticamente entre as 3 abas.

## [v2.34.1] - 2026-11-22 — Correção do Envio de `@refresh` no Ragexe 2022 (`SendInput` Unicode) e Dessincronia do Estado `CHAT`
### Fixed
- **Envio real de `@refresh` no chat do Ragexe 2022 (`Utils/Interop.cs`, `Model/AutoRefreshCommand.cs`, `Forms/UtilitiesForm.cs`)**:
  - **Causa raiz corrigida:** enviar `PostMessage(hWnd, WM_CHAR)` para a janela principal fazia a barra de chat abrir e fechar vazia no Ragexe 2022 (pois o texto vai para o campo `Edit`/IME focado).
  - Migrado para **`SendInput(KEYEVENTF_UNICODE)`** (com struct Win32 `INPUT` exata de 28/40 bytes e fallback `WM_CHAR`), entrega direta ao campo de chat focado independente do layout ABNT2/US; botão **`Testar`** foca a janela do jogo automaticamente em background.
  - Atalho de 1 toque em teclas de função (`F1–F24`) agora funciona **mesmo com a barra de chat já aberta** (digita `@refresh` direto e envia sem fechar a barra antes!).
- **Fim do botão travado em `CHAT` (`Forms/ToggleApplicationStateForm.cs`, `Forms/Container.cs`)**:
  - Detecção de `Enter`/`Esc` migrada para `KeyboardHook.PhysicalKeyDown` (ignora auto-repeat ao segurar `Enter`, teclas injetadas e `Enter` enviado pelo `@refresh`).
  - Auto-retomada de `CHAT` para `ON` quando a janela do jogo perde foco (ex.: Alt+Tab ou clique no 4Tools) e ao clicar diretamente no botão laranja **`CHAT`**.

## [v2.34.0] - 2026-11-22 — Pausa Momentânea (Segurar Tecla / Hold-to-Pause) na aba `Spammer`
### Added
- **Segurar para Pausar (*Hold-to-Pause*) dentro do card `Prioridade de Ações` na aba `Spammer` (`Forms/AHKForm.cs`, `Utils/ActionPriorityCoordinator.cs`, `Model/UserPreferences.cs`, `Model/Client.cs`, `Model/AutoEscape.cs`, `Model/AutoMount.cs`)**:
  - Permite definir qualquer tecla (incluindo **`Ctrl`**, **`Shift`**, **`Alt`**, **`Tab`**, **`CapsLock`** ou teclas comuns) em **`Segurar pausa:`** — enquanto você segura a tecla, o 4Tools pausa instantaneamente via `GetAsyncKeyState` e retoma no milissegundo em que solta (sem risco de travar).
  - Por padrão, **Autopot**, **Yggdrasil** e **Auto-Fuga** continuam protegendo você enquanto segura a tecla; basta marcar **`Pausar Autopot`** ao lado caso queira pausar 100% de tudo.
  - Persistido por perfil, excluído do desmontar automático em `AutoMount` e verificado em `Verificar Conflitos`.

## [v2.33.1] - 2026-11-22 — Correção Definitiva de Travamento ao Apertar `Alt` / `Alt+Tab`
### Fixed
- **Fim do travamento após apertar `Alt` (`Utils/KeyHelper.cs`, `Utils/KeyboardHook.cs`, `Model/AHK.cs`, `Model/ATKDEFMode.cs`, `Model/Macro.cs`, `Forms/Container.cs`)**:
  - **Causa raiz corrigida:** `KeyHelper.IsAltPressed()` e `Keyboard.IsKeyDown()` (WPF / `GetKeyState`) liam o estado de teclado em cache da fila de mensagens — em threads de automação em background (sem *message pump*), após `Alt+Tab`, `Alt+E/Q/M` ou perda de foco, o estado de `Alt` ficava preso em `true` congelando todo o 4Tools (`Autopot`, `Auto-skill`, `Spammer`, `Anti-debuffs`, `Montaria`, `Timers`, `@refresh`, `Auto-Fuga` e até a tecla `End` de ON/OFF).
  - Substituído por consulta em tempo real ao hardware via `GetAsyncKeyState` (`KeyHelper.IsVirtualKeyDown`, `KeyHelper.IsKeyDown`, `KeyHelper.IsAltPressed/IsControlPressed/IsShiftPressed/IsWinPressed`), além de `KeyboardHook.Alt/Control/Shift/Win` sincronizados ao vivo com o hardware, auto-limpeza de teclas presas em `heldPhysicalKeys` e supressão de `SC_KEYMENU` no `Container.WndProc` para um toque solto no `Alt` nunca prender o foco da janela WinForms.

## [v2.33.0] - 2026-11-22 — Auto-Fuga de Emergência (Asa de Mosca / Teleporte) na aba `Utilidades`
### Added
- **Auto-Fuga de Emergência (`Model/AutoEscape.cs`, `Forms/UtilitiesForm.cs`, `Model/Profile.cs`)**:
  - Dispara automaticamente a tecla configurada (**Asa de Mosca**, **Asa de Borboleta** ou **skill Teleporte**) quando o HP cair abaixo da `%` crítica escolhida (padrão `20%`, monitoramento a cada `40ms` em prioridade de emergência Nível 2).
  - **Confirmar com Enter (`skill Teleporte`)**: opcionalmente pressiona `Enter` 80ms após a tecla para confirmar automaticamente o menu de Teleporte (Noviço/Sacerdote/Carta Creamy).
  - **Proteções integradas**: desligado por padrão, recarga configurável (`500ms` a `30000ms`, padrão `2000ms`) para não desperdiçar várias asas, nunca dispara morto (`HP = 0`), opção **`Desativar em cidades`** (ligada por padrão), funciona mesmo com chat aberto se a tecla estiver em `F1–F24`, excluído do desmontar automático em `AutoMount` e checado em `Verificar Conflitos`.

## [v2.32.1] - 2026-11-22 — Nova Aba Dedicada `Utilidades` (Anti-Bug Position / `@refresh`)
### Changed
- **Nova aba `Utilidades` na barra lateral (`Forms/UtilitiesForm.cs`, `Forms/Container.cs`, `Utils/ModernControls.cs`)**:
  - Movido o recurso **Anti-Bug Position (`@refresh`)** da aba `Temporizadores` para sua própria aba dedicada **`Utilidades`** (`Utilities`) com ícone vetorial próprio, layout espaçoso e quadro explicativo **"Como funciona"**.
  - A aba **`Temporizadores`** voltou a ficar 100% limpa e focada nos 6 timers de skills.

## [v2.32.0] - 2026-11-22 — Anti-Bug Position (`@refresh`) com Atalho 1-Toque e Intervalo Automático
### Added
- **Anti-Bug Position (`@refresh`) na aba `Temporizadores` (`Model/AutoRefreshCommand.cs`, `Forms/SkillTimerForm.cs`, `Utils/Interop.cs`, `Model/Profile.cs`)**:
  - Envio direto do comando no chat (`Enter` → `WM_CHAR` independente de layout de teclado → `Enter`) com bloqueio atômico contra corrupção por outras teclas de letra durante o envio (mantendo Autopot/Yggdrasil em `F1–F24` ativos) e proteção para nunca disparar enquanto o jogador estiver digitando no chat.
  - **Atalho de 1 toque (`Hotkey`)**: corrige position bug instantaneamente no meio do combate (WoE/PvP/MVP) com debounce anti-duplo-toque de `1500ms`.
  - **Intervalo automático opcional (`Auto a cada X seg`)**: ressincroniza periodicamente (mín. `3s`, padrão `30s`), pausando automaticamente em cidade, no chat e ao sentar (`Pausar sentado`), além de resetar o contador quando o atalho manual é acionado.
  - **Comando editável** (padrão `@refresh`), botão **`Testar`**, persistência completa por perfil, exclusão do desmontar automático em `AutoMount` e detecção de conflitos em `Verificar Conflitos`.

## [v2.31.2] - 2026-11-22 — Layout Unificado de Prioridade de Ações na aba Spammer
### Changed
- **Cartão dedicado e compacto de `Prioridade de Ações` na aba `Spammer` (`Forms/AHKForm.cs`, `Utils/LocalizationManager.cs`, `Forms/GuideForm.cs`)**:
  - `Configuração do Spammer` compactada (`Compatibilidade/Velocidade Máx.`, `Config. Tecla` lado a lado e `Atraso (Delay)`), com o quadro **`Prioridade de Ações`** logo abaixo agrupando juntos **`Pausa p/ input do jogador: [120] ms`** e a linha de hierarquia **`1º Input do Jogador → 2º Fruto de Yggdrasil / Panaceia → 3º Consumíveis → 4º Auto-skills`** sem espaçamento excessivo entre os elementos.

## [v2.31.1] - 2026-11-22 — Refinamentos de Detecção de Buffer (>100000), Legenda de Prioridade e Basílica por Último
### Fixed & Changed
- **Refinamento da detecção de cabeçalho e filtragem de lixo no buffer de status (`Model/Client.cs`)**:
  - `DetectLeadingHeaderSkip(uint[] statusBuffer, int limit)` agora exige `limit >= 4` e `statusBuffer[1] > 100000` (exceto `IGNORE`/`IGNORE_2`), e valores isolados `> 100000` são ignorados automaticamente em `ExtractActiveStatuses` e `BuildActiveStatusReport`.
- **Nível 2 (Fruto de Yggdrasil / Panaceia) e ajuste visual da legenda (`Utils/ActionPriorityCoordinator.cs`, `Model/StatusEffectRecoveryBase.cs`, `Utils/LocalizationManager.cs`, `Forms/GuideForm.cs`, `Forms/AHKForm.cs`)**:
  - Adicionado `ActionPriorityCoordinator.NotifyAntiDebuffAction()` chamado em `StatusEffectRecoveryBase.SendRecoveryKey`.
  - Atualizada a legenda de prioridade para: `1º Input do Jogador → 2º Fruto de Yggdrasil / Panaceia → 3º Consumíveis → 4º Auto-skills` (e equivalente em EN-US), com fonte `Segoe UI 7.75F Bold` e `lblPriorityOrder` em `(10, 93)` / `(538, 20)` cabendo em 1 linha.
- **Basílica sempre conjurada por último e guarda reduzida para `1200ms` (`Model/Autobuff.cs`)**:
  - `Auto-skill` agora ordena `EFST_BASILICA` sempre por último via `OrderBuffsForCast` e adia sua conjuração se qualquer outro buff foi conjurado no mesmo ciclo.
  - Adicionado `SyncActiveBuffTransitions` (limpando imediatamente `lastCastTimesMs[EFST_BASILICA]` quando `EFST_BASILICA` / `EFST_BASILICA_BUFF` deixa de estar ativo) e ajustado `BASILICA_ICON_CAST_GUARD_MS = 1200`.

## [v2.31.0] - 2026-11-21 — Correção Definitiva do Offset de Status (+0x480 Ragexe 2022), Pós-Conjuração (EFST_POSTDELAY) e Basílica/Angelus/Vigor
### Fixed
- **Offset real do buffer de status no Ragexe 2022 / FroggoClient (`supported_servers.json`, `assets/etc/supported_servers.txt`, `Model/Client.cs`)**:
  - Adicionado `"statusOffset": "0x480"` para o **AureumRO** (corrigindo os 3 slots fixos de cabeçalho em `+0x474`: `[9, ponteiro, 1]`, que mascaravam permanentemente **`ANGELUS = 9`** e **`ENDURE / Vigor = 1`**!).
  - Adicionada auto-detecção `Client.DetectLeadingHeaderSkip` em `ExtractActiveStatuses` e `BuildActiveStatusReport` para pular automaticamente o cabeçalho de 3 slots mesmo em perfis locais antigos ou outros clientes Ragexe 2022+.
- **Aguardo de Pós-Conjuração (`EFST_POSTDELAY = 46`) e renovação imediata de Basílica (`Utils/EffectStatusIDs.cs`, `Model/Autobuff.cs`)**:
  - `Auto-skill` agora aguarda `EFST_POSTDELAY (46)` terminar antes de tentar disparar buffs (sem desperdiçar cooldown durante a pós-conjuração).
  - Ao detectar que um buff expirou (ex.: `EFST_BASILICA = 111` saiu do buffer), o cooldown em cache é limpo imediatamente via `SyncActiveStatusTransitions`, renovando no instante exato em que a pós-conjuração acaba.
  - Frequência de leitura do `Auto-skill` reduzida de `300ms` para **`150ms`** para resposta mais rápida.

## [v2.30.1] - 2026-11-21 — Panaceia / Anti-debuffs elevada ao Nível 2 junto com Yggdrasil
### Changed
- **Prioridade Nível 2 compartilhada entre Yggdrasil e Anti-debuffs / Panaceia (`Model/StatusEffectRecoveryBase.cs`, `Utils/ActionPriorityCoordinator.cs`, `Utils/LocalizationManager.cs`, `Forms/GuideForm.cs`)**:
  - Panaceia e demais curas de **Anti-debuffs** agora operam no **Nível 2** junto com o **Fruto/Semente de Yggdrasil** — cedem passagem apenas para o **1º Input do jogador** e têm prioridade imediata sobre **3º Poções comuns** e **4º Skills e demais**.

## [v2.30.0] - 2026-11-21 — Correção Definitiva de Basílica (Anti-Spam), Leitura de Buffer para Angelus e Melhorias em Anti-Debuffs
### Fixed
- **Basílica de volta ao `Auto-skill` (Modo Pré-Renewal) com proteção anti-spam inteligente (`Model/Buff.cs`, `Model/Autobuff.cs`)**:
  - Basílica reativada no Modo Pré-Renewal reconhecendo múltiplos handles (`EFST_BASILICA = 111` e `EFST_BASILICA_BUFF = 1122`).
  - Adicionada proteção automática de recarga (`25000ms` padrão ou `CD(ms)` se maior quando o servidor não emite ícone EFST, reduzindo automaticamente para `3000ms` de proteção de conjuração caso o servidor emita o ícone) — elimina completamente o spam infinito e o cancelamento em loop!
- **Angelus e fim real do buffer de status (`Model/Client.cs`, `Model/Autobuff.cs`, `Model/StatusEffectRecoveryBase.cs`, `Model/AutoMount.cs`)**:
  - Implementado `Client.ExtractActiveStatuses`, que interrompe a varredura ao detectar 6 slots vazios consecutivos (`IGNORE` / `0xFFFFFFFF`), evitando ler lixo de memória fora do array de buffs do cliente (que frequentemente contém inteiros pequenos como `9` = `ANGELUS` e impedia o Angelus de ativar automaticamente).
  - Atualizado o botão **`Perfis → Ver Status (IDs)`** para separar claramente os IDs ativos no buffer dos valores ignorados após o fim do buffer.
- **Anti-debuffs / Cegueira (`Model/StatusEffectRecoveryBase.cs`)**:
  - Alterado o envio de tecla de cura para `SendKeyPress` (`WM_KEYDOWN + WM_KEYUP`) e mantidos todos os múltiplos handles para Cegueira (`887`, `694`, `1205`), Silêncio (`885`, `693`, `1206`), Maldição (`884`, `692`) e Veneno (`883`, `890`, `1214`).

## [v2.29.3] - 2026-11-21 — Contador de Remontar em Milissegundos (ms)
### Changed & Fixed
- **Remontar em milissegundos (`Model/AutoMount.cs`, `Forms/MountForm.cs`, `Utils/LocalizationManager.cs`)**:
  - Alterado o campo **`Após(s)`** (que tinha mínimo travado em `2s`) para **`Após(ms)`** (`100ms` a `120000ms`, incremento de `100ms`, padrão `1000ms`) com precisão de verificação a cada `50ms`.
  - Adicionado respeito ao cooldown da Rédea no servidor (`EFST_ALL_RIDING_REUSE_LIMIT = 614`) para que o auto-remontar aguarde o fim do cooldown sem desistir caso o delay configurado seja curto.

## [v2.29.2] - 2026-11-21 — Prioridade de Ações movida para a aba Spammer
### Changed
- **Prioridade de Ações alocada na aba `Spammer` (`Forms/AHKForm.cs`, `Forms/MountForm.cs`, `Utils/LocalizationManager.cs`, `Forms/GuideForm.cs`)**:
  - Movido o ajuste de **Prioridade Jogador (ms)** e a legenda visual da hierarquia (**`1º Jogador → 2º Yggdrasil → 3º Poções → 4º Skills e demais`**) para o painel `Configuração do Spammer` na aba **`Spammer`**, lado a lado com `Config. Tecla` e `Atraso (Delay)`.
  - A aba **`Montaria`** ficou limpa e dedicada exclusivamente aos controles de montaria (Rédea) e teclas de habilidade.

## [v2.29.1] - 2026-11-21 — Correções em Angelus, Basílica, Cegueira e Inspetor de Status (EFST IDs)
### Fixed & Added
- **Angelus (`Model/Autobuff.cs`, `Utils/EffectStatusIDs.cs`)**: adicionado suporte ao alias `EFST_HEALTHSTATE_ANGELUS (888)` junto ao `EFST_ANGELUS (9)`, envio completo `WM_KEYDOWN + WM_KEYUP` (`SendKeyPress`) no `Auto-skill` e espaçamento de `50ms` entre buffs consecutivos para evitar colisão de teclas.
- **Basílica (`Model/Buff.cs`, `Model/Autobuff.cs`, `Utils/EffectStatusIDs.cs`)**: ocultada no **Modo Pré-Renewal** (pois `SC_BASILICA` no Pré-Renewal cria célula no chão sem enviar ícone EFST ao cliente, o que causava spam infinito; para Basílica periódica no Pré-Renewal, utilize `Temporizadores`) e adicionado suporte a `EFST_BASILICA_BUFF (1122)` para servidores Renewal / customizados com ícone.
- **Cegueira e Panaceia (`Model/StatusEffectRecoveryBase.cs`, `Model/StatusRecovery.cs`, `Model/DebuffsRecovery.cs`)**: unificada a resolução de aliases entre **Panaceia (tecla única)** e **teclas individuais**, cobrindo `EFST_HEALTHSTATE_BLIND (887)`, `EFST_GVG_BLIND (694)`, `EFST_HANDICAPSTATE_DEEPBLIND (1205)`, além de variantes GvG/Handicap de Maldição, Silêncio e Veneno.
- **Botão `Ver Status (IDs)` (`Forms/ProfileForm.cs`, `Model/Client.cs`)**: novo botão de diagnóstico na aba **Perfis** que lista ao vivo os IDs EFST presentes na memória do `AureumRO.exe` (com botões Atualizar e Copiar) para validar instantaneamente qualquer buff ou debuff do servidor.

## [v2.29.0] - 2026-11-21 — Aba Montaria (Descer ao Usar Habilidade), Prioridade 4 Níveis e Ajuste "Auto-skill"
### Added
- **Nova aba `Montaria` (`Model/AutoMount.cs`, `Forms/MountForm.cs`, `Forms/Container.cs`, `Utils/EffectStatusIDs.cs`)**:
  - Detecta montaria de Rédea / Halter Lead (`EFST_ALL_RIDING = 613`) sem interferir no Peco-Peco (`EFST_RIDING = 27`).
  - **Descer da montaria ao usar habilidade**: ao pressionar manualmente uma tecla de skill ou quando Spammer, Auto-skill, Temporizadores, ATK x DEF ou Macro Músicas disparam uma habilidade estando montado, o 4Tools aperta a tecla da montaria automaticamente (com proteção anti-duplo-toque de 600ms e delay configurável) e dispara a habilidade em seguida.
  - Inclui **Pausar Auto-skill montado**, **Remontar após inatividade (opcional)**, detecção automática das teclas das outras abas e 6 slots de teclas extras manuais.
- **Motor de Prioridade em 4 Níveis (`Utils/ActionPriorityCoordinator.cs`, `Utils/KeyboardHook.cs`, `Model/Autopot.cs`, `Model/Autobuff.cs`, `Model/StatusEffectRecoveryBase.cs`, `Model/AutoRefreshSpammer.cs`, `Model/AHK.cs`, `Model/ATKDEFMode.cs`, `Model/Macro.cs`)**:
  - **1º Input do jogador**: qualquer tecla física nova pressionada no jogo pausa o 4Tools por alguns milissegundos (padrão `120ms`, configurável na aba `Montaria`, sem bloquear ao segurar teclas).
  - **2º Yggdrasil**: tem prioridade absoluta sobre poções comuns e skills sempre que o HP/SP cair abaixo do limite de emergência ou logo após usar Yggdrasil.
  - **3º Poções comuns**: têm prioridade sobre skills e demais automações.
  - **4º Skills e demais**: Auto-skill, consumíveis, anti-debuffs, temporizadores e spammers cedem passagem para ações de prioridade maior.
### Changed
- **Renomeada a guia `Auto-Skills` → `Auto-skill`** na barra lateral, cabeçalho, tooltips e Guia / Ajuda.

## [v2.28.0] - 2026-11-21 — Auto-Skills Self-Target Auto-Click, Hiding, Mirror Image (Pre-Skill), Suffragium & Basilica
### Added
- **New Pre-Renewal Class Buffs in `Auto-Skills` (`Model/Buff.cs`, `Utils/EffectStatusIDs.cs`, `Resources/4RTools/Icons.resx`, `Utils/LocalizationManager.cs`)**:
  - **Thief (`Gatuno / Evoluções`)**: **Esconderijo (`Hiding` — `EFST_HIDING = 4`)**.
  - **Ninja**: **Imagem Falsa (`Mirror Image` — `EFST_NJ_BUNSINJYUTSU = 207`)** with automatic prerequisite handling in `Model/Autobuff.cs` (activates **Aura Ninja** first and waits until `AURA_NINJA` is active before casting *Mirror Image*).
  - **Acolyte / Priest / High Priest (`Noviço / Evoluções`)**: **Bênção (`BLESSING`)**, **Aumentar Agilidade (`INC_AGI`)**, **Assumptio (`ASSUMPTIO`)**, **Basílica (`EFST_BASILICA = 111`)**, **Kyrie Eleison (`EFST_KYRIE = 19`)**, **Sufrágio (`EFST_SUFFRAGIUM = 16`)**, and **Aspersio (`ASPERSIO = 17`)**.
- **Automatic Self-Target Click for Target Buffs (`Model/Autobuff.cs`, `Utils/Interop.cs`, `Forms/SkillAutoBuffForm.cs`)**:
  - Added **`Auto-clique em si (alvo)`** (enabled by default): when `Auto-Skills` recasts target-based buffs (*Bênção*, *Aumentar Agilidade*, *Assumptio*, *Kyrie Eleison*, *Sufrágio*, *Impositio Manus*, *Aspersio*), 4Tools automatically clicks the center of the RO window (your character) via `PostMessage` without stealing your mouse cursor.

## [v2.27.0] - 2026-11-21 — Cleaner Sidebar Navigation Names
### Changed
- **Renamed sidebar navigation items (`Forms/Container.cs`, `Utils/LocalizationManager.cs`, `Forms/GuideForm.cs`)** for cleaner, more natural PT-BR labels while keeping **Auto-Skills** and **Consumíveis** as separate direct tabs:
  - `Spam de Skills` → **`Spammer`**
  - `Debuffs` → **`Anti-debuffs`**
  - `Autobuff - Skills` → **`Auto-Skills`**
  - `Autobuff - Itens` → **`Consumíveis`**
  - `Timers de Skill` → **`Temporizadores`**
  - `Macros` → **`Macros`**

## [v2.26.0] - 2026-11-21 — Complete Built-In Guide & Hover Popups Across Every Feature
### Added
- **Built-In Searchable `Guia / Ajuda` (`Guide / Help`) Tab (`Forms/GuideForm.cs`, `Forms/Container.cs`, `Utils/ModernControls.cs`)**:
  - Added a dedicated **`Guia / Ajuda`** item in the sidebar with a vector help-circle icon (`SidebarIconKind.Guide`).
  - Covers all 9 areas of **AureumRO 4Tools** in clear, practical PT-BR and EN with live accent-insensitive keyword search (*Primeiros Passos e Barra Superior*, *Autopot e Yggdrasil*, *Spam de Skills*, *Autobuff — Skills*, *Autobuff — Itens*, *Recuperação de Debuffs*, *Timers de Skill*, *Macros: Troca, Músicas e ATK x DEF*, *Perfis, Backup e Conflitos*).
- **Complete Hover Tooltips (Popups) Everywhere (`Utils/FormUtils.cs`, `Utils/LocalizationManager.cs`, all Forms)**:
  - Standardized every tooltip with `FormUtils.CreateHelpToolTip` (`AutoPopDelay = 20s`, `InitialDelay = 250ms`, `ShowAlways = true`) so popups stay readable even while hovering from the game.
  - Added detailed bilingual hover popups to **every single control across all 13 forms**: Top Bar & Sidebar nav buttons, Autopot & Yggdrasil, ON/OFF + Global Key + Audio/Focus/Chat, Skill Spammer (3-state checkboxes, Compatibility vs Speed Boost, No Shift, Mouse Flick, Delay), Autobuff Skills & Items, Debuff Recovery, Skill Timers, Macros Hub tabs, Macro Troca, Macro Músicas, Macro ATK x DEF (renamed trigger label to **`Skill (Gatilho):`** for clarity), and Profiles / Backup / Character Auto-Switch.

## [v2.25.0] - 2026-11-20 — Clean Debuff Recovery (Item & Skill Curable Only)
### Changed & Removed
- **Kept strictly the 7 self-curable Pre-Renewal debuffs in `Recuperação de Debuffs` (`Model/Buff.cs`, `Forms/DebuffRecoveryForm.cs`, `Utils/LocalizationManager.cs`)**:
  - Removed all Soul-inflicted, crowd-control, and non-curable statuses (*Hipotermia*, *Queimadura*, *Sangramento*, *Ferimento Crítico*, *Atordoamento*, *Congelamento*, *Petrificação*, *Sono*, *Prisão da Teia*) that cannot be cured via consumable or self-cast skill on AureumRO.
  - Fits cleanly on a single screen with zero scrolling in 2 clear cards:
    1. **Panaceia / Geleia Real (Cura por Consumível)** (`6`): *Envenenamento*, *Silêncio*, *Cegueira*, *Maldição*, *Confusão / Caos*, *Alucinação* + **`Tecla única (todos os 6)`** shortcut.
    2. **Aumentar Agilidade / Velocidade** (`1`): *Diminuir Agilidade* (*Aumentar AGI / Pergaminho / Guyak / Guaraná*).

## [v2.24.1] - 2026-11-20 — Remove Cursed Circle & Auto Stand Card + Fix GroupBox Titles
### Removed & Fixed
- **Removed `Campo Amaldiçoado` (`Cursed Circle`)** from `Model/Buff.cs` (`16` total AureumRO debuffs across the 3 cure categories: `6` Panaceia / Geleia Real, `5` Efeitos Especiais e Almas, `5` Controle e Imobilização).
- **Removed `Postura & Utilidade` / `Levantar automaticamente`** card from `Forms/DebuffRecoveryForm.cs` (and updated the section subtitle in `Utils/LocalizationManager.cs`), bringing **Panaceia / Geleia Real** straight to the top of the tab.
- **Fixed WinForms `&` mnemonic swallowing** in GroupBox titles (`Efeitos Especiais e Almas`, `Controle e Imobilização`).

## [v2.24.0] - 2026-11-20 — Debuffs Categorized by Cure Method & Elvira Candy Removed
### Changed & Removed
- **Removed `Doce de Elvira` (`Elvira Candy`) (`Forms/DebuffRecoveryForm.cs`)**: completely removed the Elvira Candy card from the Debuff Recovery tab since the item does not exist on AureumRO (while keeping JSON schema compatibility with older profiles).
- **Merged `Recuperação de Status` cleanly into the new layout (`Forms/DebuffRecoveryForm.cs`)**:
  - Moved **`Levantar automaticamente (quando forçado a sentar)`** into a sleek 44px **`Postura & Utilidade`** bar at the top.
  - Integrated the 1-key **`Panaceia / Geleia Real`** shortcut (`Tecla única (todos os 6):`) directly into the header of the **Panaceia / Geleia Real** card so players can bind all 6 curable statuses with a single key OR configure per-status keys (e.g., *Poção Verde*, *Água Benta*) right below it.
- **Categorized all 17 AureumRO Debuffs by Cure / Counter Method (`Model/Buff.cs`, `Forms/DebuffRecoveryForm.cs`, `Utils/LocalizationManager.cs`)**:
  1. **Panaceia / Geleia Real (Cura por Consumível)** (6): *Veneno*, *Silêncio*, *Cegueira*, *Maldição*, *Confusão / Caos*, *Alucinação*.
  2. **Efeitos Especiais & Almas (Sem cura por Panaceia)** (5): *Diminuir Agilidade*, *Hipotermia*, *Queimadura*, *Sangramento*, *Ferimento Crítico*.
  3. **Controle & Imobilização (Troca de Equip / Emergência)** (6): *Atordoamento*, *Congelamento*, *Petrificação*, *Sono*, *Prisão da Teia*, *Campo Amaldiçoado*.
  - Added localized tooltips on every status detailing which consumable, skill, or action removes or counters it.

## [v2.23.0] - 2026-11-20 — AureumRO Almas Status Effects & Debuff Alias Detection
### Added
- **AureumRO Almas Status Effects (`Model/Buff.cs`, `Utils/EffectStatusIDs.cs`, `Forms/DebuffRecoveryForm.cs`, `Utils/LocalizationManager.cs`, `Resources/4RTools/Icons.resx`)**:
  - Added all **9 status effects inflicted by AureumRO Souls (Almas)** from the official wiki with official Ragnarok Online icons and full PT-BR/EN names beside each icon:
    - **Hipotermia** (`EFST_FREEZING = 351` + alias `MISTY_FROST = 1141`) — *Alma de Iceicle*
    - **Queimadura** (`EFST_BURNING = 881` + alias `EFST_BURNT = 737`) — *Alma de Imp*, *Alma de Salamander*
    - **Cegueira** (`BLIND = 887`) — *Alma de Waste Stove*, *Alma de Grizzly*
    - **Alucinação** (`HALLUCINATION = 34`) — *Alma de Crab*, *Alma de Tengu*
    - **Congelamento** (`EFST_FREEZE = 876`) — *Alma de Ktullanux*, *Alma de Violy*, *Alma de Swordfish*
    - **Sono** (`EFST_SLEEP = 878`) — *Alma de Nightmare*
    - **Petrificação** (`EFST_STONE = 875` + alias `EFST_STONE_ING = 880`) — *Alma de Medusa*, *Alma de Phreeoni*
    - **Prisão da Teia** (`EFST_SPIDERWEB = 129`) — *Alma de Giant Spider*
    - **Campo Amaldiçoado** (`EFST_CURSEDCIRCLE_TARGET = 417`) — *Alma de Chen Verdadeiro*
- **Canonical rAthena Alias Detection (`Model/DebuffsRecovery.cs`, `Model/StatusRecovery.cs`)**:
  - Automatically matches client-side Opt1/Opt2/Icon EFST aliases (`EFST_BURNT -> EFST_BURNING`, `MISTY_FROST -> EFST_FREEZING`, `EFST_HEALTHSTATE_BLOODING -> EFST_BLEEDING`, `EFST_STONE_ING -> EFST_STONE`) and includes Hipotermia + Queimadura in **Bala de Elvira** quick-recovery.
  - Upgraded `DebuffRecoveryForm` to dynamic scrollable height and `PictureBoxSizeMode.Zoom` so all 17 debuff icons render uncropped.

## [v2.22.0] - 2026-11-20 — Standardized Macros Suite & Clean Pre-Renewal Debuff Layout
### Changed & Removed
- **Standardized `Macros` Sub-Tabs (`Forms/MacroSongForm.cs`, `Forms/ATKDEFForm.cs`, `Utils/LocalizationManager.cs`)**:
  - Unified `Macro Músicas` to the exact same card grammar as `Macro Troca` (`Gatilho:` + `Delay(ms):` + Dagger + Instrument + `Limpar` / `Remover macro` in the top header bar above a horizontal divider; `Músicas:` progressive sequence + `+ Adicionar música` below).
  - Framed `Troca DEF` and `Troca ATK` in matching bordered cards with **`Limpar DEF`** / **`Limpar ATK`** buttons, standardized labels (`Gatilho:`, `Delay(ms):`, `Troca(ms):`), and added a clear tooltip on **`Com clique`** explaining when to keep it checked (target/ground skills vs. self-cast skills).
- **Pre-Renewal Debuffs Audit & Full-Name Layout (`Model/Buff.cs`, `Forms/DebuffRecoveryForm.cs`)**:
  - Removed 3 Renewal-only debuffs that do not exist in Pre-Renewal (**Hipotermia / Freezing** `EFST_FREEZING`, **Sono Profundo / Deep Sleep** `EFST_DEEP_SLEEP`, and **Azar / Infortúnio** `EFST_HANDICAPSTATE_MISFORTUNE`).
  - Redesigned **Efeitos / Debuffs** into a crisp 2-column × 4-row grid displaying the **full localized status name** next to each icon and hotkey box (*Ferimento Crítico*, *Maldição*, *Sangramento*, *Silêncio*, *Diminuir Agilidade*, *Caos / Confusão*, *Atordoamento*, *Veneno*).

## [v2.21.0] - 2026-11-20 — Unified Macros Tab & Dedicated Switch Trigger Key
### Added & Changed
- **Unified `Macros` Tab (`Forms/MacrosHubForm.cs`, `Forms/Container.cs`, `Forms/ATKDEFForm.cs`)**: grouped **Macro Troca**, **Macro Músicas**, and **Macro ATK x DEF** under a single **`Macros`** item in the left sidebar with 3 golden segmented sub-tabs, keeping the sidebar cleaner and more organized.
- **Dedicated Trigger Key (`Gatilho:`) in `Macro Troca` (`Forms/MacroSwitchForm.cs`)**: added a dedicated trigger hotkey field (`inTriggerSwitch1..5`) in the header row of each switch card alongside **Loop**, **Limpar**, and **Remover troca** (matching *Macro Músicas*), allowing the trigger key to be configured independently from the equipment sequence while remaining 100% compatible with existing saved profiles.
- **Cleaner Progressive Action Buttons (`Forms/MacroSwitchForm.cs`, `Forms/MacroSongForm.cs`)**: hides `- Remover campo` / `- Remover música` when only 1 slot is visible (and hides `+ Adicionar` when at max slots), eliminating low-contrast disabled buttons.

## [v2.20.0] - 2026-11-20 — Pre-Renewal Common vs. ROPs Consumables & Progressive Skill Timers
### Added & Changed
- **Pre-Renewal Filter + Common vs. ROPs Split on `Autobuff - Itens` (`Model/Buff.cs`, `Model/BuffRenderer.cs`, `Forms/StuffAutoBuffForm.cs`, `Forms/SkillAutoBuffForm.cs`)**:
  - Reorganized consumables into clear **Itens Comuns** (*Poções e Utilidades*, *Elementais e Resistências*, *Comidas e Caixas*) and **Itens de ROPs** (*Combate e Poções*, *Comidas e EXP*, *Pergaminhos*) sections, plus a **Renewal / Outros** group automatically hidden when **Modo Pré-Renewal** is checked (synced globally with *Autobuff - Skills*).
  - Added an instant origin selector (**Todos (Comuns + ROPs)** / **Apenas Itens Comuns** / **Apenas Itens de ROPs**) alongside bilingual search and **Pausar ao sentar**.
- **Simplified Progressive `Skill Timers` (`Forms/SkillTimerForm.cs`)**: starts cleanly with only **Timer 1** visible and expands on demand via **`+ Adicionar timer`** (up to 6 timers in a clean 3×2 grid) with per-timer **`Limpar`** and **`Remover timer`** controls, auto-expanding any configured timers from saved profiles.

## [v2.19.0] - 2026-11-20 — Simplified Progressive Song Macro UI
### Changed
- **Simplified `Macro Músicas` (`Forms/MacroSongForm.cs`, `Utils/LocalizationManager.cs`)**: replaced the 4×8 static grid with a clean progressive interface that starts with **Música 1** showing **1 campo de música** (plus Trigger, Delay, Dagger, and Instrument) and expands on demand via **`+ Adicionar música`** (up to 8 steps across 2 neat rows with `→`/`↴` indicators), **`- Remover música`**, **`Resetar`**, **`+ Adicionar macro musical`** (up to 4 lanes), and **`Remover macro`** while staying 100% compatible with existing saved profiles.

## [v2.18.0] - 2026-11-20 — Full 10-Slot ATK × DEF Equipment Switch
### Added
- **Complete 10 Equipment Slots in `Modo ATK x DEF` (`Forms/ATKDEFForm.cs`, `Utils/LocalizationManager.cs`, `Resources/4RTools/Icons.resx`)**: added **Cabeça (Topo)** (`inDef6`/`inAtk6`), **Cabeça (Meio)** (`inDef7`/`inAtk7`), **Cabeça (Baixo)** (`inDef8`/`inAtk8`), **Acessório Esquerdo** (`inDef9`/`inAtk9`), and **Acessório Direito** (`inDef10`/`inAtk10`) alongside the existing Armor, Weapon, Shield, Garment, and Shoes slots in a clean symmetric 2×5 grid with transparent RO icons and localized PT-BR/EN tooltips (100% compatible with existing profiles).

## [v2.17.0] - 2026-11-20 — Simplified Progressive Equipment Switch Macro UI
### Changed
- **Simplified `Macro Troca de Equipamentos` (`Forms/MacroSwitchForm.cs`, `Utils/LocalizationManager.cs`)**: replaced the cluttered 5×7 static grid with a clean progressive interface that starts with **Troca 1** showing **1 campo de troca** (`Tecla`, `Delay(ms)`, `Clique`, `Loop`) and lets you expand on demand via **`+ Adicionar campo`** (up to 7 steps with `→` flow arrows), **`- Remover campo`**, **`Limpar`**, **`+ Adicionar troca`** (up to 5 switches), and **`Remover troca`** while staying 100% compatible with existing saved profiles (auto-expanding any configured lanes and steps on load).

## [v2.16.0] - 2026-11-20 — Complete Pre-Renewal Bard / Clown & Dancer / Gypsy Skills
### Added
- **18 Pre-Renewal Bard/Clown & Dancer/Gypsy Skills (`Model/Buff.cs`, `Utils/EffectStatusIDs.cs`, `Utils/LocalizationManager.cs`)**: added all classic songs, dances, ensembles, and transcendent buffs with official 24×24 RO icons, Pre-Renewal Mode visibility, and bilingual PT-BR/EN search:
  - **Bardo / Menestrel:** *Assovio* (`EFST_WHISTLE = 70`), *Crepúsculo Sangrento* (`EFST_ASSASSINCROSS = 71`), *Maçãs de Idun* (`EFST_APPLEIDUN = 73`).
  - **Odalisca / Cigana:** *Sibilo* (`EFST_HUMMING = 74`), *Não Me Abandones* (`EFST_DONTFORGETME = 75`), *Beijo da Sorte* (`EFST_FORTUNEKISS = 76`), *Dança Cigana* (`EFST_SERVICEFORYOU = 77`).
  - **Duetos & Transclasse (Menestrel / Cigana):** *Banquete de Njord* (`78`), *Ritmo Caótico* (`79`), *Rufar dos Tambores* (`80`), *Anel dos Nibelungos* (`81`), *Lamento de Loki* (`82`), *Canção Preciosa* (`83`), *Ode a Siegfried* (`84`), *Controle de Marionete* (`121`), *Bênção Protetora* (`123`), *Grito de Liberdade* (`189`) e *Bastão de Hermod* (`190`).

## [v2.15.1] - 2026-11-20 — Button Text Alignment Fix
### Fixed
- **Profile "Create / Criar" Button Text Alignment (`Forms/ProfileForm.Designer.cs`, `Utils/ThemeManager.cs`)**: increased `btnSave` height from `20px` to `24px` (vertically centered with `txtProfileName`) and enforced `Padding.Empty` + `ContentAlignment.MiddleCenter` on flat text buttons so labels never clip or shift downward.

## [v2.15.0] - 2026-11-20 — Exclusivo AureumRO
### Changed & Removed
- **Removed "Servers" (`Servidores`) Tab (`Forms/Container.cs`)**: removed the Servers navigation section from the sidebar so the interface stays focused and clean for AureumRO players.
- **Exclusive AureumRO Configuration (`supported_servers.json`, `assets/etc/supported_servers.txt`, `Forms/ClientUpdaterForm.cs`)**: stripped all third-party servers so only **AureumRO** (`0x015FF908` / `0x01602568`) is bundled and accepted (while still supporting remote `AureumRO` address updates via `supported_servers.json`).
- **Auto-Select AureumRO Client (`Forms/Container.cs`)**: automatically selects the running `AureumRO.exe` window when a single client instance is detected.

## [v2.14.1] - 2026-11-20 — Battle Chant (Canto de Batalha)
### Added
- **Battle Chant (`EFST_GOSPEL` / `109`) in Swordsman Skills (`Model/Buff.cs`, `Utils/EffectStatusIDs.cs`, `Utils/LocalizationManager.cs`, `assets/images/pa_gospel.png`)**: added Paladin's **Canto de Batalha (Battle Chant / Gospel)** with its official 24×24 RO skill icon, Pre-Renewal Mode visibility, and bilingual PT-BR/EN search support (`canto`, `batalha`, `battle chant`, `gospel`).

## [v2.14.0] - 2026-11-20 — Pre-Renewal Mode & Sit-Safe AutoBuff
### Added
- **Pre-Renewal Mode Filter (`Model/Buff.cs`, `Model/BuffRenderer.cs`, `Forms/SkillAutoBuffForm.cs`, `Model/AppSettings.cs`)**: new **Modo Pré-Renewal (ocultar 3rd/4th)** toggle on the *Autobuff - Skills* tab (enabled by default and saved in `settings.json`) that filters the skill grid to the 61 authentic Pre-Renewal / Transcendent / Expanded skills and hides 3rd/4th job skills and Renewal-only classes (Doram, Soul Ascetic, Night Watch, Hyper Novice, Homunculus S) without losing saved bindings.
- **Pause Autobuffs When Sitting (`Model/Autobuff.cs`, `Forms/SkillAutoBuffForm.cs`, `Forms/StuffAutoBuffForm.cs`)**: detects `EFST_SIT` in the active status buffer and automatically pauses skill & consumable autobuffs while sitting (configurable per profile via **Pausar ao sentar**, enabled by default) so converters, foods, and buffs aren't wasted while recovering SP.

## [v2.13.3] - 2026-11-20 — UI Polish & Town Pause
### Fixed & Added
- **Transparent Icons Everywhere (`Utils/ThemeManager.cs`, `Model/BuffRenderer.cs`, `Forms/AutopotForm.cs`, `assets/etc/Yggdrasil.png`)**: converted `Yggdrasil.bmp` and opaque white-background PNG assets to transparent PNGs and added automatic border flood-fill transparency in `ThemeManager.StripLightBackground` for all `PictureBox` thumbnails (removing white squares in Dark & Light modes while preserving interior highlights).
- **Fixed Label/Input Overlaps Across Forms (`SkillAutoBuffForm`, `StuffAutoBuffForm`, `AutopotForm`, `SkillTimerForm`, `ATKDEFForm`, `MacroSwitchForm`, `MacroSongForm`, `AddServerForm`, `ToggleApplicationStateForm`)**: right-aligned labels and adjusted horizontal spacing so PT-BR and EN text never overlaps input boxes.
- **Replaced "Always on Top" with "Use buffs in town?" (`Usar buffs na cidade?`) (`Forms/Container.cs`, `Model/Client.cs`, `Model/Autopot.cs`, `Model/AppSettings.cs`)**: when unchecked (default), pauses Autopot (HP/SP & Yggdrasil), Autobuff (Skills & Items), Status/Debuff Recovery, and Skill Timers while the character is in any town/city map (using optional `mapAddress` in `supported_servers.json`).

## [v2.13.2] - 2026-11-20 — AureumRO 4Tools
### Changed
- **Rebranding to `AureumRO 4Tools`**: renamed project display name, executable output (`AureumRO 4Tools.exe`), window/Mini HUD titles, dialogs, release packages (`AureumRO-4Tools-<tag>.zip`), and documentation to **AureumRO 4Tools**.
- **Developer Credits**: updated application footer, assembly metadata, and documentation development credits to **Oráculo**.

## [v2.13.1] - 2026-11-20 — AureumRO Distribution Polish
### Fixed
- **Guaranteed Built-in AureumRO Server Support (`Forms/ClientUpdaterForm.cs`, `Utils/AppConfig.cs`)**: always merges embedded `supported_servers.txt` (including `AureumRO` `0x015FF908` / `0x01602568`) with local overrides and remote `iOraculo/4RTools/main/supported_servers.json` (deduplicated by process & addresses), so standalone `4RTools.exe` detects `AureumRO` immediately even offline or without an external `supported_servers.json`.
- **Reliable AutoPatcher Asset Selection & Relaunch (`Forms/AutoPatcher.cs`)**: explicitly selects `.zip`/`.rar` release archives instead of assuming `assets[0]` and automatically relaunches the updated `4RTools.exe` after patching.
- **Themed Startup Splash Dialogs (`Forms/AutoPatcher.cs`, `Forms/ClientUpdaterForm.cs`)**: applied the AureumRO Obsidian & Gold theme to startup updater windows.

## [v2.13.0] - 2026-11-20 — AureumRO Edition
### Added & Changed
- **AureumRO Official Visual Identity (`Utils/ThemeManager.cs`, `Utils/ModernControls.cs`, `Utils/VitalBar.cs`, `Forms/Container.cs`, `Forms/MiniHudForm.cs`)**:
  - Applied the official **AureumRO** color palette from `https://aureumro.com/`: **Obsidian & Royal Gold** Dark Mode (now default on first launch: `#0B0701`, `#120C03`, `#1B1304`, `#1E1506`, `#F4D97A`, `#F3EAD0`) and **Warm Parchment & Antique Gold** Light Mode (`#F2E8CC`, `#FAF4E0`, `#FDF8E8`, `#D4C098`, `#A08123`, `#2A1E0E`).
  - Replaced app, window, topbar, and system-tray ON/OFF icons (`logo-4rtools.ico`, `logo_4rtools_on.ico`, `logo_4rtools_off.ico`) with the official **AureumRO Golden Winged-A Emblem** featuring emerald ON (`#2ECC71`) and bronze/ruby OFF status badges.
  - Styled active segmented tabs with AureumRO's metallic gold CTA gradient (`#FFF0C2` → `#ECC861` → `#A67E1A`) and cards with subtle warm gold top highlights.
  - Updated branding, links, and footer with **AureumRO** (`Site`, `Wiki`, `Discord`, `GitHub`) and added **AureumRO** (`0x015FF908` / `0x01602568`) to built-in `supported_servers.json`.

## [v2.12.0] - 2026-11-19
### Added
- **Modern Redesigned Interface** (`Forms/Container.cs`, `Utils/ModernControls.cs`, `Utils/ThemeManager.cs`): replaced legacy multi-row tabs and MDI chrome with a structured desktop layout featuring a clean top bar (live ON/OFF emblem, client/profile selectors, Dark Mode / Mini HUD / Always on Top / PT-BR-EN controls), grouped left sidebar with custom GDI+ vector icons and a live session summary card, always-visible Combat Dashboard (Autopot/Yggdrasil segmented pills + ON/OFF + live HP/SP bars), active section cards with descriptive titles/subtitles, and a visible footer with GitHub & Discord links.
- **Portuguese (PT-BR) interface by default + live `PT-BR / EN` switcher** (`LocalizationManager`): translates all tabs, forms, buttons, tooltips, tray menu, dialogs, server tester messages, and conflict reports live without restarting, including bilingual `PT-BR (EN)` tooltips and accent-insensitive bilingual search for all 267 skills, consumables, and debuffs.
- **Floating Mini HUD** (`MiniHudForm`): compact, draggable, translucent always-on-top overlay displaying live `ON`/`OFF`/`CHAT` status, active profile/character, and real-time HP/SP bars; toggleable from the main header with persisted visibility and screen coordinates in `settings.json`.
- **Keybind Conflict Inspector** (`KeybindConflictDetector`): inspects the active profile across all modules (Toggle, Autopot, AHK, Autobuff, Recovery, Skill Timers, ATK/DEF, Song Macro, Macro Switch) and reports duplicate key assignments while allowing intentional shared cure keys.
- **Full `.zip` Backup & Restore** (`ProfileBackupManager`): export and restore all profiles, custom servers (`supported_servers.json`), and app settings (`settings.json`) in a single archive with Zip Slip and JSON validation.
- **Live Server Address Tester**: new **Test** button in `AddServerForm` (`Client.TestMemoryAddresses`) verifies HP/SP, character name, and active buff count directly against the running Ragnarok process before saving.
- **Smart AutoBuff controls**: configurable **Min SP%** threshold, per-buff recast **Cooldown (`CD(ms)`)**, and automatic pause during incapacitating debuffs (Silence, Freezing, Stun, Deep Sleep) on both Autobuff Skills and Stuffs tabs.
- **6 Skill Timer slots** (`SkillTimerForm` + `Profile`): added a second row with 3 additional independent Skill Timers (`4`, `5`, `6`), persisted per profile and included in keybind conflict inspection.
- **Automated Release workflow** (`.github/workflows/release.yml`): builds, tests, packages `4RTools-<tag>.zip` + `4RTools.exe` + `SHA256SUMS.txt`, and publishes GitHub Releases automatically on `v*` tags.

## [v2.11.0] - 2026-11-19
### Added
- **Dark Mode** and **Always on Top** toggles in the main header, persisted globally in `settings.json` (`AppSettings` + `ThemeManager`).
- **Real-time HP/SP vital bars** (`VitalBar`) below the active character name using the 16-byte bulk vitals read.
- **Auto-Switch Profile by Character**: bind profiles to character names in the Profiles tab (`ProfileForm`) to automatically switch profiles when logging in or changing characters (while automations are `OFF`).
- **Chat-Safe Pause**: optional `Chat` checkbox on the ON/OFF panel pauses automations while typing in Ragnarok chat (`Enter` toggles, `Esc` resumes, `CHAT` orange indicator) while keeping F-key Autopot active (`ChatPauseState`).
- **Clear All Keys** button in Skill Spammer (`AHKForm`).
- Search/filter bar and Clear All button on Autobuff Skills and Autobuff Stuffs tabs.
- Optional "Focus" mode to pause automations when the Ragnarok window is not in focus (saved per profile).
- Persistent Sound toggle setting per profile.
- Automatic client-close detection that cleanly stops all automations when the game process exits.
- Per-lane `Loop` toggle in Macro Switch.
- Profile Import and Export buttons in the Profiles tab.
- Rotating diagnostic file logger (`logs/4rtools.log`) and unhandled exception capture.
- Automated Windows CI workflow (`.github/workflows/ci.yml`) and xUnit test suite (`tests/4RTools.Tests`).

### Performance & Architecture
- Replaced 100 individual `ReadProcessMemory` syscalls per loop with a single 400-byte bulk read in `AutoBuff`, `StatusRecovery`, and `DebuffsRecovery` (and single 16-byte bulk vitals read in `Autopot`).
- Eliminated 150+ redundant JSON disk writes on profile switches via `ProfileSingleton.SuppressSave()` and atomic file writes.
- Consolidated `StatusRecovery` and `DebuffsRecovery` onto shared `StatusEffectRecoveryBase`.
- Requested minimal read-only process permissions (`PROCESS_VM_READ | PROCESS_QUERY_LIMITED_INFORMATION`), properly disposed native handles and `Process`/`SoundPlayer` objects, and removed unused ads/telemetry code.

### Fixed
- Replaced obsolete/deadlocking `Thread.Suspend()` in `_4RThread` with cooperative `CancellationToken` background threads; stopped thread leaks in `ATKDEFMode` and `AutoRefreshSpammer`.
- Fixed `Autopot` `hpPotCount` counter bug, dead-character potting, and duplicate event handler accumulation on profile switches.
- Fixed `AutoBuff` Quagmire loop using `break` instead of `continue`.
- Fixed `MacroSongForm` Reset button not resetting the lane and fixed `cDagger`/`cInstrument` array checks.
- Fixed `MacroSwitchForm` click lookup bug, sparse slot `KeyNotFoundException`, and `hasClick` preservation.
- Wired disconnected `Auto Stand` checkbox and `Elvira Candy Cooldown` input in `DebuffRecoveryForm`.
- Fixed `ProfileSingleton.Load` leaking settings from previous profiles, fixed profile names with dots, and sanitized profile names.
- Replaced fragile `Enum.Parse` key conversions with `KeyInterop`-based `KeyHelper` and added `try/finally` safety for `AHK` `noShift`.
- Hardened `AutoPatcher` (handle lifecycle, `NaN` progress guard, Zip Slip validation, `.zip` support) and `ClientUpdaterForm` (`ProgressBar.Maximum` overflow).

## v[1.0.0] - 25/02/2022 (First Stable Version)
- Autopot (HP/SP with delay customization)
- Autobuff Status (Only for Poison, Silence, Blind, Confusion, Curse, Hallucinationwalk)
- Autoclick (Skill Spammer with delay customization) 
- Profiles Support (How many profiles you want)
- Ingame ON/OFF Button (End Key), you don't need to minimize your game screen.



## [v1.1.0] - 2022-02-26
### Added
- Auto-Refresh Spammer
- Auto status. Removes Poison, silence, blind, confusion, curse and hallucination automatically using Panacea, Green Potion or Royal Jelly
- New UI to manager profiles

### Fixed
- Fixed a bug where AHK was consuming more than 5% of CPU. Now the consumer of the CPU is under of 1%.
- Always request execution level as administrator


## [v1.2.0] - 2022-02-28
### Added
- Autobuff for Stuffs
- Added new keys (Space, Page UP, Page Down, Insert, Delete)

### Fixed
- On change profile not start threads anymore.
- App don't crash anymore with a invalid json profile.

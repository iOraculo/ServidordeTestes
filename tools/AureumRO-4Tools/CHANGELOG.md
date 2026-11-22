# Changelogs

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

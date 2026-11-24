using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public static class LocalizationManager
    {
        public const string LANG_PT_BR = "pt-BR";
        public const string LANG_EN_US = "en-US";

        private static string currentLanguage = LANG_PT_BR;

        // Exact-match dictionary mapping English Designer/UI strings to Portuguese (PT-BR).
        // Reverse lookup is generated automatically so switching PT-BR <-> EN-US live works seamlessly.
        private static readonly Dictionary<string, string> EnToPt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Container & Tabs
            { "Ragnarok Client", "Cliente Ragnarok" },
            { "Profile", "Perfil" },
            { "Character Name:", "Personagem:" },
            { "Dark Mode", "Modo Escuro" },
            { "Always on Top", "Sempre no Topo" },
            { "Use buffs in town?", "Usar buffs na cidade?" },
            { "Use in town?", "Usar na cidade?" },
            { "Pause in towns", "Pausar em cidades" },
            { "Mini HUD", "Mini HUD" },
            { "Join in our discord", "Entre no nosso Discord" },
            { "Developed by Oráculo", "Desenvolvido por Oráculo" },
            { "Skill Spammer", "Spammer" },
            { "Debuff", "Debuffs" },
            { "Anti-Debuffs", "Anti-debuffs" },
            { "Auto-skill", "Auto-skill" },
            { "Mount", "Montaria" },
            { "Skills", "Habilidades" },
            { "Consumables", "Consumíveis" },
            { "Autobuff - Skills", "Autobuff - Skills" },
            { "Autobuff - Stuffs", "Autobuff - Itens" },
            { "Skill timers", "Temporizadores" },
            { "Macro Switch", "Macro Troca" },
            { "Macro Songs", "Macro Músicas" },
            { "ATK x DEF Mode", "Modo ATK x DEF" },
            { "Profiles", "Perfis" },
            { "Guide / Help", "Guia / Ajuda" },
            { "Search guide:", "Buscar no guia:" },
            { "Skill (Trigger):", "Skill (Gatilho):" },
            { "Servers", "Servidores" },
            { "COMBAT & AUTOMATION", "COMBATE & AUTOMAÇÃO" },
            { "MANAGEMENT", "GERENCIAMENTO" },

            // ToggleApplicationStateForm
            { "ON / OFF", "LIGAR / DESLIGAR" },
            { "Sound", "Som" },
            { "Focus", "Foco" },
            { "Chat", "Chat" },
            { "Close", "Fechar" },
            { "Press the key to start!", "Aperte a tecla p/ iniciar!" },
            { "Press the key to stop!", "Aperte a tecla p/ parar!" },
            { "Chat paused (Esc/Enter)", "Pausa de chat (Esc/Enter)" },
            { "Client closed!", "Cliente fechado!" },
            { "Please select the Ragnarok Client!!", "Selecione o cliente RO!" },

            // AutopotForm
            { "Delay", "Atraso" },

            // AHKForm
            { "AHK Configuration", "Configuração do Spammer" },
            { "Compatibility", "Compatibilidade" },
            { "Speed boost", "Velocidade Máx." },
            { "Key Config", "Config. Tecla" },
            { "Mouse Flick", "Mouse Flick" },
            { "No Shift", "Sem Shift" },
            { "Spammer Delay", "Atraso (Delay)" },
            { "Example", "Legenda" },
            { "With mouse click", "Com clique" },
            { "No mouse click", "Sem clique" },
            { "Deactivated", "Desativado" },
            { "Clear All Keys", "Limpar Teclas" },

            // DebuffRecoveryForm
            { "Status Recovery", "Recuperação de Status" },
            { "Status Effects", "Efeitos / Debuffs" },
            { "Panacea / Royal Jelly (Consumable Cure)", "Panaceia / Geleia Real (Cura por Consumível)" },
            { "Increase Agility / Speed", "Aumentar Agilidade / Velocidade" },
            { "Inc. AGI / AGI Scroll / Guyak / Guarana", "Aumentar AGI / Pergaminho / Guyak / Guaraná" },
            { "Single key (all 6):", "Tecla única (todos os 6):" },
            { "Or set by status (Green Pot / Holy Water)", "Ou defina por status (Poção Verde / Água Benta)" },
            { "Enable Auto Stand (when forced to sit)", "Levantar automaticamente (quando forçado a sentar)" },
            { "Elvira Candy", "Doce de Elvira" },
            { "Cooldown", "Recarga" },

            // SkillAutoBuffForm & StuffAutoBuffForm
            { "Class:", "Classe:" },
            { "Filter:", "Filtrar:" },
            { "SP%:", "SP%:" },
            { "CD:", "CD:" },
            { "Min SP%:", "SP Mín%:" },
            { "CD(ms):", "CD(ms):" },
            { "Clear Skills", "Limpar Skills" },
            { "Clear Stuffs", "Limpar Itens" },
            { "Pre-Renewal Mode (hide 3rd/4th)", "Modo Pré-Renewal (ocultar 3rd/4th)" },
            { "Pause when sitting", "Pausar ao sentar" },
            { "Auto-click self (target)", "Auto-clique em si (alvo)" },
            { "Pause when sitting (save consumables)", "Pausar ao sentar (economizar itens)" },
            { "Archer Skills", "Arqueiro / Evoluções" },
            { "Swordsman Skills", "Espadachim / Evoluções" },
            { "Mage Skills", "Mago / Evoluções" },
            { "Merchant Skills", "Mercador / Evoluções" },
            { "Thief Skills", "Gatuno / Evoluções" },
            { "Acolyte Skills", "Noviço / Evoluções" },
            { "Taekwon Skills", "Taekwon / Evoluções" },
            { "Ninja Skills", "Ninja / Evoluções" },
            { "Gunslinger Skills", "Justiceiro / Evoluções" },
            { "Summoner", "Invocador (Doram)" },
            { "Hyper Novice", "Hiper Aprendiz" },
            { "Night Watch", "Vigilante Noturno" },
            { "Soul Ascetic", "Asceta da Alma" },
            { "Homunculus", "Homúnculo" },
            { "Potions", "Poções" },
            { "Elementals", "Elementais / Conversores" },
            { "Boxes / Speed / Status", "Caixas / Velocidade / Status" },
            { "Foods", "Comidas" },
            { "Scroll Buffs", "Pergaminhos" },
            { "ETC", "Outros (ETC)" },
            { "Candies", "Doces" },
            { "EXP", "EXP / Drop" },
            { "Pre-Renewal Mode", "Modo Pré-Renewal" },
            { "Common Items — Potions & Utilities", "Itens Comuns — Poções e Utilidades" },
            { "Common Items — Elementals & Resists", "Itens Comuns — Elementais e Resistências" },
            { "Common Items — Foods & Boxes", "Itens Comuns — Comidas e Caixas" },
            { "ROPs Items — Combat & Potions", "Itens de ROPs — Combate e Poções" },
            { "ROPs Items — Foods & EXP", "Itens de ROPs — Comidas e EXP" },
            { "ROPs Items — Scrolls", "Itens de ROPs — Pergaminhos" },
            { "Renewal / Other (Runes, 3rd/4th, Candies)", "Renewal / Outros (Runas, 3rd/4th, Doces)" },

            // SkillTimerForm
            { "Skill timer 1", "Timer de Skill 1" },
            { "Skill timer 2", "Timer de Skill 2" },
            { "Skill timer 3", "Timer de Skill 3" },
            { "Skill timer 4", "Timer de Skill 4" },
            { "Skill timer 5", "Timer de Skill 5" },
            { "Skill timer 6", "Timer de Skill 6" },
            { "Key", "Tecla" },
            { "sec", "seg" },
            { "+ Add Timer", "+ Adicionar timer" },
            { "Remove Timer", "Remover timer" },

            // MacroSongForm & MacroSwitchForm
            { "Reset", "Resetar" },
            { "Switch 1", "Troca 1" },
            { "Switch 2", "Troca 2" },
            { "Switch 3", "Troca 3" },
            { "Switch 4", "Troca 4" },
            { "Switch 5", "Troca 5" },
            { "Keys:", "Teclas:" },
            { "Delays(ms):", "Delay(ms):" },
            { "Click", "Clique" },
            { "+ Add Slot", "+ Adicionar campo" },
            { "- Remove Slot", "- Remover campo" },
            { "+ Add Switch", "+ Adicionar troca" },
            { "Remove Switch", "Remover troca" },
            { "Song Macro 1", "Música 1" },
            { "Song Macro 2", "Música 2" },
            { "Song Macro 3", "Música 3" },
            { "Song Macro 4", "Música 4" },
            { "+ Add Song", "+ Adicionar música" },
            { "- Remove Song", "- Remover música" },
            { "+ Add Song Macro", "+ Adicionar macro musical" },
            { "Remove Macro", "Remover macro" },
            { "Switch Macro", "Macro Troca" },
            { "Song Macro", "Macro Músicas" },
            { "ATK x DEF Macro", "Macro ATK x DEF" },
            { "Trigger:", "Gatilho:" },
            { "(Key that triggers this switch)", "(Tecla que aciona esta troca)" },
            { "Songs:", "Músicas:" },
            { "Switch(ms):", "Troca(ms):" },
            { "Clear DEF", "Limpar DEF" },
            { "Clear ATK", "Limpar ATK" },

            // ATKDEFForm
            { "ATK Switch", "Troca ATK" },
            { "DEF Switch", "Troca DEF" },
            { "Spammer Key", "Tecla Spammer" },
            { "Configuration", "Configuração" },
            { "Switch Delay", "Atraso da Troca" },

            // MountForm
            { "Mount (Halter Lead)", "Montaria (Rédea)" },
            { "Enable Mount", "Ativar Montaria" },
            { "Mount Key:", "Tecla da Montaria:" },
            { "Dismount delay(ms):", "Delay descer(ms):" },
            { "Dismount when using skill", "Descer da montaria ao usar habilidade" },
            { "Pause Auto-skill while mounted", "Pausar Auto-skill montado" },
            { "Auto remount after idle", "Remontar após inatividade" },
            { "After(ms):", "Após(ms):" },
            { "Skill Keys (Auto Dismount)", "Teclas de Habilidade (Descer Automático)" },
            { "Include keys from Spammer, Auto-skill, Skill Timers and Macros", "Incluir teclas do Spammer, Auto-skill, Temporizadores e Macros" },
            { "Extra skill keys:", "Teclas extras:" },
            { "Action Priority", "Prioridade de Ações" },
            { "Player Priority", "Prioridade Jogador" },
            { "Player pause:", "Pausa jogador:" },
            { "Hold pause:", "Segurar pausa:" },
            { "Pause Autopot", "Pausar Autopot" },
            { "Player input pause:", "Pausa p/ input do jogador:" },
            { "Pause 4RTools on player key (ms):", "Pausa ao input do jogador (ms):" },

            // Anti-Position Bug (@refresh) & Utilities
            { "Utilities", "Utilidades" },
            { "How it works", "Como funciona" },
            { "Anti-Position Bug (@refresh)", "Anti-Bug Position (@refresh)" },
            { "Enable @refresh", "Ativar @refresh" },
            { "Command:", "Comando:" },
            { "Hotkey (1-tap):", "Atalho (1 toque):" },
            { "Auto every:", "Auto a cada:" },
            { "Emergency Auto-Escape (Fly Wing / Teleport)", "Auto-Fuga de Emergência (Asa de Mosca / Teleporte)" },
            { "Enable Auto-Escape", "Ativar Auto-Fuga" },
            { "Escape Key:", "Tecla Fuga:" },
            { "Trigger HP <", "Fugir se HP <" },
            { "Cooldown (ms):", "Recarga (ms):" },
            { "Press Enter after key (Teleport skill)", "Confirmar com Enter (skill Teleporte)" },
            { "Disable in towns", "Desativar em cidades" },

            // ProfileForm & EditProfileName
            { "Create a new profile", "Criar um novo perfil" },
            { "Create", "Criar" },
            { "Profile List", "Lista de Perfis" },
            { "Remove Selected Profile", "Remover" },
            { "Edit", "Editar" },
            { "Copy", "Copiar" },
            { "Export", "Exportar" },
            { "Import", "Importar" },
            { "Backup All (.zip)", "Backup (.zip)" },
            { "Restore (.zip)", "Restaurar (.zip)" },
            { "Check Key Conflicts", "Verificar Conflitos" },
            { "Inspect Status IDs", "Ver Status (IDs)" },
            { "Auto-Switch by Character", "Troca por Personagem" },
            { "Enable Auto-Switch", "Ativar Troca Automática" },
            { "Use Active Character", "Usar Personagem Ativo" },
            { "Save", "Salvar" },
            { "Clear", "Limpar" },
            { "Auto-loads profile when this character is detected (while OFF).", "Carrega o perfil ao detectar este personagem (em OFF)." },
            { "Edit Profile Name", "Renomear Perfil" },
            { "New profile name:", "Novo nome do perfil:" },

            // ServersForm & AddServerForm
            { "Add Server", "Adicionar Servidor" },
            { "Add New Server", "Adicionar Novo Servidor" },
            { "Server Properties", "Propriedades do Servidor" },
            { "HP Address:", "Endereço HP:" },
            { "Name Address:", "Endereço Nome:" },
            { "Process Name:", "Processo:" },
            { "Test", "Testar" },
            { "Click Test while logged in to verify addresses.", "Clique em Testar logado no jogo para validar." },
            { "Name", "Nome" },
            { "HP Address", "Endereço HP" },
            { "Name Address", "Endereço Nome" },
            { "Delete", "Excluir" },

            // Updaters
            { "Updating AureumTools...", "Atualizando AureumTools..." },
            { "Updating Supported Servers...", "Atualizando servidores suportados..." }
        };

        private static readonly Dictionary<string, string> PtToEn = BuildReverseDictionary(EnToPt);

        // Keyed message table for dialogs, tooltips, reports, and dynamic text.
        private static readonly Dictionary<string, string> MessagesPt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Common titles
            { "title.warning", "Aviso" },
            { "title.error", "Erro" },
            { "title.confirm", "Confirmar" },
            { "title.message", "Mensagem" },
            { "title.yes", "Sim" },
            { "title.no", "Não" },

            // Container
            { "tooltip.dark_mode", "Alternar entre tema claro e escuro (clique no sol/lua)" },
            { "tooltip.mini_hud", "Exibir janela flutuante compacta com status e barras de HP/SP" },
            { "tooltip.top_most", "Manter o AureumTools sempre acima da janela do Ragnarok" },
            { "tooltip.use_buffs_in_town", "Quando marcado (padrão), pausa Autopot (HP/SP), Auto-skill, Consumíveis, Temporizadores e Anti-debuffs enquanto estiver em cidades. Desmarque para manter tudo ativo na cidade." },
            { "tooltip.language", "Alterar o idioma da interface (Português / Inglês)" },
            { "tooltip.refresh_clients", "Atualizar lista de clientes Ragnarok abertos" },
            { "tooltip.website", "Abrir site oficial do AureumRO (aureumro.com)" },
            { "tooltip.wiki", "Abrir Wiki oficial do AureumRO" },
            { "tooltip.discord", "Entrar no Discord oficial do AureumRO" },
            { "tooltip.github", "Abrir repositório do projeto no GitHub" },
            { "footer.website", "Site" },
            { "footer.wiki", "Wiki" },
            { "footer.powered", "✦ AureumTools · Desenvolvido por Oráculo  •  Minimize para enviar à bandeja" },
            { "sidebar.status_on", "Automação ativa" },
            { "sidebar.status_chat", "Pausado (Chat)" },
            { "sidebar.status_town", "Pausado (Cidade)" },
            { "sidebar.status_off", "Pronto (OFF)" },
            { "sidebar.profile_prefix", "Perfil: {0}" },
            { "sidebar.client_connected", "Cliente: {0}" },
            { "sidebar.client_none", "Cliente: não selecionado" },
            { "section.spammer.title", "Spammer" },
            { "section.spammer.subtitle", "Configure teclas de ataque rápido, atraso do spammer e prioridade do input do jogador." },
            { "section.debuff.title", "Anti-debuffs" },
            { "section.debuff.subtitle", "Cure automaticamente apenas status removíveis por itens ou habilidades." },
            { "section.autobuff.title", "Auto-skill" },
            { "section.autobuff.subtitle", "Renove buffs de habilidades e consumíveis (Comuns e ROPs) automaticamente." },
            { "section.buff_skills.title", "Auto-skill" },
            { "section.buff_skills.subtitle", "Mantenha buffs de classe ativos com filtro de busca, SP mínimo e recarga." },
            { "section.buff_stuffs.title", "Consumíveis" },
            { "section.buff_stuffs.subtitle", "Renove poções, comidas, conversores elementais e caixas automaticamente." },
            { "section.timers.title", "Temporizadores" },
            { "section.timers.subtitle", "Dispare até 6 habilidades em intervalos fixos de tempo (em segundos)." },
            { "section.macro_switch.title", "Macro Troca de Equipamentos" },
            { "section.macro_switch.subtitle", "Crie sequências rápidas de troca de equipamentos com atrasos personalizados." },
            { "section.macro_songs.title", "Macro Músicas" },
            { "section.macro_songs.subtitle", "Automatize sequências musicais de Bardo/Odalisca com troca de instrumento." },
            { "section.atkdef.title", "Modo ATK x DEF" },
            { "section.atkdef.subtitle", "Alterne automaticamente entre equipamentos de ataque e defesa durante o spam." },
            { "section.macros.title", "Macros" },
            { "section.macros.subtitle", "Gerencie Macro Troca, Macro Músicas e Macro ATK x DEF em um só lugar." },
            { "section.mount.title", "Montaria" },
            { "section.mount.subtitle", "Desça automaticamente da montaria (Rédea) ao usar habilidades e configure teclas extras." },
            { "section.utilities.title", "Utilidades" },
            { "section.utilities.subtitle", "Anti-Bug Position (@refresh) e Auto-Fuga de Emergência (Asa de Mosca / Teleporte)." },
            { "section.aureum.title", "Aureum Hub" },
            { "section.aureum.subtitle", "Hora da Votação (timer 12h + lembrete), Coleção de Cartas (28) e Coleção de Mapas (311)." },
            { "section.profiles.title", "Perfis, Backup e Diagnóstico" },
            { "section.profiles.subtitle", "Gerencie perfis, vínculo automático por personagem, backup .zip e conflitos de tecla." },
            { "section.guide.title", "Guia Completo do AureumTools" },
            { "section.guide.subtitle", "Entenda todas as abas, modos de macro e dicas — passe o mouse nos campos para ver pop-ups." },
            { "section.servers.title", "Servidores Suportados" },
            { "section.servers.subtitle", "Adicione, edite e teste endereços de memória HP/Nome de servidores customizados." },
            { "tooltip.process_select", "Selecione a janela aberta do AureumRO.exe que receberá as automações (seleciona sozinho quando há apenas 1 janela)." },
            { "tooltip.profile_select", "Troque rapidamente entre seus perfis salvos (cada perfil guarda teclas de Autopot, Spammer, Buffs, Debuffs, Timers e Macros)." },
            { "tooltip.tab_autopot", "Autopot padrão: usa poções comuns (Branca, Azul, Mastela, etc.) quando o HP ou SP cair abaixo da % definida." },
            { "tooltip.tab_ygg", "Autopot Yggdrasil (emergência): configure uma % mais baixa que a do Autopot comum para usar Fruto/Semente de Yggdrasil só no sufoco." },
            { "tooltip.vital_hp", "HP atual do personagem lido em tempo real da memória do AureumRO." },
            { "tooltip.vital_sp", "SP atual do personagem lido em tempo real da memória do AureumRO." },
            { "tooltip.session_card", "Resumo ao vivo da sessão: estado da automação (ON/OFF/Chat/Cidade), perfil ativo e cliente conectado." },
            { "tooltip.nav_spammer", "Spammer: segure uma tecla no jogo para disparar habilidades rapidamente (com ou sem clique do mouse)." },
            { "tooltip.nav_debuff", "Anti-debuffs: cura automática dos 7 status curáveis por Panaceia, Geleia Real, Poção Verde, Água Benta ou Aumentar AGI." },
            { "tooltip.nav_autobuff", "Auto-skill: reúne buffs de Habilidades e Consumíveis (Comuns e ROPs) com renovação automática." },
            { "tooltip.autobuff_tab_skills", "Habilidades: renova automaticamente buffs de classe assim que o ícone do buff expira no jogo." },
            { "tooltip.autobuff_tab_items", "Consumíveis: renova poções de ASPD, comidas, conversores elementais, caixas e pergaminhos (Comuns e ROPs)." },
            { "tooltip.nav_buff_skills", "Auto-skill: renova automaticamente buffs de classe assim que o ícone do buff expira no jogo." },
            { "tooltip.nav_buff_stuffs", "Consumíveis: renova poções de ASPD, comidas, conversores elementais, caixas e pergaminhos (Comuns e ROPs)." },
            { "tooltip.nav_timers", "Temporizadores: aperta até 6 teclas automaticamente em intervalos fixos de tempo (em segundos)." },
            { "tooltip.nav_macros", "Macros: reúne Macro Troca (sequência de equips), Macro Músicas (Bardo/Odalisca) e Macro ATK x DEF (troca + spammer)." },
            { "tooltip.nav_mount", "Montaria: desce automaticamente da montaria (Rédea) ao usar habilidades manuais ou automatizadas." },
            { "tooltip.nav_utilities", "Utilidades: Anti-Bug Position (@refresh) e Auto-Fuga de Emergência (Asa de Mosca / Teleporte)." },
            { "tooltip.nav_aureum", "Aureum Hub: Hora da Votação (timers de 12h + lembrete), Coleção de Cartas (com marcação tachada) e Coleção de Mapas (com setas de direção e drops)." },
            { "tooltip.nav_profiles", "Perfis & Backup: crie, renomeie, copie, faça backup .zip, verifique conflitos de teclas e vincule perfis ao personagem." },
            { "tooltip.nav_guide", "Guia / Ajuda: manual completo e pesquisável explicando todas as funcionalidades do AureumTools." },
            { "tooltip.autopot_hp_key", "Tecla da poção de HP (ex.: Poção Branca, Mastela, Compacta). Aperte Backspace/Esc para limpar." },
            { "tooltip.autopot_hp_pct", "Porcentagem de HP abaixo da qual a poção de HP será usada automaticamente (ex.: 80%)." },
            { "tooltip.autopot_sp_key", "Tecla da poção de SP (ex.: Poção Azul, Morango, Limão). Aperte Backspace/Esc para limpar." },
            { "tooltip.autopot_sp_pct", "Porcentagem de SP abaixo da qual a poção de SP será usada automaticamente (ex.: 40%)." },
            { "tooltip.autopot_ygg_hp_key", "Tecla de emergência (Fruto/Semente de Yggdrasil) para recuperar HP crítico." },
            { "tooltip.autopot_ygg_hp_pct", "Porcentagem crítica de HP para disparar Yggdrasil (use um valor menor que o do Autopot padrão, ex.: 35%)." },
            { "tooltip.autopot_ygg_sp_key", "Tecla de emergência (Fruto/Semente de Yggdrasil) para recuperar SP crítico." },
            { "tooltip.autopot_ygg_sp_pct", "Porcentagem crítica de SP para disparar Yggdrasil (ex.: 15%)." },
            { "tooltip.autopot_delay", "Intervalo em milissegundos (ms) entre cada tentativa de potar (valores comuns: 50 a 150ms)." },
            { "tooltip.toggle_btn", "Liga ou desliga todas as automações do AureumTools de uma só vez." },
            { "tooltip.toggle_key", "Tecla global (ex.: End, Insert, F12) para ligar ou desligar o AureumTools direto de dentro do jogo." },
            { "tooltip.toggle_audio", "Toca um aviso sonoro curto sempre que a automação for ligada ou desligada." },
            { "tooltip.ahk_key", "Clique na caixinha para alternar entre 3 modos:\n• Vazio: desativado\n• Quadrado (■): spamma só a tecla, sem clicar (skills self-cast)\n• Marcado (✓): spamma tecla + clique do mouse (skills de alvo/chão)" },
            { "tooltip.ahk_delay", "Intervalo em milissegundos (ms) entre cada repetição da skill enquanto segura a tecla (menor = mais rápido; ex.: 10 a 30ms)." },
            { "tooltip.ahk_compat", "Modo Compatibilidade: envio padrão mais estável; permite usar 'Sem Shift' e 'Mouse Flick'." },
            { "tooltip.ahk_speed", "Modo Speed Boost: disparo mais agressivo para máximo spam (desativa 'Sem Shift' e 'Mouse Flick')." },
            { "tooltip.ahk_noshift", "Sem Shift: evita que segurar Shift bloqueie ou interfira no spam de teclas." },
            { "tooltip.ahk_flick", "Mouse Flick: realiza um micro-movimento no cursor entre os cliques para melhorar registro de skills de alvo/chão." },
            { "tooltip.ahk_clear", "Desmarca todas as teclas configuradas no Spam de Skills deste perfil." },
            { "tooltip.buff_search", "Filtre buffs pelo nome em Português ou Inglês (ex.: 'Benção', 'Asura', 'Conversor', 'Comida')." },
            { "tooltip.buff_clear_skills", "Limpa todas as teclas de Autobuff — Skills deste perfil (pede confirmação)." },
            { "tooltip.buff_clear_stuffs", "Limpa todas as teclas de Autobuff — Itens deste perfil (pede confirmação)." },
            { "tooltip.timer_delay", "Intervalo em segundos entre cada disparo automático desta tecla (ex.: 10 = aperta a tecla a cada 10 segundos)." },
            { "tooltip.timer_key", "Tecla da habilidade ou item que será pressionada periodicamente enquanto a automação estiver ON." },
            { "tooltip.timer_clear", "Limpa a tecla e reseta o tempo deste timer para 5 segundos." },
            { "tooltip.timer_remove", "Limpa e oculta este timer da grade." },
            { "tooltip.timer_add", "Adiciona mais um timer independente (até 6 timers simultâneos)." },
            { "tooltip.macro_tab_switch", "Macro Troca: ao apertar um Gatilho, aperta uma sequência de equipamentos/teclas com atrasos individuais (até 5 trocas)." },
            { "tooltip.macro_tab_songs", "Macro Músicas: automatiza sequências de músicas/danças de Bardo e Odalisca com troca automática de Adaga e Instrumento." },
            { "tooltip.macro_tab_atkdef", "Macro ATK x DEF: Troca + Spammer! Ao segurar a tecla da Skill, equipa o set ATK, spamma a skill (com ou sem clique) e, ao soltar, equipa de volta o set DEF." },
            { "tooltip.switch_click", "Quando marcado, também envia um clique esquerdo do mouse após apertar a tecla deste passo." },
            { "tooltip.switch_add_slot", "Adiciona mais um passo na sequência desta troca (até 7 teclas em cadeia)." },
            { "tooltip.switch_remove_slot", "Remove o último passo desta sequência de troca." },
            { "tooltip.switch_clear", "Limpa o gatilho, todas as teclas e atrasos desta troca." },
            { "tooltip.switch_remove_lane", "Limpa e remove este card de troca." },
            { "tooltip.switch_add_lane", "Adiciona um novo bloco de troca independente (até 5 trocas)." },
            { "tooltip.song_add_slot", "Adiciona mais uma música à sequência deste macro (até 8 músicas)." },
            { "tooltip.song_remove_slot", "Remove a última música da sequência." },
            { "tooltip.song_clear", "Limpa o gatilho, instrumento, adaga e todas as músicas deste macro." },
            { "tooltip.song_remove_lane", "Limpa e remove este macro musical." },
            { "tooltip.song_add_lane", "Adiciona outro macro musical independente (até 4 macros)." },
            { "tooltip.atkdef_trigger", "Tecla da Skill (Gatilho): SEGURE esta tecla no jogo para equipar o set ATK e spammar a skill; SOLTE para equipar automaticamente o set DEF." },
            { "tooltip.atkdef_delay", "Delay do Spammer (ms): intervalo entre cada disparo da skill enquanto você mantém a tecla pressionada (ex.: 10ms)." },
            { "tooltip.atkdef_switch_delay", "Delay da Troca (ms): pausa entre cada peça de equipamento ao vestir o set ATK ou voltar para o set DEF (ex.: 40–60ms evita falhar troca)." },
            { "tooltip.atkdef_clear_def", "Limpa todas as 10 teclas configuradas no set de Defesa (DEF)." },
            { "tooltip.atkdef_clear_atk", "Limpa todas as 10 teclas configuradas no set de Ataque (ATK)." },
            { "tooltip.profile_name", "Digite o nome do novo perfil (ex.: 'Algoz PvP', 'Sniper MVP', 'Bardo WoE')." },
            { "tooltip.profile_create", "Cria um novo perfil em branco com o nome digitado." },
            { "tooltip.profile_rename", "Renomeia o perfil selecionado na lista." },
            { "tooltip.profile_copy", "Duplica o perfil selecionado com todas as suas teclas e configurações." },
            { "tooltip.profile_delete", "Exclui permanentemente o perfil selecionado (o perfil Default não pode ser removido)." },
            { "tooltip.profile_export", "Exporta apenas o perfil selecionado para um arquivo .json compartilhável." },
            { "tooltip.profile_import", "Importa um perfil .json salvo anteriormente." },
            { "tooltip.profile_backup", "Compacta TODOS os seus perfis e configurações em um único arquivo .zip de backup." },
            { "tooltip.profile_restore", "Restaura perfis e configurações a partir de um arquivo .zip de backup." },
            { "tooltip.profile_conflicts", "Verifica se a mesma tecla foi configurada em duas funções diferentes no perfil atual (ex.: Autopot + Spammer)." },
            { "tooltip.profile_inspect_status", "Mostra ao vivo todos os IDs de buffs e debuffs (EFST) lidos da memória do AureumRO.exe neste momento — útil para diagnosticar qualquer skill ou status." },
            { "tooltip.profile_autoswitch", "Carrega automaticamente o perfil vinculado ao detectar o nome do personagem logado no jogo (quando em OFF)." },
            { "tooltip.profile_use_char", "Lê automaticamente o nome do personagem logado na janela selecionada do AureumRO." },
            { "tooltip.profile_save_char", "Vincula o nome do personagem digitado ao perfil selecionado na lista." },
            { "tooltip.profile_clear_char", "Remove o vínculo de personagem do perfil selecionado." },
            { "msg.client_unsupported", "Este cliente não é suportado. Apenas Spammers e Macros vão funcionar." },
            { "msg.profile_load_error", "Erro ao carregar o novo perfil.\nDetalhes: {0}" },

            // ToggleApplicationStateForm
            { "tooltip.focus_only", "Pausar automações quando a janela do Ragnarok não estiver em foco" },
            { "tooltip.chat_pause", "Pausa de Chat: aperte Enter no Ragnarok para pausar skills ao digitar (Autopot em F1–F24 pota direto com chat aberto; em teclas de letra/número fecha o chat automaticamente para potar)" },
            { "status.press_to_start", "Aperte a tecla p/ iniciar!" },
            { "status.press_to_stop", "Aperte a tecla p/ parar!" },
            { "status.chat_paused", "Pausa de chat (Esc/Enter)" },
            { "status.client_closed", "Cliente fechado!" },
            { "status.select_client", "Selecione o cliente RO!" },
            { "tray.close", "Fechar" },

            // AHKForm
            { "msg.ahk_clear_confirm", "Limpar todas as teclas configuradas no Skill Spammer deste perfil?" },
            { "title.ahk_clear", "Limpar Teclas" },

            // Autobuff Forms
            { "skill.all_classes", "Todas as Classes" },
            { "tooltip.skill_class_filter", "Filtre as habilidades por classe (ex.: Gatuno, Noviço, Mago, Mercador, Taekwon)." },
            { "tooltip.min_sp", "Pausar Autobuff quando o SP estiver abaixo desta % (0 = desativado)" },
            { "tooltip.buff_cd", "Tempo de recarga por buff em milissegundos (ex.: 1500ms evita uso duplo)" },
            { "tooltip.pre_renewal", "Exibe apenas skills de Pré-Renewal / Transclasses (oculta 3ª/4ª classe, Doram, Night Watch, Hyper Novice e Soul Ascetic)" },
            { "tooltip.pre_renewal_items", "Exibe apenas consumíveis de Pré-Renewal / Gold Times (oculta Runas de RK, comidas 3rd, doces de 4ª classe e itens custom)" },
            { "tooltip.item_origin", "Filtre entre todos os consumíveis, apenas Itens Comuns ou apenas Itens de ROPs" },
            { "filter.items_all", "Todos (Comuns + ROPs)" },
            { "filter.items_common", "Apenas Itens Comuns" },
            { "filter.items_rops", "Apenas Itens de ROPs" },
            { "tooltip.pause_sitting", "Pausa Autobuffs (skills e consumíveis) automaticamente enquanto o personagem estiver sentado (EFST_SIT)" },
            { "tooltip.auto_self_click", "Envia um clique automático no centro da janela (em cima do seu personagem) ao usar skills de alvo em si mesmo: Bênção, Aumentar Agilidade, Assumptio, Kyrie Eleison, Sufrágio, Impositio Manus, Aspersio, Proteção Química Total e skills Ka (Kaahi/Kaupe/Kaite/Kaizel)." },
            { "msg.clear_skills_confirm", "Limpar todas as teclas de skills de Autobuff nesta aba?" },
            { "msg.clear_stuffs_confirm", "Limpar todas as teclas de itens de Autobuff nesta aba?" },

            // MacroSwitchForm tooltips
            { "tooltip.switch_trigger_key", "Tecla 1 (gatilho): aperte esta tecla no jogo para executar a sequência de troca" },
            { "tooltip.switch_trigger_dedicated", "Tecla gatilho: aperte esta tecla no jogo para executar a sequência de equipamentos abaixo" },
            { "tooltip.switch_step_key", "Próxima tecla enviada automaticamente na sequência desta troca" },
            { "tooltip.switch_delay", "Atraso em milissegundos (ms) antes de pressionar esta tecla" },
            { "tooltip.switch_loop", "Repete continuamente a sequência ao apertar o gatilho (aperte de novo para parar)" },

            // MacroSongForm tooltips
            { "tooltip.song_trigger", "Tecla gatilho: aperte no jogo para executar esta sequência musical" },
            { "tooltip.song_delay", "Atraso em milissegundos (ms) entre cada música da sequência" },
            { "tooltip.song_step", "Tecla da música/dança nesta etapa da sequência" },
            { "tooltip.song_dagger", "Tecla da Adaga/Arma secundária (equipada após cada música para cancelar animação)" },
            { "tooltip.song_instrument", "Tecla do Instrumento/Chicote (equipado antes de tocar cada música)" },
            { "tooltip.atkdef_click", "Envia clique esquerdo junto com a tecla de ataque (marque para skills de alvo/chão; desmarque para skills sem alvo)" },

            // ATKDEFForm equipment slot tooltips
            { "equip.hat_top", "Cabeça (Topo)" },
            { "equip.hat_mid", "Cabeça (Meio)" },
            { "equip.hat_low", "Cabeça (Baixo)" },
            { "equip.armor", "Armadura" },
            { "equip.weapon", "Arma" },
            { "equip.shield", "Escudo" },
            { "equip.garment", "Capa / Manto" },
            { "equip.shoes", "Calçado / Botas" },
            { "equip.acc_left", "Acessório Esquerdo" },
            { "equip.acc_right", "Acessório Direito" },

            // ProfileForm
            { "label.profile_prefix", "Perfil: {0}" },
            { "title.active_char", "Personagem Ativo" },
            { "msg.select_client_first", "Selecione um cliente Ragnarok suportado primeiro para ler o nome do personagem ativo." },
            { "msg.char_not_detected", "Não foi possível detectar o nome do personagem ativo no cliente Ragnarok." },
            { "title.char_binding", "Vínculo de Personagem" },
            { "msg.binding_cleared", "Vínculo de personagem removido para o perfil '{0}'." },
            { "msg.binding_saved", "Perfil '{0}' vinculado ao personagem '{1}'." },
            { "msg.binding_save_error", "Falha ao salvar vínculo de personagem: {0}" },
            { "msg.profile_select_delete", "Nenhum perfil selecionado! Selecione um perfil na lista para remover." },
            { "msg.profile_cannot_delete_default", "Não é possível remover o perfil Default!" },
            { "msg.profile_select_rename", "Selecione um perfil na lista para renomear." },
            { "msg.profile_cannot_rename_default", "Não é possível renomear o perfil Default!" },
            { "msg.profile_select_copy", "Selecione um perfil na lista para copiar." },
            { "msg.profile_cannot_copy_default", "Não é possível copiar o perfil Default!" },
            { "filter.profile_json", "Perfil AureumTools (*.json)|*.json" },
            { "title.export_profile", "Exportar Perfil" },
            { "msg.export_success", "Perfil exportado com sucesso!" },
            { "msg.export_error", "Falha ao exportar perfil: {0}" },
            { "title.import_profile", "Importar Perfil" },
            { "msg.import_success", "Perfil '{0}' importado com sucesso!" },
            { "msg.import_error", "Falha ao importar perfil: {0}" },
            { "filter.backup_zip", "Backup AureumTools (*.zip)|*.zip" },
            { "title.backup_all", "Backup Completo" },
            { "msg.backup_success", "Backup criado com sucesso ({0} arquivo(s) exportado(s))!" },
            { "msg.backup_error", "Falha ao criar backup: {0}" },
            { "title.restore_backup", "Restaurar Backup" },
            { "msg.restore_confirm", "Restaurar perfis, servidores customizados e configurações deste backup?\nPerfis existentes com o mesmo nome serão atualizados." },
            { "msg.restore_summary", "Backup restaurado!\nPerfis restaurados: {0}\nServidores customizados restaurados: {1}\nConfigurações restauradas: {2}" },
            { "msg.restore_error", "Falha ao restaurar backup: {0}" },
            { "title.key_conflicts", "Inspetor de Conflito de Teclas" },
            { "title.status_inspect", "Status Ativos na Memória (EFST IDs)" },
            { "status_inspect.empty", "Nenhum status/buff ativo encontrado no buffer de memória agora." },
            { "status_inspect.total", "Total de status ativos detectados: {0}" },

            // ServersForm & AddServerForm
            { "msg.server_delete_confirm", "Tem certeza que deseja excluir este servidor?" },
            { "msg.server_deleted", "Servidor {0} excluído com sucesso!" },
            { "title.edit_server", "Editar Servidor {0}" },
            { "msg.server_added", "Servidor {0} adicionado com sucesso!" },
            { "msg.server_saved", "Servidor {0} salvo com sucesso!" },
            { "title.test_success", "Teste de Endereço OK" },
            { "title.test_result", "Teste de Endereço" },
            { "label.test_ok_inline", "OK: {0}\nHP {1}/{2}" },
            { "label.test_failed_inline", "Teste falhou. Veja detalhes." },
            { "label.connected", "Conectado" },

            // Client.TestMemoryAddresses
            { "test.empty_process", "O nome do processo não pode ficar vazio." },
            { "test.invalid_hp", "Endereço HP inválido. Informe um valor hexadecimal de 1 a 8 dígitos." },
            { "test.invalid_name", "Endereço Nome inválido. Informe um valor hexadecimal de 1 a 8 dígitos." },
            { "test.process_not_running", "O processo '{0}' não está em execução. Abra o cliente do jogo primeiro." },
            { "test.open_failed", "Falha ao abrir a memória do processo '{0}'. Tente executar o AureumTools como Administrador." },
            { "test.read_error", "Erro ao ler memória do processo: {0}" },
            { "test.ok_report", "Conexão OK!\nPersonagem: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nBuffs ativos: {5}" },
            { "test.not_detected", "(não detectado)" },
            { "test.empty_values", "Os endereços retornaram 0/vazio (HP: {0}/{1}, Nome: vazio).\nVerifique se está logado em um mapa e se os endereços correspondem a este cliente." },

            // KeybindConflictDetector
            { "conflicts.none", "Nenhum conflito de tecla detectado no perfil '{0}'!" },
            { "conflicts.header", "Encontrado(s) {0} conflito(s) de tecla no perfil '{1}':" },
            { "usage.toggle", "Tecla Ligar/Desligar" },
            { "usage.hold_pause", "Segurar para Pausar (Hold-to-Pause)" },
            { "usage.autopot_hp", "Autopot (HP)" },
            { "usage.autopot_sp", "Autopot (SP)" },
            { "usage.ygg_hp", "Autopot Yggdrasil (HP)" },
            { "usage.ygg_sp", "Autopot Yggdrasil (SP)" },
            { "usage.ahk_click", "Spam de Skills (Com clique)" },
            { "usage.ahk_noclick", "Spam de Skills (Sem clique)" },
            { "usage.autobuff", "Autobuff ({0})" },
            { "usage.status_recovery", "Recuperação de Status" },
            { "usage.debuff_recovery", "Recuperação de Debuffs" },
            { "usage.skill_timer", "Timer de Skill {0}" },
            { "usage.atkdef_spam", "ATK/DEF Spammer" },
            { "usage.atkdef_atk", "ATK/DEF (Equip. ATK)" },
            { "usage.atkdef_def", "ATK/DEF (Equip. DEF)" },
            { "usage.song_macro", "Macro Músicas" },
            { "usage.macro_switch", "Macro Troca" },
            { "usage.macro_trigger", "{0} (Linha {1} Gatilho)" },
            { "usage.macro_instrument", "{0} (Instrumento)" },
            { "usage.macro_dagger", "{0} (Adaga)" },
            { "usage.macro_slot", "{0} (Linha {1} Slot)" },
            { "usage.mount_key", "Montaria (Tecla da Rédea)" },
            { "usage.refresh_hotkey", "Anti-Bug Position (@refresh)" },
            { "usage.escape_key", "Auto-Fuga (Asa de Mosca / Teleporte)" },

            // MountForm & Action Priority
            { "mount.priority_order", "1º Input do Jogador → 2º Fruto de Yggdrasil / Panaceia → 3º Consumíveis → 4º Auto-skills" },
            { "tooltip.mount_enabled", "Ativa o controle automático de Montaria (Rédea / Halter Lead — EFST_ALL_RIDING) quando o AureumTools estiver ligado." },
            { "tooltip.mount_key", "Tecla do atalho da Rédea / Montaria na sua barra do jogo." },
            { "tooltip.mount_dismount_delay", "Pausa em milissegundos (ms) após apertar a tecla da montaria para esperar o servidor desmontar antes de lançar a habilidade (padrão: 80ms)." },
            { "tooltip.mount_dismount_on_skill", "Quando marcado, se você estiver montado na Rédea e usar uma habilidade (manualmente ou pelo AureumTools), desce da montaria automaticamente e executa a skill!" },
            { "tooltip.mount_pause_autobuff", "Se marcado, o Auto-skill aguarda enquanto você estiver montado em vez de desmontar você durante o deslocamento." },
            { "tooltip.mount_auto_remount", "Opcional: remonta automaticamente após alguns segundos sem usar habilidades (somente se foi o AureumTools quem desmontou você)." },
            { "tooltip.mount_remount_delay", "Tempo de inatividade em milissegundos (ms) após o último uso de skill para remontar automaticamente (ex.: 500ms, 1000ms)." },
            { "tooltip.mount_include_tabs", "Detecta automaticamente todas as teclas já configuradas no Spammer, Auto-skill, Temporizadores, ATK x DEF e Macro Músicas para desmontar ao apertar." },
            { "tooltip.mount_extra_skill", "Tecla extra de habilidade usada manualmente no jogo: ao apertar montado, o AureumTools desce da montaria e dispara a habilidade." },
            { "tooltip.mount_clear_extra", "Limpa as 6 teclas extras de habilidade." },
            { "tooltip.priority_player_yield", "Tempo em milissegundos (ms) que o AureumTools aguarda sempre que você aperta uma tecla no jogo, garantindo prioridade total às suas ações (padrão: 120ms; 0 = desativado)." },
            { "tooltip.hold_pause_key", "Tecla de pausa momentânea (Hold-to-Pause): enquanto você SEGURAR esta tecla (ex.: Ctrl, Shift, Alt, Tab, CapsLock), o AureumTools pausa na hora e retoma ao soltar. Aperte Esc/Backspace para limpar." },
            { "tooltip.hold_pause_autopot", "Por padrão, Autopot, Yggdrasil e Auto-Fuga continuam protegendo você enquanto segura a tecla de pausa. Marque esta opção para pausar 100% de tudo." },
            { "tooltip.priority_order", "Hierarquia fixa de prioridade: 1º Input do Jogador → 2º Fruto de Yggdrasil / Panaceia → 3º Consumíveis → 4º Auto-skills." },

            // Anti-Position Bug (@refresh)
            { "refresh.hint", "Dica: envia Enter → @refresh → Enter direto no chat (não dispara enquanto você digita)." },
            { "utilities.info_body", "• Atalho (1 toque): corrige position bug na hora durante WoE/PvP/MVP (funciona mesmo em cidade ou sentado, com debounce de 1,5s).\n• Auto periódico: ressincroniza automaticamente no intervalo definido, pausando no chat, em cidade e ao sentar.\n• Segurança no chat: pausa teclas de letra do AureumTools durante o envio (~80ms) para não corromper @refresh, mantendo Autopot/Ygg (F1–F24) ativos." },
            { "tooltip.refresh_enabled", "Ativa o recurso Anti-Bug Position (@refresh) enquanto o AureumTools estiver ligado (ON)." },
            { "tooltip.refresh_cmd", "Comando enviado no chat do jogo para ressincronizar sua posição (padrão: @refresh)." },
            { "tooltip.refresh_pause_sit", "Se marcado, o @refresh automático periódico não dispara enquanto o personagem estiver sentado." },
            { "tooltip.refresh_hotkey", "Tecla de atalho de 1 toque: ao apertar no jogo, envia @refresh na hora para corrigir bug de posição." },
            { "tooltip.refresh_clear", "Limpa a tecla de atalho do @refresh." },
            { "tooltip.refresh_auto", "Opcional: dispara @refresh automaticamente de tempos em tempos (pausa em cidade e no chat)." },
            { "tooltip.refresh_interval", "Intervalo em segundos entre cada @refresh automático (mínimo: 3s, padrão: 30s)." },
            { "tooltip.refresh_test", "Envia o comando @refresh agora para testar na janela do jogo conectada." },

            // Emergency Auto-Escape
            { "escape.hint", "Dica: não dispara morto (HP = 0); coloque a tecla em F1–F24 para funcionar mesmo com chat aberto." },
            { "tooltip.escape_enabled", "Ativa o Auto-Fuga de Emergência: usa Asa de Mosca, Asa de Borboleta ou Teleporte quando o HP cair abaixo da % definida." },
            { "tooltip.escape_key", "Tecla da Asa de Mosca, Asa de Borboleta ou habilidade Teleporte na sua barra do jogo." },
            { "tooltip.escape_clear", "Limpa a tecla de Auto-Fuga." },
            { "tooltip.escape_hp", "Porcentagem crítica de HP abaixo da qual o AureumTools aciona a fuga automaticamente (ex.: 20%)." },
            { "tooltip.escape_cooldown", "Intervalo mínimo em milissegundos (ms) entre disparos para evitar gastar várias asas durante o teleporte (padrão: 2000ms)." },
            { "tooltip.escape_confirm_enter", "Marque se usar a skill Teleporte (Noviço/Sacerdote/Carta Creamy): pressiona Enter automaticamente 80ms após a tecla para confirmar." },
            { "tooltip.escape_pause_town", "Mantém o Auto-Fuga desativado dentro de cidades para evitar gastar asas acidentalmente." },

            // Multi-Account 3-in-1 Shell
            { "shell.account_tab", "Conta {0}" },
            { "shell.start_all", "Ligar Todas" },
            { "shell.stop_all", "Parar Todas" },
            { "tooltip.shell_account_tab", "Alterna para a Conta {0} (cada aba possui Cliente, Perfil, ON/OFF e automações independentes)." },
            { "tooltip.shell_start_all", "Liga todas as contas que já possuem uma janela do AureumRO selecionada." },
            { "tooltip.shell_stop_all", "Desliga imediatamente todas as contas ativas (emergência)." },

            // Aureum Hub (Hora da Votação + Coleção de Cartas + Coleção de Mapas)
            { "aureum.sub_vote", "Hora da Votação" },
            { "aureum.sub_collection", "Coleção" },
            { "aureum.sub_cards", "Cartas ({0})" },
            { "aureum.sub_maps", "Mapas ({0})" },
            { "aureum.open_vote_page", "Abrir aureumro.com/votar" },
            { "aureum.mark_all_voted", "Marcar Todos (12h)" },
            { "aureum.copy_vote_shop", "/navi Loja de Votos" },
            { "aureum.sync_topsites", "Sincronizar" },
            { "aureum.vote_site_meta", "+{0} ponto · cooldown {1}h · entrega automática in-game" },
            { "aureum.vote_site_schedule", "Último voto: {0} · Próximo: {1}" },
            { "aureum.vote_last_time", "Últ. voto:" },
            { "aureum.vote_apply_time", "Definir" },
            { "aureum.vote_last_time_tip", "Informe a hora da última votação (ex.: 14:30) para calcular automaticamente o tempo restante." },
            { "aureum.vote_invalid_time", "Informe um horário válido no formato HH:mm (ex.: 14:30)." },
            { "aureum.vote_now", "Votar (+{0}h)" },
            { "aureum.vote_reset", "Zerar" },
            { "aureum.vote_ready", "DISPONÍVEL AGORA" },
            { "aureum.vote_reminder", "Avisar quando liberar novo voto" },
            { "aureum.vote_sound", "Tocar som no lembrete" },
            { "aureum.vote_info", "Vote logado em aureumro.com/votar para receber os pontos in-game, ou informe o horário da última votação (HH:mm) para acompanhar o tempo restante." },
            { "aureum.vote_alert_title", "AureumRO — Hora da Votação!" },
            { "aureum.vote_alert_body", "Novo voto disponível em: {0}. Abra aureumro.com/votar para garantir seus pontos!" },
            { "aureum.all_categories", "Todas as Categorias" },
            { "aureum.all_cities", "Todas as Cidades" },
            { "aureum.all_bonuses", "Todos os Bônus" },
            { "aureum.status_all", "Todos" },
            { "aureum.status_pending", "Pendentes" },
            { "aureum.status_complete", "Completos" },
            { "aureum.copy_card_npc", "/navi Colecionador" },
            { "aureum.hide_collected", "Ocultar obtidas" },
            { "aureum.mark_visible", "Marcar visíveis" },
            { "aureum.clear_visible", "Desmarcar visív." },
            { "aureum.mark_collection", "Marcar 5" },
            { "aureum.unmark_collection", "Desmarcar 5" },
            { "aureum.cards_progress", "Progresso: {0}/{1} cartas · {2}/{3} coleções" },
            { "aureum.card_click_hint", "Clique para marcar ou desmarcar como já colecionada (fica tachada)." },
            { "aureum.cards_search_tip", "Pesquise por nome da carta, nome da coleção ou bônus..." },
            { "aureum.maps_by_city", "Por Cidade" },
            { "aureum.maps_by_bonus", "Por Tipo de Bônus" },
            { "aureum.maps_progress", "Progresso: {0}/{1} mapas concluídos ({2} exibidos)" },
            { "aureum.maps_search_tip", "Pesquise por mapa, código (ex.: prt_monk), cidade, bônus ou item de drop..." },
            { "aureum.col_ref", "Ref." },
            { "aureum.col_city", "Cidade" },
            { "aureum.col_bonus_type", "Tipo de Bônus" },
            { "aureum.col_map", "Mapa" },
            { "aureum.col_code", "Código" },
            { "aureum.col_directions", "Como chegar" },
            { "aureum.col_bonus", "Bônus" },
            { "aureum.copy_warp", "Copiar código" },
            { "aureum.map_select_hint", "Selecione um mapa para ver a rota passo a passo e todos os itens necessários (@mc)." },
            { "aureum.copied", "Copiado!" },
            { "aureum.no_results", "Nenhum item encontrado para o filtro atual." },
            { "tooltip.aureum_sub_vote", "Acompanhe o tempo restante dos topsites oficiais e receba aviso quando puder votar novamente." },
            { "tooltip.aureum_sub_collection", "Acompanhe seu progresso na Coleção de Cartas (28) e Coleção de Mapas (311) do AureumRO." },
            { "tooltip.aureum_sub_cards", "Exibe todas as 28 coleções de cartas; clique em cada carta para marcar/tachar." },
            { "tooltip.aureum_sub_maps", "Exibe todos os 311 mapas com cidade de referência, setas de direção e itens necessários." },
            { "tooltip.aureum_open_vote", "Abre a página oficial de votação do AureumRO no navegador." },
            { "tooltip.aureum_mark_all_voted", "Inicia o timer de 12 horas em todos os topsites listados de uma vez." },
            { "tooltip.aureum_sync_topsites", "Atualiza a lista de topsites oficiais diretamente da API do AureumRO." },
            { "tooltip.aureum_vote_reminder", "Exibe uma notificação na área de trabalho assim que um topsite liberar novo voto." },
            { "tooltip.aureum_vote_sound", "Reproduz um alerta sonoro junto com o aviso de voto liberado." },
            { "tooltip.aureum_card_cat", "Filtra as coleções de cartas por categoria de bônus." },
            { "tooltip.aureum_card_status", "Mostra todas as coleções, apenas pendentes ou apenas completas." },
            { "tooltip.aureum_hide_cards", "Oculta automaticamente as cartas que você já marcou como obtidas." },
            { "tooltip.aureum_mark_vis_cards", "Marca como obtidas todas as cartas atualmente visíveis pelo filtro." },
            { "tooltip.aureum_clear_vis_cards", "Desmarca todas as cartas atualmente visíveis pelo filtro." },
            { "tooltip.aureum_map_mode", "Alterna o filtro entre agrupar por Cidade de referência ou por Tipo de Bônus." },
            { "tooltip.aureum_map_filter", "Filtra os mapas pela cidade ou categoria de bônus selecionada." },
            { "tooltip.aureum_map_status", "Mostra todos os mapas, apenas pendentes ou apenas concluídos." },
            { "tooltip.aureum_mark_vis_maps", "Marca como concluídos todos os mapas atualmente exibidos na tabela." },
            { "tooltip.aureum_clear_vis_maps", "Desmarca todos os mapas atualmente exibidos na tabela." }
        };

        private static readonly Dictionary<string, string> MessagesEn = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "title.warning", "Warning" },
            { "title.error", "Error" },
            { "title.confirm", "Confirm" },
            { "title.message", "Message" },
            { "title.yes", "Yes" },
            { "title.no", "No" },

            { "tooltip.dark_mode", "Switch between Light and Dark interface themes (click Sun/Moon)" },
            { "tooltip.mini_hud", "Show a compact floating overlay with status and HP/SP bars" },
            { "tooltip.top_most", "Keep the AureumTools window pinned above Ragnarok" },
            { "tooltip.use_buffs_in_town", "When checked (default), pauses Autopot (HP/SP), Auto-skill, Consumables, Skill Timers, and Anti-Debuffs while in towns. Uncheck to keep automations active in towns." },
            { "tooltip.language", "Switch interface language (Portuguese / English)" },
            { "tooltip.refresh_clients", "Refresh open Ragnarok clients" },
            { "tooltip.website", "Open the official AureumRO website (aureumro.com)" },
            { "tooltip.wiki", "Open the official AureumRO Wiki" },
            { "tooltip.discord", "Join the official AureumRO Discord server" },
            { "tooltip.github", "Open the project repository on GitHub" },
            { "footer.website", "Website" },
            { "footer.wiki", "Wiki" },
            { "footer.powered", "✦ AureumTools · Developed by Oráculo  •  Minimize to send to system tray" },
            { "sidebar.status_on", "Automation active" },
            { "sidebar.status_chat", "Paused (Chat)" },
            { "sidebar.status_town", "Paused (Town)" },
            { "sidebar.status_off", "Ready (OFF)" },
            { "sidebar.profile_prefix", "Profile: {0}" },
            { "sidebar.client_connected", "Client: {0}" },
            { "sidebar.client_none", "Client: not selected" },
            { "section.spammer.title", "Skill Spammer" },
            { "section.spammer.subtitle", "Configure rapid-cast skill keys, spammer delay, and player-input priority timing." },
            { "section.debuff.title", "Anti-Debuffs" },
            { "section.debuff.subtitle", "Automatically cure negative status effects removable by items or skills." },
            { "section.autobuff.title", "Auto-skill" },
            { "section.autobuff.subtitle", "Automatically renew class skills and consumables (Common and ROPs) in one tab." },
            { "section.buff_skills.title", "Auto-skill" },
            { "section.buff_skills.subtitle", "Keep class buffs active with search filtering, minimum SP%, and recast cooldown." },
            { "section.buff_stuffs.title", "Consumables" },
            { "section.buff_stuffs.subtitle", "Automatically renew potions, foods, elemental converters, and buff items." },
            { "section.timers.title", "Skill Timers" },
            { "section.timers.subtitle", "Trigger up to 6 skills automatically on fixed intervals (in seconds)." },
            { "section.macro_switch.title", "Equipment Switch Macro" },
            { "section.macro_switch.subtitle", "Build fast equipment switch chains with customizable per-step delays." },
            { "section.macro_songs.title", "Song Macros" },
            { "section.macro_songs.subtitle", "Automate Bard/Dancer song chains with automatic instrument/dagger swaps." },
            { "section.atkdef.title", "ATK x DEF Mode" },
            { "section.atkdef.subtitle", "Automatically swap between offensive and defensive gear while spamming skills." },
            { "section.macros.title", "Macros" },
            { "section.macros.subtitle", "Manage Switch Macro, Song Macro, and ATK x DEF Macro in one unified tab." },
            { "section.mount.title", "Mount" },
            { "section.mount.subtitle", "Automatically dismount Halter Lead mounts when casting skills and manage extra skill keys." },
            { "section.utilities.title", "Utilities" },
            { "section.utilities.subtitle", "Anti-Position Bug (@refresh) and Emergency Auto-Escape (Fly Wing / Teleport)." },
            { "section.aureum.title", "Aureum Hub" },
            { "section.aureum.subtitle", "Vote Timer (12h + reminder), Card Collections (28), and Map Collections (311)." },
            { "section.profiles.title", "Profiles, Backup & Diagnostics" },
            { "section.profiles.subtitle", "Manage profiles, auto-switch by character, full .zip backups, and keybind conflicts." },
            { "section.guide.title", "Complete AureumTools Guide" },
            { "section.guide.subtitle", "Learn every tab, macro mode, and tip — hover over any field in the app for a quick popup." },
            { "section.servers.title", "Supported Servers" },
            { "section.servers.subtitle", "Add, edit, and live-test HP/Name memory addresses for custom Ragnarok servers." },
            { "tooltip.process_select", "Select the open AureumRO.exe window to automate (auto-selected when only 1 window is open)." },
            { "tooltip.profile_select", "Quickly switch between saved profiles (each stores Autopot, Spammer, Buffs, Debuffs, Timers, and Macros)." },
            { "tooltip.tab_autopot", "Standard Autopot: uses regular potions (White, Blue, Mastela, etc.) when HP or SP drops below your threshold %." },
            { "tooltip.tab_ygg", "Yggdrasil Autopot (emergency): set a lower % than standard Autopot to save Yggdrasil Berry/Seed for critical moments." },
            { "tooltip.vital_hp", "Current character HP read live from AureumRO memory." },
            { "tooltip.vital_sp", "Current character SP read live from AureumRO memory." },
            { "tooltip.session_card", "Live session summary: automation state (ON/OFF/Chat/Town), active profile, and connected client." },
            { "tooltip.nav_spammer", "Skill Spammer: hold a key in-game to rapidly fire skills (with or without automatic mouse clicks)." },
            { "tooltip.nav_debuff", "Anti-Debuffs: automatically cures the 7 item/skill-curable statuses (Panacea, Royal Jelly, Green Pot, Holy Water, Increase AGI)." },
            { "tooltip.nav_autobuff", "Auto-skill: combines class Skills and Consumables (Common and ROPs) with automatic renewal." },
            { "tooltip.autobuff_tab_skills", "Skills: automatically recasts class buffs whenever their status icon expires in-game." },
            { "tooltip.autobuff_tab_items", "Consumables: automatically refreshes ASPD pots, foods, elemental converters, boxes, and scrolls (Common and ROPs)." },
            { "tooltip.nav_buff_skills", "Auto-skill: automatically recasts class buffs whenever their status icon expires in-game." },
            { "tooltip.nav_buff_stuffs", "Consumables: automatically refreshes ASPD pots, foods, elemental converters, boxes, and scrolls (Common and ROPs)." },
            { "tooltip.nav_timers", "Skill Timers: presses up to 6 keys automatically on fixed second intervals." },
            { "tooltip.nav_macros", "Macros: combines Switch Macro (gear chains), Song Macro (Bard/Dancer), and ATK x DEF Macro (swap + spammer)." },
            { "tooltip.nav_mount", "Mount: automatically dismounts Halter Lead mounts when casting manual or automated skills." },
            { "tooltip.nav_utilities", "Utilities: Anti-Position Bug (@refresh) and Emergency Auto-Escape (Fly Wing / Teleport)." },
            { "tooltip.nav_aureum", "Aureum Hub: Vote Timer (12h + reminder), Card Collections (clickable strikethrough tracker), and Map Collections (with directional arrows & drops)." },
            { "tooltip.nav_profiles", "Profiles & Backup: create, rename, copy, export .zip backups, detect key conflicts, and auto-switch by character." },
            { "tooltip.nav_guide", "Guide / Help: complete searchable handbook explaining every feature in AureumTools." },
            { "tooltip.autopot_hp_key", "HP potion hotkey (e.g., White Potion, Mastela, Slim White). Press Backspace/Esc to clear." },
            { "tooltip.autopot_hp_pct", "HP percentage threshold below which the HP potion is used automatically (e.g., 80%)." },
            { "tooltip.autopot_sp_key", "SP potion hotkey (e.g., Blue Potion, Strawberry, Lemon). Press Backspace/Esc to clear." },
            { "tooltip.autopot_sp_pct", "SP percentage threshold below which the SP potion is used automatically (e.g., 40%)." },
            { "tooltip.autopot_ygg_hp_key", "Emergency HP hotkey (Yggdrasil Berry / Seed) for critical HP recovery." },
            { "tooltip.autopot_ygg_hp_pct", "Critical HP threshold to trigger Yggdrasil (set lower than standard Autopot, e.g., 35%)." },
            { "tooltip.autopot_ygg_sp_key", "Emergency SP hotkey (Yggdrasil Berry / Seed) for critical SP recovery." },
            { "tooltip.autopot_ygg_sp_pct", "Critical SP threshold to trigger Yggdrasil (e.g., 15%)." },
            { "tooltip.autopot_delay", "Delay in milliseconds (ms) between potion attempts (typical: 50–150ms)." },
            { "tooltip.toggle_btn", "Turns all AureumTools automations ON or OFF at once." },
            { "tooltip.toggle_key", "Global hotkey (e.g., End, Insert, F12) to toggle AureumTools ON/OFF directly from inside the game." },
            { "tooltip.toggle_audio", "Plays a short audio cue whenever automation is toggled ON or OFF." },
            { "tooltip.ahk_key", "Click the box to cycle 3 states:\n• Empty: disabled\n• Filled square (■): spam key only, no click (self-cast skills)\n• Checked (✓): spam key + mouse click (target/ground skills)" },
            { "tooltip.ahk_delay", "Delay in milliseconds (ms) between skill repeats while holding the key (lower = faster; e.g., 10–30ms)." },
            { "tooltip.ahk_compat", "Compatibility Mode: standard reliable input; supports 'No Shift' and 'Mouse Flick'." },
            { "tooltip.ahk_speed", "Speed Boost Mode: aggressive high-speed spam (disables 'No Shift' and 'Mouse Flick')." },
            { "tooltip.ahk_noshift", "No Shift: prevents holding Shift from interfering with spammed keys." },
            { "tooltip.ahk_flick", "Mouse Flick: adds a tiny cursor nudge between clicks to improve target/ground skill registration." },
            { "tooltip.ahk_clear", "Unchecks all configured Skill Spammer keys in this profile." },
            { "tooltip.buff_search", "Filter buffs by English or Portuguese name (e.g., 'Blessing', 'Asura', 'Converter', 'Food')." },
            { "tooltip.buff_clear_skills", "Clears all Skill Autobuff hotkeys on this tab (asks confirmation)." },
            { "tooltip.buff_clear_stuffs", "Clears all Consumable Autobuff hotkeys on this tab (asks confirmation)." },
            { "tooltip.timer_delay", "Interval in seconds between automatic presses of this key (e.g., 10 = press every 10 seconds)." },
            { "tooltip.timer_key", "Skill or item hotkey pressed periodically while automation is ON." },
            { "tooltip.timer_clear", "Clears this timer's key and resets its interval to 5 seconds." },
            { "tooltip.timer_remove", "Clears and hides this timer card." },
            { "tooltip.timer_add", "Adds another independent skill timer (up to 6 simultaneous timers)." },
            { "tooltip.macro_tab_switch", "Switch Macro: pressing a Trigger runs a sequence of gear/key presses with per-step delays (up to 5 chains)." },
            { "tooltip.macro_tab_songs", "Song Macro: automates Bard/Dancer song chains with automatic Dagger & Instrument swaps." },
            { "tooltip.macro_tab_atkdef", "ATK x DEF Macro: Gear Swap + Spammer! Hold the Skill key to equip ATK gear, spam the skill (with/without click), and release to swap back to DEF gear." },
            { "tooltip.switch_click", "When checked, also sends a left mouse click after pressing this step's key." },
            { "tooltip.switch_add_slot", "Adds another step to this switch sequence (up to 7 chained keys)." },
            { "tooltip.switch_remove_slot", "Removes the last step from this switch sequence." },
            { "tooltip.switch_clear", "Clears the trigger, all keys, and delays in this switch." },
            { "tooltip.switch_remove_lane", "Clears and removes this switch card." },
            { "tooltip.switch_add_lane", "Adds another independent switch chain (up to 5 switches)." },
            { "tooltip.song_add_slot", "Adds another song slot to this macro sequence (up to 8 songs)." },
            { "tooltip.song_remove_slot", "Removes the last song slot from this sequence." },
            { "tooltip.song_clear", "Clears the trigger, instrument, dagger, and all songs in this macro." },
            { "tooltip.song_remove_lane", "Clears and removes this song macro card." },
            { "tooltip.song_add_lane", "Adds another independent song macro (up to 4 macros)." },
            { "tooltip.atkdef_trigger", "Skill Key (Trigger): HOLD this key in-game to equip ATK gear and spam the skill; RELEASE to automatically equip DEF gear." },
            { "tooltip.atkdef_delay", "Spammer Delay (ms): interval between skill casts while holding the key (e.g., 10ms)." },
            { "tooltip.atkdef_switch_delay", "Switch Delay (ms): pause between each equipment piece when swapping to ATK or back to DEF (e.g., 40–60ms avoids dropped swaps)." },
            { "tooltip.atkdef_clear_def", "Clears all 10 hotkeys configured in the DEF equipment set." },
            { "tooltip.atkdef_clear_atk", "Clears all 10 hotkeys configured in the ATK equipment set." },
            { "tooltip.profile_name", "Enter a name for the new profile (e.g., 'SinX PvP', 'Sniper MVP', 'Clown WoE')." },
            { "tooltip.profile_create", "Creates a new blank profile with the entered name." },
            { "tooltip.profile_rename", "Renames the currently selected profile in the list." },
            { "tooltip.profile_copy", "Duplicates the selected profile with all its hotkeys and settings." },
            { "tooltip.profile_delete", "Permanently deletes the selected profile (Default cannot be deleted)." },
            { "tooltip.profile_export", "Exports only the selected profile to a shareable .json file." },
            { "tooltip.profile_import", "Imports a previously exported .json profile." },
            { "tooltip.profile_backup", "Bundles ALL profiles and settings into a single .zip backup archive." },
            { "tooltip.profile_restore", "Restores profiles and settings from a .zip backup archive." },
            { "tooltip.profile_conflicts", "Checks whether the same key is assigned to multiple actions in the current profile (e.g., Autopot + Spammer)." },
            { "tooltip.profile_inspect_status", "Displays live buff and debuff EFST IDs read from AureumRO.exe memory right now — useful for diagnosing any skill or debuff." },
            { "tooltip.profile_autoswitch", "Automatically loads the bound profile when the logged-in character name is detected (while OFF)." },
            { "tooltip.profile_use_char", "Automatically reads the logged-in character name from the selected AureumRO window." },
            { "tooltip.profile_save_char", "Binds the entered character name to the profile selected in the list." },
            { "tooltip.profile_clear_char", "Removes the character binding from the selected profile." },
            { "msg.client_unsupported", "This client is not supported. Only Spammers and macro will work." },
            { "msg.profile_load_error", "Error while loading the new profile.\nDetails: {0}" },

            { "tooltip.focus_only", "Pause automations when the Ragnarok window is not focused" },
            { "tooltip.chat_pause", "Chat-Safe Pause: press Enter in Ragnarok to pause skills while typing (F1–F24 Autopot fires directly with chat open; typing-key Autopot auto-closes chat to pot)" },
            { "status.press_to_start", "Press the key to start!" },
            { "status.press_to_stop", "Press the key to stop!" },
            { "status.chat_paused", "Chat paused (Esc/Enter)" },
            { "status.client_closed", "Client closed!" },
            { "status.select_client", "Please select the Ragnarok Client!!" },
            { "tray.close", "Close" },

            { "msg.ahk_clear_confirm", "Clear all configured Skill Spammer keys in this profile?" },
            { "title.ahk_clear", "Clear All Keys" },

            { "skill.all_classes", "All Classes" },
            { "tooltip.skill_class_filter", "Filter skills by class (e.g., Thief, Acolyte, Mage, Merchant, Taekwon)." },
            { "tooltip.min_sp", "Pause Autobuff when SP is below this % (0 = disabled)" },
            { "tooltip.buff_cd", "Per-buff recast cooldown in milliseconds (e.g. 1500ms prevents double-use)" },
            { "tooltip.pre_renewal", "Show only Pre-Renewal / Transcendent class skills (hides 3rd/4th jobs, Doram, Night Watch, Hyper Novice, and Soul Ascetic)" },
            { "tooltip.pre_renewal_items", "Show only Pre-Renewal / Gold Times consumables (hides RK Runes, 3rd foods, 4th class candies, and custom items)" },
            { "tooltip.item_origin", "Filter between all consumables, Common Items only, or ROPs Items only" },
            { "filter.items_all", "All (Common + ROPs)" },
            { "filter.items_common", "Common Items Only" },
            { "filter.items_rops", "ROPs Items Only" },
            { "tooltip.pause_sitting", "Automatically pauses Autobuffs (skills & consumables) while your character is sitting (EFST_SIT)" },
            { "tooltip.auto_self_click", "Automatically sends a click to the center of the RO window (your character) when casting target buffs on yourself: Blessing, Increase Agility, Assumptio, Kyrie Eleison, Suffragium, Impositio Manus, Aspersio, Full Chemical Protection, and Ka skills (Kaahi/Kaupe/Kaite/Kaizel)." },
            { "msg.clear_skills_confirm", "Clear all skill autobuff keys on this tab?" },
            { "msg.clear_stuffs_confirm", "Clear all consumable autobuff keys on this tab?" },

            { "tooltip.switch_trigger_key", "Key 1 (trigger): press this key in-game to run this switch chain" },
            { "tooltip.switch_trigger_dedicated", "Trigger key: press this key in-game to run the equipment sequence below" },
            { "tooltip.switch_step_key", "Next key pressed automatically in this switch chain" },
            { "tooltip.switch_delay", "Delay in milliseconds (ms) before pressing this key" },
            { "tooltip.switch_loop", "Continuously repeat this chain when pressing the trigger (press again to stop)" },

            { "tooltip.song_trigger", "Trigger key: press in-game to run this song sequence" },
            { "tooltip.song_delay", "Delay in milliseconds (ms) between each song in the sequence" },
            { "tooltip.song_step", "Song/dance skill hotkey for this step" },
            { "tooltip.song_dagger", "Dagger/Secondary weapon key (equipped after each song to cancel animation)" },
            { "tooltip.song_instrument", "Instrument/Whip key (equipped before playing each song)" },
            { "tooltip.atkdef_click", "Sends a left click with the attack spam key (check for target/ground skills; uncheck for self-cast skills)" },

            { "equip.hat_top", "Headgear (Top)" },
            { "equip.hat_mid", "Headgear (Mid)" },
            { "equip.hat_low", "Headgear (Low)" },
            { "equip.armor", "Armor" },
            { "equip.weapon", "Weapon" },
            { "equip.shield", "Shield" },
            { "equip.garment", "Garment" },
            { "equip.shoes", "Footgear" },
            { "equip.acc_left", "Left Accessory" },
            { "equip.acc_right", "Right Accessory" },

            { "label.profile_prefix", "Profile: {0}" },
            { "title.active_char", "Active Character" },
            { "msg.select_client_first", "Select a supported Ragnarok client first to read the active character name." },
            { "msg.char_not_detected", "Could not detect an active character name from the Ragnarok client." },
            { "title.char_binding", "Character Binding" },
            { "msg.binding_cleared", "Character binding cleared for profile '{0}'." },
            { "msg.binding_saved", "Profile '{0}' bound to character '{1}'." },
            { "msg.binding_save_error", "Failed to save character binding: {0}" },
            { "msg.profile_select_delete", "No profile selected! To delete a profile, first select an option from the Profile list." },
            { "msg.profile_cannot_delete_default", "Cannot delete the Default profile!" },
            { "msg.profile_select_rename", "To rename a profile, first select one from the Profile list." },
            { "msg.profile_cannot_rename_default", "Cannot rename the Default profile!" },
            { "msg.profile_select_copy", "To copy a profile, first select one from the Profile list." },
            { "msg.profile_cannot_copy_default", "Cannot copy the Default profile!" },
            { "filter.profile_json", "AureumTools Profile (*.json)|*.json" },
            { "title.export_profile", "Export Profile" },
            { "msg.export_success", "Profile exported successfully!" },
            { "msg.export_error", "Failed to export profile: {0}" },
            { "title.import_profile", "Import Profile" },
            { "msg.import_success", "Profile '{0}' imported successfully!" },
            { "msg.import_error", "Failed to import profile: {0}" },
            { "filter.backup_zip", "AureumTools Backup (*.zip)|*.zip" },
            { "title.backup_all", "Backup All" },
            { "msg.backup_success", "Backup created successfully ({0} file(s) exported)!" },
            { "msg.backup_error", "Failed to create backup: {0}" },
            { "title.restore_backup", "Restore Backup" },
            { "msg.restore_confirm", "Restore profiles, custom servers, and settings from this backup?\nExisting profiles with the same name will be updated." },
            { "msg.restore_summary", "Backup restored!\nProfiles restored: {0}\nCustom servers restored: {1}\nApp settings restored: {2}" },
            { "msg.restore_error", "Failed to restore backup: {0}" },
            { "title.key_conflicts", "Keybind Conflict Inspector" },
            { "title.status_inspect", "Active Status IDs in Memory (EFST)" },
            { "status_inspect.empty", "No active statuses/buffs found in the client memory buffer right now." },
            { "status_inspect.total", "Total active statuses detected: {0}" },

            { "msg.server_delete_confirm", "Are you sure want to delete this Server?" },
            { "msg.server_deleted", "Server {0} successfully deleted!" },
            { "title.edit_server", "Edit Server {0}" },
            { "msg.server_added", "Server {0} successfully added!" },
            { "msg.server_saved", "Server {0} successfully saved!" },
            { "title.test_success", "Address Test Successful" },
            { "title.test_result", "Address Test" },
            { "label.test_ok_inline", "OK: {0}\nHP {1}/{2}" },
            { "label.test_failed_inline", "Test failed. Check details." },
            { "label.connected", "Connected" },

            { "test.empty_process", "Process name cannot be empty." },
            { "test.invalid_hp", "HP Address is invalid. Enter a valid 1-8 digit hex value." },
            { "test.invalid_name", "Name Address is invalid. Enter a valid 1-8 digit hex value." },
            { "test.process_not_running", "Process '{0}' is not currently running. Launch the game client first." },
            { "test.open_failed", "Failed to open process memory for '{0}'. Try running AureumTools as Administrator." },
            { "test.read_error", "Error reading process memory: {0}" },
            { "test.ok_report", "Connection OK!\nCharacter: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nActive Buffs: {5}" },
            { "test.not_detected", "(not detected)" },
            { "test.empty_values", "Addresses read 0/empty (HP: {0}/{1}, Name: empty).\nMake sure you are logged in on a map and the hex addresses match this client." },

            { "conflicts.none", "No keybind conflicts detected in profile '{0}'!" },
            { "conflicts.header", "Found {0} keybind conflict(s) in profile '{1}':" },
            { "usage.toggle", "ON/OFF Toggle Hotkey" },
            { "usage.hold_pause", "Hold-to-Pause Key" },
            { "usage.autopot_hp", "Autopot (HP)" },
            { "usage.autopot_sp", "Autopot (SP)" },
            { "usage.ygg_hp", "Yggdrasil Autopot (HP)" },
            { "usage.ygg_sp", "Yggdrasil Autopot (SP)" },
            { "usage.ahk_click", "Skill Spammer (Click)" },
            { "usage.ahk_noclick", "Skill Spammer (No-Click)" },
            { "usage.autobuff", "Autobuff ({0})" },
            { "usage.status_recovery", "Status Recovery" },
            { "usage.debuff_recovery", "Debuff Recovery" },
            { "usage.skill_timer", "Skill Timer {0}" },
            { "usage.atkdef_spam", "ATK/DEF Spammer" },
            { "usage.atkdef_atk", "ATK/DEF (ATK Equip)" },
            { "usage.atkdef_def", "ATK/DEF (DEF Equip)" },
            { "usage.song_macro", "Song Macro" },
            { "usage.macro_switch", "Macro Switch" },
            { "usage.macro_trigger", "{0} (Lane {1} Trigger)" },
            { "usage.macro_instrument", "{0} (Instrument)" },
            { "usage.macro_dagger", "{0} (Dagger)" },
            { "usage.macro_slot", "{0} (Lane {1} Slot)" },
            { "usage.mount_key", "Mount (Halter Lead Key)" },
            { "usage.refresh_hotkey", "Anti-Position Bug (@refresh)" },
            { "usage.escape_key", "Auto-Escape (Fly Wing / Teleport)" },

            { "mount.priority_order", "1st Player Input → 2nd Yggdrasil Berry / Panacea → 3rd Consumables → 4th Auto-skills" },
            { "tooltip.mount_enabled", "Enables automatic Mount (Halter Lead — EFST_ALL_RIDING) management while AureumTools is ON." },
            { "tooltip.mount_key", "Hotkey assigned to your Halter Lead / Mount item in-game." },
            { "tooltip.mount_dismount_delay", "Delay in milliseconds (ms) after pressing the Mount key to let the server dismount before casting the skill (default: 80ms)." },
            { "tooltip.mount_dismount_on_skill", "When checked, automatically dismounts your Halter Lead mount before casting skills (both manual presses and AureumTools automations)!" },
            { "tooltip.mount_pause_autobuff", "When checked, pauses Auto-skill while mounted instead of dismounting you while traveling." },
            { "tooltip.mount_auto_remount", "Optional: automatically remounts after a few seconds without skill activity (only if AureumTools dismounted you)." },
            { "tooltip.mount_remount_delay", "Idle duration in milliseconds (ms) after the last skill cast before automatically remounting (e.g., 500ms, 1000ms)." },
            { "tooltip.mount_include_tabs", "Automatically tracks all skill keys already bound in Spammer, Auto-skill, Skill Timers, ATK x DEF, and Song Macros." },
            { "tooltip.mount_extra_skill", "Extra manual skill hotkey: pressing it while mounted automatically dismounts and fires the skill." },
            { "tooltip.mount_clear_extra", "Clears all 6 extra skill hotkeys." },
            { "tooltip.priority_player_yield", "Duration in milliseconds (ms) that AureumTools yields whenever you press a key in-game, giving your actions top priority (default: 120ms; 0 = disabled)." },
            { "tooltip.hold_pause_key", "Hold-to-Pause key: while HELD (e.g., Ctrl, Shift, Alt, Tab, CapsLock), AureumTools pauses immediately and resumes the moment you release it. Press Esc/Backspace to clear." },
            { "tooltip.hold_pause_autopot", "By default, Autopot, Yggdrasil, and Auto-Escape keep protecting you while holding the pause key. Check this to pause 100% of automations." },
            { "tooltip.priority_order", "Fixed priority hierarchy: 1st Player Input → 2nd Yggdrasil Berry / Panacea → 3rd Consumables → 4th Auto-skills." },

            // Anti-Position Bug (@refresh)
            { "refresh.hint", "Tip: sends Enter → @refresh → Enter directly in chat (never triggers while you are typing)." },
            { "utilities.info_body", "• 1-tap Hotkey: fixes position desync instantly in WoE/PvP/MVP (works in town or sitting, with a 1.5s debounce).\n• Periodic Auto: automatically resyncs on your chosen interval, pausing while chatting, in town, or sitting.\n• Chat Safety: pauses AureumTools letter keys during the ~80ms send so @refresh is never corrupted, while keeping F1–F24 Autopot/Ygg active." },
            { "tooltip.refresh_enabled", "Enables the Anti-Position Bug (@refresh) helper while AureumTools is ON." },
            { "tooltip.refresh_cmd", "Chat command sent to resync your character position (default: @refresh)." },
            { "tooltip.refresh_pause_sit", "When checked, periodic auto @refresh pauses while your character is sitting." },
            { "tooltip.refresh_hotkey", "1-tap hotkey: press it in-game to immediately send @refresh and fix position desync." },
            { "tooltip.refresh_clear", "Clears the @refresh hotkey." },
            { "tooltip.refresh_auto", "Optional: sends @refresh periodically on a fixed interval (pauses in town and while chatting)." },
            { "tooltip.refresh_interval", "Interval in seconds between automatic @refresh commands (minimum: 3s, default: 30s)." },
            { "tooltip.refresh_test", "Sends the @refresh command once right now to test on the connected game window." },

            // Emergency Auto-Escape
            { "escape.hint", "Tip: never triggers when dead (HP = 0); bind to F1–F24 to escape even while chat is open." },
            { "tooltip.escape_enabled", "Enables Emergency Auto-Escape: triggers your Fly Wing, Butterfly Wing, or Teleport key whenever HP drops below the set %." },
            { "tooltip.escape_key", "Hotkey assigned to Fly Wing, Butterfly Wing, or Teleport skill on your in-game hotbar." },
            { "tooltip.escape_clear", "Clears the Auto-Escape hotkey." },
            { "tooltip.escape_hp", "Critical HP percentage threshold below which AureumTools triggers escape automatically (e.g., 20%)." },
            { "tooltip.escape_cooldown", "Minimum cooldown in milliseconds (ms) between triggers so you don't waste multiple wings while teleporting (default: 2000ms)." },
            { "tooltip.escape_confirm_enter", "Enable when using the Teleport skill (Acolyte/Priest/Creamy Card): automatically presses Enter 80ms later to confirm." },
            { "tooltip.escape_pause_town", "Keeps Auto-Escape disabled in towns so you never accidentally burn wings in Prontera." },

            // Multi-Account 3-in-1 Shell
            { "shell.account_tab", "Account {0}" },
            { "shell.start_all", "Start All" },
            { "shell.stop_all", "Stop All" },
            { "tooltip.shell_account_tab", "Switch to Account {0} (each tab has its own independent Client, Profile, ON/OFF, and automations)." },
            { "tooltip.shell_start_all", "Turns ON all accounts that have an AureumRO window selected." },
            { "tooltip.shell_stop_all", "Immediately turns OFF all running accounts (emergency stop)." },

            // Aureum Hub (Vote Timer + Card Collections + Map Collections)
            { "aureum.sub_vote", "Vote Time" },
            { "aureum.sub_collection", "Collection" },
            { "aureum.sub_cards", "Cards ({0})" },
            { "aureum.sub_maps", "Maps ({0})" },
            { "aureum.open_vote_page", "Open aureumro.com/votar" },
            { "aureum.mark_all_voted", "Mark All Voted (12h)" },
            { "aureum.copy_vote_shop", "/navi Vote Shop" },
            { "aureum.sync_topsites", "Sync" },
            { "aureum.vote_site_meta", "+{0} point · {1}h cooldown · auto in-game delivery" },
            { "aureum.vote_site_schedule", "Last vote: {0} · Next: {1}" },
            { "aureum.vote_last_time", "Last vote:" },
            { "aureum.vote_apply_time", "Set" },
            { "aureum.vote_last_time_tip", "Enter the time of your last vote (e.g., 14:30) to automatically calculate the remaining cooldown." },
            { "aureum.vote_invalid_time", "Please enter a valid time in HH:mm format (e.g., 14:30)." },
            { "aureum.vote_now", "Vote (+{0}h)" },
            { "aureum.vote_reset", "Reset" },
            { "aureum.vote_ready", "AVAILABLE NOW" },
            { "aureum.vote_reminder", "Notify when a new vote is ready" },
            { "aureum.vote_sound", "Play reminder sound" },
            { "aureum.vote_info", "Vote while signed in on aureumro.com/votar to receive points in-game, or enter your last vote time (HH:mm) to track the remaining cooldown." },
            { "aureum.vote_alert_title", "AureumRO — Vote Time!" },
            { "aureum.vote_alert_body", "New vote ready on: {0}. Open aureumro.com/votar to claim your points!" },
            { "aureum.all_categories", "All Categories" },
            { "aureum.all_cities", "All Cities" },
            { "aureum.all_bonuses", "All Bonus Types" },
            { "aureum.status_all", "All" },
            { "aureum.status_pending", "Pending" },
            { "aureum.status_complete", "Completed" },
            { "aureum.copy_card_npc", "/navi Card Collector" },
            { "aureum.hide_collected", "Hide collected" },
            { "aureum.mark_visible", "Mark visible" },
            { "aureum.clear_visible", "Unmark visible" },
            { "aureum.mark_collection", "Mark 5" },
            { "aureum.unmark_collection", "Unmark 5" },
            { "aureum.cards_progress", "Progress: {0}/{1} cards · {2}/{3} collections" },
            { "aureum.card_click_hint", "Click to mark or unmark as collected (displays with strikethrough)." },
            { "aureum.cards_search_tip", "Search by card name, collection name, or bonus..." },
            { "aureum.maps_by_city", "By City" },
            { "aureum.maps_by_bonus", "By Bonus Type" },
            { "aureum.maps_progress", "Progress: {0}/{1} maps completed ({2} shown)" },
            { "aureum.maps_search_tip", "Search by map, code (e.g., prt_monk), city, bonus, or drop item..." },
            { "aureum.col_ref", "Ref." },
            { "aureum.col_city", "City" },
            { "aureum.col_bonus_type", "Bonus Type" },
            { "aureum.col_map", "Map" },
            { "aureum.col_code", "Code" },
            { "aureum.col_directions", "Directions" },
            { "aureum.col_bonus", "Bonus" },
            { "aureum.copy_warp", "Copy code" },
            { "aureum.map_select_hint", "Select a map to view step-by-step directions and all required drop items (@mc)." },
            { "aureum.copied", "Copied!" },
            { "aureum.no_results", "No entries match the current filter." },
            { "tooltip.aureum_sub_vote", "Track remaining cooldowns across official topsites and receive alerts when a new vote is ready." },
            { "tooltip.aureum_sub_collection", "Track your AureumRO Card Collection (28) and Map Collection (311) progress." },
            { "tooltip.aureum_sub_cards", "Browse all 28 card collections; click any card to mark it collected with strikethrough." },
            { "tooltip.aureum_sub_maps", "Browse all 311 maps with reference city, directional arrows, and required drop items." },
            { "tooltip.aureum_open_vote", "Opens the official AureumRO vote page in your browser." },
            { "tooltip.aureum_mark_all_voted", "Starts the 12-hour cooldown timer on all listed topsites at once." },
            { "tooltip.aureum_sync_topsites", "Refreshes the official topsite list directly from the AureumRO API." },
            { "tooltip.aureum_vote_reminder", "Shows a desktop notification as soon as a topsite becomes available again." },
            { "tooltip.aureum_vote_sound", "Plays an alert sound along with the vote-ready reminder." },
            { "tooltip.aureum_card_cat", "Filter card collections by bonus category." },
            { "tooltip.aureum_card_status", "Show all collections, pending only, or completed only." },
            { "tooltip.aureum_hide_cards", "Automatically hides cards you have already marked as collected." },
            { "tooltip.aureum_mark_vis_cards", "Marks all cards currently visible under the active filter as collected." },
            { "tooltip.aureum_clear_vis_cards", "Unmarks all cards currently visible under the active filter." },
            { "tooltip.aureum_map_mode", "Switch grouping between Reference City and Bonus Type." },
            { "tooltip.aureum_map_filter", "Filter maps by the selected city or bonus category." },
            { "tooltip.aureum_map_status", "Show all maps, pending only, or completed only." },
            { "tooltip.aureum_mark_vis_maps", "Marks all maps currently shown in the table as completed." },
            { "tooltip.aureum_clear_vis_maps", "Unmarks all maps currently shown in the table." }
        };

        public static string CurrentLanguage
        {
            get { return currentLanguage; }
        }

        public static bool IsPortuguese
        {
            get { return string.Equals(currentLanguage, LANG_PT_BR, StringComparison.OrdinalIgnoreCase); }
        }

        public static void SetLanguage(string language)
        {
            if (string.Equals(language, LANG_EN_US, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                currentLanguage = LANG_EN_US;
            }
            else
            {
                currentLanguage = LANG_PT_BR;
            }
        }

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            Dictionary<string, string> primary = IsPortuguese ? MessagesPt : MessagesEn;
            string value;
            if (primary.TryGetValue(key, out value))
            {
                return value;
            }

            if (MessagesEn.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }

        public static string TranslateText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string translated;
            if (IsPortuguese)
            {
                if (EnToPt.TryGetValue(text, out translated))
                {
                    return translated;
                }
            }
            else
            {
                if (PtToEn.TryGetValue(text, out translated))
                {
                    return translated;
                }
            }

            return text;
        }

        public static void ApplyToControlTree(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            root.SuspendLayout();
            try
            {
                ApplyToControl(root);
            }
            finally
            {
                root.ResumeLayout(true);
            }
        }

        private static void ApplyToControl(Control control)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            // Never translate user-editable input boxes or dropdown items.
            if (!(control is TextBox) && !(control is ComboBox) && !(control is ListBox) && !(control is NumericUpDown))
            {
                if (!string.IsNullOrEmpty(control.Text))
                {
                    string translated = TranslateText(control.Text);
                    if (!string.Equals(translated, control.Text, StringComparison.Ordinal))
                    {
                        control.Text = translated;
                    }
                }
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (!string.IsNullOrEmpty(col.HeaderText))
                    {
                        col.HeaderText = TranslateText(col.HeaderText);
                    }

                    DataGridViewButtonColumn btnCol = col as DataGridViewButtonColumn;
                    if (btnCol != null && !string.IsNullOrEmpty(btnCol.Text))
                    {
                        btnCol.Text = TranslateText(btnCol.Text);
                    }
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyToControl(child);
            }
        }

        private sealed class BuffTranslation
        {
            public readonly string Pt;
            public readonly string En;

            public BuffTranslation(string pt, string en)
            {
                this.Pt = pt;
                this.En = en;
            }
        }

        // Complete bilingual PT-BR (bRO) and EN-US dictionary for all Skills, Consumables, and Debuffs.
        private static readonly Dictionary<string, BuffTranslation> BuffTranslations = new Dictionary<string, BuffTranslation>(StringComparer.OrdinalIgnoreCase)
        {
            // Archer / Bard / Dancer / Ranger / Maestro / Wanderer / Windhawk / Troubadour / Trouvere
            { "Concentration", new BuffTranslation("Concentração", "Concentration") },
            { "Wind Walk", new BuffTranslation("Caminhar no Vento", "Wind Walk") },
            { "True Sight", new BuffTranslation("Visão Real", "True Sight") },
            { "Ilimitar", new BuffTranslation("Ilimitar", "Unlimit") },
            { "A Poem of Bragi", new BuffTranslation("Poema de Bragi", "A Poem of Bragi") },
            { "A Whistle", new BuffTranslation("Assovio", "A Whistle") },
            { "Assassin Cross of Sunset", new BuffTranslation("Crepúsculo Sangrento", "Assassin Cross of Sunset") },
            { "The Apple of Idun", new BuffTranslation("Maçãs de Idun", "The Apple of Idun") },
            { "Humming", new BuffTranslation("Sibilo", "Humming") },
            { "Please Don't Forget Me", new BuffTranslation("Não Me Abandones", "Please Don't Forget Me") },
            { "Fortune's Kiss", new BuffTranslation("Beijo da Sorte", "Fortune's Kiss") },
            { "Service for You", new BuffTranslation("Dança Cigana", "Service for You") },
            { "Mr. Kim A Rich Man", new BuffTranslation("Banquete de Njord", "Mr. Kim A Rich Man") },
            { "Eternal Chaos", new BuffTranslation("Ritmo Caótico", "Eternal Chaos") },
            { "Drum on the Battlefield", new BuffTranslation("Rufar dos Tambores", "Drum on the Battlefield") },
            { "Ring of Nibelungen", new BuffTranslation("Anel dos Nibelungos", "Ring of Nibelungen") },
            { "Loki's Veil", new BuffTranslation("Lamento de Loki", "Loki's Veil") },
            { "Into the Abyss", new BuffTranslation("Canção Preciosa", "Into the Abyss") },
            { "Invulnerable Siegfried", new BuffTranslation("Ode a Siegfried", "Invulnerable Siegfried") },
            { "Marionette Control", new BuffTranslation("Controle de Marionete", "Marionette Control") },
            { "Moonlit Water Mill", new BuffTranslation("Bênção Protetora", "Moonlit Water Mill") },
            { "Longing for Freedom", new BuffTranslation("Grito de Liberdade", "Longing for Freedom") },
            { "Wand of Hermode", new BuffTranslation("Bastão de Hermod", "Wand of Hermode") },
            { "Windmill Rush", new BuffTranslation("Sinfonia dos Ventos", "Windmill Rush") },
            { "Moonlight Serenade", new BuffTranslation("Serenata ao Luar", "Moonlight Serenade") },
            { "Frigg's Song", new BuffTranslation("Canção de Frigga", "Frigg's Song") },
            { "Mystic Symphony", new BuffTranslation("Sinfonia Mística", "Mystic Symphony") },
            { "Jawaii Serenade", new BuffTranslation("Serenata de Jawaii", "Jawaii Serenade") },
            { "Musical Interlude", new BuffTranslation("Interlúdio Musical", "Musical Interlude") },
            { "Prontera March", new BuffTranslation("Marcha de Prontera", "Prontera March") },
            { "Swing Dance", new BuffTranslation("Ritmo Contagiante", "Swing Dance") },
            { "Calamity Gale", new BuffTranslation("Vendaval da Calamidade", "Calamity Gale") },
            { "Fear Breeze", new BuffTranslation("Disparo Selvagem", "Fear Breeze") },

            // Swordsman / Knight / Crusader / RK / RG / Dragon Knight / Imperial Guard
            { "Endure", new BuffTranslation("Vigor", "Endure") },
            { "Auto Berserk", new BuffTranslation("Instinto de Sobrevivência", "Auto Berserk") },
            { "Guard", new BuffTranslation("Bloqueio", "Auto Guard") },
            { "Shield Reflection", new BuffTranslation("Escudo Refletor", "Reflect Shield") },
            { "Spear Quicken", new BuffTranslation("Rapidez com Lança", "Spear Quicken") },
            { "Defending Aura", new BuffTranslation("Aura Sagrada", "Defending Aura") },
            { "Dedication", new BuffTranslation("Dedicação", "Dedication") },
            { "Frenzy", new BuffTranslation("Frenesi", "Frenzy") },
            { "Twohand Quicken", new BuffTranslation("Rapidez com Duas Mãos", "Twohand Quicken") },
            { "Parry", new BuffTranslation("Aparar Golpe", "Parry") },
            { "Aura Blade", new BuffTranslation("Lâmina de Aura", "Aura Blade") },
            { "Enchant Blade", new BuffTranslation("Encantar Lâmina", "Enchant Blade") },
            { "Shrink", new BuffTranslation("Submissão", "Shrink") },
            { "Battle Chant", new BuffTranslation("Canto de Batalha", "Battle Chant / Gospel") },
            { "Inspiration", new BuffTranslation("Consagração", "Inspiration") },
            { "Prestige", new BuffTranslation("Prestígio Divino", "Prestige") },
            { "Shield Spell", new BuffTranslation("Aegis Domini", "Shield Spell") },
            { "Vanguard Force", new BuffTranslation("Força da Vanguarda", "Vanguard Force") },
            { "Reflect Damage", new BuffTranslation("Reflexão Amplificada", "Reflect Damage") },
            { "Vigor", new BuffTranslation("Vigor Dracônico", "Vigor") },
            { "Servant Weapon", new BuffTranslation("Arma Serva", "Servant Weapon") },
            { "Attack Stance", new BuffTranslation("Postura de Ataque", "Attack Stance") },
            { "Guard Stance", new BuffTranslation("Postura de Guarda", "Guard Stance") },
            { "Rebound Shield", new BuffTranslation("Escudo de Rebote", "Rebound Shield") },
            { "Guardian Shield", new BuffTranslation("Escudo Guardião", "Guardian Shield") },
            { "Holy Shield", new BuffTranslation("Escudo Sagrado", "Holy Shield") },
            { "Exceed Break", new BuffTranslation("Exceder Limite", "Exceed Break") },
            { "One-Hand Quicken", new BuffTranslation("Rapidez com Uma Mão", "One-Hand Quicken") },

            // Mage / Wizard / Sage / Warlock / Sorcerer / Arch Mage
            { "Energy Coat", new BuffTranslation("Proteção Arcana", "Energy Coat") },
            { "Sight", new BuffTranslation("Chama Reveladora", "Sight") },
            { "Sight Blaster", new BuffTranslation("Explosão Protetora", "Sight Blaster") },
            { "Autospell", new BuffTranslation("Desejo Arcano", "Autospell") },
            { "Double Casting", new BuffTranslation("Lanças Duplas", "Double Casting") },
            { "Memorize", new BuffTranslation("Presciência", "Memorize") },
            { "Telekinesis Intense", new BuffTranslation("Telecinesia", "Telekinesis Intense") },
            { "Amplification", new BuffTranslation("Amplificação Mística", "Mystical Amplification") },
            { "Recognized Spell", new BuffTranslation("Maestria Arcana", "Recognized Spell") },
            { "Climax", new BuffTranslation("Clímax", "Climax") },

            // Merchant / Blacksmith / Alchemist / Mechanic / Genetic / Meister / Biolo
            { "Crazy Uproar", new BuffTranslation("Grito de Guerra", "Crazy Uproar") },
            { "Power-Thrust", new BuffTranslation("Força Violenta", "Power-Thrust") },
            { "Adrenaline Rush", new BuffTranslation("Adrenalina Pura", "Adrenaline Rush") },
            { "Advanced Adrenaline Rush", new BuffTranslation("Adrenalina Concentrada", "Advanced Adrenaline Rush") },
            { "Maximum Power-Thrust", new BuffTranslation("Força Violentíssima", "Maximum Power-Thrust") },
            { "Weapon Perfection", new BuffTranslation("Manejo Perfeito", "Weapon Perfection") },
            { "Power Maximize", new BuffTranslation("Amplificar Poder", "Power Maximize") },
            { "Cart Boost", new BuffTranslation("Impulso no Carrinho", "Cart Boost") },
            { "Meltdown", new BuffTranslation("Golpe Estilhaçante", "Meltdown") },
            { "Full Chemical Protection", new BuffTranslation("Proteção Química Total", "Full Chemical Protection") },
            { "Acceleration", new BuffTranslation("Aceleração", "Acceleration") },
            { "Research Report", new BuffTranslation("Relatório de Pesquisa", "Research Report") },
            { "Create Hell Tree", new BuffTranslation("Criar Árvore Infernal", "Create Hell Tree") },

            // Thief / Assassin / Rogue / GX / SC / Shadow Cross / Abyss Chaser
            { "Hiding", new BuffTranslation("Esconderijo", "Hiding") },
            { "Cloaking", new BuffTranslation("Furtividade", "Cloaking") },
            { "Chase Walk", new BuffTranslation("Túnel de Fuga", "Chase Walk") },
            { "Poison React", new BuffTranslation("Refletir Veneno", "Poison React") },
            { "Reject Sword", new BuffTranslation("Instinto de Defesa", "Reject Sword") },
            { "Preserve", new BuffTranslation("Preservar", "Preserve") },
            { "Enchant Deadly Poison", new BuffTranslation("Encantar com Veneno Mortal", "Enchant Deadly Poison") },
            { "Weapon Blocking", new BuffTranslation("Reflexo de Combate", "Weapon Blocking") },
            { "Dancing Knife", new BuffTranslation("Lâminas Dançantes", "Dancing Knife") },
            { "Enchanting Shadow", new BuffTranslation("Encantar Sombra", "Enchanting Shadow") },
            { "Potent Venom", new BuffTranslation("Veneno Potente", "Potent Venom") },
            { "Shadow Exceed", new BuffTranslation("Exceder Sombra", "Shadow Exceed") },
            { "Abyss Slayer", new BuffTranslation("Matador Abissal", "Abyss Slayer") },
            { "Abyss Dagger", new BuffTranslation("Adaga Abissal", "Abyss Dagger") },

            // Acolyte / Priest / Monk / AB / Sura / Cardinal / Inquisitor
            { "Blessing", new BuffTranslation("Bênção", "Blessing") },
            { "Increase Agility", new BuffTranslation("Aumentar Agilidade", "Increase Agility") },
            { "Basilica", new BuffTranslation("Basílica", "Basilica") },
            { "Kyrie Eleison", new BuffTranslation("Kyrie Eleison", "Kyrie Eleison") },
            { "Suffragium", new BuffTranslation("Sufrágio", "Suffragium") },
            { "Aspersio", new BuffTranslation("Aspersio", "Aspersio") },
            { "Gloria", new BuffTranslation("Glória", "Gloria") },
            { "Magnificat", new BuffTranslation("Magnificat", "Magnificat") },
            { "Angelus", new BuffTranslation("Angelus", "Angelus") },
            { "Ruwach", new BuffTranslation("Revelação", "Ruwach") },
            { "Steel Body", new BuffTranslation("Corpo Fechado", "Steel Body") },
            { "Rising Dragon", new BuffTranslation("Dragão Ascendente", "Rising Dragon") },
            { "Gentle Touch-Revitalize", new BuffTranslation("Chakra do Vigor", "Gentle Touch-Revitalize") },
            { "Gentle Touch-Convert", new BuffTranslation("Chakra da Fúria", "Gentle Touch-Convert") },
            { "Fury", new BuffTranslation("Fúria Interior", "Fury") },
            { "Impositio Manus", new BuffTranslation("Impositio Manus", "Impositio Manus") },
            { "Competentia", new BuffTranslation("Competentia", "Competentia") },
            { "Offertorium", new BuffTranslation("Offertorium", "Offertorium") },
            { "Sincere Faith", new BuffTranslation("Fé Sincera", "Sincere Faith") },
            { "Firm Faith", new BuffTranslation("Fé Firme", "Firm Faith") },
            { "Powerful Faith", new BuffTranslation("Fé Poderosa", "Powerful Faith") },
            { "First Faith Power", new BuffTranslation("Poder da Primeira Fé", "First Faith Power") },
            { "Second Judgement", new BuffTranslation("Segundo Julgamento", "Second Judgement") },
            { "Third Exorcism Flame", new BuffTranslation("Terceira Chama do Exorcismo", "Third Exorcism Flame") },

            // Ninja
            { "Cicada Skin Shed", new BuffTranslation("Troca de Pele", "Cicada Skin Shed") },
            { "Ninja Aura", new BuffTranslation("Aura Ninja", "Ninja Aura") },
            { "Mirror Image", new BuffTranslation("Imagem Falsa (req. Aura Ninja)", "Mirror Image (req. Ninja Aura)") },
            { "Izayoi", new BuffTranslation("Izayoi", "Izayoi") },

            // Taekwon / Star Gladiator / Star Emperor / Sky Emperor
            { "Mild Wind (Earth)", new BuffTranslation("Brisa Leve - Terra", "Mild Wind - Earth") },
            { "Mild Wind (Fire)", new BuffTranslation("Brisa Leve - Fogo", "Mild Wind - Fire") },
            { "Mild Wind (Water)", new BuffTranslation("Brisa Leve - Água", "Mild Wind - Water") },
            { "Mild Wind (Wind)", new BuffTranslation("Brisa Leve - Vento", "Mild Wind - Wind") },
            { "Mild Wind (Ghost)", new BuffTranslation("Brisa Leve - Fantasma", "Mild Wind - Ghost") },
            { "Mild Wind (Holy)", new BuffTranslation("Brisa Leve - Sagrado", "Mild Wind - Holy") },
            { "Mild Wind (Shadow)", new BuffTranslation("Brisa Leve - Sombrio", "Mild Wind - Shadow") },
            { "Calor solar", new BuffTranslation("Calor Solar", "Solar Heat") },
            { "Proteção solar", new BuffTranslation("Proteção Solar", "Solar Protection") },
            { "Calor lunar", new BuffTranslation("Calor Lunar", "Lunar Heat") },
            { "Proteção lunar", new BuffTranslation("Proteção Lunar", "Lunar Protection") },
            { "Calor estelar", new BuffTranslation("Calor Estelar", "Stellar Heat") },
            { "Proteção estelar", new BuffTranslation("Proteção Estelar", "Stellar Protection") },
            { "Tumbling", new BuffTranslation("Cambalhota", "Tumbling") },
            { "Kaahi", new BuffTranslation("Kaahi", "Kaahi") },
            { "Kaupe", new BuffTranslation("Kaupe", "Kaupe") },
            { "Kaite", new BuffTranslation("Kaite", "Kaite") },
            { "Kaizel", new BuffTranslation("Kaizel", "Kaizel") },
            { "Enchanting Sky", new BuffTranslation("Encantar Céu", "Enchanting Sky") },
            { "Universal Stance", new BuffTranslation("Postura Universal", "Universal Stance") },

            // Gunslinger
            { "Gatling Fever", new BuffTranslation("Ataque Gatling", "Gatling Fever") },
            { "Madness Canceller", new BuffTranslation("Resistência Final", "Madness Canceller") },
            { "Adjustment", new BuffTranslation("Pânico do Justiceiro", "Adjustment") },
            { "Increase Accuracy", new BuffTranslation("Aumentar Precisão", "Increase Accuracy") },

            // Summoner / Spirit Handler
            { "Bunch of Shrimp", new BuffTranslation("Chuva de Mariscos", "Bunch of Shrimp") },
            { "Temporary Communion", new BuffTranslation("Comunhão Temporária", "Temporary Communion") },
            { "Marine Festival of Kisul", new BuffTranslation("Festival Marinho de Kisul", "Marine Festival of Kisul") },
            { "Sandy Festival of Kisul", new BuffTranslation("Festival Arenoso de Kisul", "Sandy Festival of Kisul") },
            { "Colors of Hyunrok Lv 1", new BuffTranslation("Cores de Hyunrok Nv 1", "Colors of Hyunrok Lv 1") },
            { "Colors of Hyunrok Lv 2", new BuffTranslation("Cores de Hyunrok Nv 2", "Colors of Hyunrok Lv 2") },
            { "Colors of Hyunrok Lv 3", new BuffTranslation("Cores de Hyunrok Nv 3", "Colors of Hyunrok Lv 3") },
            { "Colors of Hyunrok Lv 4", new BuffTranslation("Cores de Hyunrok Nv 4", "Colors of Hyunrok Lv 4") },
            { "Colors of Hyunrok Lv 5", new BuffTranslation("Cores de Hyunrok Nv 5", "Colors of Hyunrok Lv 5") },
            { "Colors of Hyunrok Lv 6", new BuffTranslation("Cores de Hyunrok Nv 6", "Colors of Hyunrok Lv 6") },

            // Soul Ascetic
            { "Soul of Heaven and Earth", new BuffTranslation("Alma do Céu e da Terra", "Soul of Heaven and Earth") },

            // Night Watch
            { "Hidden Card", new BuffTranslation("Carta na Manga", "Hidden Card") },
            { "Intensive Aim", new BuffTranslation("Mira Intensiva", "Intensive Aim") },
            { "Auto Firing Launcher", new BuffTranslation("Lançador Automático", "Auto Firing Launcher") },
            { "Platinum Altar", new BuffTranslation("Altar de Platina", "Platinum Altar") },
            { "Heat Barrel", new BuffTranslation("Cano Superaquecido", "Heat Barrel") },
            { "Eternal Chain", new BuffTranslation("Corrente Eterna", "Eternal Chain") },
            { "Grenade Fragment Lv 1", new BuffTranslation("Fragmento de Granada Nv 1", "Grenade Fragment Lv 1") },
            { "Grenade Fragment Lv 2", new BuffTranslation("Fragmento de Granada Nv 2", "Grenade Fragment Lv 2") },
            { "Grenade Fragment Lv 3", new BuffTranslation("Fragmento de Granada Nv 3", "Grenade Fragment Lv 3") },
            { "Grenade Fragment Lv 4", new BuffTranslation("Fragmento de Granada Nv 4", "Grenade Fragment Lv 4") },
            { "Grenade Fragment Lv 5", new BuffTranslation("Fragmento de Granada Nv 5", "Grenade Fragment Lv 5") },
            { "Grenade Fragment Lv 6", new BuffTranslation("Fragmento de Granada Nv 6", "Grenade Fragment Lv 6") },

            // Hyper Novice
            { "Rule Break", new BuffTranslation("Quebra de Regras", "Rule Break") },
            { "Breaking Limit", new BuffTranslation("Quebra de Limite", "Breaking Limit") },

            // Homunculus
            { "Pyroclastic", new BuffTranslation("Piroclástico", "Pyroclastic") },
            { "HOMUN_LAST", new BuffTranslation("Têmpera de Homúnculo", "Homunculus Tempering") },

            // Potions
            { "Concentration Potion", new BuffTranslation("Poção da Concentração", "Concentration Potion") },
            { "Awakening Potion", new BuffTranslation("Poção do Despertar", "Awakening Potion") },
            { "Berserk Potion", new BuffTranslation("Poção da Fúria Selvagem", "Berserk Potion") },
            { "Regeneration Potion", new BuffTranslation("Poção de Regeneração", "Regeneration Potion") },
            { "HP Increase Potion", new BuffTranslation("Poção Grande de HP", "HP Increase Potion") },
            { "SP Increase Potion", new BuffTranslation("Poção Grande de SP", "SP Increase Potion") },
            { "Red Herb Activator", new BuffTranslation("Ativador de Erva Vermelha", "Red Herb Activator") },
            { "Blue Herb Activator", new BuffTranslation("Ativador de Erva Azul", "Blue Herb Activator") },
            { "Golden X", new BuffTranslation("Poção Dourada X", "Golden X") },
            { "Energy Drink", new BuffTranslation("Bebida Energética", "Energy Drink") },
            { "Mega Resist Potion", new BuffTranslation("Poção Anti-Elemento Mega", "Mega Resist Potion") },
            { "Full SwingK Potion", new BuffTranslation("Poção do Furor Físico", "Full SwingK Potion") },
            { "Mana Plus Potion", new BuffTranslation("Poção do Furor Mágico", "Mana Plus Potion") },
            { "Blessing Of Tyr", new BuffTranslation("Bênção de Tyr", "Blessing of Tyr") },
            { "True Limit Power Booster (Poção Ilimitada)", new BuffTranslation("Poção Ilimitada", "True Limit Power Booster") },
            { "Infinity Drink", new BuffTranslation("Poção Infinita", "Infinity Drink") },
            { "Red Booster (ATK +30, MATK +30)", new BuffTranslation("Reforço Vermelho +30 ATQ/ATQM", "Red Booster +30 ATK/MATK") },
            { "Poção Fantástica (Sealed Kiel Card)", new BuffTranslation("Poção Fantástica", "Fantastic Potion - Sealed Kiel") },
            { "Poção do Furor Físico", new BuffTranslation("Poção do Furor Físico", "Physical Fury Potion") },
            { "Poção Mágica", new BuffTranslation("Poção Mágica", "Magic Potion") },
            { "True ASPD Intensifying Potion", new BuffTranslation("Poção de Vel. de Ataque", "True ASPD Intensifying Potion") },
            { "True Medium Life Potion", new BuffTranslation("Poção Média de Vida", "True Medium Life Potion") },

            // Elementals
            { "Elemental Converter (Fire)", new BuffTranslation("Conversor Elemental - Fogo", "Elemental Converter - Fire") },
            { "Elemental Converter (Wind)", new BuffTranslation("Conversor Elemental - Vento", "Elemental Converter - Wind") },
            { "Elemental Converter (Earth)", new BuffTranslation("Conversor Elemental - Terra", "Elemental Converter - Earth") },
            { "Elemental Converter (Water)", new BuffTranslation("Conversor Elemental - Água", "Elemental Converter - Water") },
            { "Cursed Water", new BuffTranslation("Água Amaldiçoada", "Cursed Water") },
            { "Fire Conversor", new BuffTranslation("Conversor de Fogo", "Fire Converter") },
            { "Wind Conversor", new BuffTranslation("Conversor de Vento", "Wind Converter") },
            { "Earth Conversor", new BuffTranslation("Conversor de Terra", "Earth Converter") },
            { "Water Conversor", new BuffTranslation("Conversor de Água", "Water Converter") },
            { "Aspersio Scroll", new BuffTranslation("Pergaminho de Aspersio", "Aspersio Scroll") },
            { "GHOST Conversor", new BuffTranslation("Conversor Fantasma", "Ghost Converter") },
            { "Fireproof Potion", new BuffTranslation("Poção Anti-Fogo", "Fireproof Potion") },
            { "Waterproof Potion", new BuffTranslation("Poção Anti-Água", "Waterproof Potion") },
            { "Windproof Potion", new BuffTranslation("Poção Anti-Vento", "Windproof Potion") },
            { "Earthproof Potion", new BuffTranslation("Poção Anti-Terra", "Earthproof Potion") },

            // Foods
            { "STR Food", new BuffTranslation("Comida de FOR", "STR Food") },
            { "AGI Food", new BuffTranslation("Comida de AGI", "AGI Food") },
            { "VIT Food", new BuffTranslation("Comida de VIT", "VIT Food") },
            { "INT Food", new BuffTranslation("Comida de INT", "INT Food") },
            { "DEX Food", new BuffTranslation("Comida de DES", "DEX Food") },
            { "LUK Food", new BuffTranslation("Comida de SOR", "LUK Food") },
            { "3RD STR Food", new BuffTranslation("Comida +20 FOR", "3rd Class STR Food") },
            { "3RD AGI Food", new BuffTranslation("Comida +20 AGI", "3rd Class AGI Food") },
            { "3RD VIT Food", new BuffTranslation("Comida +20 VIT", "3rd Class VIT Food") },
            { "3RD INT Food", new BuffTranslation("Comida +20 INT", "3rd Class INT Food") },
            { "3RD DEX Food", new BuffTranslation("Comida +20 DES", "3rd Class DEX Food") },
            { "3RD LUK Food", new BuffTranslation("Comida +20 SOR", "3rd Class LUK Food") },
            { "Almighty", new BuffTranslation("Bolinho Divino", "Almighty") },
            { "CASH Food", new BuffTranslation("Comida de ROPs/Cash", "Cash Food") },
            { "Acarajé", new BuffTranslation("Acarajé", "Acaraje") },
            { "STR Biscuit Stick", new BuffTranslation("Palito de FOR", "STR Biscuit Stick") },
            { "AGI Biscuit Stick", new BuffTranslation("Palito de AGI", "AGI Biscuit Stick") },
            { "VIT Biscuit Stick", new BuffTranslation("Palito de VIT", "VIT Biscuit Stick") },
            { "INT Biscuit Stick", new BuffTranslation("Palito de INT", "INT Biscuit Stick") },
            { "DEX Biscuit Stick", new BuffTranslation("Palito de DES", "DEX Biscuit Stick") },
            { "LUK Biscuit Stick", new BuffTranslation("Palito de SOR", "LUK Biscuit Stick") },
            { "STR Bubble Gum Green", new BuffTranslation("Chiclete de FOR - Verde", "STR Bubble Gum Green") },
            { "AGI Bubble Gum Red", new BuffTranslation("Chiclete de AGI - Vermelho", "AGI Bubble Gum Red") },
            { "INT Bubble Gum Yellow", new BuffTranslation("Chiclete de INT - Amarelo", "INT Bubble Gum Yellow") },
            { "DEX Bubble Gum Orange", new BuffTranslation("Chiclete de DES - Laranja", "DEX Bubble Gum Orange") },
            { "Winter Cookie ATK", new BuffTranslation("Biscoito de Inverno ATQ", "Winter Cookie ATK") },
            { "Flora Cookie MATK", new BuffTranslation("Biscoito da Flora ATQM", "Flora Cookie MATK") },
            { "Banquete Revigorante", new BuffTranslation("Banquete Revigorante", "Invigorating Banquet") },

            // Boxes / Speed / Status
            { "Drowsiness Box", new BuffTranslation("Caixa da Sonolência", "Drowsiness Box") },
            { "Resentment Box", new BuffTranslation("Caixa do Ressentimento", "Resentment Box") },
            { "Sunlight Box", new BuffTranslation("Caixa da Luz do Sol", "Sunlight Box") },
            { "Box of Gloom", new BuffTranslation("Caixa da Escuridão", "Box of Gloom") },
            { "Box of Thunder", new BuffTranslation("Caixa do Trovão", "Box of Thunder") },
            { "Speed Potion / Guyak", new BuffTranslation("Poção do Vento / Guyak", "Speed Potion / Guyak") },
            { "Anodyne", new BuffTranslation("Analgésico", "Anodyne") },
            { "Aloevera", new BuffTranslation("Aloevera", "Aloevera") },
            { "Abrasivo", new BuffTranslation("Abrasivo", "Abrasive") },
            { "Combat Pill", new BuffTranslation("Pílula de Combate", "Combat Pill") },
            { "Advance Combat Pill", new BuffTranslation("Grande Pílula de Combate", "Enriched Combat Pill") },
            { "Celermine Juice", new BuffTranslation("Suco Celular Enriquecido", "Enriched Celermine Juice") },
            { "Guarana Candy", new BuffTranslation("Bala de Guaraná", "Guarana Candy") },
            { "Poison Bottle", new BuffTranslation("Garrafa de Veneno", "Poison Bottle") },

            // Scroll Buffs
            { "Éden Scroll", new BuffTranslation("Pergaminho do Éden", "Eden Scroll") },
            { "Increase Agility Scroll", new BuffTranslation("Pergaminho de Aumentar Agilidade", "Increase Agility Scroll") },
            { "Bless Scroll", new BuffTranslation("Pergaminho de Bênção", "Blessing Scroll") },
            { "Full Chemical Protection (Scroll)", new BuffTranslation("Pergaminho de Proteção Química Total", "Full Chemical Protection Scroll") },
            { "Burn Incense", new BuffTranslation("Incenso Queimado", "Burnt Incense") },
            { "Link Scroll", new BuffTranslation("Pergaminho de Espírito", "Soul Link Scroll") },
            { "Monster Transform", new BuffTranslation("Transformação em Monstro", "Monster Transform") },
            { "Assumptio", new BuffTranslation("Assumptio", "Assumptio") },
            { "Holy Armor Scroll", new BuffTranslation("Pergaminho de Armadura Sagrada", "Holy Armor Scroll") },
            { "Shadow Armor Scroll", new BuffTranslation("Pergaminho de Armadura Sombria", "Shadow Armor Scroll") },
            { "Soul Scroll", new BuffTranslation("Pergaminho da Alma", "Soul Scroll") },
            { "Undead Element Scroll", new BuffTranslation("Pergaminho de Propriedade Maldita", "Undead Element Scroll") },

            // ETC
            { "THURISAZ Rune", new BuffTranslation("Runa Thurisaz", "Thurisaz Rune") },
            { "OTHILA Rune", new BuffTranslation("Runa Othila", "Othila Rune") },
            { "HAGALAZ Rune", new BuffTranslation("Runa Hagalaz", "Hagalaz Rune") },
            { "LUX AMINA Rune", new BuffTranslation("Runa Lux Anima", "Lux Anima Rune") },
            { "Cat Can", new BuffTranslation("Lata de Comida para Gato", "Cat Can") },
            { "HE Bubble Gum", new BuffTranslation("Goma de Mascar Especial", "HE Bubble Gum") },
            { "Frost Giant Blood (GvG GIANT)", new BuffTranslation("Sangue do Gigante de Gelo", "Frost Giant Blood - GvG Giant") },
            { "Battle Manual (GvG GOLEM)", new BuffTranslation("Pedra de Golem", "Golem Stone - GvG Golem") },
            { "Magic Candy", new BuffTranslation("Doce Mágico", "Magic Candy") },
            { "Ghostring", new BuffTranslation("Pergaminho de Ghostring", "Ghostring") },
            { "Angeling", new BuffTranslation("Pergaminho de Angeling", "Angeling") },
            { "Tao Gunka", new BuffTranslation("Pergaminho de Tao Gunka", "Tao Gunka") },
            { "Orc Lord", new BuffTranslation("Pergaminho do Senhor dos Orcs", "Orc Lord") },
            { "Orc Hero", new BuffTranslation("Pergaminho do Orc Herói", "Orc Hero") },
            { "MISTRESS", new BuffTranslation("Pergaminho da Abelha-Rainha", "Mistress") },

            // Candies
            { "Sweets Macaron Cake", new BuffTranslation("Bolo de Macaron", "Sweets Macaron Cake") },
            { "Sweets Strawberry Parfait", new BuffTranslation("Parfait de Morango", "Sweets Strawberry Parfait") },
            { "Popcorn Festival Buff", new BuffTranslation("Pipoca do Festival", "Popcorn Festival Buff") },
            { "Doce Hiper Açucarado", new BuffTranslation("Doce Hiper Açucarado", "Spark Candy") },
            { "Elixir Ultra Milagroso", new BuffTranslation("Elixir Ultra Milagroso", "Ultra Miracle Elixir") },
            { "Cherry Blossom Rice Cake", new BuffTranslation("Bolinho de Flor de Cerejeira", "Cherry Blossom Rice Cake") },

            // EXP
            { "Goma de Mascar", new BuffTranslation("Goma de Mascar", "Bubble Gum") },
            { "Manual de Combate", new BuffTranslation("Manual de Combate", "Battle Manual") },
            { "Manual de Combate de classe", new BuffTranslation("Manual de Combate de Classe", "Job Battle Manual") },

            // Debuffs
            { "Critical Wounds", new BuffTranslation("Ferimento Crítico", "Critical Wounds") },
            { "FREEZING", new BuffTranslation("Hipotermia", "Freezing (Frost)") },
            { "Burning", new BuffTranslation("Queimadura", "Burning") },
            { "Curse", new BuffTranslation("Maldição", "Curse") },
            { "Bleeding", new BuffTranslation("Sangramento", "Bleeding") },
            { "Silence", new BuffTranslation("Silêncio", "Silence") },
            { "Decrease Agi", new BuffTranslation("Diminuir Agilidade", "Decrease Agility") },
            { "Confusion / chaos", new BuffTranslation("Confusão / Caos", "Confusion / Chaos") },
            { "Blind", new BuffTranslation("Cegueira", "Blind") },
            { "Hallucination", new BuffTranslation("Alucinação", "Hallucination") },
            { "STUN", new BuffTranslation("Atordoamento", "Stun") },
            { "Deep Sleep", new BuffTranslation("Sono Profundo", "Deep Sleep") },
            { "Poison", new BuffTranslation("Envenenamento", "Poison") },
            { "Frozen", new BuffTranslation("Congelamento", "Frozen") },
            { "Sleep", new BuffTranslation("Sono", "Sleep") },
            { "Stone Curse", new BuffTranslation("Petrificação", "Stone Curse") },
            { "Spider Web", new BuffTranslation("Prisão da Teia", "Spider Web") },
            { "Cursed Circle", new BuffTranslation("Campo Amaldiçoado", "Cursed Circle") },
            { "Lucky Water", new BuffTranslation("Azar / Infortúnio", "Misfortune / Lucky Water") }
        };

        public static bool HasBuffTranslation(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return false;
            }

            return BuffTranslations.ContainsKey(rawName.Trim());
        }

        public static string GetBuffShortDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            string trimmed = rawName.Trim();
            BuffTranslation entry;
            if (!BuffTranslations.TryGetValue(trimmed, out entry))
            {
                return trimmed;
            }

            return IsPortuguese ? entry.Pt : entry.En;
        }

        public static string GetBuffDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            string trimmed = rawName.Trim();
            BuffTranslation entry;
            if (!BuffTranslations.TryGetValue(trimmed, out entry))
            {
                return trimmed;
            }

            if (!IsPortuguese)
            {
                return entry.En;
            }

            if (string.Equals(NormalizeForSearch(entry.Pt), NormalizeForSearch(entry.En), StringComparison.OrdinalIgnoreCase))
            {
                return entry.Pt;
            }

            return string.Format("{0} ({1})", entry.Pt, entry.En);
        }

        public static bool MatchesBuffSearch(string rawName, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(rawName))
            {
                return false;
            }

            string normalizedQuery = NormalizeForSearch(query);
            string trimmed = rawName.Trim();
            if (NormalizeForSearch(trimmed).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            BuffTranslation entry;
            if (BuffTranslations.TryGetValue(trimmed, out entry))
            {
                if (NormalizeForSearch(entry.Pt).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (NormalizeForSearch(entry.En).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MatchesSearchText(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return NormalizeForSearch(text).IndexOf(NormalizeForSearch(query), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string normalized = text.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalized[i]);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalized[i]);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static Dictionary<string, string> BuildReverseDictionary(Dictionary<string, string> source)
        {
            Dictionary<string, string> reverse = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (!reverse.ContainsKey(pair.Value))
                {
                    reverse[pair.Value] = pair.Key;
                }
            }
            return reverse;
        }
    }
}

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
            { "Mini HUD", "Mini HUD" },
            { "Join in our discord", "Entre no nosso Discord" },
            { "Developed by Oráculo", "Desenvolvido por Oráculo" },
            { "Skill Spammer", "Spammer" },
            { "Debuff", "Debuffs" },
            { "Anti-Debuffs", "Anti-debuffs" },
            { "Auto-Skills", "Auto-Skills" },
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
            { "Filter:", "Filtrar:" },
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
            { "Updating AureumRO 4Tools...", "Atualizando AureumRO 4Tools..." },
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
            { "tooltip.dark_mode", "Alternar entre tema claro e escuro" },
            { "tooltip.mini_hud", "Exibir janela flutuante compacta com status e barras de HP/SP" },
            { "tooltip.top_most", "Manter o AureumRO 4Tools sempre acima da janela do Ragnarok" },
            { "tooltip.use_buffs_in_town", "Se desmarcado, pausa Autopot (HP/SP), Autobuffs, Timers e Recuperação enquanto estiver em cidades" },
            { "tooltip.language", "Alterar o idioma da interface (Português / Inglês)" },
            { "tooltip.refresh_clients", "Atualizar lista de clientes Ragnarok abertos" },
            { "tooltip.website", "Abrir site oficial do AureumRO (aureumro.com)" },
            { "tooltip.wiki", "Abrir Wiki oficial do AureumRO" },
            { "tooltip.discord", "Entrar no Discord oficial do AureumRO" },
            { "tooltip.github", "Abrir repositório do projeto no GitHub" },
            { "footer.website", "Site" },
            { "footer.wiki", "Wiki" },
            { "footer.powered", "✦ AureumRO 4Tools · Desenvolvido por Oráculo  •  Minimize para enviar à bandeja" },
            { "sidebar.status_on", "Automação ativa" },
            { "sidebar.status_chat", "Pausado (Chat)" },
            { "sidebar.status_town", "Pausado (Cidade)" },
            { "sidebar.status_off", "Pronto (OFF)" },
            { "sidebar.profile_prefix", "Perfil: {0}" },
            { "sidebar.client_connected", "Cliente: {0}" },
            { "sidebar.client_none", "Cliente: não selecionado" },
            { "section.spammer.title", "Spammer" },
            { "section.spammer.subtitle", "Configure teclas de ataque rápido, modo de clique e atraso do spammer." },
            { "section.debuff.title", "Anti-debuffs" },
            { "section.debuff.subtitle", "Cure automaticamente apenas status removíveis por itens ou habilidades." },
            { "section.autobuff.title", "Auto-Skills" },
            { "section.autobuff.subtitle", "Renove buffs de habilidades e consumíveis (Comuns e ROPs) automaticamente." },
            { "section.buff_skills.title", "Auto-Skills" },
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
            { "section.profiles.title", "Perfis, Backup e Diagnóstico" },
            { "section.profiles.subtitle", "Gerencie perfis, vínculo automático por personagem, backup .zip e conflitos de tecla." },
            { "section.guide.title", "Guia Completo do AureumRO 4Tools" },
            { "section.guide.subtitle", "Entenda todas as abas, modos de macro e dicas — passe o mouse sobre qualquer campo do app para ver o popup." },
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
            { "tooltip.nav_autobuff", "Auto-Skills: reúne buffs de Habilidades e Consumíveis (Comuns e ROPs) com renovação automática." },
            { "tooltip.autobuff_tab_skills", "Habilidades: renova automaticamente buffs de classe assim que o ícone do buff expira no jogo." },
            { "tooltip.autobuff_tab_items", "Consumíveis: renova poções de ASPD, comidas, conversores elementais, caixas e pergaminhos (Comuns e ROPs)." },
            { "tooltip.nav_buff_skills", "Auto-Skills (Habilidades): renova automaticamente buffs de classe assim que o ícone do buff expira no jogo." },
            { "tooltip.nav_buff_stuffs", "Auto-Skills (Consumíveis): renova poções de ASPD, comidas, conversores elementais, caixas e pergaminhos (Comuns e ROPs)." },
            { "tooltip.nav_timers", "Temporizadores: aperta até 6 teclas automaticamente em intervalos fixos de tempo (em segundos)." },
            { "tooltip.nav_macros", "Macros: reúne Macro Troca (sequência de equips), Macro Músicas (Bardo/Odalisca) e Macro ATK x DEF (troca + spammer)." },
            { "tooltip.nav_profiles", "Perfis & Backup: crie, renomeie, copie, faça backup .zip, verifique conflitos de teclas e vincule perfis ao personagem." },
            { "tooltip.nav_guide", "Guia / Ajuda: manual completo e pesquisável explicando todas as funcionalidades do AureumRO 4Tools." },
            { "tooltip.autopot_hp_key", "Tecla da poção de HP (ex.: Poção Branca, Mastela, Compacta). Aperte Backspace/Esc para limpar." },
            { "tooltip.autopot_hp_pct", "Porcentagem de HP abaixo da qual a poção de HP será usada automaticamente (ex.: 80%)." },
            { "tooltip.autopot_sp_key", "Tecla da poção de SP (ex.: Poção Azul, Morango, Limão). Aperte Backspace/Esc para limpar." },
            { "tooltip.autopot_sp_pct", "Porcentagem de SP abaixo da qual a poção de SP será usada automaticamente (ex.: 40%)." },
            { "tooltip.autopot_ygg_hp_key", "Tecla de emergência (Fruto/Semente de Yggdrasil) para recuperar HP crítico." },
            { "tooltip.autopot_ygg_hp_pct", "Porcentagem crítica de HP para disparar Yggdrasil (use um valor menor que o do Autopot padrão, ex.: 35%)." },
            { "tooltip.autopot_ygg_sp_key", "Tecla de emergência (Fruto/Semente de Yggdrasil) para recuperar SP crítico." },
            { "tooltip.autopot_ygg_sp_pct", "Porcentagem crítica de SP para disparar Yggdrasil (ex.: 15%)." },
            { "tooltip.autopot_delay", "Intervalo em milissegundos (ms) entre cada tentativa de potar (valores comuns: 50 a 150ms)." },
            { "tooltip.toggle_btn", "Liga ou desliga todas as automações do AureumRO 4Tools de uma só vez." },
            { "tooltip.toggle_key", "Tecla global (ex.: End, Insert, F12) para ligar ou desligar o 4Tools direto de dentro do jogo." },
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
            { "tooltip.profile_autoswitch", "Carrega automaticamente o perfil vinculado ao detectar o nome do personagem logado no jogo (quando em OFF)." },
            { "tooltip.profile_use_char", "Lê automaticamente o nome do personagem logado na janela selecionada do AureumRO." },
            { "tooltip.profile_save_char", "Vincula o nome do personagem digitado ao perfil selecionado na lista." },
            { "tooltip.profile_clear_char", "Remove o vínculo de personagem do perfil selecionado." },
            { "msg.client_unsupported", "Este cliente não é suportado. Apenas Spammers e Macros vão funcionar." },
            { "msg.profile_load_error", "Erro ao carregar o novo perfil.\nDetalhes: {0}" },

            // ToggleApplicationStateForm
            { "tooltip.focus_only", "Pausar automações quando a janela do Ragnarok não estiver em foco" },
            { "tooltip.chat_pause", "Pausa de Chat: aperte Enter no Ragnarok para pausar ao digitar (Esc ou Enter retoma; Autopot em F1–F24 continua ativo)" },
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
            { "tooltip.min_sp", "Pausar Autobuff quando o SP estiver abaixo desta % (0 = desativado)" },
            { "tooltip.buff_cd", "Tempo de recarga por buff em milissegundos (ex.: 1500ms evita uso duplo)" },
            { "tooltip.pre_renewal", "Exibe apenas skills de Pré-Renewal / Transclasses (oculta 3ª/4ª classe, Doram, Night Watch, Hyper Novice e Soul Ascetic)" },
            { "tooltip.pre_renewal_items", "Exibe apenas consumíveis de Pré-Renewal / Gold Times (oculta Runas de RK, comidas 3rd, doces de 4ª classe e itens custom)" },
            { "tooltip.item_origin", "Filtre entre todos os consumíveis, apenas Itens Comuns ou apenas Itens de ROPs" },
            { "filter.items_all", "Todos (Comuns + ROPs)" },
            { "filter.items_common", "Apenas Itens Comuns" },
            { "filter.items_rops", "Apenas Itens de ROPs" },
            { "tooltip.pause_sitting", "Pausa Autobuffs (skills e consumíveis) automaticamente enquanto o personagem estiver sentado (EFST_SIT)" },
            { "tooltip.auto_self_click", "Envia um clique automático no centro da janela (em cima do seu personagem) ao usar skills de alvo em si mesmo: Bênção, Aumentar Agilidade, Assumptio, Kyrie Eleison, Sufrágio, Impositio Manus e Aspersio." },
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
            { "filter.profile_json", "Perfil AureumRO 4Tools (*.json)|*.json" },
            { "title.export_profile", "Exportar Perfil" },
            { "msg.export_success", "Perfil exportado com sucesso!" },
            { "msg.export_error", "Falha ao exportar perfil: {0}" },
            { "title.import_profile", "Importar Perfil" },
            { "msg.import_success", "Perfil '{0}' importado com sucesso!" },
            { "msg.import_error", "Falha ao importar perfil: {0}" },
            { "filter.backup_zip", "Backup AureumRO 4Tools (*.zip)|*.zip" },
            { "title.backup_all", "Backup Completo" },
            { "msg.backup_success", "Backup criado com sucesso ({0} arquivo(s) exportado(s))!" },
            { "msg.backup_error", "Falha ao criar backup: {0}" },
            { "title.restore_backup", "Restaurar Backup" },
            { "msg.restore_confirm", "Restaurar perfis, servidores customizados e configurações deste backup?\nPerfis existentes com o mesmo nome serão atualizados." },
            { "msg.restore_summary", "Backup restaurado!\nPerfis restaurados: {0}\nServidores customizados restaurados: {1}\nConfigurações restauradas: {2}" },
            { "msg.restore_error", "Falha ao restaurar backup: {0}" },
            { "title.key_conflicts", "Inspetor de Conflito de Teclas" },

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
            { "test.open_failed", "Falha ao abrir a memória do processo '{0}'. Tente executar o AureumRO 4Tools como Administrador." },
            { "test.read_error", "Erro ao ler memória do processo: {0}" },
            { "test.ok_report", "Conexão OK!\nPersonagem: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nBuffs ativos: {5}" },
            { "test.not_detected", "(não detectado)" },
            { "test.empty_values", "Os endereços retornaram 0/vazio (HP: {0}/{1}, Nome: vazio).\nVerifique se está logado em um mapa e se os endereços correspondem a este cliente." },

            // KeybindConflictDetector
            { "conflicts.none", "Nenhum conflito de tecla detectado no perfil '{0}'!" },
            { "conflicts.header", "Encontrado(s) {0} conflito(s) de tecla no perfil '{1}':" },
            { "usage.toggle", "Tecla Ligar/Desligar" },
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
            { "usage.macro_slot", "{0} (Linha {1} Slot)" }
        };

        private static readonly Dictionary<string, string> MessagesEn = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "title.warning", "Warning" },
            { "title.error", "Error" },
            { "title.confirm", "Confirm" },
            { "title.message", "Message" },
            { "title.yes", "Yes" },
            { "title.no", "No" },

            { "tooltip.dark_mode", "Switch between Light and Dark interface themes" },
            { "tooltip.mini_hud", "Show a compact floating overlay with status and HP/SP bars" },
            { "tooltip.top_most", "Keep the AureumRO 4Tools window pinned above Ragnarok" },
            { "tooltip.use_buffs_in_town", "When unchecked, pauses Autopot (HP/SP), Autobuffs, Skill Timers, and Recovery while in towns" },
            { "tooltip.language", "Switch interface language (Portuguese / English)" },
            { "tooltip.refresh_clients", "Refresh open Ragnarok clients" },
            { "tooltip.website", "Open the official AureumRO website (aureumro.com)" },
            { "tooltip.wiki", "Open the official AureumRO Wiki" },
            { "tooltip.discord", "Join the official AureumRO Discord server" },
            { "tooltip.github", "Open the project repository on GitHub" },
            { "footer.website", "Website" },
            { "footer.wiki", "Wiki" },
            { "footer.powered", "✦ AureumRO 4Tools · Developed by Oráculo  •  Minimize to send to system tray" },
            { "sidebar.status_on", "Automation active" },
            { "sidebar.status_chat", "Paused (Chat)" },
            { "sidebar.status_town", "Paused (Town)" },
            { "sidebar.status_off", "Ready (OFF)" },
            { "sidebar.profile_prefix", "Profile: {0}" },
            { "sidebar.client_connected", "Client: {0}" },
            { "sidebar.client_none", "Client: not selected" },
            { "section.spammer.title", "Skill Spammer" },
            { "section.spammer.subtitle", "Configure rapid-cast skill keys, click behavior, and spammer delay." },
            { "section.debuff.title", "Anti-Debuffs" },
            { "section.debuff.subtitle", "Automatically cure negative status effects removable by items or skills." },
            { "section.autobuff.title", "Auto-Skills" },
            { "section.autobuff.subtitle", "Automatically renew class skills and consumables (Common and ROPs) in one tab." },
            { "section.buff_skills.title", "Auto-Skills" },
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
            { "section.profiles.title", "Profiles, Backup & Diagnostics" },
            { "section.profiles.subtitle", "Manage profiles, auto-switch by character, full .zip backups, and keybind conflicts." },
            { "section.guide.title", "Complete AureumRO 4Tools Guide" },
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
            { "tooltip.nav_autobuff", "Auto-Skills: combines class Skills and Consumables (Common and ROPs) with automatic renewal." },
            { "tooltip.autobuff_tab_skills", "Skills: automatically recasts class buffs whenever their status icon expires in-game." },
            { "tooltip.autobuff_tab_items", "Consumables: automatically refreshes ASPD pots, foods, elemental converters, boxes, and scrolls (Common and ROPs)." },
            { "tooltip.nav_buff_skills", "Auto-Skills (Skills): automatically recasts class buffs whenever their status icon expires in-game." },
            { "tooltip.nav_buff_stuffs", "Auto-Skills (Consumables): automatically refreshes ASPD pots, foods, elemental converters, boxes, and scrolls (Common and ROPs)." },
            { "tooltip.nav_timers", "Skill Timers: presses up to 6 keys automatically on fixed second intervals." },
            { "tooltip.nav_macros", "Macros: combines Switch Macro (gear chains), Song Macro (Bard/Dancer), and ATK x DEF Macro (swap + spammer)." },
            { "tooltip.nav_profiles", "Profiles & Backup: create, rename, copy, export .zip backups, detect key conflicts, and auto-switch by character." },
            { "tooltip.nav_guide", "Guide / Help: complete searchable handbook explaining every feature in AureumRO 4Tools." },
            { "tooltip.autopot_hp_key", "HP potion hotkey (e.g., White Potion, Mastela, Slim White). Press Backspace/Esc to clear." },
            { "tooltip.autopot_hp_pct", "HP percentage threshold below which the HP potion is used automatically (e.g., 80%)." },
            { "tooltip.autopot_sp_key", "SP potion hotkey (e.g., Blue Potion, Strawberry, Lemon). Press Backspace/Esc to clear." },
            { "tooltip.autopot_sp_pct", "SP percentage threshold below which the SP potion is used automatically (e.g., 40%)." },
            { "tooltip.autopot_ygg_hp_key", "Emergency HP hotkey (Yggdrasil Berry / Seed) for critical HP recovery." },
            { "tooltip.autopot_ygg_hp_pct", "Critical HP threshold to trigger Yggdrasil (set lower than standard Autopot, e.g., 35%)." },
            { "tooltip.autopot_ygg_sp_key", "Emergency SP hotkey (Yggdrasil Berry / Seed) for critical SP recovery." },
            { "tooltip.autopot_ygg_sp_pct", "Critical SP threshold to trigger Yggdrasil (e.g., 15%)." },
            { "tooltip.autopot_delay", "Delay in milliseconds (ms) between potion attempts (typical: 50–150ms)." },
            { "tooltip.toggle_btn", "Turns all AureumRO 4Tools automations ON or OFF at once." },
            { "tooltip.toggle_key", "Global hotkey (e.g., End, Insert, F12) to toggle 4Tools ON/OFF directly from inside the game." },
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
            { "tooltip.profile_autoswitch", "Automatically loads the bound profile when the logged-in character name is detected (while OFF)." },
            { "tooltip.profile_use_char", "Automatically reads the logged-in character name from the selected AureumRO window." },
            { "tooltip.profile_save_char", "Binds the entered character name to the profile selected in the list." },
            { "tooltip.profile_clear_char", "Removes the character binding from the selected profile." },
            { "msg.client_unsupported", "This client is not supported. Only Spammers and macro will work." },
            { "msg.profile_load_error", "Error while loading the new profile.\nDetails: {0}" },

            { "tooltip.focus_only", "Pause automations when the Ragnarok window is not focused" },
            { "tooltip.chat_pause", "Chat-Safe Pause: press Enter in Ragnarok to pause automations while typing (Esc or Enter resumes; F-key Autopot stays active)" },
            { "status.press_to_start", "Press the key to start!" },
            { "status.press_to_stop", "Press the key to stop!" },
            { "status.chat_paused", "Chat paused (Esc/Enter)" },
            { "status.client_closed", "Client closed!" },
            { "status.select_client", "Please select the Ragnarok Client!!" },
            { "tray.close", "Close" },

            { "msg.ahk_clear_confirm", "Clear all configured Skill Spammer keys in this profile?" },
            { "title.ahk_clear", "Clear All Keys" },

            { "tooltip.min_sp", "Pause Autobuff when SP is below this % (0 = disabled)" },
            { "tooltip.buff_cd", "Per-buff recast cooldown in milliseconds (e.g. 1500ms prevents double-use)" },
            { "tooltip.pre_renewal", "Show only Pre-Renewal / Transcendent class skills (hides 3rd/4th jobs, Doram, Night Watch, Hyper Novice, and Soul Ascetic)" },
            { "tooltip.pre_renewal_items", "Show only Pre-Renewal / Gold Times consumables (hides RK Runes, 3rd foods, 4th class candies, and custom items)" },
            { "tooltip.item_origin", "Filter between all consumables, Common Items only, or ROPs Items only" },
            { "filter.items_all", "All (Common + ROPs)" },
            { "filter.items_common", "Common Items Only" },
            { "filter.items_rops", "ROPs Items Only" },
            { "tooltip.pause_sitting", "Automatically pauses Autobuffs (skills & consumables) while your character is sitting (EFST_SIT)" },
            { "tooltip.auto_self_click", "Automatically sends a click to the center of the RO window (your character) when casting target buffs on yourself: Blessing, Increase Agility, Assumptio, Kyrie Eleison, Suffragium, Impositio Manus, and Aspersio." },
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
            { "filter.profile_json", "AureumRO 4Tools Profile (*.json)|*.json" },
            { "title.export_profile", "Export Profile" },
            { "msg.export_success", "Profile exported successfully!" },
            { "msg.export_error", "Failed to export profile: {0}" },
            { "title.import_profile", "Import Profile" },
            { "msg.import_success", "Profile '{0}' imported successfully!" },
            { "msg.import_error", "Failed to import profile: {0}" },
            { "filter.backup_zip", "AureumRO 4Tools Backup (*.zip)|*.zip" },
            { "title.backup_all", "Backup All" },
            { "msg.backup_success", "Backup created successfully ({0} file(s) exported)!" },
            { "msg.backup_error", "Failed to create backup: {0}" },
            { "title.restore_backup", "Restore Backup" },
            { "msg.restore_confirm", "Restore profiles, custom servers, and settings from this backup?\nExisting profiles with the same name will be updated." },
            { "msg.restore_summary", "Backup restored!\nProfiles restored: {0}\nCustom servers restored: {1}\nApp settings restored: {2}" },
            { "msg.restore_error", "Failed to restore backup: {0}" },
            { "title.key_conflicts", "Keybind Conflict Inspector" },

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
            { "test.open_failed", "Failed to open process memory for '{0}'. Try running AureumRO 4Tools as Administrator." },
            { "test.read_error", "Error reading process memory: {0}" },
            { "test.ok_report", "Connection OK!\nCharacter: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nActive Buffs: {5}" },
            { "test.not_detected", "(not detected)" },
            { "test.empty_values", "Addresses read 0/empty (HP: {0}/{1}, Name: empty).\nMake sure you are logged in on a map and the hex addresses match this client." },

            { "conflicts.none", "No keybind conflicts detected in profile '{0}'!" },
            { "conflicts.header", "Found {0} keybind conflict(s) in profile '{1}':" },
            { "usage.toggle", "ON/OFF Toggle Hotkey" },
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
            { "usage.macro_slot", "{0} (Lane {1} Slot)" }
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
            { "Acceleration", new BuffTranslation("Aceleração", "Acceleration") },
            { "Research Report", new BuffTranslation("Relatório de Pesquisa", "Research Report") },
            { "Create Hell Tree", new BuffTranslation("Criar Árvore Infernal", "Create Hell Tree") },

            // Thief / Assassin / Rogue / GX / SC / Shadow Cross / Abyss Chaser
            { "Hiding", new BuffTranslation("Esconderijo", "Hiding") },
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

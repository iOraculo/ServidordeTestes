using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class GuideForm : Form, IObserver
    {
        private sealed class GuideSectionData
        {
            public string Key;
            public string TitlePt;
            public string BodyPt;
            public string TitleEn;
            public string BodyEn;
        }

        private sealed class GuideCardUi
        {
            public GuideSectionData Data;
            public GroupBox Group;
            public Label BodyLabel;
            public bool IsVisible;
        }

        private const int CARD_LEFT = 10;
        private const int CARD_WIDTH = 548;
        private const int BODY_LEFT = 12;
        private const int BODY_WIDTH = 524;
        private const int TOP_BAR_HEIGHT = 40;

        private readonly List<GuideCardUi> cards = new List<GuideCardUi>();
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnClearSearch;
        private Label lblEmptyState;
        private ToolTip guideToolTip;
        private Font bodyFont;

        public GuideForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "GuideForm";
            this.Text = "Guide / Help";
            this.AutoScroll = true;
            this.Size = new Size(584, 281);
            this.bodyFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.guideToolTip = FormUtils.CreateHelpToolTip();

            BuildUi();
            if (subject != null)
            {
                subject.Attach(this);
            }
        }

        public int TotalSectionCount
        {
            get { return this.cards.Count; }
        }

        public int VisibleSectionCount
        {
            get
            {
                int count = 0;
                foreach (GuideCardUi card in this.cards)
                {
                    if (card.IsVisible)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private void BuildUi()
        {
            this.SuspendLayout();
            try
            {
                this.lblSearch = new Label
                {
                    AutoSize = true,
                    Location = new Point(10, 13),
                    Name = "lblGuideSearch",
                    Text = "Search guide:"
                };

                this.txtSearch = new TextBox
                {
                    Location = new Point(112, 10),
                    Name = "txtGuideSearch",
                    Size = new Size(370, 20),
                    Tag = FormUtils.SKIP_RESET_TAG
                };
                this.txtSearch.TextChanged += (s, e) => ApplySearchFilter(this.txtSearch.Text);

                this.btnClearSearch = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(490, 9),
                    Name = "btnClearGuideSearch",
                    Size = new Size(68, 22),
                    Text = "Clear",
                    UseVisualStyleBackColor = true
                };
                this.btnClearSearch.Click += (s, e) => this.txtSearch.Text = string.Empty;

                this.lblEmptyState = new Label
                {
                    AutoSize = false,
                    Location = new Point(10, TOP_BAR_HEIGHT + 16),
                    Name = "lblGuideEmpty",
                    Size = new Size(CARD_WIDTH, 28),
                    Tag = ThemeManager.TAG_MUTED_TEXT,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Visible = false
                };

                this.Controls.Add(this.lblSearch);
                this.Controls.Add(this.txtSearch);
                this.Controls.Add(this.btnClearSearch);
                this.Controls.Add(this.lblEmptyState);

                foreach (GuideSectionData section in BuildGuideSections())
                {
                    GroupBox gp = new GroupBox
                    {
                        AutoSize = false,
                        Name = "gbGuide_" + section.Key,
                        Size = new Size(CARD_WIDTH, 90),
                        TabStop = false
                    };

                    Label lblBody = new Label
                    {
                        AutoSize = false,
                        Font = this.bodyFont,
                        Location = new Point(BODY_LEFT, 20),
                        Name = "lblGuideBody_" + section.Key,
                        Size = new Size(BODY_WIDTH, 60)
                    };

                    gp.Controls.Add(lblBody);
                    this.Controls.Add(gp);

                    this.cards.Add(new GuideCardUi
                    {
                        Data = section,
                        Group = gp,
                        BodyLabel = lblBody,
                        IsVisible = true
                    });
                }

                RefreshLanguage();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private static List<GuideSectionData> BuildGuideSections()
        {
            return new List<GuideSectionData>
            {
                new GuideSectionData
                {
                    Key = "getting_started",
                    TitlePt = "1. Primeiros Passos e Barra Superior",
                    BodyPt =
                        "• Cliente (AureumRO.exe): quando há 1 janela aberta do AureumRO, o 4Tools seleciona automaticamente. Se abrir mais janelas, clique no botão atualizar (↻) e escolha qual janela controlar.\n" +
                        "• Ligar / Desligar (ON/OFF): clique no botão grande ou pressione a tecla global configurada (padrão: End). Marque 'Som' para ouvir aviso ao ligar/desligar.\n" +
                        "• Foco e Chat: 'Foco' pausa as automações quando você sai da janela do jogo. 'Chat' pausa ao apertar Enter para digitar no chat (Enter ou Esc retoma; Autopot em F1–F24 continua protegendo você).\n" +
                        "• Cidade, Mini-HUD e Visual: desmarque 'Usar Buffs em Cidade' para economizar consumíveis/SP nas cidades. Use 'Mini-HUD' para uma barra flutuante compacta de HP/SP sobre o jogo, além de Tema Escuro/Claro e Idioma PT/EN.",
                    TitleEn = "1. Getting Started and Top Bar",
                    BodyEn =
                        "• Client (AureumRO.exe): when 1 AureumRO window is open, 4Tools selects it automatically. With multiple windows, click Refresh (↻) and choose which window to control.\n" +
                        "• Toggle ON / OFF: click the main status button or press your global toggle key (default: End). Enable 'Audio' for a sound cue on toggle.\n" +
                        "• Focus and Chat Pause: 'Focus' pauses automations whenever Ragnarok is unfocused. 'Chat' pauses when pressing Enter to type (Enter or Esc resumes; F1–F24 Autopot remains active).\n" +
                        "• Town Pause, Mini-HUD and Theme: uncheck 'Use Buffs in Town' to save SP and consumables in towns. Toggle 'Mini-HUD' for a compact HP/SP overlay, Dark/Light theme, and PT/EN language."
                },
                new GuideSectionData
                {
                    Key = "autopot",
                    TitlePt = "2. Autopot e Yggdrasil",
                    BodyPt =
                        "• Autopot (padrão): defina a tecla da poção de HP (ex.: Branca/Mastela) e de SP (ex.: Azul/Morango) e a porcentagem limite (ex.: HP 80%, SP 40%). Sempre que cair abaixo da %, o 4Tools usa a poção.\n" +
                        "• Yggdrasil (emergência): funciona junto com o Autopot padrão! Coloque o Fruto ou Semente de Yggdrasil numa % menor (ex.: HP 35%, SP 15%) para só gastar Yggdrasil em situações críticas.\n" +
                        "• Atraso (ms): intervalo entre cada clique da poção (recomendado: 50 a 150ms).",
                    TitleEn = "2. Autopot and Yggdrasil",
                    BodyEn =
                        "• Standard Autopot: set your HP potion key (White/Mastela) and SP potion key (Blue/Strawberry) plus thresholds (e.g., HP 80%, SP 40%). Whenever vitals drop below the threshold, 4Tools pots automatically.\n" +
                        "• Yggdrasil (emergency): runs alongside Standard Autopot! Bind Yggdrasil Berry/Seed to a lower threshold (e.g., HP 35%, SP 15%) so it only fires in critical emergencies.\n" +
                        "• Delay (ms): interval between potion presses (recommended: 50–150ms)."
                },
                new GuideSectionData
                {
                    Key = "spammer",
                    TitlePt = "3. Spammer",
                    BodyPt =
                        "• 3 estados em cada caixinha de tecla (clique para alternar):\n" +
                        "   – Vazio: tecla desativada.\n" +
                        "   – Quadrado preenchido (■): spamma APENAS a tecla, sem clicar (ideal para skills sem alvo / self-cast como Impacto Explosivo, Crux Magnum, Rajada Congelante).\n" +
                        "   – Marcado (✓): spamma tecla + clique esquerdo do mouse (ideal para skills de alvo ou chão como Bomba Ácida, Rajada de Flechas, Lanças, Nevasca).\n" +
                        "• Compatibilidade vs. Speed Boost: Compatibilidade é o modo mais estável e libera 'Sem Shift' e 'Mouse Flick' (micro-movimento do mouse para melhorar cliques em skills de alvo). Speed Boost foca na máxima velocidade de spam.\n" +
                        "• Atraso (ms): quanto menor, mais rápido dispara (valores usuais: 10 a 30ms).",
                    TitleEn = "3. Skill Spammer",
                    BodyEn =
                        "• 3 states on every key checkbox (click to cycle):\n" +
                        "   – Empty: disabled.\n" +
                        "   – Filled square (■): spams ONLY the key without clicking (ideal for self-cast skills like Magnum Break, Grand Cross, Frost Nova).\n" +
                        "   – Checked (✓): spams key + left mouse click (ideal for target/ground skills like Acid Demonstration, Double Strafe, Bolts, Storm Gust).\n" +
                        "• Compatibility vs. Speed Boost: Compatibility is the most stable mode and enables 'No Shift' and 'Mouse Flick' (tiny cursor nudge for reliable target/ground clicks). Speed Boost prioritizes raw speed.\n" +
                        "• Delay (ms): lower values fire faster (typical: 10–30ms)."
                },
                new GuideSectionData
                {
                    Key = "autobuff_skills",
                    TitlePt = "4. Auto-skill",
                    BodyPt =
                        "• O 4Tools lê diretamente os ícones de buff ativos na memória do jogo e aperta a tecla da skill assim que o buff acabar.\n" +
                        "• Auto-clique em si (alvo): quando marcado, buffs que exigem clique no alvo (Bênção, Aumentar Agilidade, Assumptio, Kyrie Eleison, Sufrágio, Impositio Manus e Aspersio) clicam automaticamente no centro da janela (em cima do seu personagem)!\n" +
                        "• Pré-skill automática (Ninja): configure Aura Ninja + Imagem Falsa juntas — o 4Tools ativa a Aura Ninja primeiro e só dispara Imagem Falsa quando a Aura estiver ativa.\n" +
                        "• Modo Pré-Renewal, SP Mín%, CD(ms) e Pausar ao sentar: mantém visíveis apenas skills Pré-Renewal (incluindo Esconderijo, Basílica, Canto de Batalha e músicas de Bardo/Odalisca) e evita gastar SP à toa.",
                    TitleEn = "4. Auto-skill",
                    BodyEn =
                        "• 4Tools reads active status icons directly from client memory and recasts the skill as soon as the buff expires.\n" +
                        "• Auto-click self (target): when enabled, target-cast buffs (Blessing, Increase Agility, Assumptio, Kyrie Eleison, Suffragium, Impositio Manus, and Aspersio) automatically click the center of the RO window (your character)!\n" +
                        "• Automatic prerequisite (Ninja): bind both Ninja Aura and Mirror Image — 4Tools activates Ninja Aura first and waits until it is active before casting Mirror Image.\n" +
                        "• Pre-Renewal Mode, Min SP%, CD(ms), and Pause when sitting: focuses on Pre-Renewal skills (including Hiding, Basilica, Battle Chant, and Bard/Dancer songs) without wasting SP."
                },
                new GuideSectionData
                {
                    Key = "autobuff_items",
                    TitlePt = "5. Consumíveis",
                    BodyPt =
                        "• Renova automaticamente poções de ASPD, conversores elementais, comidas, caixas, pergaminhos e buffs de EXP assim que o ícone expira.\n" +
                        "• Divisão Comuns vs. ROPs: os itens do AureumRO são organizados em 'Itens Comuns' e 'Itens de ROPs', com seletor rápido no topo para filtrar apenas Comuns, apenas ROPs ou Todos.\n" +
                        "• Pausar ao sentar e Modo Pré-Renewal: ficam sincronizados entre Auto-skill e Consumíveis para evitar desperdício de itens.",
                    TitleEn = "5. Consumables",
                    BodyEn =
                        "• Automatically refreshes ASPD potions, elemental converters, foods, boxes, scrolls, and EXP buffs as soon as their icon expires.\n" +
                        "• Common vs. ROPs Split: AureumRO items are organized into 'Common Items' and 'ROPs Items', with a quick filter dropdown at the top.\n" +
                        "• Pause when sitting and Pre-Renewal Mode: stay synced across Auto-skill and Consumables to avoid wasting consumables."
                },
                new GuideSectionData
                {
                    Key = "debuffs",
                    TitlePt = "6. Anti-debuffs",
                    BodyPt =
                        "• Mostra exclusivamente os 7 status que podem ser curados por item ou skill no AureumRO:\n" +
                        "• Panaceia / Geleia Real (6 status): configure a 'Tecla única (todos os 6)' para curar Veneno, Silêncio, Cegueira, Maldição, Confusão/Caos e Alucinação com um único botão — ou defina teclas individuais (ex.: Poção Verde para Veneno/Silêncio/Cegueira e Água Benta para Maldição).\n" +
                        "• Aumentar Agilidade / Velocidade: configure uma tecla (Aumentar AGI, Pergaminho, Poção do Vento/Guyak ou Bala de Guaraná) para remover Diminuir Agilidade na hora.",
                    TitleEn = "6. Anti-Debuffs",
                    BodyEn =
                        "• Displays strictly the 7 status ailments curable by items or skills on AureumRO:\n" +
                        "• Panacea / Royal Jelly (6 statuses): set 'Single key (all 6)' to cure Poison, Silence, Blind, Curse, Confusion/Chaos, and Hallucination with one key — or set per-status keys (e.g., Green Potion for Poison/Silence/Blind and Holy Water for Curse).\n" +
                        "• Increase Agility / Speed: bind a key (Increase AGI, AGI Scroll, Speed Potion/Guyak, or Guarana Candy) to counter Decrease Agility immediately."
                },
                new GuideSectionData
                {
                    Key = "timers",
                    TitlePt = "7. Temporizadores",
                    BodyPt =
                        "• Pressiona uma tecla automaticamente em intervalos fixos de tempo (em segundos), independente de ícone de status.\n" +
                        "• Ideal para habilidades sem ícone na tela, armadilhas, itens sem status visível ou ações periódicas.\n" +
                        "• Começa limpo com o Timer 1; clique em '+ Adicionar timer' para expandir até 6 timers independentes.",
                    TitleEn = "7. Skill Timers",
                    BodyEn =
                        "• Presses a hotkey automatically on a fixed second interval, regardless of status icons.\n" +
                        "• Ideal for skills without a status icon, traps, or periodic utility actions.\n" +
                        "• Starts cleanly with Timer 1; click '+ Add Timer' to expand up to 6 independent timers."
                },
                new GuideSectionData
                {
                    Key = "macros",
                    TitlePt = "8. Macros (Troca, Músicas e ATK x DEF)",
                    BodyPt =
                        "• Macro Troca (só troca de equipamentos/teclas): aperte o 'Gatilho' uma vez para enviar uma sequência de até 7 teclas com Delay(ms) e Clique opcionais por passo (ex.: trocar 4 peças de equip com 1 botão). Marque 'Loop' para repetir até apertar o gatilho de novo.\n" +
                        "• Macro Músicas (Bardo/Odalisca): aperte o 'Gatilho' para tocar até 8 músicas em sequência, trocando automaticamente para a Adaga (cancelar/cortar) e de volta para o Instrumento/Chicote.\n" +
                        "• Macro ATK x DEF (Troca + Spammer integrado!): coloque a tecla da sua SKILL de ataque em 'Skill (Gatilho)'. Ao SEGURAR essa tecla, o 4Tools veste todo o set ATK (usando 'Troca(ms)'), spamma a própria skill enquanto você segura (usando 'Delay(ms)', com ou sem 'Com clique') e, ao SOLTAR a tecla, veste de volta todo o set DEF automaticamente!",
                    TitleEn = "8. Macros (Switch, Songs, and ATK x DEF)",
                    BodyEn =
                        "• Switch Macro (gear/key chains only): press 'Trigger' once to fire up to 7 keys in sequence with per-step Delay(ms) and optional Click (e.g., swap 4 gear pieces with 1 key). Check 'Loop' to repeat until pressed again.\n" +
                        "• Song Macro (Bard/Dancer): press 'Trigger' to play up to 8 songs in order, automatically swapping to Dagger (cancel) and back to Instrument/Whip.\n" +
                        "• ATK x DEF Macro (Gear Swap + Spammer combined!): put your attack SKILL hotkey in 'Skill (Trigger)'. While you HOLD that key, 4Tools equips your full ATK set (using 'Switch(ms)'), spams the skill while held (using 'Delay(ms)', with or without 'With click'), and when you RELEASE the key, swaps back to your full DEF set automatically!"
                },
                new GuideSectionData
                {
                    Key = "mount_priority",
                    TitlePt = "9. Montaria e Prioridade de Ações",
                    BodyPt =
                        "• Montaria (Rédea): informe a tecla da sua Rédea e ative 'Descer da montaria ao usar habilidade'. Sempre que você estiver montado e usar uma habilidade (manualmente ou pelo Spammer/Auto-skill/Temporizadores/Macros), o 4Tools desce da montaria automaticamente antes de disparar a skill!\n" +
                        "• Pausar Auto-skill montado e Remontar após inatividade: opções extras para não desmontar enquanto caminha pela cidade/mapa ou para remontar sozinho após ficar alguns segundos sem atacar.\n" +
                        "• Prioridade de Ações (4 níveis): o 4Tools sempre respeita a ordem fixa:\n" +
                        "   1º Input do jogador no jogo (pausa o 4Tools por alguns milissegundos — padrão 120ms, ajustável na aba Montaria).\n" +
                        "   2º Yggdrasil (tem prioridade sobre poções comuns e skills).\n" +
                        "   3º Poções comuns (têm prioridade sobre skills e demais automações).\n" +
                        "   4º Skills (Auto-skill), consumíveis, anti-debuffs e temporizadores.",
                    TitleEn = "9. Mount and Action Priority",
                    BodyEn =
                        "• Mount (Halter Lead): bind your Halter Lead hotkey and enable 'Dismount when using skill'. Whenever you are mounted and cast a skill (manually or via Spammer/Auto-skill/Timers/Macros), 4Tools automatically dismounts before firing the skill!\n" +
                        "• Pause Auto-skill while mounted and Auto remount after idle: optional helpers to stay mounted while traveling or automatically remount after a few idle seconds.\n" +
                        "• Action Priority (4 tiers): 4Tools enforces a strict priority order:\n" +
                        "   1st Player input in-game (briefly pauses 4Tools — default 120ms, adjustable in the Mount tab).\n" +
                        "   2nd Yggdrasil (takes priority over regular potions and skills).\n" +
                        "   3rd Standard potions (take priority over skills and other automations).\n" +
                        "   4th Skills (Auto-skill), consumables, anti-debuffs, and timers."
                },
                new GuideSectionData
                {
                    Key = "profiles",
                    TitlePt = "10. Perfis, Backup, Conflitos e Dicas Rápidas",
                    BodyPt =
                        "• Perfis por personagem/build: crie perfis separados (ex.: PvM, MVP, WoE), copie ou renomeie com 1 clique.\n" +
                        "• Troca automática por personagem: vincule um perfil ao nome do seu personagem ('Usar Personagem Ativo' → 'Salvar') e marque 'Ativar Troca Automática' para carregar o perfil sozinho ao trocar de char (enquanto em OFF).\n" +
                        "• Verificar Conflitos e Backup .zip: use 'Verificar Conflitos' para descobrir teclas duplicadas entre abas, e 'Backup Geral (.zip)' para salvar todos os seus perfis com segurança.\n" +
                        "• Popups em todo o app: sempre que tiver dúvida em qualquer botão, caixa ou opção, basta parar o mouse em cima por um instante para ver a explicação detalhada!",
                    TitleEn = "10. Profiles, Backup, Conflicts, and Quick Tips",
                    BodyEn =
                        "• Per-character/build profiles: create separate profiles (e.g., PvM, MVP, WoE), copy, or rename in 1 click.\n" +
                        "• Auto-Switch by Character: bind a profile to your character name ('Use Active Character' → 'Save') and enable 'Enable Auto-Switch' to auto-load when switching characters (while OFF).\n" +
                        "• Key Conflicts and .zip Backup: use 'Check Key Conflicts' to spot duplicate keys across tabs, and 'Backup All (.zip)' to safely back up all profiles.\n" +
                        "• Hover popups everywhere: whenever you're unsure about any button, field, or checkbox, just hover your mouse over it for a detailed popup!"
                }
            };
        }

        public void ApplySearchFilter(string query)
        {
            string normalizedQuery = Normalize(query);
            bool pt = LocalizationManager.IsPortuguese;

            this.SuspendLayout();
            try
            {
                int y = TOP_BAR_HEIGHT + 6;
                int visibleCount = 0;

                foreach (GuideCardUi card in this.cards)
                {
                    string title = pt ? card.Data.TitlePt : card.Data.TitleEn;
                    string body = pt ? card.Data.BodyPt : card.Data.BodyEn;

                    bool match = string.IsNullOrEmpty(normalizedQuery)
                        || Normalize(title).Contains(normalizedQuery)
                        || Normalize(body).Contains(normalizedQuery);

                    card.IsVisible = match;
                    card.Group.Visible = match;
                    if (match)
                    {
                        card.Group.Location = new Point(CARD_LEFT, y);
                        y += card.Group.Height + 8;
                        visibleCount++;
                    }
                }

                this.lblEmptyState.Visible = (visibleCount == 0);
                this.AutoScrollMinSize = new Size(0, Math.Max(TOP_BAR_HEIGHT + 40, y + 8));
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void RefreshLanguage()
        {
            bool pt = LocalizationManager.IsPortuguese;

            this.lblSearch.Text = pt ? "Buscar no guia:" : "Search guide:";
            this.btnClearSearch.Text = pt ? "Limpar" : "Clear";
            this.lblEmptyState.Text = pt
                ? "Nenhum tópico encontrado para essa busca."
                : "No guide topics match your search.";

            this.guideToolTip.SetToolTip(
                this.txtSearch,
                pt
                    ? "Digite qualquer palavra-chave (ex.: 'clique', 'atk', 'panaceia', 'ygg', 'chat', 'musica') para filtrar o guia."
                    : "Type any keyword (e.g., 'click', 'atk', 'panacea', 'ygg', 'chat', 'song') to filter guide sections.");
            this.guideToolTip.SetToolTip(
                this.btnClearSearch,
                pt ? "Limpa o campo de busca do guia." : "Clears the guide search filter.");

            foreach (GuideCardUi card in this.cards)
            {
                string title = pt ? card.Data.TitlePt : card.Data.TitleEn;
                string body = pt ? card.Data.BodyPt : card.Data.BodyEn;

                card.Group.Text = title;
                card.BodyLabel.Text = body;

                Size measured = TextRenderer.MeasureText(
                    body,
                    this.bodyFont,
                    new Size(BODY_WIDTH, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

                int bodyHeight = Math.Max(36, measured.Height + 6);
                card.BodyLabel.Size = new Size(BODY_WIDTH, bodyHeight);
                card.Group.Size = new Size(CARD_WIDTH, 20 + bodyHeight + 10);
            }

            ApplySearchFilter(this.txtSearch != null ? this.txtSearch.Text : string.Empty);
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string decomposed = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(decomposed.Length);
            for (int i = 0; i < decomposed.Length; i++)
            {
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(decomposed[i]);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(decomposed[i]);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            if (subj.Message.code == MessageCode.LANGUAGE_CHANGED)
            {
                RefreshLanguage();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.guideToolTip != null)
                {
                    this.guideToolTip.Dispose();
                    this.guideToolTip = null;
                }
                if (this.bodyFont != null)
                {
                    this.bodyFont.Dispose();
                    this.bodyFont = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class GuideForm : Form, IObserver
    {
        private sealed class GuideStepData
        {
            public string HeadingPt;
            public string DescPt;
            public string HeadingEn;
            public string DescEn;
        }

        private sealed class GuideSectionData
        {
            public string Key;
            public string BadgeSymbol;
            public Color AccentColor;
            public string TitlePt;
            public string TaglinePt;
            public string TipPt;
            public string TitleEn;
            public string TaglineEn;
            public string TipEn;
            public List<GuideStepData> Steps;
        }

        private sealed class GuideStepUi
        {
            public GuideStepData Data;
            public Label HeadingLabel;
            public Label DescLabel;
        }

        private sealed class GuideCardUi
        {
            public GuideSectionData Data;
            public Panel CardPanel;
            public Panel HeaderPanel;
            public Label BadgeLabel;
            public Label TitleLabel;
            public Label TaglineLabel;
            public List<GuideStepUi> StepUis;
            public Panel TipPanel;
            public Label TipLabel;
            public bool IsVisible;
        }

        private const int CARD_LEFT = 10;
        private const int CARD_WIDTH = 576;
        private const int HEADER_HEIGHT = 46;
        private const int CONTENT_LEFT = 16;
        private const int CONTENT_WIDTH = 544;
        private const int TOP_BAR_HEIGHT = 44;

        private readonly List<GuideCardUi> cards = new List<GuideCardUi>();
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnClearSearch;
        private Label lblEmptyState;
        private ToolTip guideToolTip;

        private Font badgeFont;
        private Font cardTitleFont;
        private Font cardTaglineFont;
        private Font stepHeadingFont;
        private Font stepBodyFont;
        private Font tipFont;

        public GuideForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "GuideForm";
            this.Text = "Guide / Help";
            this.AutoScroll = true;
            this.DoubleBuffered = true;
            this.Size = new Size(616, 289);

            this.badgeFont = new Font("Segoe UI Symbol", 13.5F, FontStyle.Bold);
            this.cardTitleFont = FontManager.Bold(10.25F);
            this.cardTaglineFont = FontManager.Italic(8.75F);
            this.stepHeadingFont = FontManager.Bold(9.25F);
            this.stepBodyFont = FontManager.Regular(9F);
            this.tipFont = FontManager.Regular(8.75F);

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
                    Location = new Point(10, 14),
                    Name = "lblGuideSearch",
                    Text = "Search guide:"
                };

                this.txtSearch = new TextBox
                {
                    Location = new Point(112, 11),
                    Name = "txtGuideSearch",
                    Size = new Size(398, 22),
                    Tag = FormUtils.SKIP_RESET_TAG
                };
                this.txtSearch.TextChanged += (s, e) => ApplySearchFilter(this.txtSearch.Text);

                this.btnClearSearch = new Button
                {
                    Cursor = Cursors.Hand,
                    Location = new Point(518, 9),
                    Name = "btnClearGuideSearch",
                    Size = new Size(68, 25),
                    Text = "Clear",
                    UseVisualStyleBackColor = true
                };
                this.btnClearSearch.Click += (s, e) => this.txtSearch.Text = string.Empty;

                this.lblEmptyState = new Label
                {
                    AutoSize = false,
                    Location = new Point(10, TOP_BAR_HEIGHT + 18),
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
                    GuideCardUi card = CreateGuideCard(section);
                    this.cards.Add(card);
                    this.Controls.Add(card.CardPanel);
                }

                RefreshLanguage();
                ApplyCustomThemeStyling();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private GuideCardUi CreateGuideCard(GuideSectionData section)
        {
            Panel cardPanel = new Panel
            {
                Name = "gbGuide_" + section.Key,
                Size = new Size(CARD_WIDTH, 140),
                Tag = ThemeManager.PRESERVE_COLORS_TAG
            };
            cardPanel.Paint += (s, e) => PaintCardBorder(cardPanel, section.AccentColor, e.Graphics);

            Panel headerPanel = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(CARD_WIDTH - 2, HEADER_HEIGHT),
                Tag = ThemeManager.PRESERVE_COLORS_TAG
            };
            headerPanel.Paint += (s, e) =>
            {
                using (SolidBrush stripe = new SolidBrush(section.AccentColor))
                {
                    e.Graphics.FillRectangle(stripe, 0, 0, 5, headerPanel.Height);
                }
            };

            Label badgeLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = this.badgeFont,
                ForeColor = section.AccentColor,
                Location = new Point(12, 6),
                Size = new Size(30, 30),
                Text = section.BadgeSymbol,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = ThemeManager.PRESERVE_COLORS_TAG,
                UseMnemonic = false
            };

            Label titleLabel = new Label
            {
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Font = this.cardTitleFont,
                Location = new Point(46, 5),
                Size = new Size(CARD_WIDTH - 62, 19),
                Tag = ThemeManager.PRESERVE_COLORS_TAG,
                UseMnemonic = false
            };

            Label taglineLabel = new Label
            {
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Font = this.cardTaglineFont,
                Location = new Point(46, 24),
                Size = new Size(CARD_WIDTH - 62, 16),
                Tag = ThemeManager.PRESERVE_COLORS_TAG,
                UseMnemonic = false
            };

            headerPanel.Controls.Add(badgeLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(taglineLabel);
            cardPanel.Controls.Add(headerPanel);

            List<GuideStepUi> stepUis = new List<GuideStepUi>();
            foreach (GuideStepData step in section.Steps)
            {
                Label headingLbl = new Label
                {
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Font = this.stepHeadingFont,
                    Location = new Point(CONTENT_LEFT, 56),
                    Size = new Size(CONTENT_WIDTH, 18),
                    Tag = ThemeManager.PRESERVE_COLORS_TAG,
                    UseMnemonic = false
                };

                Label descLbl = new Label
                {
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Font = this.stepBodyFont,
                    Location = new Point(CONTENT_LEFT + 14, 76),
                    Size = new Size(CONTENT_WIDTH - 14, 34),
                    Tag = ThemeManager.PRESERVE_COLORS_TAG,
                    UseMnemonic = false
                };

                cardPanel.Controls.Add(headingLbl);
                cardPanel.Controls.Add(descLbl);
                stepUis.Add(new GuideStepUi
                {
                    Data = step,
                    HeadingLabel = headingLbl,
                    DescLabel = descLbl
                });
            }

            Panel tipPanel = new Panel
            {
                Location = new Point(CONTENT_LEFT, 120),
                Size = new Size(CONTENT_WIDTH, 36),
                Tag = ThemeManager.PRESERVE_COLORS_TAG
            };
            tipPanel.Paint += (s, e) =>
            {
                using (SolidBrush leftBar = new SolidBrush(section.AccentColor))
                {
                    e.Graphics.FillRectangle(leftBar, 0, 0, 3, tipPanel.Height);
                }
            };

            Label tipLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = this.tipFont,
                Location = new Point(10, 6),
                Size = new Size(CONTENT_WIDTH - 18, 24),
                Tag = ThemeManager.PRESERVE_COLORS_TAG,
                UseMnemonic = false
            };
            tipPanel.Controls.Add(tipLabel);
            cardPanel.Controls.Add(tipPanel);

            return new GuideCardUi
            {
                Data = section,
                CardPanel = cardPanel,
                HeaderPanel = headerPanel,
                BadgeLabel = badgeLabel,
                TitleLabel = titleLabel,
                TaglineLabel = taglineLabel,
                StepUis = stepUis,
                TipPanel = tipPanel,
                TipLabel = tipLabel,
                IsVisible = true
            };
        }

        private static void PaintCardBorder(Panel panel, Color accent, Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool dark = ThemeManager.IsDarkMode;
            Color borderColor = dark
                ? Color.FromArgb(89, 66, 20)
                : Color.FromArgb(212, 192, 152);
            using (GraphicsPath path = SidebarNavButton.CreateRoundedRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8))
            using (Pen pen = new Pen(borderColor, 1f))
            {
                g.DrawPath(pen, path);
            }
        }

        private static List<GuideSectionData> BuildGuideSections()
        {
            return new List<GuideSectionData>
            {
                new GuideSectionData
                {
                    Key = "getting_started",
                    BadgeSymbol = "✦",
                    AccentColor = Color.FromArgb(214, 166, 46),
                    TitlePt = "1. Primeiros Passos & 3 Contas em 1",
                    TaglinePt = "Conecte suas janelas do AureumRO e controle tudo em um só lugar",
                    TipPt = "★ Dica: passe o mouse sobre qualquer botão ou caixinha do AureumTools para abrir uma explicação rápida!",
                    TitleEn = "1. Getting Started & 3-in-1 Accounts",
                    TaglineEn = "Connect your AureumRO windows and control everything in one place",
                    TipEn = "★ Tip: hover your mouse over any button or box in AureumTools to open an instant popup explanation!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ 3 Contas em 1 Janela (Conta 1 | Conta 2 | Conta 3)",
                            DescPt = "Alterne no topo entre 3 sessões completas e independentes — cada conta tem seu próprio Cliente (PID), Perfil, ON/OFF e automações rodando juntas em segundo plano.",
                            HeadingEn = "▸ 3 Accounts in 1 Window (Account 1 | Account 2 | Account 3)",
                            DescEn = "Switch at the top between 3 independent sessions — each tab has its own Client (PID), Profile, ON/OFF, and background automations running together."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Cliente (AureumRO.exe) & Botão Atualizar (↻)",
                            DescPt = "Com 1 janela aberta, o AureumTools conecta automaticamente. Com várias janelas, clique em ↻ e escolha qual PID fica em cada aba (Contas 2 e 3 evitam repetir o PID).",
                            HeadingEn = "▸ Client (AureumRO.exe) & Refresh Button (↻)",
                            DescEn = "With 1 game window open, AureumTools connects automatically. With multiple windows, click ↻ and pick a PID per tab."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Ligar / Desligar (ON/OFF), Mini HUD e Barra Superior",
                            DescPt = "Aperte sua tecla global (padrão: End) ou clique em ON/OFF. As opções Som, Foco, Chat e Mini HUD ficam organizadas no bloco LIGAR/DESLIGAR; no topo ficam Idioma, 'Pausar em cidades' (marcado por padrão) e o botão Sol/Lua do tema.",
                            HeadingEn = "▸ Toggle ON / OFF, Mini HUD & Top Bar",
                            DescEn = "Press your global hotkey (default: End) or click ON/OFF. Sound, Focus, Chat, and Mini HUD sit neatly in ON/OFF; Language, 'Pause in towns' (checked by default), and the Sun/Moon theme icon sit on the top bar."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "autopot",
                    BadgeSymbol = "♥",
                    AccentColor = Color.FromArgb(224, 92, 92),
                    TitlePt = "2. Autopot & Yggdrasil (Unificados)",
                    TaglinePt = "Recuperação normal e de emergência sempre visíveis no painel",
                    TipPt = "★ Dica: coloque Yggdrasil numa % menor que o Autopot comum (ex.: Poção Branca 80% e Yggdrasil 35%) para só gastar Fruto no sufoco!",
                    TitleEn = "2. Autopot & Yggdrasil (Unified)",
                    TaglineEn = "Standard potions and emergency recovery visible together at a glance",
                    TipEn = "★ Tip: set Yggdrasil to a lower % than standard Autopot (e.g., White Potion 80% and Yggdrasil 35%) so Berries only fire in emergencies!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Autopot Comum (Poção Branca / Poção Azul)",
                            DescPt = "Defina as teclas das suas poções de HP e SP junto com a % limite. Sempre que seu HP ou SP cair abaixo do valor, o AureumTools pota automaticamente.",
                            HeadingEn = "▸ Standard Autopot (White Potion / Blue Potion)",
                            DescEn = "Bind your HP and SP potion keys with percentage thresholds. Whenever vitals dip below the threshold, AureumTools pots automatically."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Yggdrasil (Emergência)",
                            DescPt = "Roda junto com o Autopot padrão em prioridade de emergência (Nível 2), ideal para Fruto ou Semente de Yggdrasil quando o dano é pesado.",
                            HeadingEn = "▸ Yggdrasil (Emergency)",
                            DescEn = "Runs alongside Standard Autopot at emergency priority (Tier 2), ideal for Yggdrasil Berry or Seed during heavy spikes."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Atraso (ms)",
                            DescPt = "Intervalo entre cada tentativa de potar. Valores entre 50ms e 150ms costumam dar o melhor equilíbrio entre resposta e fluidez.",
                            HeadingEn = "▸ Delay (ms)",
                            DescEn = "Interval between potion presses. Values between 50ms and 150ms give the best balance between speed and smoothness."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "spammer",
                    BadgeSymbol = "⚡",
                    AccentColor = Color.FromArgb(245, 158, 11),
                    TitlePt = "3. Spammer & Prioridade de Ações",
                    TaglinePt = "Disparo rápido de skills respeitando seus comandos manuais",
                    TipPt = "★ Dica: ajuste 'Pausa jogador' (em ms) para controlar por quanto tempo o AureumTools cede prioridade sempre que você pressiona uma tecla manual!",
                    TitleEn = "3. Skill Spammer & Action Priority",
                    TaglineEn = "Rapid skill casting that always respects your manual inputs",
                    TipEn = "★ Tip: tune 'Player pause' (ms) to control how long AureumTools yields priority whenever you press a manual key!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ 3 Estados em Cada Tecla (Clique para Alternar)",
                            DescPt = "Vazio = desativado  •  Quadrado (■) = spamma só a tecla sem clicar (ex.: Impacto Explosivo, Crux Magnum)  •  Marcado (✓) = spamma tecla + clique do mouse (ex.: Bomba Ácida, Lanças, Rajada).",
                            HeadingEn = "▸ 3 States on Every Key (Click to Cycle)",
                            DescEn = "Empty = off  •  Square (■) = spams key only without click (self-cast skills)  •  Checked (✓) = spams key + left click (target/ground skills)."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Compatibilidade vs. Speed Boost",
                            DescPt = "Compatibilidade é mais estável e libera 'Sem Shift' e 'Mouse Flick'. Speed Boost foca na velocidade máxima de disparo.",
                            HeadingEn = "▸ Compatibility vs. Speed Boost",
                            DescEn = "Compatibility is the most stable mode and unlocks 'No Shift' and 'Mouse Flick'. Speed Boost prioritizes raw spam speed."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Hierarquia de Prioridade (4 Níveis)",
                            DescPt = "1º Seu Input no teclado  →  2º Yggdrasil / Panaceia / Auto-Fuga  →  3º Consumíveis  →  4º Auto-skills. Suas teclas sempre vêm primeiro!",
                            HeadingEn = "▸ 4-Tier Action Priority",
                            DescEn = "1st Your Keyboard Input  →  2nd Yggdrasil / Panacea / Auto-Escape  →  3rd Consumables  →  4th Auto-skills. Your actions always come first!"
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "autobuff_skills",
                    BadgeSymbol = "★",
                    AccentColor = Color.FromArgb(96, 165, 250),
                    TitlePt = "4. Auto-skill (Com Filtro de Classe)",
                    TaglinePt = "Renovação inteligente lendo os buffs ativos na memória do jogo",
                    TipPt = "★ Dica: use o filtro 'Classe:' no topo para achar rapidamente Furtividade, Corpo Fechado, Proteção Química Total, Kaahi/Kaupe/Kaite e muito mais!",
                    TitleEn = "4. Auto-skill (With Class Filter)",
                    TaglineEn = "Smart buff renewal by reading active status icons from game memory",
                    TipEn = "★ Tip: use the 'Class:' dropdown at the top to jump straight to Cloaking, Steel Body, Full Chemical Protection, Kaahi/Kaupe/Kaite, and more!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Filtro por Classe & Busca Bilíngue",
                            DescPt = "Escolha sua classe no seletor 'Classe:' ou digite o nome em Português ou Inglês (ex.: 'Furtividade', 'Bênção', 'Corpo Fechado') para localizar instantaneamente.",
                            HeadingEn = "▸ Class Filter & Bilingual Search",
                            DescEn = "Pick your class from the 'Class:' dropdown or type in English or Portuguese (e.g., 'Cloaking', 'Blessing', 'Steel Body') to filter instantly."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Auto-clique em Si (Alvo) & Pré-skill Automática",
                            DescPt = "Buffs que pedem clique no alvo (Bênção, Agi, Assumptio, Kyrie, Aspersio, Proteção Química Total, Kaahi/Kaupe/Kaite/Kaizel) clicam no seu personagem sozinhos! No Ninja, Aura Ninja é usada antes da Imagem Falsa.",
                            HeadingEn = "▸ Auto-Click Self (Target) & Auto Prerequisite",
                            DescEn = "Target buffs (Blessing, Agi, Assumptio, Kyrie, Aspersio, Full Chemical Protection, Kaahi/Kaupe/Kaite/Kaizel) auto-click your character! For Ninja, Ninja Aura casts automatically before Mirror Image."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Modo Pré-Renewal, SP% Mínimo e Pausar ao Sentar",
                            DescPt = "Oculta skills 3rd/4th que não existem no AureumRO e protege seu SP quando estiver sentado ou com mana baixa.",
                            HeadingEn = "▸ Pre-Renewal Mode, Min SP% & Pause When Sitting",
                            DescEn = "Hides non-Pre-Renewal 3rd/4th skills and saves your SP while sitting or low on mana."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "autobuff_items",
                    BadgeSymbol = "⚗",
                    AccentColor = Color.FromArgb(52, 211, 153),
                    TitlePt = "5. Consumíveis (Comuns & ROPs)",
                    TaglinePt = "Poções de ASPD, comidas, conversores elementais, caixas e pergaminhos",
                    TipPt = "★ Dica: passe o mouse sobre qualquer ícone para ver o nome completo em Português e Inglês; todos os sprites seguem os ícones originais do jogo!",
                    TitleEn = "5. Consumables (Common & ROPs)",
                    TaglineEn = "ASPD potions, foods, elemental converters, boxes, and scrolls",
                    TipEn = "★ Tip: hover any icon to see its full name in English and Portuguese; every sprite matches the authentic in-game item icon!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Separação Comuns vs. ROPs",
                            DescPt = "Os itens são organizados em blocos de 'Itens Comuns' e 'Itens de ROPs', com um seletor no topo para exibir Todos, só Comuns ou só ROPs.",
                            HeadingEn = "▸ Common vs. ROPs Organization",
                            DescEn = "Items are grouped into 'Common Items' and 'ROPs Items', with a quick filter at the top to view All, Common only, or ROPs only."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Economia Automática",
                            DescPt = "'Pausar ao sentar' e 'Pausar em cidades' evitam queimar comidas, conversores e pergaminhos caros enquanto você descansa ou conversa em Prontera.",
                            HeadingEn = "▸ Automatic Item Savings",
                            DescEn = "'Pause when sitting' and 'Pause in towns' keep expensive foods, converters, and scrolls from firing while resting or idling in town."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "debuffs",
                    BadgeSymbol = "✚",
                    AccentColor = Color.FromArgb(167, 139, 250),
                    TitlePt = "6. Anti-debuffs",
                    TaglinePt = "Cura instantânea dos 7 status removíveis no AureumRO",
                    TipPt = "★ Dica: configure apenas a 'Tecla única (todos os 6)' com Panaceia ou Geleia Real e já cure 6 debuffs de uma só vez!",
                    TitleEn = "6. Anti-Debuffs",
                    TaglineEn = "Instant cure for all 7 curable status ailments on AureumRO",
                    TipEn = "★ Tip: bind Panacea or Royal Jelly to 'Single key (all 6)' to automatically cure 6 debuffs with a single hotkey!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Panaceia / Geleia Real (6 Debuffs)",
                            DescPt = "Cura Veneno, Silêncio, Cegueira, Maldição, Confusão e Alucinação automaticamente — com tecla única ou teclas individuais (Poção Verde, Água Benta).",
                            HeadingEn = "▸ Panacea / Royal Jelly (6 Debuffs)",
                            DescEn = "Cures Poison, Silence, Blind, Curse, Confusion, and Hallucination automatically — with a single key or individual keys (Green Pot, Holy Water)."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Aumentar Agilidade / Velocidade",
                            DescPt = "Remove Diminuir Agilidade na hora usando Aumentar AGI, Pergaminho de Agilidade, Poção do Vento/Guyak ou Bala de Guaraná.",
                            HeadingEn = "▸ Increase Agility / Speed",
                            DescEn = "Immediately counters Decrease Agility using Increase AGI, AGI Scroll, Speed Potion/Guyak, or Guarana Candy."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "timers",
                    BadgeSymbol = "⏱",
                    AccentColor = Color.FromArgb(56, 189, 248),
                    TitlePt = "7. Temporizadores",
                    TaglinePt = "Dispare até 6 teclas automaticamente em intervalos fixos",
                    TipPt = "★ Dica: ótimo para habilidades ou utilidades que não exibem ícone de buff na lateral da tela!",
                    TitleEn = "7. Skill Timers",
                    TaglineEn = "Trigger up to 6 hotkeys automatically on fixed second intervals",
                    TipEn = "★ Tip: perfect for skills or utilities that don't display a status icon on screen!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Até 6 Timers Independentes",
                            DescPt = "Começa limpo com o Timer 1. Clique em '+ Adicionar timer' para abrir mais espaços e defina a tecla + o tempo em segundos.",
                            HeadingEn = "▸ Up to 6 Independent Timers",
                            DescEn = "Starts clean with Timer 1. Click '+ Add Timer' to expand up to 6 slots and configure each key + interval in seconds."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "macros",
                    BadgeSymbol = "⚔",
                    AccentColor = Color.FromArgb(249, 115, 22),
                    TitlePt = "8. Macros (Troca, Músicas & ATK x DEF)",
                    TaglinePt = "Trocas de equipamento em cadeia, sequências musicais e combo ATK/DEF",
                    TipPt = "★ Dica: no Macro ATK x DEF, use 'Troca(ms)' entre 40 e 60ms para trocar várias peças sem o servidor perder nenhum clique!",
                    TitleEn = "8. Macros (Switch, Songs & ATK x DEF)",
                    TaglineEn = "Chained gear swaps, Bard/Dancer songs, and ATK/DEF combos",
                    TipEn = "★ Tip: in ATK x DEF Macro, set 'Switch(ms)' to 40–60ms so the server registers every gear piece cleanly!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Macro Troca (Switch)",
                            DescPt = "Aperte 1 gatilho para vestir uma sequência de até 7 peças com atraso individual e clique opcional. Marque 'Loop' para repetir.",
                            HeadingEn = "▸ Switch Macro",
                            DescEn = "Press 1 trigger to equip up to 7 gear slots in sequence with per-step delay and optional click. Enable 'Loop' to repeat."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Macro Músicas (Bardo / Odalisca)",
                            DescPt = "Toca até 8 músicas/danças em ordem, trocando automaticamente para Adaga (cortar animação) e de volta para o Instrumento/Chicote.",
                            HeadingEn = "▸ Song Macro (Bard / Dancer)",
                            DescEn = "Plays up to 8 songs/dances in order, automatically swapping to Dagger (cancel) and back to Instrument/Whip."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Macro ATK x DEF (Troca + Spammer)",
                            DescPt = "Coloque sua SKILL em 'Skill (Gatilho)': enquanto SEGURA, veste o set ATK e spamma a skill; ao SOLTAR, volta instantaneamente para o set DEF!",
                            HeadingEn = "▸ ATK x DEF Macro (Swap + Spammer)",
                            DescEn = "Put your attack SKILL in 'Skill (Trigger)': while HELD, equips ATK gear and spams; on RELEASE, swaps back to DEF gear!"
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "mount_priority",
                    BadgeSymbol = "♞",
                    AccentColor = Color.FromArgb(192, 132, 252),
                    TitlePt = "9. Montaria (Rédea)",
                    TaglinePt = "Desça da montaria automaticamente ao lançar habilidades",
                    TipPt = "★ Dica: ative 'Pausar Auto-skill montado' se não quiser descer da Rédea enquanto apenas atravessa um mapa!",
                    TitleEn = "9. Mount (Halter Lead)",
                    TaglineEn = "Automatically dismount your Halter Lead whenever you cast a skill",
                    TipEn = "★ Tip: enable 'Pause Auto-skill while mounted' if you want to stay mounted while traveling across maps!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Desmontar Automático & Teclas Extras",
                            DescPt = "Informe a tecla da sua Rédea e ative 'Descer da montaria ao usar habilidade'. Funciona com teclas do Spammer, Auto-skill, Macros e até 6 teclas manuais extras.",
                            HeadingEn = "▸ Auto Dismount & Extra Skill Keys",
                            DescEn = "Bind your Halter Lead key and enable 'Dismount when using skill'. Works with Spammer, Auto-skill, Macros, and up to 6 extra manual keys."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Remontar Após Inatividade",
                            DescPt = "Opcional: remonta sozinho após alguns milissegundos sem usar habilidades (somente se foi o AureumTools quem desmontou você).",
                            HeadingEn = "▸ Auto Remount After Idle",
                            DescEn = "Optional: automatically remounts after a brief idle delay (only when AureumTools triggered the dismount)."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "utilities",
                    BadgeSymbol = "⚑",
                    AccentColor = Color.FromArgb(45, 212, 191),
                    TitlePt = "10. Utilidades (@refresh & Auto-Fuga)",
                    TaglinePt = "Corrija bug de posição com 1 toque e escape automaticamente no sufoco",
                    TipPt = "★ Dica: coloque a Asa de Mosca em uma tecla F1–F24 para que o Auto-Fuga funcione até mesmo se o chat estiver aberto!",
                    TitleEn = "10. Utilities (@refresh & Auto-Escape)",
                    TaglineEn = "Fix position desync in 1 tap and auto-escape critical HP spikes",
                    TipEn = "★ Tip: bind Fly Wing to an F1–F24 key so Auto-Escape works even if your chat box is open!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Anti-Bug Position (@refresh)",
                            DescPt = "Envia Enter → @refresh → Enter no chat automaticamente via Atalho de 1 toque (ideal em WoE/PvP/MVP) ou em intervalo automático configurável.",
                            HeadingEn = "▸ Anti-Position Bug (@refresh)",
                            DescEn = "Automatically sends Enter → @refresh → Enter via a 1-tap Hotkey (great for WoE/PvP/MVP) or on a periodic interval."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Auto-Fuga de Emergência (Asa de Mosca / Teleporte)",
                            DescPt = "Se o HP cair abaixo da % crítica, dispara sua Asa de Mosca ou skill Teleporte (com opção 'Confirmar com Enter'). Nunca dispara morto nem em cidades.",
                            HeadingEn = "▸ Emergency Auto-Escape (Fly Wing / Teleport)",
                            DescEn = "When HP drops below your critical %, fires Fly Wing or Teleport skill (with optional 'Press Enter'). Never fires while dead or in towns."
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "aureum_hub",
                    BadgeSymbol = "★",
                    AccentColor = Color.FromArgb(234, 179, 8),
                    TitlePt = "11. Aureum Hub (Votos, Cartas & Mapas)",
                    TaglinePt = "Companheiro exclusivo do AureumRO com timers e rastreador completo",
                    TipPt = "★ Dica: já votou pelo celular? Digite a hora do último voto (ex.: 14:30) e clique em 'Definir' para calcular automaticamente quanto tempo falta!",
                    TitleEn = "11. Aureum Hub (Votes, Cards & Maps)",
                    TaglineEn = "Exclusive AureumRO companion with vote timers and full collection trackers",
                    TipEn = "★ Tip: already voted on mobile? Enter your last vote time (e.g., 14:30) and click 'Set' to automatically calculate the remaining cooldown!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Hora da Votação (Timer 12h + Lembrete)",
                            DescPt = "Acompanha os topsites oficiais do AureumRO, calcula o tempo restante a partir da hora do último voto e avisa na área de trabalho quando liberar de novo.",
                            HeadingEn = "▸ Vote Time (12h Timer + Reminder)",
                            DescEn = "Tracks official AureumRO topsites, calculates remaining time from your last vote timestamp, and sends desktop reminders when ready."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Coleção de Cartas (28 Coleções / 140 Cartas)",
                            DescPt = "Filtre por categoria ou busca e clique em cada carta para marcar/tachar as que você já tem, com botão rápido '/navi Colecionador'.",
                            HeadingEn = "▸ Card Collection (28 Collections / 140 Cards)",
                            DescEn = "Filter by category or search and click any card to mark it collected with strikethrough, plus a quick '/navi Card Collector' button."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Coleção de Mapas (311 Mapas com Setas de Direção)",
                            DescPt = "Agrupe por Cidade ou Tipo de Bônus, veja a referência e setas (← ↑ ↓ →) para chegar a pé sem precisar de @warp e consulte todos os itens de drop e % abaixo!",
                            HeadingEn = "▸ Map Collection (311 Maps with Directional Arrows)",
                            DescEn = "Group by City or Bonus Type, see reference town + arrows (← ↑ ↓ →) to walk there, and inspect all required drop items & rates below!"
                        }
                    }
                },
                new GuideSectionData
                {
                    Key = "profiles",
                    BadgeSymbol = "⚙",
                    AccentColor = Color.FromArgb(148, 163, 184),
                    TitlePt = "12. Perfis, Backup .zip & Diagnóstico",
                    TaglinePt = "Troca automática por personagem, backup seguro e inspetor de conflitos",
                    TipPt = "★ Dica: faça um 'Backup (.zip)' de tempos em tempos para guardar todos os seus perfis e configurações em um único arquivo!",
                    TitleEn = "12. Profiles, .zip Backup & Diagnostics",
                    TaglineEn = "Auto-switch by character, one-click backups, and conflict inspector",
                    TipEn = "★ Tip: create a 'Backup All (.zip)' occasionally to safely store all your profiles and settings in one archive!",
                    Steps = new List<GuideStepData>
                    {
                        new GuideStepData
                        {
                            HeadingPt = "▸ Perfis & Troca Automática por Personagem",
                            DescPt = "Crie perfis separados (PvM, MVP, WoE), vincule ao nome do personagem ('Usar Personagem Ativo' → 'Salvar') e o AureumTools troca de perfil sozinho ao logar.",
                            HeadingEn = "▸ Profiles & Auto-Switch by Character",
                            DescEn = "Create separate profiles (PvM, MVP, WoE), bind them to your character name ('Use Active Character' → 'Save'), and AureumTools loads them automatically."
                        },
                        new GuideStepData
                        {
                            HeadingPt = "▸ Verificar Conflitos & Ver Status (IDs)",
                            DescPt = "Descubra em 1 clique se a mesma tecla está duplicada entre abas e inspecione os IDs de status (EFST) lidos ao vivo da memória do jogo.",
                            HeadingEn = "▸ Check Key Conflicts & Inspect Status IDs",
                            DescEn = "Spot duplicate hotkeys across tabs in 1 click and inspect live EFST status IDs directly from game memory."
                        }
                    }
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
                int y = TOP_BAR_HEIGHT + 8;
                int visibleCount = 0;

                foreach (GuideCardUi card in this.cards)
                {
                    bool match = string.IsNullOrEmpty(normalizedQuery) || MatchesSection(card.Data, normalizedQuery, pt);

                    card.IsVisible = match;
                    card.CardPanel.Visible = match;
                    if (match)
                    {
                        card.CardPanel.Location = new Point(CARD_LEFT, y);
                        y += card.CardPanel.Height + 12;
                        visibleCount++;
                    }
                }

                this.lblEmptyState.Visible = (visibleCount == 0);
                this.AutoScrollMinSize = new Size(0, Math.Max(TOP_BAR_HEIGHT + 40, y + 10));
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private static bool MatchesSection(GuideSectionData data, string normalizedQuery, bool pt)
        {
            string title = pt ? data.TitlePt : data.TitleEn;
            string tagline = pt ? data.TaglinePt : data.TaglineEn;
            string tip = pt ? data.TipPt : data.TipEn;

            if (Normalize(title).Contains(normalizedQuery)
                || Normalize(tagline).Contains(normalizedQuery)
                || Normalize(tip).Contains(normalizedQuery))
            {
                return true;
            }

            foreach (GuideStepData step in data.Steps)
            {
                string heading = pt ? step.HeadingPt : step.HeadingEn;
                string desc = pt ? step.DescPt : step.DescEn;
                if (Normalize(heading).Contains(normalizedQuery) || Normalize(desc).Contains(normalizedQuery))
                {
                    return true;
                }
            }

            return false;
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
                    ? "Digite qualquer palavra-chave (ex.: 'clique', 'atk', 'panaceia', 'ygg', 'voto', 'furtividade') para filtrar o guia."
                    : "Type any keyword (e.g., 'click', 'atk', 'panacea', 'ygg', 'vote', 'cloaking') to filter guide sections.");
            this.guideToolTip.SetToolTip(
                this.btnClearSearch,
                pt ? "Limpa o campo de busca do guia." : "Clears the guide search filter.");

            foreach (GuideCardUi card in this.cards)
            {
                string cardTitle = pt ? card.Data.TitlePt : card.Data.TitleEn;
                card.CardPanel.Text = cardTitle;
                card.TitleLabel.Text = cardTitle;
                card.TaglineLabel.Text = pt ? card.Data.TaglinePt : card.Data.TaglineEn;

                int cursorY = HEADER_HEIGHT + 12;

                foreach (GuideStepUi stepUi in card.StepUis)
                {
                    string heading = pt ? stepUi.Data.HeadingPt : stepUi.Data.HeadingEn;
                    string desc = pt ? stepUi.Data.DescPt : stepUi.Data.DescEn;

                    stepUi.HeadingLabel.Text = heading;
                    Size headingSize = TextRenderer.MeasureText(
                        heading,
                        this.stepHeadingFont,
                        new Size(CONTENT_WIDTH, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                    int headingH = Math.Max(18, headingSize.Height + 2);
                    stepUi.HeadingLabel.Location = new Point(CONTENT_LEFT, cursorY);
                    stepUi.HeadingLabel.Size = new Size(CONTENT_WIDTH, headingH);
                    cursorY += headingH + 3;

                    stepUi.DescLabel.Text = desc;
                    Size descSize = TextRenderer.MeasureText(
                        desc,
                        this.stepBodyFont,
                        new Size(CONTENT_WIDTH - 14, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                    int descH = Math.Max(20, descSize.Height + 4);
                    stepUi.DescLabel.Location = new Point(CONTENT_LEFT + 14, cursorY);
                    stepUi.DescLabel.Size = new Size(CONTENT_WIDTH - 14, descH);
                    cursorY += descH + 10;
                }

                string tipText = pt ? card.Data.TipPt : card.Data.TipEn;
                card.TipLabel.Text = tipText;
                Size tipSize = TextRenderer.MeasureText(
                    tipText,
                    this.tipFont,
                    new Size(CONTENT_WIDTH - 18, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                int tipLabelH = Math.Max(18, tipSize.Height + 4);
                card.TipLabel.Size = new Size(CONTENT_WIDTH - 18, tipLabelH);
                card.TipPanel.Location = new Point(CONTENT_LEFT, cursorY + 2);
                card.TipPanel.Size = new Size(CONTENT_WIDTH, tipLabelH + 12);

                int cardHeight = card.TipPanel.Bottom + 12;
                card.CardPanel.Size = new Size(CARD_WIDTH, cardHeight);
            }

            ApplySearchFilter(this.txtSearch != null ? this.txtSearch.Text : string.Empty);
        }

        private void ApplyCustomThemeStyling()
        {
            bool dark = ThemeManager.IsDarkMode;
            Color cardBg = dark ? Color.FromArgb(30, 21, 6) : Color.FromArgb(255, 253, 246);
            Color headerBg = dark ? Color.FromArgb(44, 31, 10) : Color.FromArgb(247, 238, 212);
            Color titleColor = dark ? Color.FromArgb(250, 240, 212) : Color.FromArgb(46, 33, 12);
            Color taglineColor = dark ? Color.FromArgb(196, 178, 138) : Color.FromArgb(115, 96, 68);
            Color headingColor = dark ? Color.FromArgb(244, 214, 118) : Color.FromArgb(138, 94, 14);
            Color bodyColor = dark ? Color.FromArgb(236, 227, 202) : Color.FromArgb(52, 39, 18);
            Color tipBg = dark ? Color.FromArgb(42, 32, 12) : Color.FromArgb(250, 243, 222);
            Color tipFg = dark ? Color.FromArgb(248, 226, 152) : Color.FromArgb(108, 74, 14);

            foreach (GuideCardUi card in this.cards)
            {
                card.CardPanel.BackColor = cardBg;
                card.HeaderPanel.BackColor = headerBg;
                card.TitleLabel.ForeColor = titleColor;
                card.TaglineLabel.ForeColor = taglineColor;

                foreach (GuideStepUi stepUi in card.StepUis)
                {
                    stepUi.HeadingLabel.ForeColor = headingColor;
                    stepUi.DescLabel.ForeColor = bodyColor;
                }

                card.TipPanel.BackColor = tipBg;
                card.TipLabel.ForeColor = tipFg;
                card.CardPanel.Invalidate();
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            ApplyCustomThemeStyling();
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
                if (this.badgeFont != null) { this.badgeFont.Dispose(); this.badgeFont = null; }
            }

            base.Dispose(disposing);
        }
    }
}

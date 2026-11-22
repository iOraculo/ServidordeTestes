using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Input;
using _4RTools.Forms;
using _4RTools.Model;
using _4RTools.Utils;
using Xunit;

namespace _4RTools.Tests
{
    [Collection("StaticStateTests")]
    public class Wave3FeatureTests : IDisposable
    {
        private readonly string tempDir;
        private readonly string originalProfileFolder;
        private readonly string originalServerFile;
        private readonly string originalSettingsFile;
        private readonly bool originalDarkMode;
        private readonly string originalLanguage;

        public Wave3FeatureTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), "4RToolsWave3Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);

            this.originalProfileFolder = AppConfig.ProfileFolder;
            this.originalServerFile = LocalServerManager.LocalServerFilePath;
            this.originalSettingsFile = AppSettings.SettingsFilePath;
            this.originalDarkMode = ThemeManager.IsDarkMode;
            this.originalLanguage = LocalizationManager.CurrentLanguage;

            AppConfig.ProfileFolder = Path.Combine(this.tempDir, "Profile") + Path.DirectorySeparatorChar;
            LocalServerManager.LocalServerFilePath = Path.Combine(this.tempDir, "supported_servers.json");
            AppSettings.SettingsFilePath = Path.Combine(this.tempDir, "settings.json");
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
        }

        public void Dispose()
        {
            ThemeManager.ApplyTheme(null, this.originalDarkMode);
            LocalizationManager.SetLanguage(this.originalLanguage);
            AppConfig.ProfileFolder = this.originalProfileFolder;
            LocalServerManager.LocalServerFilePath = this.originalServerFile;
            AppSettings.SettingsFilePath = this.originalSettingsFile;

            try
            {
                if (Directory.Exists(this.tempDir))
                {
                    Directory.Delete(this.tempDir, true);
                }
            }
            catch
            {
                // Ignore temp cleanup errors.
            }
        }

        [Fact]
        public void AutoBuff_ShouldCastBuff_EnforcesMinSpCooldownQuagmireAndDebuffs()
        {
            AutoBuff autobuff = new AutoBuff();
            autobuff.minSpPercent = 30;
            autobuff.buffCooldownMs = 1500;
            autobuff.pauseOnDebuff = true;

            // Below minimum SP% should block cast.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 290, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 300, 1000));

            // Incapacitating debuff should block when pauseOnDebuff is enabled.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, true, 900, 1000));
            autobuff.pauseOnDebuff = false;
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, true, 900, 1000));

            // Quagmire blocks INC_AGI but still allows BLESSING.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.INC_AGI, 1000, true, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, true, false, 900, 1000));

            // Per-buff cooldown blocks only the buff recently cast until cooldown elapses.
            autobuff.RecordBuffCast(EffectStatusIDs.BLESSING, 1000);
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1500, false, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.INC_AGI, 1500, false, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 2500, false, false, 900, 1000));

            // ResetCooldowns clears tracked cast times immediately.
            autobuff.RecordBuffCast(EffectStatusIDs.BLESSING, 3000);
            autobuff.ResetCooldowns();
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 3100, false, false, 900, 1000));
        }

        [Fact]
        public void KeybindConflictDetector_DetectsRealConflictsAndAllowsSharedStatusCure()
        {
            Profile profile = new Profile("WoE");
            profile.Autopot.hpKey = Key.F1;
            profile.AHK.AddAHKEntry("chkF1", new KeyConfig(Key.F1, true));

            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.INC_AGI, Key.F2);
            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.BLESSING, Key.F2);

            // Sharing a cure key across multiple statuses in StatusRecovery is intentional (e.g. Panacea).
            profile.StatusRecovery.AddKeyToBuff(EffectStatusIDs.POISON, Key.F5);
            profile.StatusRecovery.AddKeyToBuff(EffectStatusIDs.SILENCE, Key.F5);

            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(profile);

            Assert.Equal(2, conflicts.Count);
            Assert.Contains(conflicts, c => c.Key == Key.F1);
            Assert.Contains(conflicts, c => c.Key == Key.F2);
            Assert.DoesNotContain(conflicts, c => c.Key == Key.F5);

            string ptReport = KeybindConflictDetector.FormatReport(profile.Name, conflicts);
            Assert.Contains("2 conflito(s)", ptReport);
            Assert.Contains("[F1]", ptReport);
            Assert.Contains("[F2]", ptReport);

            string ptClean = KeybindConflictDetector.FormatReport("Limpo", new List<KeybindConflict>());
            Assert.Contains("Nenhum conflito", ptClean);

            LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
            string enReport = KeybindConflictDetector.FormatReport(profile.Name, conflicts);
            Assert.Contains("Found 2 keybind conflict(s)", enReport);
            string enClean = KeybindConflictDetector.FormatReport("CleanProfile", new List<KeybindConflict>());
            Assert.Contains("No keybind conflicts detected", enClean);
        }

        [Fact]
        public void ProfileBackupManager_ExportAndImportZip_RoundTripsProfilesServersAndSettings()
        {
            ProfileSingleton.Create("Default");
            ProfileSingleton.Create("BioChem");
            ProfileSingleton.GetCurrent().Autobuff.minSpPercent = 25;
            ProfileSingleton.GetCurrent().Autobuff.buffCooldownMs = 1750;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autobuff);

            LocalServerManager.AddServer("00E12345", "00F54321", "customro");
            AppSettings.Save(new AppSettings
            {
                DarkMode = true,
                AlwaysOnTop = true,
                ShowMiniHud = true,
                MiniHudX = 320,
                MiniHudY = 140,
                Language = LocalizationManager.LANG_PT_BR
            });

            string backupZip = Path.Combine(this.tempDir, "full-backup.zip");
            int exportedCount = ProfileBackupManager.ExportBackupZip(backupZip);
            Assert.True(exportedCount >= 4);
            Assert.True(File.Exists(backupZip));

            // Wipe local files to simulate moving to a fresh PC.
            Directory.Delete(AppConfig.ProfileFolder, true);
            File.Delete(LocalServerManager.LocalServerFilePath);
            File.Delete(AppSettings.SettingsFilePath);

            BackupRestoreResult result = ProfileBackupManager.ImportBackupZip(backupZip);
            Assert.Equal(2, result.ProfilesRestored);
            Assert.True(result.ServersRestored);
            Assert.True(result.SettingsRestored);

            ProfileSingleton.Load("BioChem");
            Assert.Equal(25, ProfileSingleton.GetCurrent().Autobuff.minSpPercent);
            Assert.Equal(1750, ProfileSingleton.GetCurrent().Autobuff.buffCooldownMs);

            List<ClientDTO> restoredServers = LocalServerManager.GetLocalClients();
            Assert.Contains(restoredServers, s => s.name == "customro" && s.hpAddress == "00E12345");

            AppSettings restoredSettings = AppSettings.Load();
            Assert.True(restoredSettings.DarkMode);
            Assert.True(restoredSettings.ShowMiniHud);
            Assert.Equal(320, restoredSettings.MiniHudX);
            Assert.Equal(140, restoredSettings.MiniHudY);
            Assert.Equal(LocalizationManager.LANG_PT_BR, restoredSettings.Language);
        }

        [Fact]
        public void ProfileBackupManager_ImportBackupZip_RejectsZipSlipPathTraversal()
        {
            string badZip = Path.Combine(this.tempDir, "malicious.zip");
            using (FileStream fs = new FileStream(badZip, FileMode.Create, FileAccess.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                ZipArchiveEntry evil = archive.CreateEntry("Profile/../../evil.json");
                using (StreamWriter sw = new StreamWriter(evil.Open(), Encoding.UTF8))
                {
                    sw.Write("{\"Name\":\"Evil\"}");
                }
            }

            Assert.Throws<InvalidDataException>(() => ProfileBackupManager.ImportBackupZip(badZip));
        }

        [Fact]
        public void Client_TestMemoryAddresses_ValidatesInputsAndHandlesNonRunningProcess()
        {
            ServerAddressTestResult emptyProc = Client.TestMemoryAddresses("", "00E12345", "00F54321");
            Assert.False(emptyProc.Success);
            Assert.Contains("vazio", emptyProc.Message, StringComparison.OrdinalIgnoreCase);

            ServerAddressTestResult badHex = Client.TestMemoryAddresses("ragexe", "ZZZZ", "00F54321");
            Assert.False(badHex.Success);
            Assert.Contains("Endereço HP", badHex.Message);

            ServerAddressTestResult missingProc = Client.TestMemoryAddresses("NonExistentROProcess_4RTools_Test", "00E12345", "00F54321");
            Assert.False(missingProc.Success);
            Assert.Contains("não está em execução", missingProc.Message);
        }

        [Fact]
        public void LocalizationManager_TranslatesControlTreeBetweenPtBrAndEnUs()
        {
            using (System.Windows.Forms.Form form = new System.Windows.Forms.Form())
            using (System.Windows.Forms.Button btn = new System.Windows.Forms.Button())
            using (System.Windows.Forms.CheckBox chk = new System.Windows.Forms.CheckBox())
            using (System.Windows.Forms.TextBox input = new System.Windows.Forms.TextBox())
            using (System.Windows.Forms.DataGridView grid = new System.Windows.Forms.DataGridView())
            {
                form.Text = "Add New Server";
                btn.Text = "Save";
                chk.Text = "Dark Mode";
                input.Text = "Save"; // User input must never be overwritten.
                grid.Columns.Add("colName", "HP Address");

                form.Controls.Add(btn);
                form.Controls.Add(chk);
                form.Controls.Add(input);
                form.Controls.Add(grid);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                LocalizationManager.ApplyToControlTree(form);

                Assert.Equal("Adicionar Novo Servidor", form.Text);
                Assert.Equal("Salvar", btn.Text);
                Assert.Equal("Modo Escuro", chk.Text);
                Assert.Equal("Save", input.Text);
                Assert.Equal("Endereço HP", grid.Columns[0].HeaderText);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                LocalizationManager.ApplyToControlTree(form);

                Assert.Equal("Add New Server", form.Text);
                Assert.Equal("Save", btn.Text);
                Assert.Equal("Dark Mode", chk.Text);
                Assert.Equal("HP Address", grid.Columns[0].HeaderText);
            }
        }

        [Fact]
        public void MiniHudForm_UpdatesStatusIdentityVitalsAndThemesWithoutError()
        {
            using (MiniHudForm hud = new MiniHudForm())
            {
                hud.SetSavedPosition(-1, -1);
                hud.ApplyHudTheme(true);
                hud.UpdateStatus(true, false);
                hud.UpdateStatus(true, true);
                hud.UpdateStatus(false, false);
                hud.UpdateIdentity("LordKnight", "ArthurRO");
                hud.UpdateVitals(8500, 10000, 400, 800);
                hud.ClearVitals();
                hud.ApplyHudTheme(false);

                Assert.True(hud.TopMost);
                Assert.False(hud.ShowInTaskbar);
            }
        }

        [Fact]
        public void Profile_SkillTimers4To6_PersistLoadAndDetectConflicts()
        {
            ProfileSingleton.Create("TimerTest");
            Profile current = ProfileSingleton.GetCurrent();
            current.AutoRefreshSpammer4.RefreshKey = Key.F4;
            current.AutoRefreshSpammer4.RefreshDelay = 12;
            current.AutoRefreshSpammer5.RefreshKey = Key.F5;
            current.AutoRefreshSpammer5.RefreshDelay = 30;
            current.AutoRefreshSpammer6.RefreshKey = Key.F4; // Intentional duplicate with Timer 4 for conflict test
            current.AutoRefreshSpammer6.RefreshDelay = 60;

            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer4);
            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer5);
            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer6);

            ProfileSingleton.Load("TimerTest");
            Profile reloaded = ProfileSingleton.GetCurrent();

            Assert.Equal(Key.F4, reloaded.AutoRefreshSpammer4.RefreshKey);
            Assert.Equal(12, reloaded.AutoRefreshSpammer4.RefreshDelay);
            Assert.Equal(Key.F5, reloaded.AutoRefreshSpammer5.RefreshKey);
            Assert.Equal(30, reloaded.AutoRefreshSpammer5.RefreshDelay);
            Assert.Equal(Key.F4, reloaded.AutoRefreshSpammer6.RefreshKey);
            Assert.Equal(60, reloaded.AutoRefreshSpammer6.RefreshDelay);

            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(reloaded);
            Assert.Contains(conflicts, c => c.Key == Key.F4);

            // Legacy profile JSON without AutoRefreshSpammer04-06 must still load cleanly with defaults.
            string legacyPath = ProfileSingleton.GetProfilePath("LegacyTimers");
            File.WriteAllText(legacyPath, "{\"Name\":\"LegacyTimers\"}");
            ProfileSingleton.Load("LegacyTimers");
            Assert.Equal(Key.None, ProfileSingleton.GetCurrent().AutoRefreshSpammer4.RefreshKey);
            Assert.Equal(5, ProfileSingleton.GetCurrent().AutoRefreshSpammer5.RefreshDelay);
            Assert.Equal("AutoRefreshSpammer06", ProfileSingleton.GetCurrent().AutoRefreshSpammer6.GetActionName());
        }

        [Fact]
        public void ModernControls_ThemeAndLocalization_UpdateCorrectly()
        {
            using (System.Windows.Forms.Form shell = new System.Windows.Forms.Form())
            using (System.Windows.Forms.Panel sidebar = new System.Windows.Forms.Panel())
            using (System.Windows.Forms.Label groupLabel = new System.Windows.Forms.Label())
            using (ModernCardPanel card = new ModernCardPanel())
            using (SidebarNavButton navBtn = new SidebarNavButton("spammer", "Skill Spammer", SidebarIconKind.Spammer))
            using (SegmentedTabButton tabBtn = new SegmentedTabButton("Autopot"))
            using (StatusDot dot = new StatusDot())
            {
                shell.Tag = ThemeManager.TAG_CANVAS;
                sidebar.Tag = ThemeManager.TAG_SIDEBAR;
                groupLabel.Text = "COMBAT & AUTOMATION";
                groupLabel.Tag = ThemeManager.TAG_MUTED_TEXT;

                navBtn.IsActive = true;
                tabBtn.IsActive = true;
                dot.IsConnected = true;
                dot.CustomColor = System.Drawing.Color.FromArgb(34, 197, 94);

                sidebar.Controls.Add(groupLabel);
                sidebar.Controls.Add(navBtn);
                card.Controls.Add(tabBtn);
                card.Controls.Add(dot);
                shell.Controls.Add(sidebar);
                shell.Controls.Add(card);

                ThemeManager.ApplyTheme(shell, true);
                Assert.Equal(System.Drawing.Color.FromArgb(11, 7, 1), shell.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(18, 12, 3), sidebar.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(30, 21, 6), card.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(89, 66, 20), card.BorderColor);

                ThemeManager.ApplyTheme(shell, false);
                Assert.Equal(System.Drawing.Color.FromArgb(242, 232, 204), shell.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(250, 244, 224), sidebar.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(253, 248, 232), card.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(212, 192, 152), card.BorderColor);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                LocalizationManager.ApplyToControlTree(shell);
                Assert.Equal("COMBATE & AUTOMAÇÃO", groupLabel.Text);
                Assert.Equal("Spammer", navBtn.Text);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                LocalizationManager.ApplyToControlTree(shell);
                Assert.Equal("COMBAT & AUTOMATION", groupLabel.Text);
                Assert.Equal("Skill Spammer", navBtn.Text);
            }
        }

        [Fact]
        public void LocalizationManager_AllModernSectionsHaveTitlesAndSubtitlesInPtAndEn()
        {
            string[] sectionKeys = new string[]
            {
                "spammer", "debuff", "buff_skills", "buff_stuffs", "timers",
                "macros", "macro_switch", "macro_songs", "atkdef", "profiles", "servers"
            };

            string[] langs = new string[] { LocalizationManager.LANG_PT_BR, LocalizationManager.LANG_EN_US };
            foreach (string lang in langs)
            {
                LocalizationManager.SetLanguage(lang);
                foreach (string key in sectionKeys)
                {
                    string titleKey = "section." + key + ".title";
                    string subtitleKey = "section." + key + ".subtitle";
                    string title = LocalizationManager.T(titleKey);
                    string subtitle = LocalizationManager.T(subtitleKey);

                    Assert.False(string.IsNullOrWhiteSpace(title));
                    Assert.False(string.IsNullOrWhiteSpace(subtitle));
                    Assert.NotEqual(titleKey, title);
                    Assert.NotEqual(subtitleKey, subtitle);
                }

                Assert.NotEqual("footer.powered", LocalizationManager.T("footer.powered"));
                Assert.NotEqual("sidebar.status_on", LocalizationManager.T("sidebar.status_on"));
                Assert.NotEqual("sidebar.status_chat", LocalizationManager.T("sidebar.status_chat"));
                Assert.NotEqual("sidebar.status_off", LocalizationManager.T("sidebar.status_off"));
            }
        }

        [Fact]
        public void LocalizationManager_TranslatesAllBuffSkillsItemsAndDebuffsWithBilingualSearch()
        {
            List<Buff> allBuffs = new List<Buff>();
            allBuffs.AddRange(Buff.GetArcherSkills());
            allBuffs.AddRange(Buff.GetSwordmanSkill());
            allBuffs.AddRange(Buff.GetMageSkills());
            allBuffs.AddRange(Buff.GetMerchantSkills());
            allBuffs.AddRange(Buff.GetThiefSkills());
            allBuffs.AddRange(Buff.GetAcolyteSkills());
            allBuffs.AddRange(Buff.GetNinjaSkills());
            allBuffs.AddRange(Buff.GetTaekwonSkills());
            allBuffs.AddRange(Buff.GetGunsSkills());
            allBuffs.AddRange(Buff.GetSummonerSkills());
            allBuffs.AddRange(Buff.GetSoulAsceticSkills());
            allBuffs.AddRange(Buff.GetNightWatchSkills());
            allBuffs.AddRange(Buff.GetHyperNoviceSkills());
            allBuffs.AddRange(Buff.GetPotionsBuffs());
            allBuffs.AddRange(Buff.GetElementalsBuffs());
            allBuffs.AddRange(Buff.GetFoodBuffs());
            allBuffs.AddRange(Buff.GetBoxesBuffs());
            allBuffs.AddRange(Buff.GetScrollBuffs());
            allBuffs.AddRange(Buff.GetETCBuffs());
            allBuffs.AddRange(Buff.GetCandiesBuffs());
            allBuffs.AddRange(Buff.GetEXPBuffs());
            allBuffs.AddRange(Buff.GetHomunculusBuffs());
            allBuffs.AddRange(Buff.GetDebuffs());

            Assert.True(allBuffs.Count >= 260);
            foreach (Buff buff in allBuffs)
            {
                Assert.True(LocalizationManager.HasBuffTranslation(buff.name), "Missing translation for buff: " + buff.name);
            }

            // PT-BR mode shows "Portuguese (English)" and avoids duplicating identical names.
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Assert.Equal("Poção da Fúria Selvagem (Berserk Potion)", LocalizationManager.GetBuffDisplayName("Berserk Potion"));
            Assert.Equal("Concentração (Concentration)", LocalizationManager.GetBuffDisplayName("Concentration"));
            Assert.Equal("Atordoamento (Stun)", LocalizationManager.GetBuffDisplayName("STUN"));
            Assert.Equal("Ilimitar (Unlimit)", LocalizationManager.GetBuffDisplayName("Ilimitar"));
            Assert.Equal("Fúria Interior (Fury)", LocalizationManager.GetBuffDisplayName("Fury "));
            Assert.Equal("Magnificat", LocalizationManager.GetBuffDisplayName("Magnificat"));
            Assert.Equal("Glória", LocalizationManager.GetBuffDisplayName("Gloria"));

            // EN-US mode shows clean English names (including entries originally written in Portuguese).
            LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
            Assert.Equal("Berserk Potion", LocalizationManager.GetBuffDisplayName("Berserk Potion"));
            Assert.Equal("Unlimit", LocalizationManager.GetBuffDisplayName("Ilimitar"));
            Assert.Equal("Bubble Gum", LocalizationManager.GetBuffDisplayName("Goma de Mascar"));
            Assert.Equal("Stun", LocalizationManager.GetBuffDisplayName("STUN"));

            // Bilingual, accent-insensitive search matches both PT-BR and EN-US terms.
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "pocao"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "furia"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "berserk"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Concentration", "concentracao"));
            Assert.True(LocalizationManager.MatchesBuffSearch("STUN", "atordoamento"));
            Assert.True(LocalizationManager.MatchesBuffSearch("STUN", "stun"));
            Assert.False(LocalizationManager.MatchesBuffSearch("Berserk Potion", "Magnificat"));

            // BuffRenderer updates tooltips live when switching languages.
            using (System.Windows.Forms.GroupBox gp = new System.Windows.Forms.GroupBox())
            using (System.Windows.Forms.ToolTip tip = new System.Windows.Forms.ToolTip())
            {
                List<BuffContainer> containers = new List<BuffContainer>
                {
                    new BuffContainer(gp, new List<Buff>
                    {
                        new Buff("Berserk Potion", EffectStatusIDs.BERSERK_POTION, null)
                    })
                };

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                BuffRenderer renderer = new BuffRenderer("Autobuff", containers, tip);
                renderer.doRender(0);

                System.Windows.Forms.Control icon = gp.Controls["pbox" + (int)EffectStatusIDs.BERSERK_POTION];
                Assert.NotNull(icon);
                Assert.Equal("Poção da Fúria Selvagem (Berserk Potion)", tip.GetToolTip(icon));

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                renderer.RefreshLocalizedTooltips();
                Assert.Equal("Berserk Potion", tip.GetToolTip(icon));
            }
        }

        [Fact]
        public void EmbeddedServers_AlwaysIncludeAureumROAndTestServerWithExpectedAddresses()
        {
            string raw = ClientUpdaterForm.LoadResourceServerFile();
            List<ClientDTO> servers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ClientDTO>>(raw);

            Assert.NotNull(servers);
            ClientDTO aureum = servers.Find(s => string.Equals(s.name, "AureumRO", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(aureum);
            Assert.Equal("0x015FF908", aureum.hpAddress);
            Assert.Equal("0x01602568", aureum.nameAddress);
            Assert.True(ClientUpdaterForm.IsExclusiveServer(aureum));
            Assert.True(ClientUpdaterForm.IsAllowedServer(aureum, isLocalOverride: false));

            ClientDTO froggo = servers.Find(s => string.Equals(s.name, "FroggoClient", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(froggo);
            Assert.Equal("0x01138DDC", froggo.hpAddress);
            Assert.Equal("0x0113B7B0", froggo.nameAddress);
            Assert.Equal("0x00D2A3BC", froggo.mapAddress);
            Assert.True(ClientUpdaterForm.IsBuiltInTestServer(froggo));
            Assert.True(ClientUpdaterForm.IsAllowedServer(froggo, isLocalOverride: false));

            ClientDTO ragexe = servers.Find(s => string.Equals(s.name, "Ragexe", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(ragexe);
            Assert.True(ClientUpdaterForm.IsBuiltInTestServer(ragexe));
            Assert.True(ClientUpdaterForm.IsAllowedServer(ragexe, isLocalOverride: false));

            ClientDTO thirdParty = new ClientDTO("OtherRO", "Other", "0x01", "0x02");
            Assert.False(ClientUpdaterForm.IsExclusiveServer(thirdParty));
            Assert.False(ClientUpdaterForm.IsAllowedServer(thirdParty, isLocalOverride: false));
            Assert.True(ClientUpdaterForm.IsAllowedServer(thirdParty, isLocalOverride: true));
            Assert.Contains("iOraculo/AureumRO-4Tools", AppConfig._4RClientsURL);
        }

        [Fact]
        public void TestServerTownDetection_PronteraIsTownAndArenaIsField()
        {
            Assert.True(Client.IsTownMap("prontera"));
            Assert.True(Client.IsTownMap("prontera.rsw"));
            Assert.True(Client.IsTownMap("dicastes01.rsw"));
            Assert.False(Client.IsTownMap("pvp_n_1-5"));
            Assert.False(Client.IsTownMap("pvp_n_1-5.rsw"));
        }

        [Fact]
        public void LocalServerManager_AddServer_PreservesOptionalMapAddress()
        {
            LocalServerManager.AddServer("0x01138DDC", "0x0113B7B0", "FroggoClient", "0x00D2A3BC");
            List<ClientDTO> saved = LocalServerManager.GetLocalClients();
            ClientDTO entry = Assert.Single(saved);
            Assert.Equal("FroggoClient", entry.name);
            Assert.Equal("01138DDC", entry.hpAddress);
            Assert.Equal("0113B7B0", entry.nameAddress);
            Assert.Equal("00D2A3BC", entry.mapAddress);
        }

        [Fact]
        public void ThemeManager_StripLightBackground_RemovesWhiteBackdropAndPreservesSubjectPixels()
        {
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.White);
                    }
                }

                // Draw a solid gold/green subject in the center with a white inner highlight pixel.
                for (int y = 4; y <= 11; y++)
                {
                    for (int x = 4; x <= 11; x++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(255, 212, 175, 55));
                    }
                }
                bmp.SetPixel(7, 7, System.Drawing.Color.White);

                System.Drawing.Bitmap cleaned = ThemeManager.StripLightBackground(bmp) as System.Drawing.Bitmap;
                Assert.NotNull(cleaned);
                Assert.Equal(0, cleaned.GetPixel(0, 0).A);
                Assert.Equal(0, cleaned.GetPixel(15, 15).A);
                Assert.Equal(255, cleaned.GetPixel(5, 5).A);
                // Inner white highlight enclosed by subject must NOT be removed.
                Assert.Equal(255, cleaned.GetPixel(7, 7).A);
            }
        }

        [Fact]
        public void Client_IsTownMapAndUseBuffsInTownSetting_WorkCorrectly()
        {
            Assert.True(Client.IsTownMap("prontera.rsw"));
            Assert.True(Client.IsTownMap("Geffen"));
            Assert.True(Client.IsTownMap("moc_para01.rsw"));
            Assert.True(Client.IsTownMap("payon_in01.rsw"));
            Assert.False(Client.IsTownMap("prt_fild08.rsw"));
            Assert.False(Client.IsTownMap("gl_knt02.rsw"));
            Assert.False(Client.IsTownMap("abyss_03.rsw"));
            Assert.False(Client.IsTownMap(""));

            AppSettings defaults = AppSettings.Load();
            Assert.False(defaults.UseBuffsInTown);

            defaults.UseBuffsInTown = true;
            AppSettings.Save(defaults);
            Assert.True(AppSettings.Load().UseBuffsInTown);
        }

        [Fact]
        public void AutoBuff_PauseWhenSitting_BlocksBuffsWhileSitting()
        {
            AutoBuff autobuff = new AutoBuff();
            Assert.True(autobuff.pauseWhenSitting);

            // While sitting (EFST_SIT), autobuff must not fire when pauseWhenSitting is enabled.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, true));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, false));

            // Disabling pauseWhenSitting allows casting even while sitting.
            autobuff.pauseWhenSitting = false;
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, true));
        }

        [Fact]
        public void PreRenewalMode_FiltersOutThirdAndFourthClassSkillsAndGroups()
        {
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.CONCENTRATION));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.EDP));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.AURABLADE));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_GOSPEL));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.CARTBOOST));
            Assert.Equal(109u, (uint)EffectStatusIDs.EFST_GOSPEL);

            Buff gospel = Buff.GetSwordmanSkill().FirstOrDefault(b => b.effectStatusID == EffectStatusIDs.EFST_GOSPEL);
            Assert.NotNull(gospel);
            Assert.NotNull(gospel.icon);
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Assert.Equal("Canto de Batalha (Battle Chant / Gospel)", LocalizationManager.GetBuffDisplayName(gospel.name));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "canto"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "batalha"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "gospel"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "battle chant"));

            // Verify all Pre-Renewal Bard / Clown & Dancer / Gypsy songs, dances, and ensembles.
            EffectStatusIDs[] bardDancerIds = new EffectStatusIDs[]
            {
                EffectStatusIDs.EFST_WHISTLE,
                EffectStatusIDs.EFST_ASSASSINCROSS,
                EffectStatusIDs.EFST_APPLEIDUN,
                EffectStatusIDs.EFST_HUMMING,
                EffectStatusIDs.EFST_DONTFORGETME,
                EffectStatusIDs.EFST_FORTUNEKISS,
                EffectStatusIDs.EFST_SERVICEFORYOU,
                EffectStatusIDs.EFST_RICHMANKIM,
                EffectStatusIDs.EFST_ETERNALCHAOS,
                EffectStatusIDs.EFST_DRUMBATTLEFIELD,
                EffectStatusIDs.EFST_RINGNIBELUNGEN,
                EffectStatusIDs.EFST_ROKISWEIL,
                EffectStatusIDs.EFST_INTOABYSS,
                EffectStatusIDs.EFST_SIEGFRIED,
                EffectStatusIDs.EFST_MARIONETTE_MASTER,
                EffectStatusIDs.EFST_MOON,
                EffectStatusIDs.EFST_LONGING,
                EffectStatusIDs.EFST_HERMODE
            };
            List<Buff> archerSkills = Buff.GetArcherSkills();
            foreach (EffectStatusIDs id in bardDancerIds)
            {
                Assert.True(Buff.IsPreRenewalSkill(id), "Missing Pre-Renewal flag for: " + id);
                Buff found = archerSkills.FirstOrDefault(b => b.effectStatusID == id);
                Assert.NotNull(found);
                Assert.NotNull(found.icon);
                Assert.True(LocalizationManager.HasBuffTranslation(found.name), "Missing translation for: " + found.name);
            }
            Assert.Equal("Dança Cigana (Service for You)", LocalizationManager.GetBuffDisplayName("Service for You"));
            Assert.Equal("Maçãs de Idun (The Apple of Idun)", LocalizationManager.GetBuffDisplayName("The Apple of Idun"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Service for You", "danca cigana"));
            Assert.True(LocalizationManager.MatchesBuffSearch("The Apple of Idun", "macas de idun"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Invulnerable Siegfried", "siegfried"));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.UNLIMIT));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.ENCHANT_BLADE));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_HIDDEN_CARD));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_SHRIMP));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.GN_CARTBOOST));

            using (System.Windows.Forms.GroupBox archerGp = new System.Windows.Forms.GroupBox { Text = "Archer Skills" })
            using (System.Windows.Forms.GroupBox doramGp = new System.Windows.Forms.GroupBox { Text = "Summoner" })
            using (System.Windows.Forms.ToolTip tip = new System.Windows.Forms.ToolTip())
            {
                List<BuffContainer> containers = new List<BuffContainer>
                {
                    new BuffContainer(archerGp, new List<Buff>
                    {
                        new Buff("Concentration", EffectStatusIDs.CONCENTRATION, null),
                        new Buff("Ilimitar", EffectStatusIDs.UNLIMIT, null)
                    }),
                    new BuffContainer(doramGp, new List<Buff>
                    {
                        new Buff("Bunch of Shrimp", EffectStatusIDs.EFST_SHRIMP, null)
                    })
                };

                BuffRenderer renderer = new BuffRenderer("Autobuff", containers, tip);
                renderer.doRender(0);
                renderer.SetPreRenewalOnly(true);

                Assert.True(archerGp.Visible);
                Assert.True(archerGp.Controls["pbox" + (int)EffectStatusIDs.CONCENTRATION].Visible);
                Assert.False(archerGp.Controls["pbox" + (int)EffectStatusIDs.UNLIMIT].Visible);
                Assert.False(doramGp.Visible);

                renderer.SetPreRenewalOnly(false);
                Assert.True(archerGp.Controls["pbox" + (int)EffectStatusIDs.UNLIMIT].Visible);
                Assert.True(doramGp.Visible);
            }
        }

        [Fact]
        public void MacroSwitchForm_StartsSimplifiedWithOneSlotAndExpandsDynamically()
        {
            ProfileSingleton.Create("SwitchUiTest");
            ProfileSingleton.Load("SwitchUiTest");

            Subject subject = new Subject();
            using (MacroSwitchForm form = new MacroSwitchForm(subject))
            {
                // Fresh profile starts clean: only Switch 1 visible, with 1 slot.
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(1, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.False(form.IsLaneVisible(3));

                // Adding slots expands up to 7 and removing collapses back down cleanly.
                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                Assert.Equal(3, form.GetVisibleSlotCount(1));

                form.RemoveLastSlotFromLane(1);
                Assert.Equal(2, form.GetVisibleSlotCount(1));

                // Adding extra switch lanes reveals Switch 2 on demand.
                Assert.True(form.AddNextSwitchLane());
                Assert.True(form.IsLaneVisible(2));
                Assert.Equal(1, form.GetVisibleSlotCount(2));

                // Removing Switch 2 hides it again.
                form.ClearLane(2, true);
                Assert.False(form.IsLaneVisible(2));

                // Loading a profile with existing bindings auto-expands only configured lanes/slots.
                ChainConfig lane1 = ProfileSingleton.GetCurrent().MacroSwitch.chainConfigs.First(c => c.id == 1);
                lane1.trigger = Key.F1;
                lane1.macroEntries["in1mac1"] = new MacroKey(Key.F1, 50);
                lane1.macroEntries["in2mac1"] = new MacroKey(Key.F2, 50);
                lane1.macroEntries["in3mac1"] = new MacroKey(Key.F3, 50);

                ChainConfig lane3 = ProfileSingleton.GetCurrent().MacroSwitch.chainConfigs.First(c => c.id == 3);
                lane3.trigger = Key.F5;
                lane3.macroEntries["in1mac3"] = new MacroKey(Key.F5, 50);
                lane3.macroEntries["in2mac3"] = new MacroKey(Key.F6, 50);

                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(3, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.True(form.IsLaneVisible(3));
                Assert.Equal(2, form.GetVisibleSlotCount(3));
            }
        }

        [Fact]
        public void ATKDEFForm_ContainsAllTenEquipmentSlotsForAtkAndDef()
        {
            ProfileSingleton.Create("AtkDefTenSlotsTest");
            ProfileSingleton.Load("AtkDefTenSlotsTest");

            Subject subject = new Subject();
            using (ATKDEFForm form = new ATKDEFForm(subject))
            {
                for (int i = 1; i <= 10; i++)
                {
                    System.Windows.Forms.Control[] defMatches = form.Controls.Find("inDef" + i, true);
                    System.Windows.Forms.Control[] atkMatches = form.Controls.Find("inAtk" + i, true);
                    Assert.Single(defMatches);
                    Assert.Single(atkMatches);
                }

                // Verify new Hat Top (inDef6) and Right Accessory (inAtk10) persist through PROFILE_CHANGED.
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem("inDef6", Key.F1, ATKDEFEnum.DEF);
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem("inAtk10", Key.F9, ATKDEFEnum.ATK);
                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));

                Assert.Equal("F1", form.Controls.Find("inDef6", true)[0].Text);
                Assert.Equal("F9", form.Controls.Find("inAtk10", true)[0].Text);
            }
        }

        [Fact]
        public void MacroSongForm_StartsSimplifiedWithOneSongSlotAndExpandsDynamically()
        {
            ProfileSingleton.Create("SongUiTest");
            ProfileSingleton.Load("SongUiTest");

            Subject subject = new Subject();
            using (MacroSongForm form = new MacroSongForm(subject))
            {
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(1, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.False(form.IsLaneVisible(3));

                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                Assert.Equal(4, form.GetVisibleSlotCount(1));

                form.RemoveLastSlotFromLane(1);
                Assert.Equal(3, form.GetVisibleSlotCount(1));

                Assert.True(form.AddNextSongLane());
                Assert.True(form.IsLaneVisible(2));
                Assert.Equal(1, form.GetVisibleSlotCount(2));

                form.ResetLane(2, true);
                Assert.False(form.IsLaneVisible(2));

                ChainConfig song1 = ProfileSingleton.GetCurrent().SongMacro.chainConfigs.First(c => c.id == 1);
                song1.trigger = Key.F1;
                song1.macroEntries["in1mac1"] = new MacroKey(Key.F2, 50);
                song1.macroEntries["in2mac1"] = new MacroKey(Key.F3, 50);
                song1.macroEntries["in5mac1"] = new MacroKey(Key.F4, 50);

                ChainConfig song3 = ProfileSingleton.GetCurrent().SongMacro.chainConfigs.First(c => c.id == 3);
                song3.trigger = Key.F6;
                song3.macroEntries["in1mac3"] = new MacroKey(Key.F7, 50);

                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(5, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.True(form.IsLaneVisible(3));
                Assert.Equal(1, form.GetVisibleSlotCount(3));
            }
        }

        [Fact]
        public void StuffAutoBuffForm_PreRenewalAndCommonVsRopsFiltersWorkCorrectly()
        {
            Subject subject = new Subject();
            using (StuffAutoBuffForm form = new StuffAutoBuffForm(subject))
            {
                // Default Pre-Renewal Mode ON + All Origins: Common & ROPs visible, Renewal/Other hidden.
                form.SetPreRenewalFilter(true);
                form.ApplyOriginFilterIndex(0);
                Assert.True(form.IsGroupVisible("CommonPotionsGP"));
                Assert.True(form.IsGroupVisible("CommonElementalsGP"));
                Assert.True(form.IsGroupVisible("CommonFoodsBoxesGP"));
                Assert.True(form.IsGroupVisible("RopsCombatGP"));
                Assert.True(form.IsGroupVisible("RopsFoodsExpGP"));
                Assert.True(form.IsGroupVisible("RopsScrollsGP"));
                Assert.False(form.IsGroupVisible("RenewalOtherGP"));

                // Filter: Common Items Only (index 1).
                form.ApplyOriginFilterIndex(1);
                Assert.True(form.IsGroupVisible("CommonPotionsGP"));
                Assert.False(form.IsGroupVisible("RopsCombatGP"));
                Assert.False(form.IsGroupVisible("RopsScrollsGP"));

                // Filter: ROPs Items Only (index 2).
                form.ApplyOriginFilterIndex(2);
                Assert.False(form.IsGroupVisible("CommonPotionsGP"));
                Assert.True(form.IsGroupVisible("RopsCombatGP"));
                Assert.True(form.IsGroupVisible("RopsFoodsExpGP"));
                Assert.True(form.IsGroupVisible("RopsScrollsGP"));

                // Disabling Pre-Renewal Mode with All Origins reveals Renewal / Other items.
                form.ApplyOriginFilterIndex(0);
                form.SetPreRenewalFilter(false);
                Assert.True(form.IsGroupVisible("RenewalOtherGP"));
            }
        }

        [Fact]
        public void SkillTimerForm_StartsWithOneTimerAndExpandsDynamically()
        {
            ProfileSingleton.Create("SkillTimerUiTest");
            ProfileSingleton.Load("SkillTimerUiTest");

            Subject subject = new Subject();
            using (SkillTimerForm form = new SkillTimerForm(subject))
            {
                Assert.True(form.IsTimerVisible(1));
                Assert.False(form.IsTimerVisible(2));
                Assert.False(form.IsTimerVisible(4));

                Assert.True(form.AddNextTimer());
                Assert.True(form.IsTimerVisible(2));

                form.ClearTimer(2, true);
                Assert.False(form.IsTimerVisible(2));

                ProfileSingleton.GetCurrent().AutoRefreshSpammer4.RefreshKey = Key.F8;
                ProfileSingleton.GetCurrent().AutoRefreshSpammer4.RefreshDelay = 12;
                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));

                Assert.True(form.IsTimerVisible(1));
                Assert.False(form.IsTimerVisible(2));
                Assert.True(form.IsTimerVisible(4));
                Assert.Equal("F8", form.Controls.Find("txtSkillTimerKey4", true)[0].Text);
            }
        }

        [Fact]
        public void MacrosHubForm_SwitchesBetweenTabsAndMacroSwitchHasDedicatedTriggerKey()
        {
            ProfileSingleton.Create("MacrosHubTest");
            ProfileSingleton.Load("MacrosHubTest");

            Subject subject = new Subject();
            using (MacrosHubForm hub = new MacrosHubForm(subject))
            {
                Assert.Equal("switch", hub.ActiveTab);
                hub.SelectTab("songs");
                Assert.Equal("songs", hub.ActiveTab);
                hub.SelectTab("atkdef");
                Assert.Equal("atkdef", hub.ActiveTab);
                hub.SelectTab("switch");
                Assert.Equal("switch", hub.ActiveTab);

                // Dedicated Trigger box exists in MacroSwitchForm and updates chainConfig.trigger independently.
                System.Windows.Forms.Control[] triggerMatches = hub.Controls.Find("inTriggerSwitch1", true);
                Assert.Single(triggerMatches);
                triggerMatches[0].Text = "Q";

                ChainConfig lane1 = ProfileSingleton.GetCurrent().MacroSwitch.chainConfigs.First(c => c.id == 1);
                Assert.Equal(Key.Q, lane1.trigger);
            }
        }

        [Fact]
        public void DebuffRecoveryForm_ShowsOnlyItemAndSkillCurableDebuffs()
        {
            Assert.Equal(6, Buff.GetPanaceaCurableDebuffs().Count);
            Assert.Single(Buff.GetAgilityDebuffs());

            List<Buff> debuffs = Buff.GetDebuffs();
            Assert.Equal(7, debuffs.Count);

            // Non-curable, Soul-only, crowd-control, and Renewal-only debuffs must be excluded.
            EffectStatusIDs[] excludedDebuffs = new EffectStatusIDs[]
            {
                EffectStatusIDs.EFST_FREEZING,
                EffectStatusIDs.EFST_BURNING,
                EffectStatusIDs.EFST_BLEEDING,
                EffectStatusIDs.CRITICALWOUND,
                EffectStatusIDs.EFST_STUN,
                EffectStatusIDs.EFST_FREEZE,
                EffectStatusIDs.EFST_STONE,
                EffectStatusIDs.EFST_SLEEP,
                EffectStatusIDs.EFST_SPIDERWEB,
                EffectStatusIDs.EFST_CURSEDCIRCLE_TARGET,
                EffectStatusIDs.EFST_DEEP_SLEEP,
                EffectStatusIDs.EFST_HANDICAPSTATE_MISFORTUNE
            };
            foreach (EffectStatusIDs excluded in excludedDebuffs)
            {
                Assert.DoesNotContain(debuffs, b => b.effectStatusID == excluded);
            }

            // All 7 curable debuffs must have non-null icons.
            foreach (Buff debuff in debuffs)
            {
                Assert.NotNull(debuff.icon);
            }

            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Subject subject = new Subject();
            using (DebuffRecoveryForm form = new DebuffRecoveryForm(subject))
            {
                // Elvira Candy and Auto Stand cards must be removed from the form.
                Assert.Empty(form.Controls.Find("groupBox1", true));
                Assert.Empty(form.Controls.Find("txtNewStatusKey", true));
                Assert.Empty(form.Controls.Find("StatusRecoveryGP", true));
                Assert.Empty(form.Controls.Find("autoStandCB", true));

                // Categorized cards must exist and single-key Panacea shortcut must be inside DebuffRecoveryGP.
                Assert.Single(form.Controls.Find("DebuffRecoveryGP", true));
                Assert.Single(form.Controls.Find("gbAgilityDebuffs", true));
                Assert.Equal("DebuffRecoveryGP", form.Controls.Find("txtStatusKey", true)[0].Parent.Name);

                foreach (Buff debuff in debuffs)
                {
                    System.Windows.Forms.Control[] labelMatches = form.Controls.Find("lblDebuff" + (int)debuff.effectStatusID, true);
                    System.Windows.Forms.Control[] inputMatches = form.Controls.Find("in" + (int)debuff.effectStatusID, true);
                    Assert.Single(labelMatches);
                    Assert.Single(inputMatches);
                    Assert.False(string.IsNullOrWhiteSpace(labelMatches[0].Text));
                }

                Assert.Equal("Envenenamento", form.Controls.Find("lblDebuff" + (int)EffectStatusIDs.POISON, true)[0].Text);
                Assert.Equal("Cegueira", form.Controls.Find("lblDebuff" + (int)EffectStatusIDs.BLIND, true)[0].Text);
                Assert.Equal("Alucinação", form.Controls.Find("lblDebuff" + (int)EffectStatusIDs.HALLUCINATION, true)[0].Text);
                Assert.Equal("Diminuir Agilidade", form.Controls.Find("lblDebuff" + (int)EffectStatusIDs.EFST_DECREASE_AGI, true)[0].Text);
            }
        }

        [Fact]
        public void GuideForm_RendersAllSectionsSupportsSearchAndBilingualTooltips()
        {
            using (System.Windows.Forms.ToolTip tip = FormUtils.CreateHelpToolTip())
            {
                Assert.Equal(20000, tip.AutoPopDelay);
                Assert.True(tip.ShowAlways);
            }

            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Subject subject = new Subject();
            using (GuideForm guide = new GuideForm(subject))
            {
                Assert.Equal(10, guide.TotalSectionCount);
                Assert.Equal(10, guide.VisibleSectionCount);

                // Accent-insensitive Portuguese search for ATK x DEF / Macros
                guide.ApplySearchFilter("musicas");
                Assert.True(guide.VisibleSectionCount >= 1);
                Assert.True(guide.VisibleSectionCount < guide.TotalSectionCount);

                // Search for Mount / Priority topic
                guide.ApplySearchFilter("montaria");
                Assert.True(guide.VisibleSectionCount >= 1);

                // Nonsense search shows 0 cards
                guide.ApplySearchFilter("zzzz_nao_existe_12345");
                Assert.Equal(0, guide.VisibleSectionCount);

                // Clearing filter restores all 10 cards
                guide.ApplySearchFilter(string.Empty);
                Assert.Equal(10, guide.VisibleSectionCount);

                // Live language switch updates titles to English
                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                subject.Notify(new _4RTools.Utils.Message(MessageCode.LANGUAGE_CHANGED, LocalizationManager.LANG_EN_US));
                System.Windows.Forms.Control[] macrosGroup = guide.Controls.Find("gbGuide_macros", true);
                Assert.Single(macrosGroup);
                Assert.Contains("ATK x DEF", macrosGroup[0].Text);
                Assert.Contains("Songs", macrosGroup[0].Text);
            }
        }

        [Fact]
        public void AutoSkills_IncludesHidingMirrorImageBasilicaAndSelfTargetAcolyteBuffs()
        {
            List<Buff> thief = Buff.GetThiefSkills();
            List<Buff> ninja = Buff.GetNinjaSkills();
            List<Buff> acolyte = Buff.GetAcolyteSkills();

            Buff hiding = thief.FirstOrDefault(b => b.effectStatusID == EffectStatusIDs.EFST_HIDING);
            Assert.NotNull(hiding);
            Assert.NotNull(hiding.icon);
            Assert.True(hiding.IsPreRenewal);

            Buff mirrorImage = ninja.FirstOrDefault(b => b.effectStatusID == EffectStatusIDs.EFST_NJ_BUNSINJYUTSU);
            Assert.NotNull(mirrorImage);
            Assert.NotNull(mirrorImage.icon);
            Assert.True(mirrorImage.IsPreRenewal);

            EffectStatusIDs[] expectedAcolyte = new EffectStatusIDs[]
            {
                EffectStatusIDs.BLESSING,
                EffectStatusIDs.INC_AGI,
                EffectStatusIDs.ASSUMPTIO,
                EffectStatusIDs.EFST_KYRIE,
                EffectStatusIDs.EFST_SUFFRAGIUM,
                EffectStatusIDs.ASPERSIO
            };
            foreach (EffectStatusIDs id in expectedAcolyte)
            {
                Buff b = acolyte.FirstOrDefault(x => x.effectStatusID == id);
                Assert.NotNull(b);
                Assert.NotNull(b.icon);
                Assert.True(b.IsPreRenewal);
            }

            // Basilica exists in Acolyte list (for Renewal/custom icon servers) but is excluded from Pre-Renewal Mode
            // because Pre-Renewal SC_BASILICA does not emit an EFST icon to the client status buffer.
            Buff basilica = acolyte.FirstOrDefault(x => x.effectStatusID == EffectStatusIDs.EFST_BASILICA);
            Assert.NotNull(basilica);
            Assert.NotNull(basilica.icon);
            Assert.False(basilica.IsPreRenewal);

            // Equivalent EFST IDs: EFST_HEALTHSTATE_ANGELUS (888) satisfies ANGELUS (9); EFST_BASILICA_BUFF (1122) satisfies EFST_BASILICA (111).
            EffectStatusIDs equivalent;
            Assert.True(AutoBuff.TryGetEquivalentBuff(EffectStatusIDs.EFST_HEALTHSTATE_ANGELUS, out equivalent));
            Assert.Equal(EffectStatusIDs.ANGELUS, equivalent);
            Assert.True(AutoBuff.TryGetEquivalentBuff(EffectStatusIDs.EFST_BASILICA_BUFF, out equivalent));
            Assert.Equal(EffectStatusIDs.EFST_BASILICA, equivalent);

            // Shared recovery aliases resolve Blind / Curse / Silence / Poison variants for both Panacea and per-status cures.
            Assert.Equal(EffectStatusIDs.BLIND, StatusEffectRecoveryBase.ResolveCanonicalStatus(EffectStatusIDs.EFST_GVG_BLIND));
            Assert.Equal(EffectStatusIDs.BLIND, StatusEffectRecoveryBase.ResolveCanonicalStatus(EffectStatusIDs.EFST_HANDICAPSTATE_DEEPBLIND));
            Assert.Equal(EffectStatusIDs.SILENCE, StatusEffectRecoveryBase.ResolveCanonicalStatus(EffectStatusIDs.EFST_GVG_SILENCE));
            Assert.Equal(EffectStatusIDs.CURSE, StatusEffectRecoveryBase.ResolveCanonicalStatus(EffectStatusIDs.EFST_GVG_CURSE));
            Assert.Equal(EffectStatusIDs.POISON, StatusEffectRecoveryBase.ResolveCanonicalStatus(EffectStatusIDs.EFST_HEALTHSTATE_HEAVYPOISON));

            // Prerequisite check: Mirror Image requires Ninja Aura active first.
            EffectStatusIDs prereq;
            Assert.True(AutoBuff.TryGetPrerequisite(EffectStatusIDs.EFST_NJ_BUNSINJYUTSU, out prereq));
            Assert.Equal(EffectStatusIDs.AURA_NINJA, prereq);

            AutoBuff autobuff = new AutoBuff();
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.EFST_NJ_BUNSINJYUTSU, 1000, false, false, 100, 100, false, false));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.EFST_NJ_BUNSINJYUTSU, 1000, false, false, 100, 100, false, true));

            // Self-target auto-click detection for target-cast buffs vs self-cast buffs.
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.BLESSING));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.INC_AGI));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.ASSUMPTIO));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.EFST_KYRIE));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.EFST_SUFFRAGIUM));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.IMPOSITIO));
            Assert.True(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.ASPERSIO));
            Assert.False(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.EFST_BASILICA));
            Assert.False(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.EFST_HIDING));
            Assert.False(AutoBuff.RequiresSelfTargetClick(EffectStatusIDs.AURA_NINJA));
        }

        [Fact]
        public void ActionPriorityCoordinator_EnforcesFourTierPriorityOrder()
        {
            ActionPriorityCoordinator.Reset();
            try
            {
                ActionPriorityCoordinator.PlayerInputYieldMs = 120;
                ActionPriorityCoordinator.YggPriorityWindowMs = 250;
                ActionPriorityCoordinator.PotionPriorityWindowMs = 150;

                // Idle state: nothing yields.
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.PlayerInput, 1000));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 1000));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 1000));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1000));

                // Tier 1: Player input blocks Tier 2 (Ygg), Tier 3 (Potion), and Tier 4 (Skills) for 120ms.
                ActionPriorityCoordinator.NotifyPlayerInput(1000);
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.PlayerInput, 1050));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 1050));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 1050));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1050));
                // includePlayerInput: false ignores Tier 1 so held spammers don't delay themselves.
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1050, false));

                // After 120ms window expires, all tiers resume.
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 1120));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 1120));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1120));

                // Tier 2: Yggdrasil emergency blocks Tier 3 (Potion) and Tier 4 (Skills), but NOT Tier 2 (Ygg).
                ActionPriorityCoordinator.SetYggEmergencyActive(true);
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 1200));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 1200));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1200));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 1200, false));
                ActionPriorityCoordinator.SetYggEmergencyActive(false);

                // Tier 2: Recent Yggdrasil cast blocks Tier 3 & 4 for 250ms, then expires.
                ActionPriorityCoordinator.NotifyYggAction(2000);
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 2100));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 2100));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 2100));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 2250));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 2250));

                // Tier 3: Standard Potion blocks Tier 4 (Skills) for 150ms, but NEVER blocks Tier 2 (Yggdrasil).
                ActionPriorityCoordinator.NotifyPotionAction(3000);
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil, 3050));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.Potion, 3050));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 3050));
                Assert.True(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 3050, false));
                Assert.False(ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, 3150));
            }
            finally
            {
                ActionPriorityCoordinator.Reset();
            }
        }

        [Fact]
        public void AutoMount_PersistsInProfileTracksSkillKeysAndRendersMountForm()
        {
            Assert.Equal(613u, (uint)EffectStatusIDs.EFST_ALL_RIDING);
            Assert.Equal(614u, (uint)EffectStatusIDs.EFST_ALL_RIDING_REUSE_LIMIT);

            // Verify exact "Auto-skill" naming in both PT-BR and EN-US.
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Assert.Equal("Auto-skill", LocalizationManager.T("section.buff_skills.title"));
            Assert.Equal("Montaria", LocalizationManager.TranslateText("Mount"));

            ProfileSingleton.Create("MountFeatureTest");
            ProfileSingleton.Load("MountFeatureTest");

            Profile prof = ProfileSingleton.GetCurrent();
            prof.Autopot.hpKey = Key.F1;
            prof.AutopotYgg.hpKey = Key.F2;
            prof.AHK.AddAHKEntry("chkQ", new KeyConfig(Key.Q, true));
            prof.Autobuff.AddKeyToBuff(EffectStatusIDs.BLESSING, Key.W);
            prof.AutoRefreshSpammer1.RefreshKey = Key.E;
            prof.AtkDefMode.keySpammer = Key.R;

            prof.AutoMount.Enabled = true;
            prof.AutoMount.MountKey = Key.F9;
            prof.AutoMount.DismountOnSkill = true;
            prof.AutoMount.DismountDelayMs = 90;
            prof.AutoMount.PauseAutobuffWhileMounted = true;
            prof.AutoMount.IncludeOtherTabsSkillKeys = true;
            prof.AutoMount.AutoRemountAfterIdle = true;
            prof.AutoMount.RemountDelaySec = 7;
            prof.AutoMount.SetExtraSkillKey(0, Key.A);

            prof.UserPreferences.playerInputYieldMs = 150;
            ProfileSingleton.SetConfiguration(prof.AutoMount);
            ProfileSingleton.SetConfiguration(prof.UserPreferences);

            // Reload from disk and confirm persistence.
            ProfileSingleton.Load("MountFeatureTest");
            Profile reloaded = ProfileSingleton.GetCurrent();
            Assert.True(reloaded.AutoMount.Enabled);
            Assert.Equal(Key.F9, reloaded.AutoMount.MountKey);
            Assert.Equal(90, reloaded.AutoMount.DismountDelayMs);
            Assert.True(reloaded.AutoMount.PauseAutobuffWhileMounted);
            Assert.True(reloaded.AutoMount.AutoRemountAfterIdle);
            Assert.Equal(7, reloaded.AutoMount.RemountDelaySec);
            Assert.Equal(Key.A, reloaded.AutoMount.GetExtraSkillKey(0));
            Assert.Equal(150, reloaded.UserPreferences.playerInputYieldMs);

            // Tracked key checks: skills match, MountKey & potion keys never match.
            Assert.True(reloaded.AutoMount.IsTrackedSkillKey(Key.A, prof));
            Assert.True(reloaded.AutoMount.IsTrackedSkillKey(Key.Q, prof));
            Assert.True(reloaded.AutoMount.IsTrackedSkillKey(Key.W, prof));
            Assert.True(reloaded.AutoMount.IsTrackedSkillKey(Key.E, prof));
            Assert.True(reloaded.AutoMount.IsTrackedSkillKey(Key.R, prof));
            Assert.False(reloaded.AutoMount.IsTrackedSkillKey(Key.F9, prof));
            Assert.False(reloaded.AutoMount.IsTrackedSkillKey(Key.F1, prof));
            Assert.False(reloaded.AutoMount.IsTrackedSkillKey(Key.F2, prof));

            // MountForm loads mount keys cleanly (priority controls live in AHKForm / Spammer).
            Subject subject = new Subject();
            using (MountForm mountForm = new MountForm(subject))
            using (AHKForm ahkForm = new AHKForm(subject))
            {
                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));
                Assert.Equal("F9", mountForm.Controls.Find("txtMountKey", true)[0].Text);
                Assert.Equal("A", mountForm.Controls.Find("txtExtraSkill1", true)[0].Text);
                Assert.Empty(mountForm.Controls.Find("numPlayerYield", true));
                Assert.Single(ahkForm.Controls.Find("numPlayerYield", true));
                Assert.Single(ahkForm.Controls.Find("lblPriorityOrder", true));
                Assert.Equal(150, ActionPriorityCoordinator.PlayerInputYieldMs);
            }

            ActionPriorityCoordinator.Reset();
        }

        [Fact]
        public void MiniHudForm_UpdatesPauseBadgesAndTelemetry()
        {
            using (MiniHudForm hud = new MiniHudForm())
            {
                hud.UpdateStatus(false, false, false, false);
                Assert.Equal("OFF", hud.StatusBadgeText);

                hud.UpdateStatus(true, true, true, true);
                Assert.Equal("CHAT", hud.StatusBadgeText);

                hud.UpdateStatus(true, false, true, true);
                Assert.Equal("CIDADE", hud.StatusBadgeText);

                hud.UpdateStatus(true, false, false, true);
                Assert.Equal("FOCO", hud.StatusBadgeText);

                hud.UpdateStatus(true, false, false, false);
                Assert.Equal("ON", hud.StatusBadgeText);

                // Backwards-compatible 2-arg overload
                hud.UpdateStatus(true, true);
                Assert.Equal("CHAT", hud.StatusBadgeText);

                hud.UpdateTelemetry("pvp_n_1-5", 7);
                Assert.Equal("Mapa: pvp_n_1-5", hud.MapInfoText);
                Assert.Equal("Buffs: 7", hud.BuffCountText);

                hud.ClearVitals();
                Assert.Equal("Mapa: --", hud.MapInfoText);
                Assert.Equal("Buffs: 0", hud.BuffCountText);
            }
        }

        [Fact]
        public void ClientUpdaterForm_IsBuiltInTestServerName_DistinguishesTestFromOfficial()
        {
            Assert.True(ClientUpdaterForm.IsBuiltInTestServerName("FroggoClient"));
            Assert.True(ClientUpdaterForm.IsBuiltInTestServerName("ragexe"));
            Assert.False(ClientUpdaterForm.IsBuiltInTestServerName("AureumRO"));
            Assert.False(ClientUpdaterForm.IsBuiltInTestServerName(""));
            Assert.False(ClientUpdaterForm.IsBuiltInTestServerName(null));
        }
    }
}

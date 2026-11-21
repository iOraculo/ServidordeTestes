using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;
using Xunit;

namespace _4RTools.Tests
{
    [Collection("StaticStateTests")]
    public class ProfileAndServerTests : IDisposable
    {
        private readonly string tempDir;
        private readonly string originalProfileFolder;
        private readonly string originalServerFile;

        public ProfileAndServerTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), "4RToolsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);

            this.originalProfileFolder = AppConfig.ProfileFolder;
            this.originalServerFile = LocalServerManager.LocalServerFilePath;

            AppConfig.ProfileFolder = Path.Combine(this.tempDir, "Profile") + Path.DirectorySeparatorChar;
            LocalServerManager.LocalServerFilePath = Path.Combine(this.tempDir, "supported_servers.json");
        }

        public void Dispose()
        {
            AppConfig.ProfileFolder = this.originalProfileFolder;
            LocalServerManager.LocalServerFilePath = this.originalServerFile;

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
        public void Profile_Load_DoesNotLeakPreviousProfileSettings()
        {
            ProfileSingleton.Create("Knight");
            ProfileSingleton.GetCurrent().Autopot.hpPercent = 85;
            ProfileSingleton.GetCurrent().Autopot.hpKey = Key.F1;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autopot);

            // Create a second profile with minimal JSON (missing Autopot section).
            string sparsePath = ProfileSingleton.GetProfilePath("Wizard");
            File.WriteAllText(sparsePath, "{\"Name\":\"Wizard\"}");

            ProfileSingleton.Load("Wizard");

            Assert.Equal("Wizard", ProfileSingleton.GetCurrent().Name);
            Assert.Equal(0, ProfileSingleton.GetCurrent().Autopot.hpPercent);
            Assert.Equal(Key.None, ProfileSingleton.GetCurrent().Autopot.hpKey);
        }

        [Fact]
        public void Profile_SuppressSave_PreventsDiskWrites()
        {
            ProfileSingleton.Create("SuppressTest");
            string path = ProfileSingleton.GetProfilePath("SuppressTest");
            DateTime before = File.GetLastWriteTimeUtc(path);

            using (ProfileSingleton.SuppressSave())
            {
                ProfileSingleton.GetCurrent().Autopot.hpPercent = 77;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autopot);
            }

            ProfileSingleton.Load("SuppressTest");
            Assert.Equal(0, ProfileSingleton.GetCurrent().Autopot.hpPercent);
        }

        [Fact]
        public void Profile_ListAll_HandlesDotsInNamesAndIgnoresNonJson()
        {
            ProfileSingleton.Create("My.Char.v1");
            File.WriteAllText(Path.Combine(AppConfig.ProfileFolder, "notes.txt"), "ignore");

            List<string> profiles = Profile.ListAll();
            Assert.Contains("My.Char.v1", profiles);
            Assert.DoesNotContain("notes", profiles);
        }

        [Fact]
        public void Profile_RenameCopyExportImport_WorkEndToEnd()
        {
            ProfileSingleton.Create("Sniper");
            ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused = true;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);

            string copy1 = ProfileSingleton.Copy("Sniper");
            string copy2 = ProfileSingleton.Copy("Sniper");
            Assert.Equal("Sniper Copy", copy1);
            Assert.Equal("Sniper Copy (2)", copy2);

            ProfileSingleton.Rename("Sniper", "Ranger");
            Assert.Equal("Ranger", ProfileSingleton.GetCurrent().Name);

            string exportPath = Path.Combine(this.tempDir, "exported_ranger.json");
            ProfileSingleton.Export("Ranger", exportPath);
            Assert.True(File.Exists(exportPath));

            string importedName = ProfileSingleton.Import(exportPath);
            Assert.Equal("exported_ranger", importedName);

            ProfileSingleton.Load(importedName);
            Assert.True(ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("../evil")]
        [InlineData("bad/name")]
        [InlineData("bad\\name")]
        public void Profile_ValidateProfileName_RejectsInvalidNames(string invalidName)
        {
            Assert.Throws<ArgumentException>(() => ProfileSingleton.ValidateProfileName(invalidName));
        }

        [Theory]
        [InlineData("00E1234A", "00E1234A")]
        [InlineData("0x00e1234a", "00E1234A")]
        [InlineData("e1234a", "00E1234A")]
        [InlineData("0xGGGG1234", null)]
        [InlineData("123456789", null)]
        [InlineData("", null)]
        public void LocalServerManager_NormalizeHexAddress_ValidatesAndNormalizes(string input, string expected)
        {
            Assert.Equal(expected, LocalServerManager.NormalizeHexAddress(input));
        }

        [Fact]
        public void LocalServerManager_AddAndRemove_PersistsCorrectly()
        {
            LocalServerManager.AddServer("0x00E12345", "00F54321", "ragexe");
            List<ClientDTO> clients = LocalServerManager.GetLocalClients();

            Assert.Single(clients);
            Assert.Equal("ragexe", clients[0].name);
            Assert.Equal("00E12345", clients[0].hpAddress);

            clients[0].index = 0;
            LocalServerManager.RemoveClient(clients[0]);
            Assert.Empty(LocalServerManager.GetLocalClients());
        }

        [Fact]
        public void Macro_ResetMacro_OnlyClearsTargetLane()
        {
            Macro macro = new Macro(Macro.ACTION_NAME_SONG_MACRO, 4);
            macro.chainConfigs[0].trigger = Key.F1;
            macro.chainConfigs[1].trigger = Key.F2;

            macro.ResetMacro(1);

            Assert.Equal(Key.None, macro.chainConfigs[0].trigger);
            Assert.Equal(Key.F2, macro.chainConfigs[1].trigger);
        }

        [Fact]
        public void StatusAndDebuffRecovery_AddAndSerializeCleanly()
        {
            StatusRecovery statusRec = new StatusRecovery();
            statusRec.autoStand = true;
            statusRec.newStatusDelay = 500;
            statusRec.AddKeyToBuff(EffectStatusIDs.SILENCE, Key.F3);
            statusRec.AddKeyToBuff(EffectStatusIDs.POISON, Key.Escape);

            Dictionary<EffectStatusIDs, Key> snapshot = statusRec.GetBuffMappingSnapshot();
            Assert.Single(snapshot);
            Assert.Equal(Key.F3, snapshot[EffectStatusIDs.SILENCE]);
            Assert.Contains("newStatusDelay", statusRec.GetConfiguration());
        }
    }
}

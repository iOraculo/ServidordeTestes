using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;
using Xunit;

namespace _4RTools.Tests
{
    [Collection("StaticStateTests")]
    public class FeatureTests : IDisposable
    {
        private readonly string tempDir;
        private readonly string originalProfileFolder;
        private readonly string originalSettingsPath;
        private readonly bool originalDarkMode;

        public FeatureTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), "4RToolsFeatureTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);

            this.originalProfileFolder = AppConfig.ProfileFolder;
            this.originalSettingsPath = AppSettings.SettingsFilePath;
            this.originalDarkMode = ThemeManager.IsDarkMode;

            AppConfig.ProfileFolder = Path.Combine(this.tempDir, "Profile") + Path.DirectorySeparatorChar;
            AppSettings.SettingsFilePath = Path.Combine(this.tempDir, "settings.json");
            ChatPauseState.Resume();
        }

        public void Dispose()
        {
            ChatPauseState.Resume();
            ThemeManager.ApplyTheme(null, this.originalDarkMode);
            AppConfig.ProfileFolder = this.originalProfileFolder;
            AppSettings.SettingsFilePath = this.originalSettingsPath;

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
        public void AppSettings_SaveAndLoad_RoundTripsAndProvidesDefaultsWhenMissing()
        {
            AppSettings defaults = AppSettings.Load();
            Assert.True(defaults.DarkMode);
            Assert.False(defaults.AlwaysOnTop);
            Assert.True(defaults.AutoSwitchProfileByCharacter);

            AppSettings custom = new AppSettings
            {
                DarkMode = false,
                AlwaysOnTop = true,
                AutoSwitchProfileByCharacter = false
            };
            AppSettings.Save(custom);

            AppSettings reloaded = AppSettings.Load();
            Assert.False(reloaded.DarkMode);
            Assert.True(reloaded.AlwaysOnTop);
            Assert.False(reloaded.AutoSwitchProfileByCharacter);
        }

        [Fact]
        public void ChatPauseState_ToggleAndResume_NotifiesSubscribers()
        {
            int notifications = 0;
            EventHandler handler = (s, e) => notifications++;
            ChatPauseState.StateChanged += handler;
            try
            {
                Assert.False(ChatPauseState.IsChatting);

                ChatPauseState.ToggleChatting();
                Assert.True(ChatPauseState.IsChatting);
                Assert.Equal(1, notifications);

                // Calling SetChatting with same value should not raise duplicate notification.
                ChatPauseState.SetChatting(true);
                Assert.Equal(1, notifications);

                ChatPauseState.Resume();
                Assert.False(ChatPauseState.IsChatting);
                Assert.Equal(2, notifications);
            }
            finally
            {
                ChatPauseState.StateChanged -= handler;
                ChatPauseState.Resume();
            }
        }

        [Theory]
        [InlineData(Key.F1, false)]
        [InlineData(Key.F9, false)]
        [InlineData(Key.F12, false)]
        [InlineData(Key.F24, false)]
        [InlineData(Key.None, false)]
        [InlineData(Key.Q, true)]
        [InlineData(Key.A, true)]
        [InlineData(Key.D1, true)]
        [InlineData(Key.Space, true)]
        public void ChatPauseState_IsTypingKey_DistinguishesFunctionKeysFromTypingKeys(Key key, bool expectedTypingKey)
        {
            Assert.Equal(expectedTypingKey, ChatPauseState.IsTypingKey(key));
        }

        [Fact]
        public void Client_ShouldPauseAutopotKey_AllowsFunctionKeysWhileChatting()
        {
            ProfileSingleton.Create("ChatTest");
            ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused = false;
            Client client = new Client("ragexe", 0x00E12345, 0x00F12345);

            try
            {
                ChatPauseState.SetChatting(true);

                Assert.True(client.ShouldPauseAutomation());
                Assert.False(client.ShouldPauseAutopotKey(Key.F1));
                Assert.True(client.ShouldPauseAutopotKey(Key.Q));

                ChatPauseState.Resume();

                Assert.False(client.ShouldPauseAutomation());
                Assert.False(client.ShouldPauseAutopotKey(Key.Q));
            }
            finally
            {
                ChatPauseState.Resume();
                client.Dispose();
            }
        }

        [Fact]
        public void Profile_CharacterBinding_SetGetFindAndClear_WorkCaseInsensitively()
        {
            ProfileSingleton.Create("Default");
            ProfileSingleton.Create("HighWizard");
            ProfileSingleton.Create("LordKnight");

            ProfileSingleton.SetBoundCharacter("HighWizard", "  MerlinRO  ");
            ProfileSingleton.SetBoundCharacter("LordKnight", "ArthurRO");

            Assert.Equal("MerlinRO", ProfileSingleton.GetBoundCharacter("HighWizard"));
            Assert.Equal("HighWizard", ProfileSingleton.FindProfileForCharacter("merlinro"));
            Assert.Equal("LordKnight", ProfileSingleton.FindProfileForCharacter("ARTHURRO"));
            Assert.Null(ProfileSingleton.FindProfileForCharacter("UnknownChar"));

            // Active profile should also reflect the updated bound character immediately.
            Assert.Equal("ArthurRO", ProfileSingleton.GetCurrent().UserPreferences.boundCharacter);

            ProfileSingleton.SetBoundCharacter("HighWizard", string.Empty);
            Assert.Equal(string.Empty, ProfileSingleton.GetBoundCharacter("HighWizard"));
            Assert.Null(ProfileSingleton.FindProfileForCharacter("MerlinRO"));
        }

        [Fact]
        public void ThemeManager_ApplyTheme_StylesControlsAndPreservesTaggedStatusButtons()
        {
            using (Form form = new Form())
            using (Button statusButton = new Button())
            using (Button normalButton = new Button())
            using (TextBox input = new TextBox())
            using (VitalBar hpBar = new VitalBar("HP", Color.FromArgb(220, 76, 76)))
            {
                statusButton.Tag = ThemeManager.PRESERVE_COLORS_TAG;
                statusButton.BackColor = Color.Green;
                statusButton.ForeColor = Color.White;

                form.Controls.Add(statusButton);
                form.Controls.Add(normalButton);
                form.Controls.Add(input);
                form.Controls.Add(hpBar);

                ThemeManager.ApplyTheme(form, true);
                Assert.True(ThemeManager.IsDarkMode);
                Assert.Equal(Color.FromArgb(30, 21, 6), form.BackColor);
                Assert.Equal(Color.FromArgb(42, 29, 8), input.BackColor);
                Assert.Equal(Color.Green, statusButton.BackColor);
                Assert.Equal(Color.White, statusButton.ForeColor);

                hpBar.UpdateValues(7500, 10000);
                hpBar.ClearValues();

                ThemeManager.ApplyTheme(form, false);
                Assert.False(ThemeManager.IsDarkMode);
                Assert.Equal(Color.FromArgb(253, 248, 232), form.BackColor);
                Assert.Equal(Color.FromArgb(255, 251, 237), input.BackColor);
                Assert.Equal(Color.Green, statusButton.BackColor);
            }
        }

        [Fact]
        public void AHK_ClearAHKEntries_ClearsAllConfiguredKeys()
        {
            AHK ahk = new AHK();
            ahk.AddAHKEntry("chkQ", new KeyConfig(Key.Q, true));
            ahk.AddAHKEntry("chkW", new KeyConfig(Key.W, false));
            Assert.Equal(2, ahk.GetEntriesSnapshot().Count);

            ahk.ClearAHKEntries();
            Assert.Empty(ahk.GetEntriesSnapshot());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ProfileForm : Form
    {
        private readonly Container container;
        private readonly ToolTip profileToolTip;
        private CheckBox cbAutoSwitch;
        private Label lblTargetProfile;
        private TextBox txtBoundCharacter;
        private Button btnExportProfile;
        private Button btnImportProfile;
        private Button btnBackupZip;
        private Button btnRestoreZip;
        private Button btnCheckConflicts;
        private Button btnUseActiveChar;
        private Button btnSaveBinding;
        private Button btnClearBinding;
        private bool isUpdatingBindingUi;

        public ProfileForm(Container container)
        {
            InitializeComponent();
            this.container = container;
            this.profileToolTip = FormUtils.CreateHelpToolTip(this.components);
            InitializeImportExportButtons();
            InitializeCharacterBindingPanel();
            InitializeBackupAndDiagnosticsBar();
            this.lbProfilesList.SelectedIndexChanged += lbProfilesList_SelectedIndexChanged;
            RefreshProfileList();
            RefreshCharacterBindingUi();
            RefreshTooltips();
        }

        public void RefreshTooltips()
        {
            if (this.profileToolTip == null)
            {
                return;
            }

            this.profileToolTip.SetToolTip(this.txtProfileName, LocalizationManager.T("tooltip.profile_name"));
            this.profileToolTip.SetToolTip(this.btnSave, LocalizationManager.T("tooltip.profile_create"));
            this.profileToolTip.SetToolTip(this.btnEditProfile, LocalizationManager.T("tooltip.profile_rename"));
            this.profileToolTip.SetToolTip(this.btnCopy, LocalizationManager.T("tooltip.profile_copy"));
            this.profileToolTip.SetToolTip(this.btnRemoveProfile, LocalizationManager.T("tooltip.profile_delete"));
            if (this.btnExportProfile != null) this.profileToolTip.SetToolTip(this.btnExportProfile, LocalizationManager.T("tooltip.profile_export"));
            if (this.btnImportProfile != null) this.profileToolTip.SetToolTip(this.btnImportProfile, LocalizationManager.T("tooltip.profile_import"));
            if (this.btnBackupZip != null) this.profileToolTip.SetToolTip(this.btnBackupZip, LocalizationManager.T("tooltip.profile_backup"));
            if (this.btnRestoreZip != null) this.profileToolTip.SetToolTip(this.btnRestoreZip, LocalizationManager.T("tooltip.profile_restore"));
            if (this.btnCheckConflicts != null) this.profileToolTip.SetToolTip(this.btnCheckConflicts, LocalizationManager.T("tooltip.profile_conflicts"));
            if (this.cbAutoSwitch != null) this.profileToolTip.SetToolTip(this.cbAutoSwitch, LocalizationManager.T("tooltip.profile_autoswitch"));
            if (this.txtBoundCharacter != null) this.profileToolTip.SetToolTip(this.txtBoundCharacter, LocalizationManager.T("tooltip.profile_autoswitch"));
            if (this.btnUseActiveChar != null) this.profileToolTip.SetToolTip(this.btnUseActiveChar, LocalizationManager.T("tooltip.profile_use_char"));
            if (this.btnSaveBinding != null) this.profileToolTip.SetToolTip(this.btnSaveBinding, LocalizationManager.T("tooltip.profile_save_char"));
            if (this.btnClearBinding != null) this.profileToolTip.SetToolTip(this.btnClearBinding, LocalizationManager.T("tooltip.profile_clear_char"));
        }

        private void InitializeImportExportButtons()
        {
            this.btnExportProfile = new Button
            {
                Location = new Point(267, 164),
                Name = "btnExportProfile",
                Size = new Size(78, 23),
                Text = "Export",
                UseVisualStyleBackColor = true
            };
            this.btnExportProfile.Click += btnExport_Click;

            this.btnImportProfile = new Button
            {
                Location = new Point(267, 193),
                Name = "btnImportProfile",
                Size = new Size(78, 23),
                Text = "Import",
                UseVisualStyleBackColor = true
            };
            this.btnImportProfile.Click += btnImport_Click;

            this.Controls.Add(this.btnExportProfile);
            this.Controls.Add(this.btnImportProfile);
        }

        private void InitializeBackupAndDiagnosticsBar()
        {
            this.btnBackupZip = new Button
            {
                Location = new Point(23, 224),
                Name = "btnBackupZip",
                Size = new Size(115, 25),
                Text = "Backup All (.zip)",
                UseVisualStyleBackColor = true
            };
            this.btnBackupZip.Click += btnBackupZip_Click;

            this.btnRestoreZip = new Button
            {
                Location = new Point(146, 224),
                Name = "btnRestoreZip",
                Size = new Size(115, 25),
                Text = "Restore (.zip)",
                UseVisualStyleBackColor = true
            };
            this.btnRestoreZip.Click += btnRestoreZip_Click;

            this.btnCheckConflicts = new Button
            {
                Location = new Point(267, 224),
                Name = "btnCheckConflicts",
                Size = new Size(140, 25),
                Text = "Check Key Conflicts",
                UseVisualStyleBackColor = true
            };
            this.btnCheckConflicts.Click += btnCheckConflicts_Click;

            this.Controls.Add(this.btnBackupZip);
            this.Controls.Add(this.btnRestoreZip);
            this.Controls.Add(this.btnCheckConflicts);
        }

        private void InitializeCharacterBindingPanel()
        {
            GroupBox gbAutoSwitch = new GroupBox
            {
                Location = new Point(358, 13),
                Name = "gbAutoSwitchCharacter",
                Size = new Size(190, 204),
                TabStop = false,
                Text = "Auto-Switch by Character"
            };

            this.cbAutoSwitch = new CheckBox
            {
                AutoSize = true,
                Location = new Point(10, 20),
                Name = "cbAutoSwitch",
                Text = "Enable Auto-Switch",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbAutoSwitch.CheckedChanged += cbAutoSwitch_CheckedChanged;

            this.lblTargetProfile = new Label
            {
                Location = new Point(10, 44),
                Name = "lblTargetProfile",
                Size = new Size(170, 15),
                Text = "Profile: Default"
            };

            Label lblBoundChar = new Label
            {
                AutoSize = true,
                Location = new Point(10, 64),
                Name = "lblBoundChar",
                Text = "Character Name:"
            };

            this.txtBoundCharacter = new TextBox
            {
                Location = new Point(10, 81),
                Name = "txtBoundCharacter",
                Size = new Size(168, 20),
                Tag = FormUtils.SKIP_RESET_TAG
            };

            this.btnUseActiveChar = new Button
            {
                Location = new Point(10, 108),
                Name = "btnUseActiveChar",
                Size = new Size(168, 23),
                Text = "Use Active Character",
                UseVisualStyleBackColor = true
            };
            this.btnUseActiveChar.Click += btnUseActiveChar_Click;

            this.btnSaveBinding = new Button
            {
                Location = new Point(10, 137),
                Name = "btnSaveBinding",
                Size = new Size(80, 23),
                Text = "Save",
                UseVisualStyleBackColor = true
            };
            this.btnSaveBinding.Click += btnSaveBinding_Click;

            this.btnClearBinding = new Button
            {
                Location = new Point(98, 137),
                Name = "btnClearBinding",
                Size = new Size(80, 23),
                Text = "Clear",
                UseVisualStyleBackColor = true
            };
            this.btnClearBinding.Click += btnClearBinding_Click;

            Label lblBindingHint = new Label
            {
                Font = new Font("Microsoft Sans Serif", 7.5F),
                Location = new Point(10, 166),
                Name = "lblBindingHint",
                Size = new Size(170, 32),
                Text = "Auto-loads profile when this character is detected (while OFF)."
            };

            gbAutoSwitch.Controls.Add(this.cbAutoSwitch);
            gbAutoSwitch.Controls.Add(this.lblTargetProfile);
            gbAutoSwitch.Controls.Add(lblBoundChar);
            gbAutoSwitch.Controls.Add(this.txtBoundCharacter);
            gbAutoSwitch.Controls.Add(this.btnUseActiveChar);
            gbAutoSwitch.Controls.Add(this.btnSaveBinding);
            gbAutoSwitch.Controls.Add(this.btnClearBinding);
            gbAutoSwitch.Controls.Add(lblBindingHint);

            this.Controls.Add(gbAutoSwitch);
        }

        private string GetTargetProfileForBinding()
        {
            if (this.lbProfilesList.SelectedItem != null)
            {
                return this.lbProfilesList.SelectedItem.ToString();
            }

            Profile current = ProfileSingleton.GetCurrent();
            return current != null ? current.Name : "Default";
        }

        public void RefreshCharacterBindingUi()
        {
            if (this.txtBoundCharacter == null || this.lblTargetProfile == null || this.cbAutoSwitch == null)
            {
                return;
            }

            this.isUpdatingBindingUi = true;
            try
            {
                string targetProfile = GetTargetProfileForBinding();
                this.cbAutoSwitch.Checked = this.container != null && this.container.GetAutoSwitchByCharacter();
                this.lblTargetProfile.Text = LocalizationManager.T("label.profile_prefix", targetProfile);
                this.txtBoundCharacter.Text = ProfileSingleton.GetBoundCharacter(targetProfile);
            }
            finally
            {
                this.isUpdatingBindingUi = false;
            }
        }

        private void lbProfilesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshCharacterBindingUi();
        }

        private void cbAutoSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingBindingUi || this.container == null)
            {
                return;
            }

            this.container.SetAutoSwitchByCharacter(this.cbAutoSwitch.Checked);
        }

        private void btnUseActiveChar_Click(object sender, EventArgs e)
        {
            Client client = ClientSingleton.GetClient();
            if (client == null || !client.IsProcessAlive() || !client.HasMemorySupport)
            {
                MessageBox.Show(
                    LocalizationManager.T("msg.select_client_first"),
                    LocalizationManager.T("title.active_char"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string name = client.ReadCharacterName();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    LocalizationManager.T("msg.char_not_detected"),
                    LocalizationManager.T("title.active_char"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.txtBoundCharacter.Text = name.Trim();
        }

        private void btnSaveBinding_Click(object sender, EventArgs e)
        {
            string targetProfile = GetTargetProfileForBinding();
            try
            {
                string charName = this.txtBoundCharacter.Text != null ? this.txtBoundCharacter.Text.Trim() : string.Empty;
                ProfileSingleton.SetBoundCharacter(targetProfile, charName);
                RefreshCharacterBindingUi();

                if (!string.IsNullOrEmpty(charName) && this.container != null)
                {
                    Client client = ClientSingleton.GetClient();
                    if (client != null && client.IsProcessAlive() && client.HasMemorySupport)
                    {
                        string activeChar = client.ReadCharacterName();
                        if (!string.IsNullOrWhiteSpace(activeChar))
                        {
                            this.container.TryAutoSwitchProfileForCharacter(activeChar.Trim());
                        }
                    }
                }

                string msg = string.IsNullOrEmpty(charName)
                    ? LocalizationManager.T("msg.binding_cleared", targetProfile)
                    : LocalizationManager.T("msg.binding_saved", targetProfile, charName);
                MessageBox.Show(msg, LocalizationManager.T("title.char_binding"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to save character binding", ex);
                MessageBox.Show(
                    LocalizationManager.T("msg.binding_save_error", ex.Message),
                    LocalizationManager.T("title.error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnClearBinding_Click(object sender, EventArgs e)
        {
            this.txtBoundCharacter.Text = string.Empty;
            btnSaveBinding_Click(sender, e);
        }

        private void RefreshProfileList()
        {
            string selected = this.lbProfilesList.SelectedItem as string;
            this.lbProfilesList.Items.Clear();

            foreach (string profile in Profile.ListAll())
            {
                if (!string.Equals(profile, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    this.lbProfilesList.Items.Add(profile);
                }
            }

            if (!string.IsNullOrEmpty(selected) && this.lbProfilesList.Items.Contains(selected))
            {
                this.lbProfilesList.SelectedItem = selected;
            }

            RefreshCharacterBindingUi();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string newProfileName = this.txtProfileName.Text;
            if (string.IsNullOrWhiteSpace(newProfileName))
            {
                return;
            }

            try
            {
                string validName = ProfileSingleton.ValidateProfileName(newProfileName);
                ProfileSingleton.Create(validName);
                RefreshProfileList();
                this.container.refreshProfileList(validName);
                this.txtProfileName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to create profile", ex);
                MessageBox.Show(ex.Message, LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_select_delete"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_cannot_delete_default"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ProfileSingleton.Delete(selectedProfile);
            RefreshProfileList();
            this.container.OnProfileDeleted(selectedProfile);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_select_rename"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_cannot_rename_default"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (EditProfileName editProfileName = new EditProfileName())
            {
                editProfileName.SetProfileName(selectedProfile);
                LocalizationManager.ApplyToControlTree(editProfileName);
                ThemeManager.ApplyCurrentTheme(editProfileName);
                if (editProfileName.ShowDialog() == DialogResult.OK)
                {
                    RefreshProfileList();
                    this.container.OnProfileRenamed(selectedProfile, editProfileName.NewProfileName);
                }
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_select_copy"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(LocalizationManager.T("msg.profile_cannot_copy_default"), LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ProfileSingleton.Copy(selectedProfile);
                RefreshProfileList();
                this.container.refreshProfileList();
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to copy profile", ex);
                MessageBox.Show(ex.Message, LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            string targetProfile = this.lbProfilesList.SelectedItem != null
                ? this.lbProfilesList.SelectedItem.ToString()
                : ProfileSingleton.GetCurrent().Name;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = LocalizationManager.T("filter.profile_json");
                dialog.FileName = targetProfile + ".json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ProfileSingleton.Export(targetProfile, dialog.FileName);
                        MessageBox.Show(
                            LocalizationManager.T("msg.export_success"),
                            LocalizationManager.T("title.export_profile"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to export profile", ex);
                        MessageBox.Show(
                            LocalizationManager.T("msg.export_error", ex.Message),
                            LocalizationManager.T("title.error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = LocalizationManager.T("filter.profile_json");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string importedName = ProfileSingleton.Import(dialog.FileName);
                        RefreshProfileList();
                        this.container.refreshProfileList();
                        MessageBox.Show(
                            LocalizationManager.T("msg.import_success", importedName),
                            LocalizationManager.T("title.import_profile"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to import profile", ex);
                        MessageBox.Show(
                            LocalizationManager.T("msg.import_error", ex.Message),
                            LocalizationManager.T("title.error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnBackupZip_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = LocalizationManager.T("filter.backup_zip");
                dialog.FileName = "4RTools-Backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int count = ProfileBackupManager.ExportBackupZip(dialog.FileName);
                        MessageBox.Show(
                            LocalizationManager.T("msg.backup_success", count),
                            LocalizationManager.T("title.backup_all"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to export full backup", ex);
                        MessageBox.Show(
                            LocalizationManager.T("msg.backup_error", ex.Message),
                            LocalizationManager.T("title.error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnRestoreZip_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = LocalizationManager.T("filter.backup_zip");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (MessageBox.Show(
                            LocalizationManager.T("msg.restore_confirm"),
                            LocalizationManager.T("title.restore_backup"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    try
                    {
                        BackupRestoreResult result = ProfileBackupManager.ImportBackupZip(dialog.FileName);
                        RefreshProfileList();
                        this.container.refreshProfileList();
                        this.container.ReloadCurrentProfile();

                        if (result.ServersRestored)
                        {
                            this.container.NotifyServersRestored();
                        }
                        if (result.SettingsRestored)
                        {
                            this.container.ReloadAppSettings();
                        }

                        string yesLabel = LocalizationManager.T("title.yes");
                        string noLabel = LocalizationManager.T("title.no");
                        string summary = LocalizationManager.T(
                            "msg.restore_summary",
                            result.ProfilesRestored,
                            result.ServersRestored ? yesLabel : noLabel,
                            result.SettingsRestored ? yesLabel : noLabel);
                        MessageBox.Show(summary, LocalizationManager.T("title.restore_backup"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to restore backup", ex);
                        MessageBox.Show(
                            LocalizationManager.T("msg.restore_error", ex.Message),
                            LocalizationManager.T("title.error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnCheckConflicts_Click(object sender, EventArgs e)
        {
            Profile current = ProfileSingleton.GetCurrent();
            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(current);
            string report = KeybindConflictDetector.FormatReport(current != null ? current.Name : "Default", conflicts);
            MessageBoxIcon icon = conflicts.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(report, LocalizationManager.T("title.key_conflicts"), MessageBoxButtons.OK, icon);
        }
    }
}

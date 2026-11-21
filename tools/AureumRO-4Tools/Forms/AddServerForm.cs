using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AddServerForm : Form
    {
        private readonly Dictionary<string, int> inputMap = new Dictionary<string, int>();
        private readonly ClientDTO dto;
        private readonly Subject subject;
        private Button btnTest;
        private Label lblTestResult;
        private Font hintFont;

        public AddServerForm(ClientDTO dto, Subject subject)
        {
            InitializeComponent();
            NormalizeLabelLayout();
            SetupInputs();
            InitializeTestControls();

            for (int i = 0; i <= 9; i++)
            {
                this.inputMap.Add("D" + i, i);
                this.inputMap.Add("NumPad" + i, i);
            }

            if (dto != null)
            {
                this.dto = dto;
                initiateWithClientDTO(dto);
                this.Text = LocalizationManager.T("title.edit_server", dto.name);
            }

            this.subject = subject;
            LocalizationManager.ApplyToControlTree(this);
        }

        private void NormalizeLabelLayout()
        {
            this.label1.AutoSize = false;
            this.label1.Location = new Point(4, 32);
            this.label1.Size = new Size(84, 17);
            this.label1.TextAlign = ContentAlignment.MiddleRight;

            this.label4.AutoSize = false;
            this.label4.Location = new Point(4, 60);
            this.label4.Size = new Size(84, 17);
            this.label4.TextAlign = ContentAlignment.MiddleRight;

            this.label5.AutoSize = false;
            this.label5.Location = new Point(4, 93);
            this.label5.Size = new Size(84, 17);
            this.label5.TextAlign = ContentAlignment.MiddleRight;

            this.label2.Location = new Point(91, 33);
            this.label3.Location = new Point(91, 61);
            this.processCB.Location = new Point(92, 91);
            this.processCB.Size = new Size(239, 21);
        }

        private void InitializeTestControls()
        {
            this.hintFont = new Font("Microsoft Sans Serif", 7.5F);

            this.lblTestResult = new Label
            {
                Location = new Point(8, 118),
                Name = "lblTestResult",
                Size = new Size(144, 39),
                Font = this.hintFont,
                Text = "Click Test while logged in to verify addresses."
            };

            this.btnTest = new Button
            {
                Location = new Point(156, 118),
                Name = "btnTest",
                Size = new Size(84, 39),
                Text = "Test",
                UseVisualStyleBackColor = true
            };
            this.btnTest.Click += btnTest_Click;

            this.groupBox1.Controls.Add(this.lblTestResult);
            this.groupBox1.Controls.Add(this.btnTest);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string hpAddress = ReadHpAddressText();
            string nameAddress = ReadNameAddressText();

            ServerAddressTestResult testResult = Client.TestMemoryAddresses(this.processCB.Text, hpAddress, nameAddress);

            if (testResult.Success)
            {
                this.lblTestResult.ForeColor = ThemeManager.IsDarkMode ? Color.LightGreen : Color.DarkGreen;
                this.lblTestResult.Text = LocalizationManager.T(
                    "label.test_ok_inline",
                    string.IsNullOrEmpty(testResult.CharacterName) ? LocalizationManager.T("label.connected") : testResult.CharacterName,
                    testResult.CurrentHp,
                    testResult.MaxHp);
                MessageBox.Show(testResult.Message, LocalizationManager.T("title.test_success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                this.lblTestResult.ForeColor = ThemeManager.IsDarkMode ? Color.Salmon : Color.DarkRed;
                this.lblTestResult.Text = LocalizationManager.T("label.test_failed_inline");
                MessageBox.Show(testResult.Message, LocalizationManager.T("title.test_result"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string ReadHpAddressText()
        {
            return string.Concat(this.txtHP1.Text, this.txtHP2.Text, this.txtHP3.Text, this.txtHP4.Text, this.txtHP5.Text, this.txtHP6.Text, this.txtHP7.Text, this.txtHP8.Text);
        }

        private string ReadNameAddressText()
        {
            return string.Concat(this.txtName1.Text, this.txtName2.Text, this.txtName3.Text, this.txtName4.Text, this.txtName5.Text, this.txtName6.Text, this.txtName7.Text, this.txtName8.Text);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.hintFont != null)
            {
                this.hintFont.Dispose();
                this.hintFont = null;
            }
            base.OnFormClosed(e);
        }

        private void initiateWithClientDTO(ClientDTO dto)
        {
            string normalizedHp = LocalServerManager.NormalizeHexAddress(dto.hpAddress) ?? string.Empty;
            string normalizedName = LocalServerManager.NormalizeHexAddress(dto.nameAddress) ?? string.Empty;

            List<char> charsHP = normalizedHp.ToCharArray().ToList();
            List<char> charsName = normalizedName.ToCharArray().ToList();

            this.txtHP1.Text = charsHP.ElementAtOrDefault(0).ToString();
            this.txtHP2.Text = charsHP.ElementAtOrDefault(1).ToString();
            this.txtHP3.Text = charsHP.ElementAtOrDefault(2).ToString();
            this.txtHP4.Text = charsHP.ElementAtOrDefault(3).ToString();
            this.txtHP5.Text = charsHP.ElementAtOrDefault(4).ToString();
            this.txtHP6.Text = charsHP.ElementAtOrDefault(5).ToString();
            this.txtHP7.Text = charsHP.ElementAtOrDefault(6).ToString();
            this.txtHP8.Text = charsHP.ElementAtOrDefault(7).ToString();

            this.txtName1.Text = charsName.ElementAtOrDefault(0).ToString();
            this.txtName2.Text = charsName.ElementAtOrDefault(1).ToString();
            this.txtName3.Text = charsName.ElementAtOrDefault(2).ToString();
            this.txtName4.Text = charsName.ElementAtOrDefault(3).ToString();
            this.txtName5.Text = charsName.ElementAtOrDefault(4).ToString();
            this.txtName6.Text = charsName.ElementAtOrDefault(5).ToString();
            this.txtName7.Text = charsName.ElementAtOrDefault(6).ToString();
            this.txtName8.Text = charsName.ElementAtOrDefault(7).ToString();

            this.processCB.Text = dto.name;
        }

        private void onTextChange(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            int digit;
            if (this.inputMap.TryGetValue(textBox.Text, out digit))
            {
                textBox.Text = digit.ToString();
                return;
            }

            if (!LocalServerManager.IsHex(textBox.Text))
            {
                textBox.Clear();
            }
        }

        private void refreshProcessList()
        {
            string currentText = this.processCB.Text;
            this.processCB.Items.Clear();

            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        this.processCB.Items.Add(p.ProcessName);
                    }
                }
                catch
                {
                    // Ignore inaccessible processes.
                }
                finally
                {
                    p.Dispose();
                }
            }

            if (!string.IsNullOrEmpty(currentText))
            {
                this.processCB.Text = currentText;
            }
        }

        public void SetupInputs()
        {
            foreach (Control c in FormUtils.GetAll(this, typeof(TextBox)))
            {
                TextBox textBox = c as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                    textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                    textBox.TextChanged += new EventHandler(this.onTextChange);
                }
            }
        }

        private void AddServerForm_Load(object sender, EventArgs e)
        {
            refreshProcessList();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string hpAddress = ReadHpAddressText();
            string nameAddress = ReadNameAddressText();

            try
            {
                if (this.dto == null)
                {
                    LocalServerManager.AddServer(hpAddress, nameAddress, this.processCB.Text);
                    MessageBox.Show(
                        LocalizationManager.T("msg.server_added", this.processCB.Text),
                        LocalizationManager.T("title.message"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    // Validate addresses before removing the old entry so failed validation never loses the server.
                    if (LocalServerManager.NormalizeHexAddress(hpAddress) == null)
                    {
                        throw new ArgumentException(LocalizationManager.T("test.invalid_hp"));
                    }
                    if (LocalServerManager.NormalizeHexAddress(nameAddress) == null)
                    {
                        throw new ArgumentException(LocalizationManager.T("test.invalid_name"));
                    }

                    LocalServerManager.RemoveClient(this.dto);
                    LocalServerManager.AddServer(hpAddress, nameAddress, this.processCB.Text);
                    MessageBox.Show(
                        LocalizationManager.T("msg.server_saved", this.processCB.Text),
                        LocalizationManager.T("title.message"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                this.subject.Notify(new Utils.Message(MessageCode.SERVER_LIST_CHANGED, "Server Saved"));
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationManager.T("title.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

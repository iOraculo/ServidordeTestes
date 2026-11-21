using System;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ServersForm : Form, IObserver
    {
        private readonly Subject subject;

        public ServersForm(Subject subject)
        {
            InitializeComponent();
            subject.Attach(this);
            this.subject = subject;
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            switch (subj.Message.code)
            {
                case MessageCode.SERVER_LIST_CHANGED:
                    doRender();
                    break;
            }
        }

        private void ServersForm_Load(object sender, EventArgs e)
        {
            this.datagridServers.AllowUserToAddRows = false;
            doRender();
        }

        private void doRender()
        {
            this.clientDTOBindingSource.Clear();
            LocalServerManager.GetLocalClients().ForEach(c => this.clientDTOBindingSource.Add(c));
        }

        private void datagridServers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            ClientDTO current = this.clientDTOBindingSource.Current as ClientDTO;
            if (current == null)
            {
                return;
            }

            current.index = e.RowIndex;

            if (this.datagridServers.Columns[e.ColumnIndex].Name == "Delete")
            {
                if (MessageBox.Show(
                        LocalizationManager.T("msg.server_delete_confirm"),
                        LocalizationManager.T("title.message"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.clientDTOBindingSource.RemoveCurrent();
                    LocalServerManager.RemoveClient(current);
                    this.subject.Notify(new Utils.Message(MessageCode.SERVER_LIST_CHANGED, "Server Deleted"));
                    MessageBox.Show(
                        LocalizationManager.T("msg.server_deleted", current.name),
                        LocalizationManager.T("title.message"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else
            {
                AddServerForm form = new AddServerForm(current, this.subject);
                LocalizationManager.ApplyToControlTree(form);
                ThemeManager.ApplyCurrentTheme(form);
                form.Show();
            }
        }

        private void btnAddServer_Click(object sender, EventArgs e)
        {
            AddServerForm form = new AddServerForm(null, this.subject);
            LocalizationManager.ApplyToControlTree(form);
            ThemeManager.ApplyCurrentTheme(form);
            form.Show();
        }
    }
}

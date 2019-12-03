using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Baichador {
    public partial class SettingsForm : Form {

        public SettingsForm() {
            InitializeComponent();

            foreach(SettingsProperty sett in Properties.Settings.Default.Properties)
                grid.Rows.Add(sett.Name, sett.DefaultValue);
            
        }

        private void BtnSalvar_Click(object sender, EventArgs e) {
            foreach(DataGridViewRow row in grid.Rows) {
                string name = (string) row.Cells[0].Value;
                string value = (string) row.Cells[1].Value;

                SettingsProperty setting = new SettingsProperty(name);
                setting.DefaultValue = value;

                Properties.Settings.Default.Properties.Remove(name);
                Properties.Settings.Default.Properties.Add(setting);
            }

            Properties.Settings.Default.Save();
        }
    }
}

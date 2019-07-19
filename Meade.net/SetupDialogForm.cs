using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.Meade.net;

namespace ASCOM.Meade.net
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }      

        public void SetProfile(ProfileProperties profileProperties)
        {
            chkTrace.Checked = profileProperties.TraceLogger;
            // set the list of com ports to those that are currently available
            comboBoxComPort.Items.Clear();
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());      // use System.IO because it's static
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(profileProperties.ComPort))
            {
                comboBoxComPort.SelectedItem = profileProperties.ComPort;
            }
        }

        public ProfileProperties GetProfile()
        {
            var profileProperties = new ProfileProperties
            {
                TraceLogger = chkTrace.Checked,
                ComPort = comboBoxComPort.SelectedItem.ToString()
            };

            return profileProperties;
        }

        private void SetupDialogForm_Shown(object sender, EventArgs e)
        {
            Activate();
        }
    }
}
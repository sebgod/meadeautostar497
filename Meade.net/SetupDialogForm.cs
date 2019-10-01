using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ASCOM.Meade.net.Properties;

namespace ASCOM.Meade.net
{
    [ComVisible(false)] // Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();

            var assemblyInfo = new AssemblyInfo();

            Text = string.Format(Resources.SetupDialogForm_SetupDialogForm__0__Settings___1__, assemblyInfo.Product, assemblyInfo.AssemblyVersion);
        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                Process.Start("http://ascom-standards.org/");
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
            comboBoxComPort.Items.AddRange(SerialPort.GetPortNames().ToArray<object>()); // use System.IO because it's static
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(profileProperties.ComPort))
            {
                comboBoxComPort.SelectedItem = profileProperties.ComPort;
            }

            txtGuideRate.Text = profileProperties.GuideRateArcSecondsPerSecond.ToString(CultureInfo.CurrentCulture);
            try
            {
                cboPrecision.SelectedItem = profileProperties.Precision;
            }
            catch (Exception)
            {
                cboPrecision.SelectedItem = "Unchanged";
            }
        }

    public ProfileProperties GetProfile()
        {
            var profileProperties = new ProfileProperties
            {
                TraceLogger = chkTrace.Checked,
                ComPort = comboBoxComPort.SelectedItem.ToString(),
                GuideRateArcSecondsPerSecond = double.Parse(txtGuideRate.Text.Trim()),
                Precision = cboPrecision.SelectedItem.ToString()
            };

            return profileProperties;
        }

        private void SetupDialogForm_Shown(object sender, EventArgs e)
        {
            Win32Utilities.BringWindowToFront(Handle);
            Activate();
        }

        private bool _guideRateValid = true;

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double newGuideRate = double.Parse(txtGuideRate.Text.Trim());

                const double siderealArcSecondsPerSecond = 15.041;
                var percentOfSideReal = newGuideRate / siderealArcSecondsPerSecond * 100;

                lblPercentOfSiderealRate.Text = string.Format(Resources.SetupDialogForm_TextBox1_TextChanged___0_00_0___of_sidereal_rate_, percentOfSideReal);
                _guideRateValid = true;
            }
            catch (Exception)
            {
                //Surpressing this exception as if the value is not valid then it's not useful.
                _guideRateValid = false;
            }

            UpdateOkButton();
        }

        private void UpdateOkButton()
        {
            cmdOK.Enabled = _guideRateValid && (comboBoxComPort.SelectedItem != null);
        }

        private void ComboBoxComPort_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateOkButton();
        }

        public void SetReadOnlyMode()
        {
            foreach (Control control in Controls)
            {
                control.Enabled = false;
            }

            cmdCancel.Enabled = true;
            //cmdOK.Enabled = false;
            //comboBoxComPort.Enabled = false;
            //chkTrace.Enabled = false;
            //txtGuideRate.Enabled = false;
            //cboPrecision.Enabled = false;
        }
    }
}
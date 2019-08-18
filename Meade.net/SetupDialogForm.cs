using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ASCOM.Meade.net
{
    [ComVisible(false)] // Form not registered for COM!
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
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort
                .GetPortNames()); // use System.IO because it's static
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(profileProperties.ComPort))
            {
                comboBoxComPort.SelectedItem = profileProperties.ComPort;
            }

            txtGuideRate.Text = profileProperties.GuideRateArcSecondsPerSecond.ToString();
            try
            {
                cboPrecision.SelectedItem = profileProperties.Precision;
            }
            catch (Exception e)
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
            Activate();
        }

        private bool _guideRateValid = true;

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            //const double SIDRATE = 0.9972695677; //synodic/solar seconds per sidereal second
            try
            {
                double newGuideRate = double.Parse(txtGuideRate.Text.Trim());

                const double siderealArcSecondsPerSecond = 15.041;
                var percentOfSideReal = (newGuideRate / siderealArcSecondsPerSecond * 100);

                lblPercentOfSiderealRate.Text = $"({percentOfSideReal:00.0}% of sidereal rate)";
                _guideRateValid = true;
            }
            catch (Exception exception)
            {
                //Surpressing this exception as if the value is not valid then it's not useful.
                _guideRateValid = false;
            }

            UpdateOKButton();
        }

        private void UpdateOKButton()
        {
            cmdOK.Enabled = _guideRateValid && (comboBoxComPort.SelectedItem != null);
        }

        private void ComboBoxComPort_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateOKButton();
        }
    }
}
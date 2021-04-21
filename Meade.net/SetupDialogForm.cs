using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ASCOM.Meade.net.Properties;
using ASCOM.Utilities;

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

            SetItemsFromEnum(cboStopBits.Items, typeof(SerialStopBits));
            SetItemsFromEnum(cboParity.Items, typeof(SerialParity));
            SetItemsFromEnumValues(cboSpeed.Items, typeof(SerialSpeed));
            SetItemsFromEnum(cboHandShake.Items, typeof(SerialHandshake));
        }

        private void SetItemsFromEnum(IList items, Type enumItems)
        {
            items.Clear();

            foreach (var item in Enum.GetNames(enumItems))
            {
                items.Add(item);
            }
        }

        private void SetItemsFromEnumValues(IList items, Type enumItems)
        {
            items.Clear();

            foreach (int item in Enum.GetValues(enumItems))
            {
                items.Add(item);
            }
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

            cbxRtsDtr.Checked = profileProperties.RtsDtrEnabled;

            txtGuideRate.Text = profileProperties.GuideRateArcSecondsPerSecond.ToString(CultureInfo.CurrentCulture);
            try
            {
                cboPrecision.SelectedItem = profileProperties.Precision;
            }
            catch (Exception)
            {
                cboPrecision.SelectedItem = "Unchanged";
            }

            try
            {
                cboGuidingStyle.SelectedItem = profileProperties.GuidingStyle;
            }
            catch (Exception)
            {
                cboGuidingStyle.SelectedItem = "Auto";
            }

            numDatabits.Value = profileProperties.DataBits;

            try
            {
                cboStopBits.SelectedItem = profileProperties.StopBits;
            }
            catch (Exception)
            {
                cboStopBits.SelectedItem = "One";
            }


            try
            {
                cboParity.SelectedItem = profileProperties.Parity;
            }
            catch (Exception)
            {
                cboParity.SelectedItem = "None";
            }

            try
            {
                cboSpeed.SelectedItem = profileProperties.Speed;
            }
            catch (Exception)
            {
                cboParity.SelectedItem = "9600";
            }


            try
            {
                cboHandShake.SelectedItem = profileProperties.Handshake;
            }
            catch (Exception)
            {
                cboHandShake.SelectedItem = "None";
            }

            txtBacklashSteps.Text = profileProperties.BacklashCompensation.ToString(CultureInfo.CurrentCulture);
            txtElevation.Text = profileProperties.SiteElevation.ToString(CultureInfo.CurrentCulture);

            cbxReverseDirection.Checked = profileProperties.ReverseFocusDirection;
            cbxDynamicBreaking.Checked = profileProperties.DynamicBreaking;
            nudSettleTime.Value = profileProperties.SettleTime;

            cbxSendDateTime.Checked = profileProperties.SendDateTime;
            cbxSkipPrompts.Checked = profileProperties.SkipPrompts;
        }

    public ProfileProperties GetProfile()
        {
            var profileProperties = new ProfileProperties
            {
                TraceLogger = chkTrace.Checked,
                ComPort = comboBoxComPort.SelectedItem.ToString(),
                RtsDtrEnabled = cbxRtsDtr.Checked,
                DataBits = Convert.ToInt32(numDatabits.Value),
                StopBits = cboStopBits.SelectedItem.ToString(),
                Parity = cboParity.SelectedItem.ToString(),
                Speed = Convert.ToInt32(cboSpeed.SelectedItem),
                Handshake = cboHandShake.SelectedItem.ToString(),
                GuideRateArcSecondsPerSecond = double.Parse(txtGuideRate.Text.Trim()),
                Precision = cboPrecision.SelectedItem.ToString(),
                GuidingStyle = cboGuidingStyle.SelectedItem.ToString(),
                BacklashCompensation = int.Parse(txtBacklashSteps.Text),
                ReverseFocusDirection = cbxReverseDirection.Checked,
                DynamicBreaking = cbxDynamicBreaking.Checked,
                SiteElevation = double.Parse(txtElevation.Text),
                SettleTime = Convert.ToInt16(nudSettleTime.Value),
                SendDateTime = cbxSendDateTime.Checked,
                SkipPrompts = cbxSkipPrompts.Checked
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

        private void txtElevation_TextChanged_1(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(txtElevation.Text, "[^0-9]"))
            {
                MessageBox.Show(Resources.SetupDialogForm_txtElevation_TextChanged_1_Please_enter_only_numbers_);
                txtElevation.Text = txtElevation.Text.Remove(txtElevation.Text.Length - 1);
            }
        }

        private void txtBacklashSteps_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(txtBacklashSteps.Text, "[^0-9]"))
            {
                MessageBox.Show(Resources.SetupDialogForm_txtElevation_TextChanged_1_Please_enter_only_numbers_);
                txtBacklashSteps.Text = txtElevation.Text.Remove(txtBacklashSteps.Text.Length - 1);
            }
        }
    }
}
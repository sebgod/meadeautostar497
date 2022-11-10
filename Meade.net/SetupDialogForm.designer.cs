using System.ComponentModel;
using System.Windows.Forms;

namespace ASCOM.Meade.net
{
    partial class SetupDialogForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupDialogForm));
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.comboBoxComPort = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtGuideRate = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lblPercentOfSiderealRate = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cboPrecision = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cboGuidingStyle = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtBacklashSteps = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.cbxReverseDirection = new System.Windows.Forms.CheckBox();
            this.cbxDynamicBreaking = new System.Windows.Forms.CheckBox();
            this.cbxRtsDtr = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.cbxSendDateTime = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtElevation = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.nudSettleTime = new System.Windows.Forms.NumericUpDown();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.cboStopBits = new System.Windows.Forms.ComboBox();
            this.numDatabits = new System.Windows.Forms.NumericUpDown();
            this.cboParity = new System.Windows.Forms.ComboBox();
            this.cboSpeed = new System.Windows.Forms.ComboBox();
            this.cboHandShake = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.cboParkedBehaviour = new System.Windows.Forms.ComboBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.txtParkedAlt = new System.Windows.Forms.TextBox();
            this.txtParkedAz = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.txtFocalLength = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.txtApertureDiameter = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.label29 = new System.Windows.Forms.Label();
            this.txtApertureArea = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSettleTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDatabits)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            resources.ApplyResources(this.cmdOK, "cmdOK");
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.UseVisualStyleBackColor = true;
            // 
            // cmdCancel
            // 
            resources.ApplyResources(this.cmdCancel, "cmdCancel");
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // picASCOM
            // 
            resources.ApplyResources(this.picASCOM, "picASCOM");
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = global::ASCOM.Meade.net.Properties.Resources.ASCOM;
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // chkTrace
            // 
            resources.ApplyResources(this.chkTrace, "chkTrace");
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.UseVisualStyleBackColor = true;
            // 
            // comboBoxComPort
            // 
            this.comboBoxComPort.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxComPort, "comboBoxComPort");
            this.comboBoxComPort.Name = "comboBoxComPort";
            this.comboBoxComPort.SelectedValueChanged += new System.EventHandler(this.ComboBoxComPort_SelectedValueChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // txtGuideRate
            // 
            resources.ApplyResources(this.txtGuideRate, "txtGuideRate");
            this.txtGuideRate.Name = "txtGuideRate";
            this.toolTip1.SetToolTip(this.txtGuideRate, resources.GetString("txtGuideRate.ToolTip"));
            this.txtGuideRate.TextChanged += new System.EventHandler(this.TextBox1_TextChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // lblPercentOfSiderealRate
            // 
            resources.ApplyResources(this.lblPercentOfSiderealRate, "lblPercentOfSiderealRate");
            this.lblPercentOfSiderealRate.Name = "lblPercentOfSiderealRate";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cboPrecision
            // 
            this.cboPrecision.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPrecision.FormattingEnabled = true;
            this.cboPrecision.Items.AddRange(new object[] {
            resources.GetString("cboPrecision.Items"),
            resources.GetString("cboPrecision.Items1"),
            resources.GetString("cboPrecision.Items2")});
            resources.ApplyResources(this.cboPrecision, "cboPrecision");
            this.cboPrecision.Name = "cboPrecision";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // cboGuidingStyle
            // 
            this.cboGuidingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboGuidingStyle.FormattingEnabled = true;
            this.cboGuidingStyle.Items.AddRange(new object[] {
            resources.GetString("cboGuidingStyle.Items"),
            resources.GetString("cboGuidingStyle.Items1"),
            resources.GetString("cboGuidingStyle.Items2")});
            resources.ApplyResources(this.cboGuidingStyle, "cboGuidingStyle");
            this.cboGuidingStyle.Name = "cboGuidingStyle";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // txtBacklashSteps
            // 
            resources.ApplyResources(this.txtBacklashSteps, "txtBacklashSteps");
            this.txtBacklashSteps.Name = "txtBacklashSteps";
            this.txtBacklashSteps.TextChanged += new System.EventHandler(this.txtBacklashSteps_TextChanged);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // cbxReverseDirection
            // 
            resources.ApplyResources(this.cbxReverseDirection, "cbxReverseDirection");
            this.cbxReverseDirection.Name = "cbxReverseDirection";
            this.cbxReverseDirection.UseVisualStyleBackColor = true;
            // 
            // cbxDynamicBreaking
            // 
            resources.ApplyResources(this.cbxDynamicBreaking, "cbxDynamicBreaking");
            this.cbxDynamicBreaking.Name = "cbxDynamicBreaking";
            this.cbxDynamicBreaking.UseVisualStyleBackColor = true;
            // 
            // cbxRtsDtr
            // 
            resources.ApplyResources(this.cbxRtsDtr, "cbxRtsDtr");
            this.cbxRtsDtr.Name = "cbxRtsDtr";
            this.toolTip1.SetToolTip(this.cbxRtsDtr, resources.GetString("cbxRtsDtr.ToolTip"));
            this.cbxRtsDtr.UseVisualStyleBackColor = true;
            // 
            // cbxSendDateTime
            // 
            resources.ApplyResources(this.cbxSendDateTime, "cbxSendDateTime");
            this.cbxSendDateTime.Name = "cbxSendDateTime";
            this.toolTip1.SetToolTip(this.cbxSendDateTime, resources.GetString("cbxSendDateTime.ToolTip"));
            this.cbxSendDateTime.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // txtElevation
            // 
            resources.ApplyResources(this.txtElevation, "txtElevation");
            this.txtElevation.Name = "txtElevation";
            this.txtElevation.TextChanged += new System.EventHandler(this.txtElevation_TextChanged_1);
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // nudSettleTime
            // 
            resources.ApplyResources(this.nudSettleTime, "nudSettleTime");
            this.nudSettleTime.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.nudSettleTime.Name = "nudSettleTime";
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // cboStopBits
            // 
            this.cboStopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStopBits.FormattingEnabled = true;
            resources.ApplyResources(this.cboStopBits, "cboStopBits");
            this.cboStopBits.Name = "cboStopBits";
            // 
            // numDatabits
            // 
            resources.ApplyResources(this.numDatabits, "numDatabits");
            this.numDatabits.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numDatabits.Name = "numDatabits";
            // 
            // cboParity
            // 
            this.cboParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboParity.FormattingEnabled = true;
            resources.ApplyResources(this.cboParity, "cboParity");
            this.cboParity.Name = "cboParity";
            // 
            // cboSpeed
            // 
            this.cboSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSpeed.FormattingEnabled = true;
            resources.ApplyResources(this.cboSpeed, "cboSpeed");
            this.cboSpeed.Name = "cboSpeed";
            // 
            // cboHandShake
            // 
            this.cboHandShake.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHandShake.FormattingEnabled = true;
            resources.ApplyResources(this.cboHandShake, "cboHandShake");
            this.cboHandShake.Name = "cboHandShake";
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // label19
            // 
            resources.ApplyResources(this.label19, "label19");
            this.label19.Name = "label19";
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            // 
            // label21
            // 
            resources.ApplyResources(this.label21, "label21");
            this.label21.Name = "label21";
            // 
            // cboParkedBehaviour
            // 
            this.cboParkedBehaviour.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboParkedBehaviour.FormattingEnabled = true;
            this.cboParkedBehaviour.Items.AddRange(new object[] {
            resources.GetString("cboParkedBehaviour.Items"),
            resources.GetString("cboParkedBehaviour.Items1"),
            resources.GetString("cboParkedBehaviour.Items2")});
            resources.ApplyResources(this.cboParkedBehaviour, "cboParkedBehaviour");
            this.cboParkedBehaviour.Name = "cboParkedBehaviour";
            this.cboParkedBehaviour.SelectionChangeCommitted += new System.EventHandler(this.cboParkedBehaviour_SelectionChangeCommitted);
            // 
            // label22
            // 
            resources.ApplyResources(this.label22, "label22");
            this.label22.Name = "label22";
            // 
            // label23
            // 
            resources.ApplyResources(this.label23, "label23");
            this.label23.Name = "label23";
            // 
            // label24
            // 
            resources.ApplyResources(this.label24, "label24");
            this.label24.Name = "label24";
            // 
            // txtParkedAlt
            // 
            resources.ApplyResources(this.txtParkedAlt, "txtParkedAlt");
            this.txtParkedAlt.Name = "txtParkedAlt";
            this.txtParkedAlt.TextChanged += new System.EventHandler(this.txtParkedAlt_TextChanged);
            // 
            // txtParkedAz
            // 
            resources.ApplyResources(this.txtParkedAz, "txtParkedAz");
            this.txtParkedAz.Name = "txtParkedAz";
            this.txtParkedAz.TextChanged += new System.EventHandler(this.txtParkedAz_TextChanged);
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.Name = "label25";
            // 
            // txtFocalLength
            // 
            resources.ApplyResources(this.txtFocalLength, "txtFocalLength");
            this.txtFocalLength.Name = "txtFocalLength";
            this.txtFocalLength.TextChanged += new System.EventHandler(this.txt_FocalLength_TextChanged_1);
            // 
            // label26
            // 
            resources.ApplyResources(this.label26, "label26");
            this.label26.Name = "label26";
            // 
            // label27
            // 
            resources.ApplyResources(this.label27, "label27");
            this.label27.Name = "label27";
            // 
            // txtApertureDiameter
            // 
            resources.ApplyResources(this.txtApertureDiameter, "txtApertureDiameter");
            this.txtApertureDiameter.Name = "txtApertureDiameter";
            // 
            // label28
            // 
            resources.ApplyResources(this.label28, "label28");
            this.label28.Name = "label28";
            // 
            // label29
            // 
            resources.ApplyResources(this.label29, "label29");
            this.label29.Name = "label29";
            // 
            // txtApertureArea
            // 
            resources.ApplyResources(this.txtApertureArea, "txtApertureArea");
            this.txtApertureArea.Name = "txtApertureArea";
            // 
            // label30
            // 
            resources.ApplyResources(this.label30, "label30");
            this.label30.Name = "label30";
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SetupDialogForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.txtApertureArea);
            this.Controls.Add(this.label30);
            this.Controls.Add(this.label27);
            this.Controls.Add(this.txtApertureDiameter);
            this.Controls.Add(this.label28);
            this.Controls.Add(this.label26);
            this.Controls.Add(this.txtFocalLength);
            this.Controls.Add(this.label25);
            this.Controls.Add(this.txtParkedAz);
            this.Controls.Add(this.txtParkedAlt);
            this.Controls.Add(this.label24);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.cboParkedBehaviour);
            this.Controls.Add(this.cbxSendDateTime);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.cboHandShake);
            this.Controls.Add(this.cboSpeed);
            this.Controls.Add(this.cboParity);
            this.Controls.Add(this.numDatabits);
            this.Controls.Add(this.cboStopBits);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.nudSettleTime);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtElevation);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.cbxRtsDtr);
            this.Controls.Add(this.cbxDynamicBreaking);
            this.Controls.Add(this.cbxReverseDirection);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtBacklashSteps);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cboGuidingStyle);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboPrecision);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblPercentOfSiderealRate);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtGuideRate);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxComPort);
            this.Controls.Add(this.chkTrace);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.SetupDialogForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSettleTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDatabits)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button cmdOK;
        private Button cmdCancel;
        private Label label1;
        private PictureBox picASCOM;
        private Label label2;
        private CheckBox chkTrace;
        private ComboBox comboBoxComPort;
        private Label label3;
        private TextBox txtGuideRate;
        private Label label4;
        private Label lblPercentOfSiderealRate;
        private Label label5;
        private ComboBox cboPrecision;
        private Label label6;
        private ComboBox cboGuidingStyle;
        private Label label7;
        private Label label8;
        private TextBox txtBacklashSteps;
        private Label label9;
        private Label label10;
        private Label label11;
        private CheckBox cbxReverseDirection;
        private CheckBox cbxDynamicBreaking;
        private CheckBox cbxRtsDtr;
        private ToolTip toolTip1;
        private Label label12;
        private TextBox txtElevation;
        private Label label13;
        private Label label14;
        private NumericUpDown nudSettleTime;
        private Label label15;
        private Label label16;
        private ComboBox cboStopBits;
        private NumericUpDown numDatabits;
        private ComboBox cboParity;
        private ComboBox cboSpeed;
        private ComboBox cboHandShake;
        private Label label17;
        private Label label18;
        private Label label19;
        private Label label20;
        private Label label21;
        private CheckBox cbxSendDateTime;
        private ComboBox cboParkedBehaviour;
        private Label label22;
        private Label label23;
        private Label label24;
        private TextBox txtParkedAlt;
        private TextBox txtParkedAz;
        private Label label25;
        private TextBox txtFocalLength;
        private Label label26;
        private Label label27;
        private TextBox txtApertureDiameter;
        private Label label28;
        private Label label29;
        private TextBox txtApertureArea;
        private Label label30;
        private Button button1;
    }
}
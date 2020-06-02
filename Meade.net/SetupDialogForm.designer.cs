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
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
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
            // SetupDialogForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
    }
}
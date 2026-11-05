namespace HeltecLoRaTerminal;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _serial?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        grpPort = new GroupBox();
        btnConnect = new Button();
        cmbBaud = new ComboBox();
        lblBaud = new Label();
        btnRefreshPorts = new Button();
        cmbPort = new ComboBox();
        lblPort = new Label();
        grpHeltec = new GroupBox();
        btnListen = new Button();
        btnInitHeltec = new Button();
        chkAutoListen = new CheckBox();
        chkAutoInit = new CheckBox();
        cmbBw = new ComboBox();
        lblBw = new Label();
        cmbSf = new ComboBox();
        lblSf = new Label();
        nudPower = new NumericUpDown();
        lblPower = new Label();
        nudFreq = new NumericUpDown();
        lblFreq = new Label();
        lblBandHint = new Label();
        grpSend = new GroupBox();
        btnSend = new Button();
        txtSendHex = new TextBox();
        lblSend = new Label();
        lblReceived = new Label();
        lstReceived = new ListBox();
        btnClearReceived = new Button();
        lblErrors = new Label();
        txtErrors = new TextBox();
        btnClearErrors = new Button();
        grpPort.SuspendLayout();
        grpHeltec.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudPower).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudFreq).BeginInit();
        grpSend.SuspendLayout();
        SuspendLayout();
        //
        // grpPort
        //
        grpPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpPort.Controls.Add(btnConnect);
        grpPort.Controls.Add(cmbBaud);
        grpPort.Controls.Add(lblBaud);
        grpPort.Controls.Add(btnRefreshPorts);
        grpPort.Controls.Add(cmbPort);
        grpPort.Controls.Add(lblPort);
        grpPort.Location = new Point(12, 8);
        grpPort.Name = "grpPort";
        grpPort.Size = new Size(780, 58);
        grpPort.TabIndex = 0;
        grpPort.TabStop = false;
        grpPort.Text = "COM (Heltec AT, 115200 8N1, без CR/LF)";
        //
        // lblPort
        //
        lblPort.AutoSize = true;
        lblPort.Location = new Point(12, 25);
        lblPort.Name = "lblPort";
        lblPort.Size = new Size(32, 15);
        lblPort.Text = "Порт";
        //
        // cmbPort
        //
        cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPort.FormattingEnabled = true;
        cmbPort.Location = new Point(50, 22);
        cmbPort.Name = "cmbPort";
        cmbPort.Size = new Size(120, 23);
        //
        // btnRefreshPorts
        //
        btnRefreshPorts.Location = new Point(176, 21);
        btnRefreshPorts.Name = "btnRefreshPorts";
        btnRefreshPorts.Size = new Size(90, 25);
        btnRefreshPorts.Text = "Обновить";
        btnRefreshPorts.UseVisualStyleBackColor = true;
        btnRefreshPorts.Click += BtnRefreshPorts_Click;
        //
        // lblBaud
        //
        lblBaud.AutoSize = true;
        lblBaud.Location = new Point(284, 25);
        lblBaud.Name = "lblBaud";
        lblBaud.Size = new Size(55, 15);
        lblBaud.Text = "Скорость";
        //
        // cmbBaud
        //
        cmbBaud.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBaud.FormattingEnabled = true;
        cmbBaud.Items.AddRange(new object[] { "9600", "57600", "115200", "230400" });
        cmbBaud.Location = new Point(345, 22);
        cmbBaud.Name = "cmbBaud";
        cmbBaud.Size = new Size(100, 23);
        //
        // btnConnect
        //
        btnConnect.Location = new Point(463, 21);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new Size(120, 25);
        btnConnect.Text = "Подключить";
        btnConnect.UseVisualStyleBackColor = true;
        btnConnect.Click += BtnConnect_Click;
        //
        // grpHeltec
        //
        grpHeltec.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpHeltec.Controls.Add(btnListen);
        grpHeltec.Controls.Add(btnInitHeltec);
        grpHeltec.Controls.Add(chkAutoInit);
        grpHeltec.Controls.Add(chkAutoListen);
        grpHeltec.Controls.Add(cmbBw);
        grpHeltec.Controls.Add(lblBw);
        grpHeltec.Controls.Add(cmbSf);
        grpHeltec.Controls.Add(lblSf);
        grpHeltec.Controls.Add(nudPower);
        grpHeltec.Controls.Add(lblPower);
        grpHeltec.Controls.Add(nudFreq);
        grpHeltec.Controls.Add(lblFreq);
        grpHeltec.Controls.Add(lblBandHint);
        grpHeltec.Location = new Point(12, 72);
        grpHeltec.Name = "grpHeltec";
        grpHeltec.Size = new Size(780, 92);
        grpHeltec.TabIndex = 1;
        grpHeltec.TabStop = false;
        grpHeltec.Text = "HeltecAtBridge — send/receive (863–928 МГц)";
        //
        // lblBandHint
        //
        lblBandHint.AutoSize = true;
        lblBandHint.ForeColor = SystemColors.GrayText;
        lblBandHint.Location = new Point(12, 22);
        lblBandHint.Name = "lblBandHint";
        lblBandHint.Size = new Size(420, 15);
        lblBandHint.Text = "Прошивка: firmware/HeltecAtBridge. Init → RX; SendHex → TX → снова RX";
        //
        // lblFreq
        //
        lblFreq.AutoSize = true;
        lblFreq.Location = new Point(12, 52);
        lblFreq.Name = "lblFreq";
        lblFreq.Size = new Size(55, 15);
        lblFreq.Text = "Freq, Hz";
        //
        // nudFreq
        //
        nudFreq.Location = new Point(73, 48);
        nudFreq.Maximum = new decimal(new int[] { 928000000, 0, 0, 0 });
        nudFreq.Minimum = new decimal(new int[] { 863000000, 0, 0, 0 });
        nudFreq.Name = "nudFreq";
        nudFreq.Size = new Size(110, 23);
        nudFreq.Value = new decimal(new int[] { 868100000, 0, 0, 0 });
        //
        // lblPower
        //
        lblPower.AutoSize = true;
        lblPower.Location = new Point(198, 52);
        lblPower.Name = "lblPower";
        lblPower.Size = new Size(52, 15);
        lblPower.Text = "Power dB";
        //
        // nudPower
        //
        nudPower.Location = new Point(256, 48);
        nudPower.Maximum = new decimal(new int[] { 22, 0, 0, 0 });
        nudPower.Name = "nudPower";
        nudPower.Size = new Size(50, 23);
        nudPower.Value = new decimal(new int[] { 14, 0, 0, 0 });
        //
        // lblSf
        //
        lblSf.AutoSize = true;
        lblSf.Location = new Point(320, 52);
        lblSf.Name = "lblSf";
        lblSf.Size = new Size(20, 15);
        lblSf.Text = "SF";
        //
        // cmbSf
        //
        cmbSf.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbSf.FormattingEnabled = true;
        cmbSf.Items.AddRange(new object[] { "5", "6", "7", "8", "9", "10", "11", "12" });
        cmbSf.Location = new Point(346, 48);
        cmbSf.Name = "cmbSf";
        cmbSf.Size = new Size(50, 23);
        //
        // lblBw
        //
        lblBw.AutoSize = true;
        lblBw.Location = new Point(410, 52);
        lblBw.Name = "lblBw";
        lblBw.Size = new Size(25, 15);
        lblBw.Text = "BW";
        //
        // cmbBw
        //
        cmbBw.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBw.FormattingEnabled = true;
        cmbBw.Items.AddRange(new object[] { "125 kHz", "250 kHz", "500 kHz" });
        cmbBw.Location = new Point(441, 48);
        cmbBw.Name = "cmbBw";
        cmbBw.Size = new Size(90, 23);
        //
        // chkAutoInit
        //
        chkAutoInit.AutoSize = true;
        chkAutoInit.Checked = true;
        chkAutoInit.CheckState = CheckState.Checked;
        chkAutoInit.Location = new Point(545, 20);
        chkAutoInit.Name = "chkAutoInit";
        chkAutoInit.Size = new Size(160, 19);
        chkAutoInit.Text = "Auto Init при Connect";
        chkAutoInit.UseVisualStyleBackColor = true;
        //
        // chkAutoListen
        //
        chkAutoListen.AutoSize = true;
        chkAutoListen.Checked = true;
        chkAutoListen.CheckState = CheckState.Checked;
        chkAutoListen.Location = new Point(545, 42);
        chkAutoListen.Name = "chkAutoListen";
        chkAutoListen.Size = new Size(130, 19);
        chkAutoListen.Text = "Держать RX";
        chkAutoListen.UseVisualStyleBackColor = true;
        //
        // btnInitHeltec
        //
        btnInitHeltec.Enabled = false;
        btnInitHeltec.Location = new Point(680, 18);
        btnInitHeltec.Name = "btnInitHeltec";
        btnInitHeltec.Size = new Size(85, 24);
        btnInitHeltec.Text = "Init";
        btnInitHeltec.UseVisualStyleBackColor = true;
        btnInitHeltec.Click += BtnInitHeltec_Click;
        //
        // btnListen
        //
        btnListen.Enabled = false;
        btnListen.Location = new Point(680, 46);
        btnListen.Name = "btnListen";
        btnListen.Size = new Size(85, 27);
        btnListen.Text = "Слушать";
        btnListen.UseVisualStyleBackColor = true;
        btnListen.Click += BtnListen_Click;
        //
        // grpSend
        //
        grpSend.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpSend.Controls.Add(btnSend);
        grpSend.Controls.Add(txtSendHex);
        grpSend.Controls.Add(lblSend);
        grpSend.Location = new Point(12, 170);
        grpSend.Name = "grpSend";
        grpSend.Size = new Size(780, 70);
        grpSend.TabIndex = 2;
        grpSend.TabStop = false;
        grpSend.Text = "Отправка AT+SendHex (до 64 байт)";
        //
        // lblSend
        //
        lblSend.AutoSize = true;
        lblSend.Location = new Point(12, 28);
        lblSend.Name = "lblSend";
        lblSend.Size = new Size(120, 15);
        lblSend.Text = "Payload hex";
        //
        // txtSendHex
        //
        txtSendHex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSendHex.Font = new Font("Consolas", 10F);
        txtSendHex.Location = new Point(100, 25);
        txtSendHex.Name = "txtSendHex";
        txtSendHex.Size = new Size(540, 23);
        txtSendHex.Text = "01 02 03 04";
        //
        // btnSend
        //
        btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSend.Enabled = false;
        btnSend.Location = new Point(654, 23);
        btnSend.Name = "btnSend";
        btnSend.Size = new Size(110, 27);
        btnSend.Text = "Отправить";
        btnSend.UseVisualStyleBackColor = true;
        btnSend.Click += BtnSend_Click;
        //
        // lblReceived
        //
        lblReceived.AutoSize = true;
        lblReceived.Location = new Point(12, 250);
        lblReceived.Name = "lblReceived";
        lblReceived.Size = new Size(140, 15);
        lblReceived.Text = "Принятые сообщения";
        //
        // lstReceived
        //
        lstReceived.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstReceived.Font = new Font("Consolas", 10F);
        lstReceived.FormattingEnabled = true;
        lstReceived.HorizontalScrollbar = true;
        lstReceived.ItemHeight = 15;
        lstReceived.Location = new Point(12, 268);
        lstReceived.Name = "lstReceived";
        lstReceived.Size = new Size(780, 199);
        //
        // btnClearReceived
        //
        btnClearReceived.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearReceived.Location = new Point(682, 245);
        btnClearReceived.Name = "btnClearReceived";
        btnClearReceived.Size = new Size(110, 23);
        btnClearReceived.Text = "Очистить";
        btnClearReceived.UseVisualStyleBackColor = true;
        btnClearReceived.Click += BtnClearReceived_Click;
        //
        // lblErrors
        //
        lblErrors.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblErrors.AutoSize = true;
        lblErrors.Location = new Point(12, 480);
        lblErrors.Name = "lblErrors";
        lblErrors.Size = new Size(48, 15);
        lblErrors.Text = "Ошибки";
        //
        // txtErrors
        //
        txtErrors.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtErrors.Font = new Font("Consolas", 9F);
        txtErrors.Location = new Point(12, 498);
        txtErrors.Multiline = true;
        txtErrors.Name = "txtErrors";
        txtErrors.ReadOnly = true;
        txtErrors.ScrollBars = ScrollBars.Vertical;
        txtErrors.Size = new Size(654, 70);
        //
        // btnClearErrors
        //
        btnClearErrors.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnClearErrors.Location = new Point(682, 498);
        btnClearErrors.Name = "btnClearErrors";
        btnClearErrors.Size = new Size(110, 27);
        btnClearErrors.Text = "Очистить";
        btnClearErrors.UseVisualStyleBackColor = true;
        btnClearErrors.Click += BtnClearErrors_Click;
        //
        // Form1
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(804, 580);
        Controls.Add(btnClearErrors);
        Controls.Add(txtErrors);
        Controls.Add(lblErrors);
        Controls.Add(btnClearReceived);
        Controls.Add(lstReceived);
        Controls.Add(lblReceived);
        Controls.Add(grpSend);
        Controls.Add(grpHeltec);
        Controls.Add(grpPort);
        MinimumSize = new Size(820, 620);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Heltec32 LoRa AT (863–928) — бинарный терминал";
        FormClosing += Form1_FormClosing;
        Load += Form1_Load;
        grpPort.ResumeLayout(false);
        grpPort.PerformLayout();
        grpHeltec.ResumeLayout(false);
        grpHeltec.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudPower).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudFreq).EndInit();
        grpSend.ResumeLayout(false);
        grpSend.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox grpPort;
    private Label lblPort;
    private ComboBox cmbPort;
    private Button btnRefreshPorts;
    private Label lblBaud;
    private ComboBox cmbBaud;
    private Button btnConnect;
    private GroupBox grpHeltec;
    private Label lblBandHint;
    private Label lblFreq;
    private NumericUpDown nudFreq;
    private Label lblPower;
    private NumericUpDown nudPower;
    private Label lblSf;
    private ComboBox cmbSf;
    private Label lblBw;
    private ComboBox cmbBw;
    private CheckBox chkAutoInit;
    private CheckBox chkAutoListen;
    private Button btnInitHeltec;
    private Button btnListen;
    private GroupBox grpSend;
    private Label lblSend;
    private TextBox txtSendHex;
    private Button btnSend;
    private Label lblReceived;
    private ListBox lstReceived;
    private Button btnClearReceived;
    private Label lblErrors;
    private TextBox txtErrors;
    private Button btnClearErrors;
}

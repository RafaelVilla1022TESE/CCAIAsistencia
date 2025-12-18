namespace CCAIAsistencia.Views;

partial class LoginView
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private Label lblTitle = null!;
    private Label lblUser = null!;
    private Label lblPassword = null!;
    private TextBox txtUser = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Button btnCancel = null!;
    private Label lblStatus = null!;

    /// <summary>
    ///  Clean up any resources being used.
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

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblTitle = new Label();
        lblUser = new Label();
        lblPassword = new Label();
        txtUser = new TextBox();
        txtPassword = new TextBox();
        btnLogin = new Button();
        btnCancel = new Button();
        lblStatus = new Label();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.Location = new Point(24, 20);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(252, 28);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "CCAIAsistencia - Acceso";
        // 
        // lblUser
        // 
        lblUser.AutoSize = true;
        lblUser.Location = new Point(26, 74);
        lblUser.Name = "lblUser";
        lblUser.Size = new Size(62, 20);
        lblUser.TabIndex = 1;
        lblUser.Text = "Usuario";
        // 
        // lblPassword
        // 
        lblPassword.AutoSize = true;
        lblPassword.Location = new Point(26, 123);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(83, 20);
        lblPassword.TabIndex = 3;
        lblPassword.Text = "Contrasena";
        // 
        // txtUser
        // 
        txtUser.BackColor = SystemColors.Info;
        txtUser.Location = new Point(132, 70);
        txtUser.Name = "txtUser";
        txtUser.PlaceholderText = "ej: admin";
        txtUser.Size = new Size(234, 27);
        txtUser.TabIndex = 2;
        // 
        // txtPassword
        // 
        txtPassword.BackColor = SystemColors.Info;
        txtPassword.Location = new Point(132, 119);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '*';
        txtPassword.PlaceholderText = "contrasena";
        txtPassword.Size = new Size(234, 27);
        txtPassword.TabIndex = 4;
        // 
        // btnLogin
        // 
        btnLogin.Location = new Point(132, 168);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(120, 34);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "Ingresar";
        btnLogin.UseVisualStyleBackColor = true;
        btnLogin.Click += OnLoginClick;
        // 
        // btnCancel
        // 
        btnCancel.Location = new Point(266, 168);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(120, 34);
        btnCancel.TabIndex = 6;
        btnCancel.Text = "Cancelar";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += OnCancelClick;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.ForeColor = Color.Firebrick;
        lblStatus.Location = new Point(26, 217);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(0, 20);
        lblStatus.TabIndex = 6;
        // 
        // LoginView
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(506, 245);
        Controls.Add(lblStatus);
        Controls.Add(btnCancel);
        Controls.Add(btnLogin);
        Controls.Add(txtPassword);
        Controls.Add(txtUser);
        Controls.Add(lblPassword);
        Controls.Add(lblUser);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginView";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CCAIAsistencia - Login";
        AcceptButton = btnLogin;
        Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "login.ico"));
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}

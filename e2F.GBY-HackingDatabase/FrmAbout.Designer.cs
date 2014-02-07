namespace e2F_GHDB_GUI
{
    partial class FrmAbout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAbout));
            this.lblHakkindaDetay = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblHakkindaDetay
            // 
            this.lblHakkindaDetay.BackColor = System.Drawing.Color.Transparent;
            this.lblHakkindaDetay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHakkindaDetay.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblHakkindaDetay.ForeColor = System.Drawing.Color.Black;
            this.lblHakkindaDetay.Location = new System.Drawing.Point(0, 0);
            this.lblHakkindaDetay.Margin = new System.Windows.Forms.Padding(10);
            this.lblHakkindaDetay.Name = "lblHakkindaDetay";
            this.lblHakkindaDetay.Padding = new System.Windows.Forms.Padding(5);
            this.lblHakkindaDetay.Size = new System.Drawing.Size(406, 258);
            this.lblHakkindaDetay.TabIndex = 31;
            this.lblHakkindaDetay.Text = resources.GetString("lblHakkindaDetay.Text");
            // 
            // FrmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 258);
            this.Controls.Add(this.lblHakkindaDetay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAbout";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hakkında";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblHakkindaDetay;
    }
}
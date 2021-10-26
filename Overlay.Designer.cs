
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MapAssist
{
    partial class Overlay
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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Overlay));
            mapOverlay = new PictureBox();
            ((ISupportInitialize)(mapOverlay)).BeginInit();
            this.SuspendLayout();
            // 
            // mapOverlay
            // 
            mapOverlay.BackColor = Color.Transparent;
            mapOverlay.Location = new Point(12, 3);
            mapOverlay.Name = "mapOverlay";
            mapOverlay.Size = new Size(0, 0);
            mapOverlay.TabIndex = 0;
            mapOverlay.TabStop = false;
            mapOverlay.Paint += new PaintEventHandler(MapOverlay_Paint);

            // 
            // frmOverlay
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1767, 996);
            Controls.Add(mapOverlay);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)(resources.GetObject("$this.Icon"));
            Name = "Overlay";
            TransparencyKey = Color.Black;
            BackColor = Color.Black;
            WindowState = FormWindowState.Maximized;
            Load += new EventHandler(Overlay_Load);
            FormClosing += new FormClosingEventHandler(Overlay_FormClosing);
            ((ISupportInitialize)(mapOverlay)).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox mapOverlay;
    }
}

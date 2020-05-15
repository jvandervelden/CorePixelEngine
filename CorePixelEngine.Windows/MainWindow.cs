using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CorePixelEngine.Windows
{
    public class MainWindow : Form
    {
        public string Title { get; set; } = "";
        public IntPtr ThreadSafeHandle { get; private set; } = IntPtr.Zero;

        public MainWindow(VectorI2d vWindowPos, VectorI2d vWindowSize, bool bFullScreen)
        {
            InitializeComponent(vWindowPos, vWindowSize, bFullScreen);
            ThreadSafeHandle = this.Handle;
        }

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent(VectorI2d vWindowPos, VectorI2d vWindowSize, bool bFullScreen)
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(vWindowSize.x, vWindowSize.y);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(vWindowPos.x, vWindowPos.y);
            this.Text = "Form1";
                        
            Timer timer = new Timer(this.components);
            timer.Interval = 1000;
            timer.Tick += TitleUpdateTimer;
            timer.Enabled = true;

            if (bFullScreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            } 
            else
            {
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
            }
        }

        private void TitleUpdateTimer(object sender, EventArgs e)
        {
            this.Text = Title;
        }
    }
}

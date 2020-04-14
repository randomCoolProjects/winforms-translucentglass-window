using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core;

namespace BluredWindow
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        TranslucentWindow win = new TranslucentWindow();
        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            CheckForIllegalCrossThreadCalls = false;
            this.MouseDown += Form1_MouseDown;
            foreach(Control c in Controls)
            {
                c.MouseDown += Form1_MouseDown;
            }
            DropShadow.Shadow(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            win.Apply(this, 2, 0.2f);

        }
    }
}

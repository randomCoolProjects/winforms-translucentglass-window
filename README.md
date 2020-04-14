# winforms-translucentglass-window
Translucent Glass Window for C# Windows Forms Application
![Demo](https://i.imgur.com/EnXQ462.png)

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

It has a little bit of flickering, but can be used as prototype.

[Documentation in Working]

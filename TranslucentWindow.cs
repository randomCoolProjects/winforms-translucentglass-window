using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Core;

public class TranslucentWindow
{
    private Form window;
    private Bitmap screenBmp;
    private Point lastWindowPos;

    private int screen_update = 5000;
    private int delay = 1;
    private float resolution = 0.15f;
    private int blur_radius = 2;

    int scrW, scrH;

    Thread main_thread;

    int fromRes(int i)
    {
        return (int)(((float)i)*resolution);
    }

    void TakeScreenShoot()
    {
        int screenLeft = SystemInformation.VirtualScreen.Left;
        int screenTop = SystemInformation.VirtualScreen.Top;
        int screenWidth = SystemInformation.VirtualScreen.Width;
        int screenHeight = SystemInformation.VirtualScreen.Height;

        scrW = screenWidth;
        scrH = screenHeight;

        var opc = window.Opacity;
        // Create a bitmap of the appropriate size to receive the screenshot.
        using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
        {
            // Draw the screenshot into our bitmap.
            var g = Graphics.FromImage(bmp);
            int extra = 0;
            foreach(var s in Screen.AllScreens)
            {
                window.Opacity = 0;
                g.CopyFromScreen(s.Bounds.Left, s.Bounds.Top, 0+extra, s.Bounds.Top, s.Bounds.Size);
                window.Opacity = opc;
                extra+=s.Bounds.Width;
            }
            g.Dispose();
            // Do something with the Bitmap here, like save it to a file:
            screenBmp = new Bitmap(bmp, fromRes(bmp.Width),
                fromRes(bmp.Height));
            screenBmp.Save("tmp.jpg");
            bmp.Dispose();
        }
    }

    void ApplyToWindow()
    {
        var pos = new Point(window.Left, window.Top);
        if (pos.X < 0) pos.X = scrW + pos.X;
        var siz = window.Size;

        var bmp = ImageFX.CropImage(screenBmp, new Rectangle(
            fromRes(pos.X), fromRes(pos.Y),
            fromRes(siz.Width), fromRes(siz.Height)
            ));

        window.BackgroundImage = ImageFX.GaussianBlurImage(bmp, blur_radius);
        bmp.Dispose();
    }

    int lastApply = 0, lastUpdate = 0;
    void Main()
    {
        while(true)
        {
            if (Environment.TickCount - lastUpdate > screen_update)
            {
                TakeScreenShoot();
                ApplyToWindow();
                lastUpdate = Environment.TickCount;
            }
            if (Environment.TickCount - lastApply > delay
                && lastWindowPos != window.Location)
            {
                ApplyToWindow();
                lastWindowPos = window.Location;
                lastApply = Environment.TickCount;
            }

            //Thread.Sleep(1);
        }
    }

    public void Apply(Form f, int BlurRadius = 2, float Resolution = 0.2f /*20%*/, int ScreenUpdate = 5000, int Delay = 10)
    {
        blur_radius = BlurRadius;
        screen_update = ScreenUpdate;
        delay = Delay;
        resolution = Resolution;

        window = f;
        window.BackgroundImageLayout = ImageLayout.Stretch;
        TakeScreenShoot();
        ApplyToWindow();
        lastUpdate = Environment.TickCount;
        main_thread = new Thread(Main);
        main_thread.Start();
    }
}


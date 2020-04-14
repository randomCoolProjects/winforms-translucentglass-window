using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Design;
using System.ComponentModel;

namespace Core
{
    public class DropShadow
    {
        public static void Shadow(Form f)
        {
            new DropShadow().ApplyShadows(f);
        }
        #region Shadowing

        #region Fields

        private bool _isAeroEnabled = false;
        private bool _isDraggingEnabled = false;
        private const int WM_NCHITTEST = 0x84;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        private const int CS_DBLCLKS = 0x8;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        #endregion

        #region Structures

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        #endregion

        #region Methods

        #region Public

        [DllImport("dwmapi.dll")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsCompositionEnabled()
        {
            if (Environment.OSVersion.Version.Major < 6) return false;

            bool enabled;
            DwmIsCompositionEnabled(out enabled);

            return enabled;
        }

        #endregion

        #region Private

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
         );

        private bool CheckIfAeroIsEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);

                return (enabled == 1) ? true : false;
            }
            return false;
        }

        #endregion

        #region Overrides

        public void ApplyShadows(Form form)
        {
            var v = 2;

            DwmSetWindowAttribute(form.Handle, 2, ref v, 4);

            MARGINS margins = new MARGINS()
            {
                bottomHeight = 1,
                leftWidth = 0,
                rightWidth = 0,
                topHeight = 0
            };

            DwmExtendFrameIntoClientArea(form.Handle, ref margins);
        }

        #endregion

        #endregion

        #endregion
    }

    public class ImageFX
    {
        public static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        public class GaussianBlur
        {
            private readonly int[] _alpha;
            private readonly int[] _red;
            private readonly int[] _green;
            private readonly int[] _blue;

            private readonly int _width;
            private readonly int _height;

            private readonly ParallelOptions _pOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

            public GaussianBlur(Bitmap image)
            {
                var rct = new Rectangle(0, 0, image.Width, image.Height);
                var source = new int[rct.Width * rct.Height];
                var bits = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                Marshal.Copy(bits.Scan0, source, 0, source.Length);
                image.UnlockBits(bits);

                _width = image.Width;
                _height = image.Height;

                _alpha = new int[_width * _height];
                _red = new int[_width * _height];
                _green = new int[_width * _height];
                _blue = new int[_width * _height];

                Parallel.For(0, source.Length, _pOptions, i =>
                {
                    _alpha[i] = (int)((source[i] & 0xff000000) >> 24);
                    _red[i] = (source[i] & 0xff0000) >> 16;
                    _green[i] = (source[i] & 0x00ff00) >> 8;
                    _blue[i] = (source[i] & 0x0000ff);
                });
            }

            public Bitmap Process(int radial)
            {
                var newAlpha = new int[_width * _height];
                var newRed = new int[_width * _height];
                var newGreen = new int[_width * _height];
                var newBlue = new int[_width * _height];
                var dest = new int[_width * _height];

                Parallel.Invoke(
                    () => gaussBlur_4(_alpha, newAlpha, radial),
                    () => gaussBlur_4(_red, newRed, radial),
                    () => gaussBlur_4(_green, newGreen, radial),
                    () => gaussBlur_4(_blue, newBlue, radial));

                Parallel.For(0, dest.Length, _pOptions, i =>
                {
                    if (newAlpha[i] > 255) newAlpha[i] = 255;
                    if (newRed[i] > 255) newRed[i] = 255;
                    if (newGreen[i] > 255) newGreen[i] = 255;
                    if (newBlue[i] > 255) newBlue[i] = 255;

                    if (newAlpha[i] < 0) newAlpha[i] = 0;
                    if (newRed[i] < 0) newRed[i] = 0;
                    if (newGreen[i] < 0) newGreen[i] = 0;
                    if (newBlue[i] < 0) newBlue[i] = 0;

                    dest[i] = (int)((uint)(newAlpha[i] << 24) | (uint)(newRed[i] << 16) | (uint)(newGreen[i] << 8) | (uint)newBlue[i]);
                });

                var image = new Bitmap(_width, _height);
                var rct = new Rectangle(0, 0, image.Width, image.Height);
                var bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
                image.UnlockBits(bits2);
                return image;
            }

            private void gaussBlur_4(int[] source, int[] dest, int r)
            {
                var bxs = boxesForGauss(r, 3);
                boxBlur_4(source, dest, _width, _height, (bxs[0] - 1) / 2);
                boxBlur_4(dest, source, _width, _height, (bxs[1] - 1) / 2);
                boxBlur_4(source, dest, _width, _height, (bxs[2] - 1) / 2);
            }

            private int[] boxesForGauss(int sigma, int n)
            {
                var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
                var wl = (int)Math.Floor(wIdeal);
                if (wl % 2 == 0) wl--;
                var wu = wl + 2;

                var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
                var m = Math.Round(mIdeal);

                var sizes = new List<int>();
                for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
                return sizes.ToArray();
            }

            private void boxBlur_4(int[] source, int[] dest, int w, int h, int r)
            {
                for (var i = 0; i < source.Length; i++) dest[i] = source[i];
                boxBlurH_4(dest, source, w, h, r);
                boxBlurT_4(source, dest, w, h, r);
            }

            private void boxBlurH_4(int[] source, int[] dest, int w, int h, int r)
            {
                var iar = (double)1 / (r + r + 1);
                Parallel.For(0, h, _pOptions, i =>
                {
                    var ti = i * w;
                    var li = ti;
                    var ri = ti + r;
                    var fv = source[ti];
                    var lv = source[ti + w - 1];
                    var val = (r + 1) * fv;
                    for (var j = 0; j < r; j++) val += source[ti + j];
                    for (var j = 0; j <= r; j++)
                    {
                        val += source[ri++] - fv;
                        dest[ti++] = (int)Math.Round(val * iar);
                    }
                    for (var j = r + 1; j < w - r; j++)
                    {
                        val += source[ri++] - dest[li++];
                        dest[ti++] = (int)Math.Round(val * iar);
                    }
                    for (var j = w - r; j < w; j++)
                    {
                        val += lv - source[li++];
                        dest[ti++] = (int)Math.Round(val * iar);
                    }
                });
            }

            private void boxBlurT_4(int[] source, int[] dest, int w, int h, int r)
            {
                var iar = (double)1 / (r + r + 1);
                Parallel.For(0, w, _pOptions, i =>
                {
                    var ti = i;
                    var li = ti;
                    var ri = ti + r * w;
                    var fv = source[ti];
                    var lv = source[ti + w * (h - 1)];
                    var val = (r + 1) * fv;
                    for (var j = 0; j < r; j++) val += source[ti + j * w];
                    for (var j = 0; j <= r; j++)
                    {
                        val += source[ri] - fv;
                        dest[ti] = (int)Math.Round(val * iar);
                        ri += w;
                        ti += w;
                    }
                    for (var j = r + 1; j < h - r; j++)
                    {
                        val += source[ri] - source[li];
                        dest[ti] = (int)Math.Round(val * iar);
                        li += w;
                        ri += w;
                        ti += w;
                    }
                    for (var j = h - r; j < h; j++)
                    {
                        val += lv - source[li];
                        dest[ti] = (int)Math.Round(val * iar);
                        li += w;
                        ti += w;
                    }
                });
            }
        }

        public static Bitmap GaussianBlurImage(Bitmap b, int r)
        {
            var g = new GaussianBlur(b);
            var result = g.Process(r);
            b.Dispose();
            g = null;
            return result;
        }
    }
}
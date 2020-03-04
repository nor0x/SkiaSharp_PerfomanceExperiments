//#define USE_BITMAP

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Xamarin.Forms;

namespace SkiaXFPlayground
{

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        SKMatrix _currentMatrix;

        /// <summary>
        /// display scale factor of the current device (retina macbook = 2)
        /// </summary>
        double _deviceScaleFactor = 2;
        double currentScale = 1;
        double startScale = 1;

        int resourceLimit = 10;
        long resourceLimitBytes = 100000000;

        string logtext = "";
        int imagesize = 7000;
        int _padding = 20;
        int _numberOfImages = 0;
        float dragX;
        float dragY;

        float _lastX;
        float _lastY;

        int imageCount = 5;
        float matrixScale = 0.28f;
        Stopwatch sw = Stopwatch.StartNew();
        TimeSpan last;
        Random rand = new Random();

        int nResources;
        long nResourceBytes;
        int nCacheResources;
        long nCacheResourcesBytes;
        SKFilterQuality _quality;
        List<MyImage> _images;
        List<byte[]> _buffer;
        public MainPage()
        {
            InitializeComponent();
            _currentMatrix = SKMatrix.MakeIdentity();
            _images = new List<MyImage>();
            _quality = SKFilterQuality.None;
            var pinch = new PinchGestureRecognizer();
            pinch.PinchUpdated += Pinch_PinchUpdated;
            SkiaView.GestureRecognizers.Add(pinch);
            SkiaView.EnableTouchEvents = true;
            SkiaView.Touch += SkiaView_Touch;

            var assembly = typeof(MainPage).GetTypeInfo().Assembly; // you can replace "this.GetType()" with "typeof(MyType)", where MyType is any type in your assembly.
            _buffer = new List<byte[]>();
            for (int i = 0; i < 24; i++)
            {
                var names = assembly.GetManifestResourceNames();
                using (Stream s = assembly.GetManifestResourceStream("SkiaXFPlayground.img" + i + ".jpg"))
                {
                    if (s != null)
                    {
                        long length = s.Length;
                        var b = new byte[length];
                        s.Read(b, 0, (int)length);
                        _buffer.Add(b);

                    }
                }
                LogLabel.Text = "Loading image: " + i;
            }
            LogLabel.Text = "buffering finished";
            var x = 0;
            var y = 0;



            var im = SKImage.FromEncodedData(_buffer.ElementAt(0));

            var img = new MyImage()
            {
                Image = im
            };
            img.Destination = new SKRect(x, y, x + imagesize, y + imagesize);
            //_lastX = img.Destination.Right + _padding;
            //_lastY = img.Destination.Top;

            LogLabel.Text = "image added ";

            
            LogLabel.Text = "decoding finished " + _images.Count;

        }

        private void SkiaView_Touch(object sender, SkiaSharp.Views.Forms.SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SkiaSharp.Views.Forms.SKTouchAction.Entered:
                    dragX = e.Location.X;
                    dragY = e.Location.Y;
                    break;
                case SkiaSharp.Views.Forms.SKTouchAction.Moved:
                    _currentMatrix.TransX = dragX + e.Location.X;
                    _currentMatrix.TransY = dragY + e.Location.Y;
                    SkiaView.InvalidateSurface();
                    break;
                case SkiaSharp.Views.Forms.SKTouchAction.Released:
                    break;
                default:
                    break;
            }
        }

        double lastScale = 0;
        private void Pinch_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {

            if (e.Status == GestureStatus.Started)
            {
                startScale = lastScale;
            }
            else if (e.Status == Xamarin.Forms.GestureStatus.Running)
            {
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(0.003, currentScale);
                _currentMatrix.ScaleX = (float)currentScale;
                _currentMatrix.ScaleY = (float)currentScale;
                SkiaView.InvalidateSurface();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (SkiaView.GRContext != null)
            {
                SkiaView.GRContext.SetResourceCacheLimits(200, 10240000000);
            }
        }

        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintGLSurfaceEventArgs e)
        {
            SkiaView.GRContext.GetResourceCacheLimits(out nResources, out nResourceBytes);

            SkiaView.GRContext.GetResourceCacheUsage(out nCacheResources, out nCacheResourcesBytes);

            e.Surface.Canvas.SetMatrix(_currentMatrix);
            e.Surface.Canvas.Clear(SKColors.LemonChiffon);
            using (SKPaint p = new SKPaint { IsAntialias = true, StrokeWidth = 2, Typeface = SKTypeface.FromFamilyName("default"), TextSize = 70})
            {
                p.FilterQuality = _quality;
                foreach (var img in _images)
                {
#if USE_BITMAP
                    e.Surface.Canvas.DrawBitmap(img.Image, img.Destination, p);
#else
                    e.Surface.Canvas.DrawImage(img.Image, img.Destination, p);
#endif
                }
                var c = sw.Elapsed;
                var ts = c - last;
                last = c;

                var fps = 1.0 / (ts.TotalSeconds);
                var fpsString = fps.ToString("00.00");
                lastScale = e.Surface.Canvas.TotalMatrix.ScaleX;
                e.Surface.Canvas.ResetMatrix();
                p.FilterQuality = SKFilterQuality.High;
                //e.Surface.Canvas.DrawText("FPS: " + fpsString, 100, 100, p);
                FPSLabel.Text = fpsString;
                ScaleLabel.Text = _currentMatrix.ScaleX.ToString();
                UsageLabel.Text = nResourceBytes.ToPrettySize(3) + " | " + nCacheResourcesBytes.ToPrettySize(2);
                NumberLabel.Text = nCacheResources.ToString() + " | " + nResources.ToString();

                //e.Surface.Canvas.DrawText("Resource Limits: \t" + nResources + " " + nResourceBytes.ToString("N"), 100, 190, p);
                //e.Surface.Canvas.DrawText("Resource Usages: \t" + nCacheResources + " " + nCacheResourcesBytes.ToString("N"), 100, 280, p);
            }
        }

        void AddImage_Clicked(System.Object sender, System.EventArgs e)
        {
            var index = rand.Next(0, _buffer.Count());
            index = 13;
            var im = SKImage.FromEncodedData(_buffer.ElementAt(index)).ToTextureImage(SkiaView.GRContext);

            var img = new MyImage()
            {
                Image = im
            };
            if (_numberOfImages % 10 == 0)
            {
                _lastX = 0;
                _lastY += imagesize + 10;
            }
            else
            {
                _lastX += imagesize + 10;
            }
            img.Destination = new SKRect(_lastX, _lastY, _lastX + imagesize, _lastY + imagesize);
            _images.Add(img);
            _numberOfImages++;
            SkiaView.InvalidateSurface();
        }

        void RemoveImage_Clicked(System.Object sender, System.EventArgs e)
        {
            if (_images.Count() != 0)
            {
                _images.RemoveAt(_images.Count() - 1);
                _numberOfImages--;
                if (_numberOfImages % 5 == 0)
                {
                    _lastX = 0;
                    _lastY -= imagesize + 10;
                }
                else
                {
                    _lastX -= imagesize + 10;
                }
            }
            SkiaView.InvalidateSurface();
        }

        void Quality_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            switch(QualityPicker.SelectedIndex)
            {
                case 0:
                    _quality = SKFilterQuality.None;
                    break;
                case 1:
                    _quality = SKFilterQuality.Low;
                    break;
                case 2:
                    _quality = SKFilterQuality.Medium;
                    break;
                case 3:
                    _quality = SKFilterQuality.High;
                    break;

            }
            SkiaView.InvalidateSurface();
        }
    }

    internal class MyImage
    {
#if USE_BITMAP
        public SKBitmap Image { get; set; }

#else
        public SKImage Image { get; set; }
#endif
        public SKRect Destination { get; set; }
    }

    public static class Ext
    {
        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        public static string ToPrettySize(this int value, int decimalPlaces = 0)
        {
            return ((long)value).ToPrettySize(decimalPlaces);
        }

        public static string ToPrettySize(this long value, int decimalPlaces = 0)
        {
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}Tb", asTb)
                : asGb > 1 ? string.Format("{0}Gb", asGb)
                : asMb > 1 ? string.Format("{0}Mb", asMb)
                : asKb > 1 ? string.Format("{0}Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }
    }

}

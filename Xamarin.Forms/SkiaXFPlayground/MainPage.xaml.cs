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
        string logtext = "";
        int imagesize = 1000;

        float matrixScale = 0.28f;

        List<MyImage> _images;

        public MainPage()
        {
            InitializeComponent();
            _currentMatrix = SKMatrix.MakeIdentity();
            _images = new List<MyImage>();

            var pinch = new PinchGestureRecognizer();
            pinch.PinchUpdated += Pinch_PinchUpdated;
            SkiaView.GestureRecognizers.Add(pinch);

            SkiaView.EnableTouchEvents = true;
            SkiaView.Touch += SkiaView_Touch;

            var assembly = typeof(MainPage).GetTypeInfo().Assembly; // you can replace "this.GetType()" with "typeof(MyType)", where MyType is any type in your assembly.
            List<byte[]> buffer = new List<byte[]>();
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
                        buffer.Add(b);

                    }
                }
                logtext = "image: " + i;
            }
            logtext = "buffering finished";
            var rand = new Random();
            var x = 0;
            var y = 0;


#if USE_BITMAP
            for (int i = 1; i < 50; i++)
            {
                var index = rand.Next(0, buffer.Count());

                var im = SKBitmap.Decode(buffer.ElementAt(index));

                var img = new MyImage()
                {
                    Image = im
                };
                img.Destination = new SKRect(x, y, x + imagesize, y + imagesize);
                if (i % 40 == 0)
                {
                    x = 0;
                    y += imagesize + 10;
                }
                else
                {
                    x += imagesize + 10;
                }
                _images.Add(img);
                logtext = "image added " + i;

            }
            logtext = "decoding finished " + _images.Count;

#else


            for (int i = 1; i < 9 ; i++)
            {
                var index = rand.Next(0, buffer.Count());

                var im = SKImage.FromEncodedData(buffer.ElementAt(i));

                var img = new MyImage()
                {
                    Image = im
                };
                img.Destination = new SKRect(x, y, x + imagesize, y + imagesize);
                if (i % 40 == 0)
                {
                    x = 0;
                    y += imagesize + 10;
                }
                else
                {
                    x += imagesize + 10;
                }
                _images.Add(img);
                logtext = "image added " + i;

            }
            logtext = "decoding finished " + _images.Count;


#endif


        }

        private void SkiaView_Touch(object sender, SkiaSharp.Views.Forms.SKTouchEventArgs e)
        {
            switch(e.ActionType)
            {
                case SkiaSharp.Views.Forms.SKTouchAction.Moved:
                    _currentMatrix.TransX = e.Location.X;
                    _currentMatrix.TransY = e.Location.Y;
                    SkiaView.InvalidateSurface();
                    break;
                default:
                    break;
            }
        }


        private void Pinch_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {

            if (e.Status == GestureStatus.Started)
            {
                startScale = _currentMatrix.ScaleX;
            }
            else if (e.Status == Xamarin.Forms.GestureStatus.Running)
            {
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(0.03, currentScale);
                _currentMatrix.ScaleX = (float)currentScale;
                _currentMatrix.ScaleY = (float)currentScale;
                SkiaView.InvalidateSurface();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            

        }


        Stopwatch sw = Stopwatch.StartNew();
        TimeSpan last;


        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintGLSurfaceEventArgs e)
        {
            
            if (SkiaView.GRContext != null)
            {
                SkiaView.GRContext.SetResourceCacheLimits(500, 1024000000);
                int li = 0;
                long maxre = 0;
                SkiaView.GRContext.GetResourceCacheLimits(out li, out maxre);

                int cli = 0;
                long cmaxre = 0;
                SkiaView.GRContext.GetResourceCacheUsage(out cli, out cmaxre);
                System.Diagnostics.Debug.WriteLine("limits: " + li + " \t" + maxre);
                System.Diagnostics.Debug.WriteLine("usage : " + cli + " \t" + cmaxre);


            }
            
            logtext = "scale: " + _currentMatrix.ScaleX;

            e.Surface.Canvas.SetMatrix(_currentMatrix);
            e.Surface.Canvas.Clear(SKColors.DarkGray);
            using (SKPaint p = new SKPaint { IsAntialias = true, StrokeWidth = 2, Typeface = SKTypeface.FromFamilyName("default"), TextSize = 100, FilterQuality = SKFilterQuality.High})
            {
                foreach(var img in _images)
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
                e.Surface.Canvas.ResetMatrix();
                e.Surface.Canvas.DrawText("FPS: " + fpsString, 100, 100, p);
                e.Surface.Canvas.DrawText("log: " + logtext, 100,200, p);
            }
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
}

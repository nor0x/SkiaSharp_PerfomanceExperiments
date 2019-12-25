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
            byte[] buffer;
            var names = assembly.GetManifestResourceNames();
            using (Stream s = assembly.GetManifestResourceStream("SkiaXFPlayground.nasa.jpg"))
            {
                if (s != null)
                {
                    long length = s.Length;
                    buffer = new byte[length];
                    s.Read(buffer, 0, (int)length);
                    var x = 0;
                    var y = 0;
                    var size = 300;
#if USE_BITMAP
                    var im = SKBitmap.Decode(buffer);
                    for (int i = 0; i < 20; i++)
                    {
                      var img = new MyImage()
                        {
                            Image = im
                        };
                        img.Destination = new SKRect(x, y, x + size, y + size);
                        if (i % 10 == 0)
                        {
                            x = 0;
                            y += size;
                        }
                        else
                        {
                            x += size + 10;
                        }
                        _images.Add(img);
                    }

#else
                    var im = SKImage.FromEncodedData(buffer);
                    for (int i = 1; i < 1001; i++)
                    {
                        var img = new MyImage()
                        {
                            Image = im
                        };
                        img.Destination = new SKRect(x, y, x + size, y + size);
                        if (i % 40 == 0)
                        {
                            x = 0;
                            y += size + 10;
                        }
                        else
                        {
                            x += size + 10;
                        }
                        _images.Add(img);
                    }

#endif
                }
            }
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
            if (SkiaView.GRContext != null)
            {
                SkiaView.GRContext.SetResourceCacheLimits(200, 1024000000);
            }
        }


        Stopwatch sw = Stopwatch.StartNew();
        TimeSpan last;
        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintGLSurfaceEventArgs e)
        {
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

#define USE_BITMAP

using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            if (Device.RuntimePlatform == Device.macOS)
            {
                var pinch = new PinchGestureRecognizer();
                pinch.PinchUpdated += Pinch_PinchUpdated;
                SkiaView.GestureRecognizers.Add(pinch);
            }
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
#if USE_BITMAP
                    var img1 = new MyImage() { Location = new SKPoint(0, 0), Image = SKBitmap.Decode(buffer) };
                    var img2 = new MyImage() { Location = new SKPoint(300, 300), Image = SKBitmap.Decode(buffer) };
                    var img3 = new MyImage() { Location = new SKPoint(600, 600), Image = SKBitmap.Decode(buffer) };
#else

                    var img1 = new MyImage() { Location = new SKPoint(0, 0), Image = SKImage.FromEncodedData(buffer) };
                    var img2 = new MyImage() { Location = new SKPoint(300, 300), Image = SKImage.FromEncodedData(buffer) };
                    var img3 = new MyImage() { Location = new SKPoint(600, 600), Image = SKImage.FromEncodedData(buffer) };

#endif
                    _images.Add(img1);
                    _images.Add(img2);
                    _images.Add(img3);
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

        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintGLSurfaceEventArgs e)
        {

            e.Surface.Canvas.SetMatrix(_currentMatrix);
            e.Surface.Canvas.Clear(SKColors.SeaGreen);
            using (SKPaint p = new SKPaint { IsAntialias = true, StrokeWidth = 2, Typeface = SKTypeface.FromFamilyName("default"), TextSize = 100, FilterQuality = SKFilterQuality.High})
            {
                foreach(var img in _images)
                {
#if USE_BITMAP
                    e.Surface.Canvas.DrawBitmap(img.Image, img.Location, p);
#else
                    e.Surface.Canvas.DrawImage(img.Image, img.Location, p);
#endif
                }
                e.Surface.Canvas.DrawText("hello world :-)", 100, 100, p);

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
        public SKPoint Location { get; set; }
        public SKSize Size { get; set; }
    }
}

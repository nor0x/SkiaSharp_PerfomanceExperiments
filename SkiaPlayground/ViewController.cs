using System;
using SkiaSharp.Views;
using AppKit;
using Foundation;
using SkiaSharp;
using SkiaSharp.Views.Mac;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using FFImageLoading;
using System.Linq;

namespace SkiaPlayground
{
    public partial class ViewController : NSViewController
    {
        SKMatrix _m = SKMatrix.MakeIdentity();
        SKMatrix _im = SKMatrix.MakeIdentity();
        SKMatrix _startM = SKMatrix.MakeIdentity();
        SKPoint _startAnchorPt;
        double _totalScale = 1;
        float _screenScale;
        bool _isPanZoom = false;
        SKPoint _totalDistance;
        SKSize _canvasSize;
        List<SKBitmap> imgs = new List<SKBitmap>();
        List<SKRect> rects = new List<SKRect>();
        Random rand = new Random();

        SKPaint HighQualityImagePaint => new SKPaint() { FilterQuality = SKFilterQuality.High, IsAntialias = true };

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            CanvasView.PaintSurface += CanvasView_PaintSurface;
            var pan = new NSPanGestureRecognizer(MapPanned);
            var zoom = new NSMagnificationGestureRecognizer(MapZoomed);

            _screenScale = (float)NSScreen.MainScreen.BackingScaleFactor;
            _canvasSize = new SKSize(20000, 2000);

            CanvasView.AddGestureRecognizer(zoom);
            CanvasView.AddGestureRecognizer(pan);

            //load images
            for (int i = 1; i < 16; i++)
            {
                var img = await ImageService.Instance.LoadFileFromApplicationBundle("img/img" + i + ".jpg").AsNSImageAsync();
                var sizeRandom = rand.NextDouble() * (1.2 - 0.2) + 0.2;
                var size = new SKSize((float)(img.Size.Width * sizeRandom), (float)(img.Size.Height * sizeRandom));
                var rect = SKRect.Empty;
                rect.Size = size;
                var pointRandomW = rand.NextDouble() * (_canvasSize.Width - 0) + 0;
                var pointRandomH = rand.NextDouble() * (_canvasSize.Height - 0) + 0;

                rect.Location = new SKPoint((float)pointRandomW, (float)pointRandomH);
                rects.Add(rect);
                imgs.Add(img.ToSKBitmap());
            }
        }

        void CanvasView_PaintSurface(object sender, SkiaSharp.Views.Mac.SKPaintGLSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear(SKColors.Black);
            e.Surface.Canvas.SetMatrix(_m);

            for(int i = 0; i < imgs.Count; i++)
            {
                e.Surface.Canvas.DrawBitmap(imgs.ElementAt(i), rects.ElementAt(i), HighQualityImagePaint);
            }
        }


        void MapPanned(NSPanGestureRecognizer obj)
        {
            var p = obj.TranslationInView(CanvasView);

            var pp = new SKPoint((float)(_canvasSize.Width * p.X / CanvasView.Bounds.Width),
                        (float)(_canvasSize.Height - (_canvasSize.Height * p.Y / CanvasView.Bounds.Height)));

            pp = _im.MapPoint(pp);

            if (!_isPanZoom)
            {
                StartPanZoom(new SKPoint((float)pp.X, (float)pp.Y));
            }
            if (_isPanZoom)
            {
                _totalDistance = new SKPoint((float)p.X, 1 - (float)p.Y);
                DoPanZoom(_startM, _startAnchorPt, _totalDistance, _totalScale);
            }
            NSCursor.ClosedHandCursor.Set();
            if (obj.State == NSGestureRecognizerState.Ended)
            {
                NSCursor.ArrowCursor.Set();
                _isPanZoom = false;
                _m.TryInvert(out _im);
            }
        }

        void MapZoomed(NSMagnificationGestureRecognizer obj)
        {
            var p = obj.LocationInView(CanvasView);
            var mouseLocation = NSEvent.CurrentMouseLocation;
            var pp = new SKPoint((float)(_canvasSize.Width * p.X / CanvasView.Bounds.Width),
                        (float)(_canvasSize.Height - (_canvasSize.Height * p.Y / CanvasView.Bounds.Height)));
            pp = _im.MapPoint(pp);
            var mouseCursorX = (float)p.X;
            var mouseCursorY = (float)(CanvasView.Frame.Height - p.Y);

            if (obj.State == NSGestureRecognizerState.Began)
            {
                StartPanZoom(new SKPoint(mouseCursorX, mouseCursorY));
            }
            else if (obj.State == NSGestureRecognizerState.Changed)
            {
                var x = obj.Magnification + 1;
                DoPanZoom(_startM, _startAnchorPt, _totalDistance, x);
            }
            else if (obj.State == NSGestureRecognizerState.Cancelled)
            {
                _isPanZoom = false;
                _m.TryInvert(out _im);
            }
            else if (obj.State == NSGestureRecognizerState.Ended)
            {
                _isPanZoom = false;
                _m.TryInvert(out _im);
            }
        }

        private void StartPanZoom(SKPoint anchorPt)
        {
            _startM = _m;
            _startAnchorPt = anchorPt;
            _totalDistance = SKPoint.Empty;
            _totalScale = 1;
            _m.ScaleX = (float)_totalScale;
            _m.ScaleY = (float)_totalScale;
            _isPanZoom = true;
        }

        private void DoPanZoom(SKMatrix startM, SKPoint anchorPt, SKPoint totalTranslation, double totalScale)
        {
            SKPoint canvasAnchorPt = new SKPoint(anchorPt.X * _screenScale, anchorPt.Y * _screenScale);
            SKPoint totalCanvasTranslation = new SKPoint(totalTranslation.X * _screenScale, totalTranslation.Y * _screenScale);
            SKMatrix canvasTranslation = SKMatrix.MakeTranslation((float)totalCanvasTranslation.X, (float)totalCanvasTranslation.Y);
            SKMatrix canvasScaling = SKMatrix.MakeScale((float)totalScale, (float)totalScale, (float)canvasAnchorPt.X, (float)canvasAnchorPt.Y);
            SKMatrix canvasCombined = SKMatrix.MakeIdentity();
            SKMatrix.Concat(ref canvasCombined, ref canvasTranslation, ref canvasScaling);
            SKMatrix.Concat(ref _m, ref canvasCombined, ref startM);
            CanvasView.SetNeedsDisplayInRect(CanvasView.Bounds);
        }
    }
}

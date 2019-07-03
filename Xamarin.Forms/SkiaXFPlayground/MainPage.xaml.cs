using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        public MainPage()
        {
            InitializeComponent();
        }

        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintGLSurfaceEventArgs e)
        {
            using (SKPaint p = new SKPaint { IsAntialias = true, StrokeWidth = 2, Typeface = SKTypeface.FromFamilyName("default"), TextSize = 100})
            {
                e.Surface.Canvas.DrawText("hello world :-)", 100, 100, p);
            }
        }

        void Handle_Tapped(object sender, System.EventArgs e)
        {

        }
    }
}

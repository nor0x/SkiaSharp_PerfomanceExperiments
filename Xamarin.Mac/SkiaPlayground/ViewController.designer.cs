// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using SkiaSharp.Views.Mac;
using System.CodeDom.Compiler;

namespace SkiaPlayground
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
        SKGLView CanvasView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CanvasView != null) {
				CanvasView.Dispose ();
				CanvasView = null;
			}
		}
	}
}

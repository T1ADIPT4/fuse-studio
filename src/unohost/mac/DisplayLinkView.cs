using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AppKit;
using CoreGraphics;
using CoreVideo;
using Foundation;
using OpenGL;

namespace Outracks.UnoHost.Mac
{
	static class CGLErrorExtensions
	{
		public static void ThrowIfFailed(this CGLErrorCode errorCode, string methodName)
		{
			if (errorCode != CGLErrorCode.NoError)
				throw new Exception(methodName + " failed: " + errorCode);
		}
	}

	abstract class DisplayLinkView : NSOpenGLView
	{
		static readonly ConcurrentDictionary<DisplayLinkView, DisplayLinkView> _views = new ConcurrentDictionary<DisplayLinkView, DisplayLinkView>();
		static CVDisplayLink _displayLink;

		readonly Action<Action> _invokeOnMainThread;


		public override bool IsOpaque
		{
			get { return true; }
		}

		protected DisplayLinkView(Action<Action> invokeOnMainThread, CGRect frame)
			: base(frame, CreatePixelFormat())
		{
			_invokeOnMainThread = invokeOnMainThread;
		}

		public static NSOpenGLPixelFormat CreatePixelFormat()
		{
			var attribs = new object[] {
				NSOpenGLPixelFormatAttribute.Accelerated,
				NSOpenGLPixelFormatAttribute.DoubleBuffer,
				NSOpenGLPixelFormatAttribute.AllowOfflineRenderers,
				NSOpenGLPixelFormatAttribute.ColorSize, 24,
				NSOpenGLPixelFormatAttribute.DepthSize, 16};

			var pixelFormat = new NSOpenGLPixelFormat(attribs);
			return pixelFormat;
		}

		public override void Update()
		{
			OpenGLContext.Update();
		}

		public override void DrawRect(CGRect dirtyRect)
		{
			Draw();
		}

		public override void PrepareOpenGL()
		{
			base.PrepareOpenGL();

			OpenGLContext.SwapInterval = true;
			_views.TryAdd(this, this);

			if (_displayLink != null)
				return;

			SetupDisplayLink();
		}

		void SetupDisplayLink()
		{
			_displayLink = new CVDisplayLink();

			// Set the renderer output callback function
			_displayLink.SetOutputCallback(DisplayLinkOutputCallback);

			// Set the display link for the current renderer
			var cglContext = OpenGLContext.CGLContext;
			var cglPixelFormat = PixelFormat.CGLPixelFormat;
			_displayLink.SetCurrentDisplay(cglContext, cglPixelFormat);
			_displayLink.Start();
		}

		void Draw()
		{
			try
			{
				_invokeOnMainThread(OnDraw);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Draw: " + e);
			}
		}

		protected abstract void OnDraw();

		bool _disposed = false;
		protected override void Dispose (bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				DisplayLinkView tmp;
				_views.TryRemove(this, out tmp);

				if(_views.IsEmpty)
					DeAllocate ();
			}
			base.Dispose (disposing);

			_disposed = true;
		}

		void DeAllocate()
		{
			if (_displayLink == null)
				return;

			_displayLink.Stop();
			_displayLink.Dispose();
			_displayLink = null;
		}

		// Private Callback function for CVDisplayLink
		static CVReturn DisplayLinkOutputCallback (CVDisplayLink displayLink, ref CVTimeStamp inNow, ref CVTimeStamp inOutputTime, CVOptionFlags flagsIn, ref CVOptionFlags flagsOut)
		{
			//CVReturn result = GetFrameForTime (inOutputTime);
			CVReturn result = CVReturn.Success;

			using (new NSAutoreleasePool ())
			{
				try
				{
					foreach(var view in _views)
						view.Value.Draw ();
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e);
					result = CVReturn.Error;
				}
			}

			return result;
		}
	}
}

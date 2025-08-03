using System.Reactive.Subjects;
using AppKit;
using OpenTK.Graphics.OpenGL;

namespace Outracks.UnoHost.Mac.UnoView.RenderTargets
{
	class IOSurfaceRenderTarget : IRenderTarget
	{
		Optional<IOSurfaceRenderTargetBuffer> _frontbuffer;
		Optional<IOSurfaceRenderTargetBuffer> _backbuffer;

		public readonly ISubject<IOSurfaceObject> SurfaceRendered = new Subject<IOSurfaceObject>();

		public int GetFramebufferHandle(Size<Pixels> size)
		{
			if (_backbuffer.HasValue && _backbuffer.Value.Size == size)
				return _backbuffer.Value.Handle;

			_backbuffer.Do(buffer => buffer.Dispose());
			_backbuffer = new IOSurfaceRenderTargetBuffer(size);

			return _backbuffer.Value.Handle;
		}

		public void Flush(NSOpenGLContext ctx)
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Flush(); // We flush to be sure the framebuffer texture has the data we want

			SurfaceRendered.OnNext(_backbuffer.Value.Surface);

			Swap();
		}

		void Swap()
		{
			var tmp = _frontbuffer;
			_frontbuffer = _backbuffer;
			_backbuffer = tmp;
		}
	}
}
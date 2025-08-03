using Uno;
using Uno.Platform;
using Uno.Platform.Internal;
using Math = System.Math;

namespace Outracks.UnoHost
{
	public interface IUnoCallbacks
	{
		void SetCursor(Cursor toCursor);
	}

	public class UnoWindow : WindowBackend
	{
		readonly IUnoCallbacks _callbacks;

		public UnoWindow(IUnoCallbacks callbacks)
		{
			_callbacks = callbacks;
			Size = new Size<Pixels>();
			Density = new Ratio<Pixels, Points>();
		}

		#region Size and density (read-only)
		public Size<Pixels> Size { get; private set; }

		public Ratio<Pixels, Points> Density { get; private set; }

		public void ChangeSize(Size<Pixels> size, Ratio<Pixels, Points> density)
		{
			Size = size;
			Density = density;
			Bootstrapper.OnWindowSizeChanged(this);
		}

		public override void SetClientSize(Int2 size)
		{
			// do nothing
		}

		public override Int2 GetClientSize()
		{
			return new Int2(Math.Max(1, (int)Size.Width), Math.Max(1, (int)Size.Height));
		}

		public override float GetDensity()
		{
			return (float)Density.Value;
		}
		#endregion

		#region Text Input (mutable)
		int _enableText = 0;
		public override void BeginTextInput(TextInputHint hint)
		{
			_enableText++;
		}

		public override void EndTextInput()
		{
			_enableText = Math.Max(0, _enableText - 1);
		}

		public override bool IsTextInputActive()
		{
			return _enableText > 0;
		}
		#endregion

		#region Cursor (mutable, one-way)
		PointerCursor _cursor;

		public override PointerCursor GetPointerCursor()
		{
			return _cursor;
		}

		public override void SetPointerCursor(PointerCursor p)
		{
			_cursor = p;
			_callbacks.SetCursor(ToCursor(p));
		}

		public static Cursor ToCursor(PointerCursor cursor)
		{
			switch (cursor)
			{
				case PointerCursor.None: return Cursor.None;
				case PointerCursor.Pointer: return Cursor.Pointer;
				case PointerCursor.IBeam: return Cursor.Text;
				default:
				// TODO: report warning?
				case PointerCursor.Default:
					return Cursor.Default;
			}
		}
		#endregion

		#region Title (constant)
		public override string GetTitle()
		{
			return "NDEv6";
		}

		public override void SetTitle(string title)
		{
			// do nothing
		}
		#endregion

		#region Fullscreen (constant)
		public override bool GetFullscreen()
		{
			return true;
		}

		public override void SetFullscreen(bool fullscreen)
		{
			// do nothing
		}
		#endregion

		#region OnscreenKeyboard (constant)
		public override bool HasOnscreenKeyboardSupport()
		{
			return false;
		}

		public override bool IsOnscreenKeyboardVisible()
		{
			return false;
		}
		#endregion

		#region Input state (TODO)
		InputState _globalInputStateHack;

		public void SetGlobalInputState(InputState state)
		{
			_globalInputStateHack = state;
		}

		public override bool GetKeyState(Key key)
		{
			var modifierKey = ToModifierKey((Key)key);
			return modifierKey != ModifierKeys.None && _globalInputStateHack.Modifiers.HasFlag(modifierKey);
		}

		public static ModifierKeys ToModifierKey(Key keys)
		{
			switch (keys)
			{
				case Key.ShiftKey:
					return ModifierKeys.Shift;
				case Key.AltKey:
					return ModifierKeys.Alt;
				case Key.ControlKey:
					return ModifierKeys.Control;
				case Key.MetaKey:
					return ModifierKeys.Meta;
				case Key.OSKey:
					return ModifierKeys.Windows | ModifierKeys.Command;
				default:
					return ModifierKeys.None;
			}
		}

		public override bool GetMouseButtonState(Uno.Platform.MouseButton button)
		{
			// TODO: Implement this.
			return false;
		}
		#endregion

		public override void Close()
		{
			// do nothing ?
		}
	}
}

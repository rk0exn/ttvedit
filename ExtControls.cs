using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace ttvedit;

public interface IThemeSwitchableWindow : IWin32Window
{
	public void SwitchTheme();
}

public abstract class ThemeSwitchableWindow : Window, IThemeSwitchableWindow
{
	[DllImport("dwmapi.dll")]
	private static extern nint DwmSetWindowAttribute(nint hwnd, int pv, ref int dw, int cb);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint hwnd, int id);

	[DllImport("user32.dll")]
	private static extern void SetWindowLong(nint hwnd, int id, int dwnewlong);

	public nint Handle => IsLoaded ? new WindowInteropHelper(this).Handle : 0;

	public void SwitchTheme()
	{
		SwitchTheme_();
	}

	public ThemeSwitchableWindow()
	{
		Loaded += (_, _) =>
		{
			SetWindowLong(Handle, -16, GetWindowLong(Handle, -16) & ~0x80000);
			var f = 2;
			DwmSetWindowAttribute(Handle, 38, ref f, sizeof(int));
		};
	}

	public bool IsChanging { get; private set; } = false;
	public bool IsLight { get; private set; } = true;

	protected ButtonEx CloseButton;

	protected ThemeManager Manager;

	protected virtual void AfterThemeChanged() { }

	private void SwitchTheme_()
	{
		var r0 = false;
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			if ((int)key.GetValue("AppsUseLightTheme", 0) == 1)
			{
				r0 = true;
			}
		}
		catch { }
		SwitchTheme(r0);
	}

	private Style GetStyle(string key) => (Style)Application.Current.FindResource(key);

	private void SwitchTheme(bool isl)
	{
		if (IsChanging || isl == IsLight) return;
		IsChanging = true;
		var i = isl ? 0 : 1;
		DwmSetWindowAttribute(Handle, 20, ref i, sizeof(int));
		Resources.Clear();
		ResourceDictionary rd = [];
		string y = isl ? "Light" : "Dark";
		Style[] basedStyles = [GetStyle($"{y}DefSys"), GetStyle($"{y}Text")];
		var cnt = 0;
		foreach (var style in basedStyles)
		{
			var s = new Style(style.TargetType, style);
			rd.Add(cnt == 1 ? "lt" : style.TargetType, s);
			cnt++;
		}
		CloseButton.Style = GetStyle($"{y}SysClose");
		Resources = rd;
		IsLight = isl;
		IsChanging = false;
		AfterThemeChanged();
	}
}

/// <summary>
/// テーマ管理などの拡張機能を提供します。
/// </summary>
public class ThemeManager(IThemeSwitchableWindow Wnd)
{
	private static readonly List<IThemeSwitchableWindow> registeredWindows = [];
	private static bool Initialized = false;

	/// <summary>
	/// マネージャーを初期化して使用できるようにします。
	/// </summary>
	/// <returns>既に初期化済みの場合は <see langword="true"/> 、初期化に成功すると <see langword="false"/>。</returns>
	public static bool InitializeManager()
	{
		if (!Initialized)
		{
			registeredWindows.Clear();
			Initialized = true;
			return true;
		}
		return false;
	}

	/// <summary>
	/// マネージャーのすべての値を破棄します。登録解除処理は行われないため、手動で操作する必要があります。登録解除は破棄前に行ってください。
	/// </summary>
	/// <returns>破棄に成功した場合は <see langword="true"/> 、既に破棄されている もしくは初期化されていない場合は <see langword="false"/>。</returns>
	public static bool DisposeManager()
	{
		if (Initialized)
		{
			registeredWindows.Clear();
			Initialized = false;
			return true;
		}
		return false;
	}

	/// <summary>
	/// 対象のウィンドウを登録します。
	/// </summary>
	/// <param name="window">登録する<see cref="ThemeSwitchableWindow"/>。</param>
	/// <returns>登録に成功した場合は <see langword="true"/> 、既に登録されている場合は <see langword="false"/>。</returns>
	public bool RegisterWindow(ThemeSwitchableWindow window)
	{
		if (!Initialized) return false;
		if (!ContainesWindow(window))
		{
			registeredWindows.Add(window);
			HookWindow(window);
			return true;
		}
		return false;
	}

	/// <summary>
	/// 登録されている対象のウィンドウを登録解除します。
	/// </summary>
	/// <param name="window">既に登録されている<see cref="ThemeSwitchableWindow"/>。</param>
	/// <returns>登録解除に成功した場合は <see langword="true"/> 、登録されていない場合は <see langword="false"/>。</returns>
	public bool UnregisterWindow(ThemeSwitchableWindow window)
	{
		if (!Initialized) return false;
		if (ContainesWindow(window))
		{
			UnhookWindow(window);
			registeredWindows.Remove(window);
			return true;
		}
		return false;
	}

	/// <summary>
	/// 登録されているウィンドウの中に含まれているか確認します。
	/// </summary>
	/// <param name="window">登録されているか確認する<see cref="ThemeSwitchableWindow"/></param>
	/// <returns>登録されている場合は <see langword="true"/> 、登録されていない場合は <see langword="false"/>。</returns>
	public static bool ContainesWindow(ThemeSwitchableWindow window)
	{
		if (!Initialized) return false;
		return registeredWindows.Contains(window);
	}

	private static nint GetWindowHandle(ThemeSwitchableWindow wnd) => wnd.Handle;

	private void HookWindow(ThemeSwitchableWindow wnd)
	{
		if (wnd.IsLoaded)
		{
			HwndSource.FromHwnd(GetWindowHandle(wnd)).AddHook(HookableWndProc);
		}
		else
		{
			wnd.Loaded += (_, _) => RegisterEvent(wnd);
		}
	}

	private void RegisterEvent(ThemeSwitchableWindow wnd)
	{
		HwndSource.FromHwnd(GetWindowHandle(wnd)).AddHook(HookableWndProc);
		wnd.Loaded -= (_, _) => RegisterEvent(wnd);
	}

	private void UnhookWindow(ThemeSwitchableWindow wnd) => HwndSource.FromHwnd(GetWindowHandle(wnd)).RemoveHook(HookableWndProc);

	private nint HookableWndProc(nint hwnd, int msg, nint wp, nint lp, ref bool handled)
	{
		if (msg == 0x1a)
		{
			Wnd.SwitchTheme();
		}
		return 0;
	}
}

/// <summary>
/// タイトルバー向けに改良されたネイティブ当たり判定の設定可能な拡張された<see cref="Button"/>です。
/// </summary>
public class ButtonEx : Button
{
	/// <summary>
	/// 継承可能なマウスイベントのトラッキング関数です。 <see cref="TRACKMOUSEEVENT"/> が必須です。
	/// </summary>
	/// <param name="lpEventTrack">パラメーター</param>
	/// <returns>C#向けにマーシャリングされた結果</returns>
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	protected static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

	[StructLayout(LayoutKind.Sequential)]
	protected struct TRACKMOUSEEVENT
	{
		public uint cbSize;
		public uint dwFlags;
		public IntPtr hwndTrack;
		public uint dwHoverTime;
	}

	protected HwndSource _parentHwndSource;

	protected static ButtonEx ReallyFocusedButton = null;

	static ButtonEx()
	{
		DefaultStyleKeyProperty.OverrideMetadata(typeof(ButtonEx), new FrameworkPropertyMetadata(typeof(ButtonEx)));
	}

	/// <summary>
	/// 初期化と内部設定を行います。
	/// </summary>
	public ButtonEx()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	/// <summary>
	/// 初期化する際にコンテンツも設定します。
	/// </summary>
	/// <param name="content"></param>
	public ButtonEx(object content) : this()
	{
		Content = content;
	}

	~ButtonEx()
	{
		if (ReallyFocusedButton == this) ReallyFocusedButton = null;
	}

	public static readonly DependencyProperty IsMouseReallyOverProperty = DependencyProperty.Register(nameof(IsMouseReallyOver), typeof(bool), typeof(ButtonEx), new(false));

	public bool IsMouseReallyOver
	{
		get => (bool)GetValue(IsMouseReallyOverProperty);
		private set => SetValue(IsMouseReallyOverProperty, value);
	}

	protected void UpdateIsMouseReallyOver() => IsMouseReallyOver = IsMouseOver || IsNCMouseOver;

	public static readonly DependencyProperty IsReallyPressedProperty = DependencyProperty.Register(nameof(IsReallyPressed), typeof(bool), typeof(ButtonEx), new(false));

	public bool IsReallyPressed
	{
		get => (bool)GetValue(IsReallyPressedProperty);
		private set => SetValue(IsReallyPressedProperty, value);
	}

	protected void UpdateIsReallyPressed() => IsReallyPressed = IsPressed || IsNCPressed;

	internal uint? HitTestCode { get; set; }

	protected bool _isNCMouseOver;

	protected bool IsNCMouseOver
	{
		get => _isNCMouseOver;
		set
		{
			if (_isNCMouseOver != value)
			{
				if ((!value || ReallyFocusedButton != null && ReallyFocusedButton != this) && (value || ReallyFocusedButton != this))
				{
					if (ReallyFocusedButton != null) ReallyFocusedButton.IsNCMouseOver = false;
				}
				ReallyFocusedButton = value ? this : null;
				_isNCMouseOver = value;
				OnIsNCMouseOverChanged();
			}
		}
	}

	protected void OnIsNCMouseOverChanged()
	{
		UpdateIsMouseReallyOver();
		if (!IsNCMouseOver)
		{
			IsNCPressed = false;
		}
	}

	protected bool _isNCPressed;

	protected bool IsNCPressed
	{
		get => _isNCPressed;
		set
		{
			if (_isNCPressed != value)
			{
				_isNCPressed = value;
				OnIsNCPressedChanged();
			}
		}
	}

	protected void OnIsNCPressedChanged() => UpdateIsReallyPressed();

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.Property == IsMouseOverProperty)
		{
			UpdateIsMouseReallyOver();
		}
		else if (e.Property == IsPressedProperty)
		{
			UpdateIsReallyPressed();
		}
	}

	protected void DoClick()
	{
		IsNCMouseOver = false;
		OnClick();
	}

	protected void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (HitTestCode.HasValue)
		{
			_parentHwndSource = (HwndSource)PresentationSource.FromVisual(this);
			System.Diagnostics.Debug.Assert(_parentHwndSource != null);
			_parentHwndSource?.AddHook(ButtonExFilterMessage);
		}
	}

	protected void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_parentHwndSource != null)
		{
			StopTrackingMouseLeave();
			_parentHwndSource.RemoveHook(ButtonExFilterMessage);
			_parentHwndSource = null;
		}
	}

	protected void StartTrackingMouseLeave()
	{
		if (_parentHwndSource == null) return;

		var tme = new TRACKMOUSEEVENT
		{
			cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
			dwFlags = 0x12,
			hwndTrack = _parentHwndSource.Handle,
			dwHoverTime = 0
		};
		TrackMouseEvent(ref tme);
	}

	protected void StopTrackingMouseLeave()
	{
		if (_parentHwndSource == null) return;

		var tme = new TRACKMOUSEEVENT
		{
			cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
			dwFlags = 0x80000010,
			hwndTrack = _parentHwndSource.Handle,
			dwHoverTime = 0
		};
		TrackMouseEvent(ref tme);
	}

	protected nint ButtonExFilterMessage(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		switch (msg)
		{
			case 0x84:
				if (IsMousePositionWithin(lParam))
				{
					IsNCMouseOver = true;
					StartTrackingMouseLeave();
					handled = true;
					return (nint)HitTestCode;
				}
				else
				{
					IsNCMouseOver = false;
				}
				break;
			case 0xa0:
			case 0x200:
				if (IsMousePositionWithin(lParam))
				{
					IsNCMouseOver = true;
					handled = true;
				}
				else
				{
					IsNCMouseOver = false;
				}
				break;
			case 0xa1:
				if (IsNCMouseOver)
				{
					IsNCPressed = true;
					handled = true;
				}
				break;
			case 0xa2:
				if (IsNCPressed)
				{
					IsNCPressed = false;
					if (IsMousePositionWithin(lParam)) OnClick();
					handled = true;
				}
				break;
			case 0x2a2:
				IsNCMouseOver = false;
				break;
		}
		return 0;
	}

	protected bool IsMousePositionWithin(nint lParam) => new Rect(new(), RenderSize).Contains(PointFromScreen(new(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam))));
}

public class GridEx : Grid
{
	public int HitTestCode { get; set; } = 0;

	private HwndSource _source;
	private Window _window;

	public GridEx()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_source = (HwndSource)PresentationSource.FromVisual(this);
		_window = Window.GetWindow(this);
		_source?.AddHook(WndProc);
		MouseLeftButtonDown += OnMouseLeftButtonDown;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_source != null)
		{
			_source.RemoveHook(WndProc);
			_source = null;
		}

		MouseLeftButtonDown -= OnMouseLeftButtonDown;
	}

	private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (HitTestCode == 2 && e.ClickCount == 1)
		{
			try
			{
				_window?.DragMove();
			}
			catch { }
		}
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		switch (msg)
		{
			case 0x84:
				if (IsMousePositionWithin(lParam))
				{
					handled = true;
					return HitTestCode;
				}
				break;
			case 0xA3:
				if (HitTestCode == 2 && IsMousePositionWithin(lParam))
				{
					if (_window != null)
					{
						_window.WindowState = _window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
					}
					handled = true;
				}
				break;
		}
		return IntPtr.Zero;
	}

	private bool IsMousePositionWithin(nint lParam)
	{
		var mousePoint = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
		Point localPoint = PointFromScreen(mousePoint);
		var rs = RenderSize;
		rs.Height -= 6;
		rs.Width -= 12;
		return new Rect(new Point(6, 6), rs).Contains(localPoint);
	}
}

public static class Utility
{
	public static int GET_X_LPARAM(nint lParam) => LOWORD((int)lParam);
	public static int GET_Y_LPARAM(nint lParam) => HIWORD((int)lParam);
	public static int HIWORD(int i) => (short)(i >> 16);
	public static int LOWORD(int i) => (short)(i & 0xFFFF);
}

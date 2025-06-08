using System.Windows;
using System.Windows.Interop;

namespace ttvedit;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : ThemeSwitchableWindow
{
	internal static MainWindow Current => _current;

	private static MainWindow _current;

	private bool diagShowing = false;

	public MainWindow()
	{
		_current = this;
		InitializeComponent();
		Manager = new(this);
		minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
		maximizeButton.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		closeButton.Click += (_, _) => Close();
		CloseButton = closeButton;
		Loaded += (_, _) =>
		{
			SizeChanged += (_, _) =>
			{
				if (WindowState == WindowState.Maximized)
				{
					maximizeButton.Content = "\uE923";
					rootGrid.Margin = new(7);
				}
				else
				{
					maximizeButton.Content = "\uE922";
					rootGrid.Margin = new(0);
				}
			};
			Manager.RegisterWindow(this);
			SwitchTheme();
			HwndSource.FromHwnd(Handle).AddHook(WndProc);
		};
	}

	private nint WndProc(nint hwnd, int msg, nint wp, nint lp, ref bool handled)
	{
		if ((msg == 0x112 && (wp & 0xffff) == 0xf060) || msg == 0x10)
		{
			if (diagShowing)
			{
				handled = true;
			}
			else
			{
				diagShowing = true;
				handled = MessageBox.Show(this, "保存されていない変更がある可能性があります。本当に終了してもよろしいですか？\nこれは保存したかどうかにかかわらず表示されます。", "終了確認", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes;
				if (handled)
				{
					Manager.UnregisterWindow(this);
				}
				diagShowing = false;
			}
		}
		return 0;
	}
}

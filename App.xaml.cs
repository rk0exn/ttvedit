using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ttvedit;

/// <summary>
/// App.xaml の相互作用ロジック
/// </summary>
public partial class App : Application
{
	[DllImport("uxtheme.dll", EntryPoint = "#135")]
	private static extern void AllowDarkModeForApp(int iEnable);

	private void ExtendSysMenuTheme(AllowDarkType type)
	{
		if ((int)type < 4 && (int)type >= 0)
		{
			AllowDarkModeForApp((int)type);
		}
		else
		{
			throw new ArgumentException(nameof(type));
		}
	}

	private enum AllowDarkType
	{
		None = 0,
		SystemSelect = 1,
		Dark = 2,
		Light = 3,
	}

	public App()
	{
		InitializeComponent();
		ExtendSysMenuTheme(AllowDarkType.SystemSelect);
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		ThemeManager.InitializeManager();
		base.OnStartup(e);
	}

	protected override void OnExit(ExitEventArgs e)
	{
		ThemeManager.DisposeManager();
		base.OnExit(e);
	}
}

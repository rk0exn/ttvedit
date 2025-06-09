using System.Collections.Generic;
using System.Windows;

namespace ttvedit;

/// <summary>
/// AddTrainDialog.xaml の相互作用ロジック
/// </summary>
public partial class AddTrainDialog : ThemeSwitchableWindow
{
	public AddTrainDialog()
	{
		Owner = MainWindow.Current;
		InitializeComponent();
		DataContext = this;
		Manager = new(this);
		closeButton.Click += (_, _) => Close();
		CloseButton = closeButton;
		Loaded += (_, _) =>
		{
			Manager.RegisterWindow(this);
			SwitchTheme();
		};
		Closing += (_, _) =>
		{
			Manager.UnregisterWindow(this);
		};
	}

	public ExtTrainInfo CreatedItem { get; private set; }
	public DayTypeMode TargetDay { get; private set; }
	public List<string> PatternNames { get; } = [];

	public AddTrainDialog(List<string> patterns) : this()
	{
		PatternNames = patterns;
		DataContext = this;
	}

	private void Add_Click(object sender, RoutedEventArgs e)
	{
		TargetDay = DayTypeBox.SelectedIndex == 0 ? DayTypeMode.Weekday : DayTypeMode.Holiday;
		if (string.IsNullOrWhiteSpace(TimeBox.Text) || string.IsNullOrEmpty((string)PatternBox.SelectedItem))
		{
			MessageBox.Show("選択が無効な項目があります。", "項目エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		CreatedItem = new ExtTrainInfo
		{
			Time = TimeBox.Text,
			PatternName = PatternBox.SelectedItem as string
		};
		DialogResult = true;
		Close();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}

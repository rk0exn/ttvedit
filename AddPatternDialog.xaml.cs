using System.Collections.Generic;
using System.Windows;

namespace ttvedit;

/// <summary>
/// AddPatternDialog.xaml の相互作用ロジック
/// </summary>
public partial class AddPatternDialog : ThemeSwitchableWindow
{
	public AddPatternDialog()
	{
		Owner = MainWindow.Current;
		InitializeComponent();
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
	public List<string> PatternNames { get; }

	public KeyValuePair<string, TrainInfoWithoutTime>? CreatedPattern { get; private set; }
	private readonly HashSet<string> _existingKeys;

	public AddPatternDialog(IEnumerable<string> existingKeys, IEnumerable<string> typeOptions) : this()
	{
		_existingKeys = [.. existingKeys];
		TrainTypeBox.ItemsSource = typeOptions;
	}

	private void Add_Click(object sender, RoutedEventArgs e)
	{
		if (_existingKeys.Contains(KeyBox.Text))
		{
			MessageBox.Show("すでにこのパターンは存在しています。別のパターン名を使用してください。", "パターンエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		if (string.IsNullOrWhiteSpace(KeyBox.Text) || string.IsNullOrWhiteSpace(NextStationBox.Text) || string.IsNullOrWhiteSpace(DirectionBox.Text) || string.IsNullOrEmpty((string)TrainTypeBox.SelectedItem))
		{
			MessageBox.Show("パターンの内容が無効です。", "パターンエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		CreatedPattern = new KeyValuePair<string, TrainInfoWithoutTime>(KeyBox.Text,
			new TrainInfoWithoutTime
			{
				Direction = DirectionBox.Text,
				NextStation = NextStationBox.Text,
				TrainType = TrainTypeBox.SelectedItem as string,
				Upside = UpsideBox.IsChecked == true
			});
		DialogResult = true;
		Close();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

	private void KeyBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
	{
		var exists = _existingKeys.Contains(KeyBox.Text);
		KeyError.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
	}
}

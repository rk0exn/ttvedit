using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ttvedit
{
	/// <summary>
	/// AddColorDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class AddColorDialog : ThemeSwitchableWindow
	{
		public KeyValuePair<string, string>? CreatedColor { get; private set; }

		public AddColorDialog()
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

		public AddColorDialog(string existingKey, string existingValue) : this()
		{
			KeyBox.Text = existingKey;
			KeyBox.IsEnabled = false;
			ColorBox.Text = existingValue;
		}

		private void Add_Click(object sender, RoutedEventArgs e)
		{
			string key = KeyBox.Text.Trim();
			string color = ColorBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(key) || !Regex.IsMatch(color, @"^[0-9a-fA-F]{6}$"))
			{
				MessageBox.Show("有効な種別名と6桁の16進数カラーコードを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			CreatedColor = new(key, "#" + color.ToUpperInvariant());
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private void ColorBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !Regex.IsMatch(e.Text, @"^[0-9a-fA-F]$");
		}

		private void ColorBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == ApplicationCommands.Paste)
			{
				e.Handled = true;
			}
		}
	}
}

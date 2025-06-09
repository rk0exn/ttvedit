using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

		private void ColorBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			colorPreview.Background = Regex.IsMatch(ColorBox.Text, @"^[0-9a-fA-F]{6}$") ? new SolidColorBrush(new ColorFormatProvider($"#{ColorBox.Text}").Color) : Brushes.Transparent;
		}

		private void ColorBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == ApplicationCommands.Paste)
			{
				e.Handled = true;
				MessageBox.Show("貼り付けはできません。", "コマンドエラー", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	public sealed class ColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string colorString = value as string;
			if (string.IsNullOrWhiteSpace(colorString)) return Brushes.Transparent;

			try
			{
				var color = new ColorFormatProvider(colorString).ToColor();
				return new SolidColorBrush(color);
			}
			catch
			{
				return Brushes.Transparent;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	public static class Extension
	{
		public static Color ToColor(this ColorFormatProvider provider) => provider.Color;
	}

	public sealed class ColorFormatProvider : IFormatProvider, ICustomFormatter
	{
		public Color Color => _color;

		private Color _color;

		public ColorFormatProvider(string colorString) => _color = ParseColor(colorString);

		private Color ParseColor(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return Colors.Transparent;
			input = input.Trim();
			var prop = typeof(Colors).GetProperty(input, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (prop != null && prop.PropertyType == typeof(Color)) return (Color)prop.GetValue(null);
			if (input.StartsWith("#"))
			{
				var hex = input.Substring(1);
				try
				{
					if (hex.Length == 6) return Color.FromRgb(Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
				}
				catch { }
			}
			return Colors.Transparent;
		}

		public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
		public string Format(string format, object arg, IFormatProvider formatProvider) => Color.ToString();
	}
}

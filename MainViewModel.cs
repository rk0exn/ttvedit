using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace ttvedit;

public class TimetableDataDto
{
	[JsonPropertyName("station_name")]
	public string StationName { get; set; }

	[JsonPropertyName("weekdays_timetable")]
	public List<ExtTrainInfoDto> WeekDaysTimetable { get; set; }

	[JsonPropertyName("holidays_timetable")]
	public List<ExtTrainInfoDto> HolidaysTimetable { get; set; }

	[JsonPropertyName("update_time")]
	public string UpdateTime { get; set; }

	[JsonPropertyName("type_colors")]
	public Dictionary<string, string> TypeColors { get; set; }

	[JsonPropertyName("patterns")]
	public Dictionary<string, TrainInfoWithoutTimeDto> Patterns { get; set; }

	[JsonPropertyName("comment")]
	public string Comment { get; set; }
}

public class TrainInfoWithoutTimeDto
{
	[JsonPropertyName("direction")]
	public string Direction { get; set; }

	[JsonPropertyName("upside")]
	public bool? Upside { get; set; }

	[JsonPropertyName("traintype")]
	public string TrainType { get; set; }

	[JsonPropertyName("next_station")]
	public string NextStation { get; set; }
}

public class ExtTrainInfoDto
{
	[JsonPropertyName("time")]
	public string Time { get; set; }

	[JsonPropertyName("pattern")]
	public string PatternName { get; set; }

	[JsonPropertyName("direction")]
	public string Direction { get; set; }

	[JsonPropertyName("upside")]
	public bool? Upside { get; set; }

	[JsonPropertyName("traintype")]
	public string TrainType { get; set; }

	[JsonPropertyName("next_station")]
	public string NextStation { get; set; }
}

public class TrainInfoWithoutTime : DependencyObject
{
	[JsonIgnore]
	public new DependencyObjectType DependencyObjectType => base.DependencyObjectType;

	[JsonPropertyName("direction")]
	public string Direction { get; set; }

	[JsonPropertyName("upside")]
	public bool? Upside { get; set; }

	[JsonPropertyName("traintype")]
	public string TrainType { get; set; }

	[JsonPropertyName("next_station")]
	public string NextStation { get; set; }

	public string ImageColor { get; set; }

	public string UpsideText => Upside == null && IsTop ? "方面" : Upside == true ? "上り" : Upside == false ? "下り" : "";

	public bool IsTop { get; set; }
}

public class TrainInfo : TrainInfoWithoutTime
{
	[JsonPropertyName("time")]
	public string Time { get; set; }
}

public sealed class ExtTrainInfo : TrainInfo
{
	[JsonPropertyName("pattern")]
	public string PatternName { get; set; }

	public bool UseV2 => !string.IsNullOrWhiteSpace(PatternName);
}

public enum DayTypeMode
{
	Weekday, Holiday
}

public enum DirectionFilterMode
{
	All, Upside, Downside
}

public sealed class TimetableData
{
	[JsonPropertyName("station_name")]
	public string StationName { get; set; }

	[JsonPropertyName("weekdays_timetable")]
	public List<ExtTrainInfo> WeekDaysTimetable { get; set; }

	[JsonPropertyName("holidays_timetable")]
	public List<ExtTrainInfo> HolidaysTimetable { get; set; }

	[JsonPropertyName("update_time")]
	public string UpdateTime { get; set; }

	[JsonPropertyName("type_colors")]
	public Dictionary<string, string> TypeColors { get; set; }

	[JsonPropertyName("patterns")]
	public Dictionary<string, TrainInfoWithoutTime> Patterns { get; set; }

	[JsonPropertyName("comment")]
	public string Comment { get; set; }

	private bool _usingPattern = false;
	public bool UsingPattern => _usingPattern;

	public List<TrainInfo> SafeWeekdaysTimetable
	{
		get
		{
			if (Patterns == null) return ToTrainInfoList(WeekDaysTimetable?.ToList());
			List<TrainInfo> result = [];
			var data = WeekDaysTimetable.ToList() ?? null;
			if (data == null) return [];
			for (var i = 0; i < data.Count; i++)
			{
				if (!data[i].UseV2)
				{
					result.Add(data[i]);
					continue;
				}
				if (Patterns.TryGetValue(data[i].PatternName, out var ttime))
				{
					result.Add(ToTrainInfo(data[i], ttime));
				}
				else
				{
					return [];
				}
			}
			if (!UsingPattern) _usingPattern = true;
			return result;
		}
	}
	public List<TrainInfo> SafeHolidaysTimetable
	{
		get
		{
			if (Patterns == null) return ToTrainInfoList(HolidaysTimetable?.ToList());
			List<TrainInfo> result = [];
			var data = HolidaysTimetable.ToList() ?? null;
			if (data == null) return [];
			for (var i = 0; i < data.Count; i++)
			{
				if (!data[i].UseV2)
				{
					result.Add(data[i]);
					continue;
				}
				if (Patterns.TryGetValue(data[i].PatternName, out var ttime))
				{
					result.Add(ToTrainInfo(data[i], ttime));
				}
				else
				{
					return [];
				}
			}
			if (!UsingPattern) _usingPattern = true;
			return result;
		}
	}

	private List<TrainInfo> ToTrainInfoList(List<ExtTrainInfo> exInfos)
	{
		List<TrainInfo> r = [];
		foreach (var exInfo in exInfos)
		{
			r.Add(exInfo);
		}
		return r;
	}

	private TrainInfo ToTrainInfo(ExtTrainInfo exInfo, TrainInfoWithoutTime patternData)
	{
		TrainInfo result = new()
		{
			Direction = patternData.Direction,
			IsTop = patternData.IsTop,
			NextStation = patternData.NextStation,
			Time = exInfo.Time,
			TrainType = patternData.TrainType,
			Upside = patternData.Upside
		};
		if (TypeColors != null && TypeColors.TryGetValue(result.TrainType, out var color))
		{
			result.ImageColor = color;
		}
		else
		{
			result.ImageColor = "Transparent";
		}
		return result;
	}
}

public class RelayCommand(Action<object> execute, Func<object, bool> canExecute = null) : ICommand
{
	private readonly Action<object> _execute = execute;
	private readonly Func<object, bool> _canExecute = canExecute;
	public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
	public void Execute(object parameter) => _execute(parameter);
	public event EventHandler CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}
}

public class MainViewModel : INotifyPropertyChanged
{
	private string _loadedFilePath = null;

	public ICommand AddColorCommand => new RelayCommand(_ =>
	{
		var dialog = new AddColorDialog();
		if (dialog.ShowDialog() == true && dialog.CreatedColor.HasValue)
		{
			if (!TypeColorList.Contains(dialog.CreatedColor.Value))
			{
				TypeColorList.Add(dialog.CreatedColor.Value);
			}
			else
			{
				MessageBox.Show("既にこの色の定義は含まれているため、追加できませんでした。\n他の色または種別名をお試しください。", "色の追加エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	});

	public ICommand EditColorCommand => new RelayCommand(item =>
	{
		if (item is KeyValuePair<string, string> kv)
		{
			var dialog = new AddColorDialog(kv.Key, kv.Value);
			if (dialog.ShowDialog() == true && dialog.CreatedColor.HasValue)
			{
				var index = TypeColorList.IndexOf(kv);
				if (index >= 0)
				{
					TypeColorList[index] = dialog.CreatedColor.Value;
				}
			}
		}
	});

	public ICommand SaveJsonCommand => new RelayCommand(_ =>
	{
		if (_loadedFilePath == null)
		{
			MessageBox.Show("ファイルから読み込まれたデータのみ上書き保存できます。\n名前を付けて保存してください。", "保存できませんでした", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		if (SaveJsonToFile(_loadedFilePath))
		{
			MessageBox.Show("保存が完了しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		else
		{
			MessageBox.Show("権限または文字列の都合により、保存に失敗しました。\n大変恐れ入りますが、今一度以下の内容をご確認ください。\n・保存したい場所が書き込み可能であるか\n・データに問題がないか", "保存エラー", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	});

	public ICommand SaveAsJsonCommand => new RelayCommand(_ =>
	{
		var dialog = new SaveFileDialog
		{
			Filter = "JSONファイル (*.json)|*.json",
			Title = "保存先ファイルを指定してください",
			ValidateNames = true
		};
		if (dialog.ShowDialog() == true)
		{
			if (SaveJsonToFile(dialog.FileName))
			{
				_loadedFilePath = dialog.FileName;
				MessageBox.Show("保存が完了しました。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("権限または文字列の都合により、保存に失敗しました。\n大変恐れ入りますが、今一度以下の内容をご確認ください。\n・保存したい場所が書き込み可能であるか\n・データに問題がないか", "保存エラー", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	});

	public ICommand CloseJsonCommand => new RelayCommand(_ =>
	{
		if (_loadedFilePath == null)
		{
			MessageBox.Show("ファイルから読み込まれたデータではないため、この機能は使用できません。", "閉じれませんでした", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		LoadSampleData();
	});

	public ICommand DiscardJsonCommand => new RelayCommand(_ =>
	{
		if (MessageBox.Show("本当に破棄してもよろしいですか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
		LoadSampleData();
	});

	private bool SaveJsonToFile(string path)
	{
		try
		{
			var dto = new TimetableDataDto
			{
				StationName = _data.StationName,
				UpdateTime = _data.UpdateTime,
				Comment = _data.Comment,
				TypeColors = TypeColorList.ToList().ToDictionary(pair => pair.Key, pair => pair.Value),
				Patterns = PatternList.ToDictionary(p => p.Key, p => new TrainInfoWithoutTimeDto
				{
					Direction = p.Value.Direction,
					Upside = p.Value.Upside,
					TrainType = p.Value.TrainType,
					NextStation = p.Value.NextStation
				}),
				WeekDaysTimetable = [.. Weekdays.Select(w => new ExtTrainInfoDto
				{
					Time = w.Time,
					PatternName = w.PatternName,
					Direction = w.Direction,
					Upside = w.Upside,
					TrainType = w.TrainType,
					NextStation = w.NextStation
				})],
				HolidaysTimetable = [.. Holidays.Select(h => new ExtTrainInfoDto
				{
					Time = h.Time,
					PatternName = h.PatternName,
					Direction = h.Direction,
					Upside = h.Upside,
					TrainType = h.TrainType,
					NextStation = h.NextStation
				})]
			};
			JsonSerializerOptions opt = new()
			{
				WriteIndented = false,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
				IgnoreReadOnlyProperties = true,
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			};
			var json = JsonSerializer.Serialize(dto, opt);
			File.WriteAllText(path, json);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public ICommand DeleteWeekdayCommand => new RelayCommand(item =>
	{
		if (item is ExtTrainInfo info) Weekdays.Remove(info);
	});

	public ICommand EditWeekdayCommand => new RelayCommand(item =>
	{
		if (item is ExtTrainInfo info && info.UseV2)
		{
			AddTrainDialog dialog = new([.. PatternList.Select(p => p.Key)]);
			dialog.DayTypeBox.SelectedIndex = 0;
			dialog.TimeBox.Text = info.Time;
			dialog.PatternBox.SelectedItem = info.PatternName;

			if (dialog.ShowDialog() == true && dialog.CreatedItem is ExtTrainInfo updated)
			{
				var index = Weekdays.IndexOf(info);
				if (index >= 0) Weekdays[index] = updated;
			}
		}
	}, item => item is ExtTrainInfo info && info.UseV2);

	public ICommand AddWeekdayCommand => new RelayCommand(_ =>
	{
		AddTrainDialog dialog = new([.. PatternList.Select(p => p.Key)]);
		if (dialog.ShowDialog() == true)
		{
			var newItem = dialog.CreatedItem;
			if (dialog.TargetDay == DayTypeMode.Weekday) Weekdays.Add(newItem);
			else Holidays.Add(newItem);
		}
	});

	public ICommand DeleteHolidayCommand => new RelayCommand(item =>
	{
		if (item is ExtTrainInfo info) Holidays.Remove(info);
	});

	public ICommand EditHolidayCommand => new RelayCommand(item =>
	{
		if (item is ExtTrainInfo info && info.UseV2)
		{
			AddTrainDialog dialog = new([.. PatternList.Select(p => p.Key)]);
			dialog.DayTypeBox.SelectedIndex = 1;
			dialog.TimeBox.Text = info.Time;
			dialog.PatternBox.SelectedItem = info.PatternName;

			if (dialog.ShowDialog() == true && dialog.CreatedItem is ExtTrainInfo updated)
			{
				var index = Holidays.IndexOf(info);
				if (index >= 0)
					Holidays[index] = updated;
			}
		}
	}, item => item is ExtTrainInfo info && info.UseV2);

	public ICommand AddHolidayCommand => new RelayCommand(_ =>
	{
		AddTrainDialog dialog = new([.. PatternList.Select(p => p.Key)]);
		dialog.DayTypeBox.SelectedIndex = 1;
		if (dialog.ShowDialog() == true)
		{
			var newItem = dialog.CreatedItem;
			if (dialog.TargetDay == DayTypeMode.Holiday) Holidays.Add(newItem);
			else Weekdays.Add(newItem);
		}
	});

	public ICommand DeletePatternCommand => new RelayCommand(item =>
	{
		if (item is KeyValuePair<string, TrainInfoWithoutTime> kv) PatternList.Remove(kv);
	}); public ICommand EditPatternCommand => new RelayCommand(item =>
	{
		if (item is KeyValuePair<string, TrainInfoWithoutTime> kv)
		{
			var dialog = new AddPatternDialog(PatternList.Select(p => p.Key), TypeColorList.Select(t => t.Key))
			{
				Owner = Application.Current.MainWindow
			};

			dialog.KeyBox.Text = kv.Key;
			dialog.KeyBox.IsEnabled = false;
			dialog.DirectionBox.Text = kv.Value.Direction;
			dialog.NextStationBox.Text = kv.Value.NextStation;
			dialog.TrainTypeBox.SelectedItem = kv.Value.TrainType;
			dialog.UpsideBox.IsChecked = kv.Value.Upside;

			if (dialog.ShowDialog() == true && dialog.CreatedPattern.HasValue)
			{
				var index = PatternList.IndexOf(kv);
				if (index >= 0)
				{
					PatternList[index] = dialog.CreatedPattern.Value;
				}
			}
		}
	});

	public ICommand AddPatternCommand => new RelayCommand(_ =>
	{
		AddPatternDialog dialog = new(PatternList.Select(p => p.Key), TypeColorList.Select(t => t.Key));
		if (dialog.ShowDialog() == true && dialog.CreatedPattern.HasValue) PatternList.Add(dialog.CreatedPattern.Value);
	});

	public ICommand DeleteColorCommand => new RelayCommand(item =>
	{
		if (item is KeyValuePair<string, string> kv) TypeColorList.Remove(kv);
	});

	public ICommand LoadJsonCommand => new RelayCommand(_ => LoadJsonFromFile());

	public event PropertyChangedEventHandler PropertyChanged;

	private TimetableData _data = new();

	public ObservableCollection<ExtTrainInfo> Weekdays { get; set; } = [];
	public ObservableCollection<ExtTrainInfo> Holidays { get; set; } = [];

	public ObservableCollection<KeyValuePair<string, TrainInfoWithoutTime>> PatternList { get; set; } = [];
	public ObservableCollection<KeyValuePair<string, string>> TypeColorList { get; set; } = [];

	public string StationName
	{
		get => _data.StationName;
		set { _data.StationName = value; OnPropertyChanged(nameof(StationName)); }
	}

	public string UpdateTime
	{
		get => _data.UpdateTime;
		set { _data.UpdateTime = value; OnPropertyChanged(nameof(UpdateTime)); }
	}

	public string Comment
	{
		get => _data.Comment;
		set { _data.Comment = value; OnPropertyChanged(nameof(Comment)); }
	}

	public void ValidateFormatCompatibility()
	{
		bool hasV1 = Weekdays.Any(w => !w.UseV2) || Holidays.Any(h => !h.UseV2);
		bool hasV2 = Weekdays.Any(w => w.UseV2) || Holidays.Any(h => h.UseV2);

		if (hasV1 && hasV2)
		{
			MessageBox.Show("v1形式とv2形式が混在しています。\nこのエディターではv1形式の項目の表示は一部サポートされていません。", "部分的な形式非対応の警告", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		else if (hasV1 && !hasV2)
		{
			MessageBox.Show("完全なv1形式のファイルはこのエディターではサポートされていません。\n読み込みを取り消します。", "完全な形式非対応のエラー", MessageBoxButton.OK, MessageBoxImage.Hand);
			_loadedFilePath = null;
			Weekdays.Clear();
			Holidays.Clear();
			PatternList.Clear();
			TypeColorList.Clear();
			LoadSampleData();
			OnPropertyChanged(nameof(Weekdays));
			OnPropertyChanged(nameof(Holidays));
			OnPropertyChanged(nameof(PatternList));
			OnPropertyChanged(nameof(TypeColorList));
			OnPropertyChanged(nameof(StationName));
			OnPropertyChanged(nameof(UpdateTime));
			OnPropertyChanged(nameof(Comment));
		}
	}

	public void LoadSampleData()
	{
		_loadedFilePath = null;
		Weekdays.Clear();
		Holidays.Clear();
		PatternList.Clear();
		TypeColorList.Clear();
		OnPropertyChanged(nameof(Weekdays));
		OnPropertyChanged(nameof(Holidays));
		OnPropertyChanged(nameof(PatternList));
		OnPropertyChanged(nameof(TypeColorList));
		OnPropertyChanged(nameof(StationName));
		OnPropertyChanged(nameof(UpdateTime));
		OnPropertyChanged(nameof(Comment));
		_data = new()
		{
			StationName = "デサインモード - 新宿駅",
			UpdateTime = "2025/05/23 00:48",
			TypeColors = new()
			{
				{ "普通", "#0080FF" },
				{ "快速", "#00FF80" },
				{ "特急", "#8000FF" }
			},
			Patterns = new()
			{
				{ "TkyNm", new() { Direction = "東京", NextStation = "千駄ヶ谷", Upside = true, TrainType = "普通" } }
			},
			WeekDaysTimetable =
			[
				new() { Time = "06:00", PatternName = "TkyNm" },
				new() { Time = "08:00", Direction = "甲府", TrainType = "特急", NextStation = "立川", Upside = false },
				new() { Time = "10:00", Direction = "東京", TrainType = "快速", NextStation = "御茶ノ水", Upside = true },
				new() { Time = "12:00", Direction = "高尾", TrainType = "普通", NextStation = "大久保", Upside = false },
				new() { Time = "14:00", Direction = "東京", TrainType = "特急", NextStation = "東京", Upside = true },
				new() { Time = "16:00", Direction = "松本", TrainType = "特急", NextStation = "八王子", Upside = false },
				new() { Time = "18:00", Direction = "東京", TrainType = "普通", NextStation = "千駄ヶ谷", Upside = true },
				new() { Time = "20:00", Direction = "高尾", TrainType = "快速", NextStation = "代々木", Upside = false },
				new() { Time = "22:00", Direction = "南小谷", TrainType = "特急" , NextStation = "八王子", Upside = false }
			],
			HolidaysTimetable = [],
			Comment = "これはデザインモードまたはファイル未選択時用のサンプルデータです。"
		};
		Weekdays = new(_data.WeekDaysTimetable ?? []);
		Holidays = new(_data.HolidaysTimetable ?? []);
		PatternList = new(_data.Patterns?.ToList() ?? []);
		TypeColorList = new(_data.TypeColors?.ToList() ?? []);
		OnPropertyChanged(nameof(Weekdays));
		OnPropertyChanged(nameof(Holidays));
		OnPropertyChanged(nameof(PatternList));
		OnPropertyChanged(nameof(TypeColorList));
		OnPropertyChanged(nameof(StationName));
		OnPropertyChanged(nameof(UpdateTime));
		OnPropertyChanged(nameof(Comment));
	}

	public MainViewModel()
	{
		LoadSampleData();
	}

	public void LoadJsonFromFile()
	{
		try
		{
			var dialog = new OpenFileDialog
			{
				Filter = "JSONファイル (*.json)|*.json",
				Title = "時刻表JSONファイルを選択してください",
				ShowReadOnly = true,
				ValidateNames = true
			};

			if (dialog.ShowDialog() == true)
			{
				var json = File.ReadAllText(dialog.FileName);
				var newData = JsonSerializer.Deserialize<TimetableData>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (newData != null)
				{
					_data = newData;
					Weekdays = new(_data.WeekDaysTimetable ?? []);
					Holidays = new(_data.HolidaysTimetable ?? []);
					PatternList = new(_data.Patterns?.ToList() ?? []);
					TypeColorList = new(_data.TypeColors?.ToList() ?? []);
					OnPropertyChanged(nameof(Weekdays));
					OnPropertyChanged(nameof(Holidays));
					OnPropertyChanged(nameof(PatternList));
					OnPropertyChanged(nameof(TypeColorList));
					OnPropertyChanged(nameof(StationName));
					OnPropertyChanged(nameof(UpdateTime));
					OnPropertyChanged(nameof(Comment));
					_loadedFilePath = dialog.FileName;
					ValidateFormatCompatibility();
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void OnPropertyChanged(string name) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

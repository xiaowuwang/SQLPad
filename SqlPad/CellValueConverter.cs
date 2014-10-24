using System;
using System.Globalization;
using System.Windows.Data;

namespace SqlPad
{
	public class CellValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var columnHeader = (ColumnHeader)parameter;
			if (value == DBNull.Value)
				return ConfigurationProvider.Configuration.ResultGrid.NullPlaceholder;

			var convertedValue = columnHeader.ValueConverter.ConvertToCellValue(value);
			if (convertedValue is DateTime)
			{
				return FormatDateTime((DateTime)value);
			}

			var convertedStringValue = convertedValue as String ?? System.Convert.ToString(convertedValue);

			return String.Empty.Equals(convertedStringValue)
				? ConfigurationProvider.Configuration.ResultGrid.NullPlaceholder
				: convertedStringValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		public static string FormatDateTime(DateTime value)
		{
			return String.IsNullOrEmpty(ConfigurationProvider.Configuration.ResultGrid.DateFormat)
					? value.ToString(CultureInfo.CurrentUICulture)
					: value.ToString(ConfigurationProvider.Configuration.ResultGrid.DateFormat);
		}
	}

	public class SelectedIndexConverter : ValueConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)value + 1;
		}
	}

	public class DateTimeLabelConverter : ValueConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (DateTime)value == DateTime.MinValue ? ValueNotAvailable : ((DateTime)value).ToString(CultureInfo.CurrentUICulture);
		}
	}

	public class NumericConverter : ValueConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? ValueNotAvailable : System.Convert.ToString(value);
		}
	}

	public class BooleanLabelConverter : ValueConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? ValueNotAvailable : (bool)value ? "Yes" : "No";
		}
	}

	public class DataSpaceConverter : ValueConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? ValueNotAvailable : PrettyPrint((long)value);
		}

		public static string PrettyPrint(long bytes)
		{
			if (bytes < 1024)
			{
				return String.Format("{0} B", bytes);
			}
			
			if (bytes < 1048576)
			{
				return String.Format("{0} kB", Math.Round(bytes / 1024m));
			}

			if (bytes < 1073741824)
			{
				return String.Format("{0} MB", Math.Round(bytes / 1048576m, 1));
			}

			if (bytes < 1099511627776)
			{
				return String.Format("{0} GB", Math.Round(bytes / 1073741824m, 2));
			}

			return String.Format("{0} TB", Math.Round(bytes / 1099511627776m, 2));
		}
	}

	public class InMemoryAllocationStatusConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var allocatedInMemoryBytes = (long?)values[0];
			var storageBytes = (long?)values[1];
			var nonPopulatedBytes = (long?)values[2];
			var populationStatus = (string)values[3];
			if (!allocatedInMemoryBytes.HasValue || !storageBytes.HasValue || !nonPopulatedBytes.HasValue || String.IsNullOrEmpty(populationStatus))
			{
				return ValueConverter.ValueNotAvailable;
			}

			var populatedRatio = Math.Round(((decimal)storageBytes.Value - nonPopulatedBytes.Value) / storageBytes.Value * 100, 2);
			var populationStatusLabel = populationStatus == "STARTED" ? " - ongoing" : null;
			var populatedRatioLabel = populatedRatio == 100 ? null : String.Format("{0} %", populatedRatio);
			var populationStatusDetail = populatedRatio == 100 && populationStatusLabel == null
				? null
				: String.Format("({0}{1})", populatedRatioLabel, populationStatusLabel);

			return String.Format("{0} {1}", DataSpaceConverter.PrettyPrint(allocatedInMemoryBytes.Value), populationStatusDetail);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public abstract class ValueConverter : IValueConverter
	{
		public static string ValueNotAvailable = "N/A";

		public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

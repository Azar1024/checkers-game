using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheckersGame.ViewModels;

public class IndexToArrayConverter : IMultiValueConverter
{
    public static readonly IndexToArrayConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is int rowIndex && values[1] is int colIndex)
            return new[] { rowIndex, colIndex };
        return Array.Empty<int>();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CellBackgroundConverter : IMultiValueConverter
{
    public static readonly CellBackgroundConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is int rowIndex && values[1] is int colIndex)
            return ((rowIndex + colIndex) % 2 == 0)
                ? Brushes.SaddleBrown
                : Brushes.BurlyWood;
        return Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// Простые конвертеры
public class IsNotNullConverter : IValueConverter
{
    public static readonly IsNotNullConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsFalseConverter : IValueConverter
{
    public static readonly IsFalseConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SelectedCellHighlightConverter : IMultiValueConverter
{
    public static readonly SelectedCellHighlightConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count < 3)
            return Brushes.Transparent;

        var selected = values[0] as (int row, int col)?;
        if (selected == null || values[1] is not int rowIndex || values[2] is not int colIndex)
            return Brushes.Transparent;

        return selected.Value.row == rowIndex && selected.Value.col == colIndex
            ? new SolidColorBrush(Colors.Gold, 0.6)
            : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class OccupiedAndNotWhiteConverter : IMultiValueConverter
{
    public static readonly OccupiedAndNotWhiteConverter Instance = new();
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool isOccupied && values[1] is bool isWhite)
            return isOccupied && !isWhite && (parameter as string != "hidden");
        return false;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsKingVisibleConverter : IValueConverter
{
    public static readonly IsKingVisibleConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CaptureEffectConverter : IValueConverter
{
    public static readonly CaptureEffectConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Placeholder: return 0.0 if captured, else 1.0
        return value is bool b && b ? 1.0 : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
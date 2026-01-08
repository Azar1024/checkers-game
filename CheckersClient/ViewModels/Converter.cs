using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheckersGame.ViewModels;

//  КОНВЕРТЕР КООРДИНАТ ДЛЯ МНОЖЕСТВЕННОЙ ПРИВЯЗКИ 
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

//  КОНВЕРТЕР ОБВОДКИ ВЫБРАННОЙ ШАШКИ
public class SelectedPieceStrokeConverter : IMultiValueConverter
{
    public static readonly SelectedPieceStrokeConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count < 3)
            return Brushes.Transparent;

        var selected = values[0] as (int row, int col)?;
        if (selected == null || values[1] is not int rowIndex || values[2] is not int colIndex)
            return Brushes.Transparent;

        return selected.Value.row == rowIndex && selected.Value.col == colIndex
            ? Brushes.Red    // цвет обводки выбранной шашки
            : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}


//  КОНВЕРТЕР ЦВЕТА КЛЕТОК 
public class CellBackgroundConverter : IMultiValueConverter
{
    public static readonly CellBackgroundConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is int rowIndex && values[1] is int colIndex)
            return ((rowIndex + colIndex) % 2 == 0)
                ? Brushes.SaddleBrown  // Темные клетки
                : Brushes.BurlyWood;   // Светлые клетки
        return Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

//  ПРОСТЫЕ КОНВЕРТЕРЫ ДЛЯ ЛОГИЧЕСКИХ ОПЕРАЦИЙ 
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

//  КОНВЕРТЕР ПОДСВЕТКИ ВЫБРАННОЙ КЛЕТКИ 
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

        //  ПОДСВЕЧИВАЕМ ЖЕЛТЫМ ПРИ СОВПАДЕНИИ КООРДИНАТ 
        return selected.Value.row == rowIndex && selected.Value.col == colIndex
            ? new SolidColorBrush(Colors.Yellow, 0.5)
            : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

//  КОНВЕРТЕР ДЛЯ ОТОБРАЖЕНИЯ ЧЕРНЫХ ШАШЕК 
public class OccupiedAndNotWhiteConverter : IMultiValueConverter
{
    public static readonly OccupiedAndNotWhiteConverter Instance = new();

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool isOccupied && values[1] is bool isWhite)
            return isOccupied && !isWhite; // Занятая клетка И НЕ белая = черная
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
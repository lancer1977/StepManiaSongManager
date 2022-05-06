using System;
using System.Globalization;
using System.Windows.Data;

namespace Stepmania.Manager.Converters;

public abstract class StrongConverter<T, T2> : IValueConverter
{
    public abstract T2 ToT2(T t);
    public abstract T ToT(T2 t2);
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;
        var t2 = ToT2((T)value);
        return t2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;
        var t = ToT((T2)value);
        return t;
    }
}
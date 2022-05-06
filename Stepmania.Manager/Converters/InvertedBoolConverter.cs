using System.Windows;
using System.Windows.Media;

namespace Stepmania.Manager.Converters;

public class BoolToVisibilityConverter : StrongConverter<bool, Visibility>
{
    public override Visibility ToT2(bool t)
    {
        return t ? Visibility.Visible : Visibility.Collapsed;
    }

    public override bool ToT(Visibility t2)
    {
        throw new System.NotImplementedException();
    }
}

public class InvertedBoolToVisibilityConverter : StrongConverter<bool, Visibility>
{
    public override Visibility ToT2(bool t)
    {
        return !t ? Visibility.Visible : Visibility.Collapsed;
    }

    public override bool ToT(Visibility t2)
    {
        throw new System.NotImplementedException();
    }
}
public class InvertedBoolConverter : StrongConverter<bool, bool>
{
    public override bool ToT2(bool t)
    {
        return !t;
    }

    public override bool ToT(bool t2)
    {
        return !t2;
    }
}

public class StringToBrushColorConverter : StrongConverter<string, Brush>
{
    public override Brush ToT2(string t)
    {
        var converter = new BrushConverter();
        var color = converter.ConvertFromString(t) as SolidColorBrush;
        var gradient = new LinearGradientBrush(color.Color, Colors.White, 5);
        return gradient;
    }

    public override string ToT(Brush t2)
    {
        throw new System.NotImplementedException();
    }
}

public class StringToBrushColorConverterFlat : StrongConverter<string, Brush>
{
    public override Brush ToT2(string t)
    {
        var converter = new BrushConverter();
        var color = converter.ConvertFromString(t) as SolidColorBrush;
        var gradient = new LinearGradientBrush(color.Color, Colors.White, 5);
        return gradient;
    }

    public override string ToT(Brush t2)
    {
        throw new System.NotImplementedException();
    }
}
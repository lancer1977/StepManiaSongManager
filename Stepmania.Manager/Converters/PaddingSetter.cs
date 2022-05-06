using System.Windows;
using System.Windows.Controls;

namespace Stepmania.Manager.Converters;

public class PaddingSetter
{
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.RegisterAttached("Padding", typeof(Thickness),
            typeof(PaddingSetter), new UIPropertyMetadata(new Thickness(), MarginChangedCallback));

    public static Thickness GetPadding(DependencyObject obj)
    {
        return (Thickness)obj.GetValue(PaddingProperty);
    }

    public static void SetPadding(DependencyObject obj, Thickness value)
    {
        obj.SetValue(PaddingProperty, value);
    }

    public static void MarginChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
    {
        var panel = sender as Panel;
        if (panel == null) return;

        panel.Loaded += new RoutedEventHandler(panel_Loaded);
    }

    static void panel_Loaded(object sender, RoutedEventArgs e)
    {
        var panel = sender as Panel;
        // Go over the children and set margin for them:
        foreach (var child in panel.Children)
        {
            if (child is not TextBlock tb) continue;
            tb.Padding = GetPadding(panel);
        }
    }
}
using System.Windows;
using System.Windows.Controls;

namespace Stepmania.Manager.Converters;

public class MarginSetter
{
    public static readonly DependencyProperty MarginProperty = DependencyProperty.RegisterAttached("Margin", typeof(Thickness), typeof(MarginSetter), new UIPropertyMetadata(new Thickness(), MarginChangedCallback));


    public static Thickness GetMargin(DependencyObject obj)
    {
        return (Thickness)obj.GetValue(MarginProperty);
    }

    public static void SetMargin(DependencyObject obj, Thickness value)
    {
        obj.SetValue(MarginProperty, value);
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
            if (child is not FrameworkElement fe) continue;
            fe.Margin = GetMargin(panel);
        }
    }
}
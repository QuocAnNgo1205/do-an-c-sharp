using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System;

namespace VinhKhanhFoodTour.App.Services;

/// <summary>
/// Markup Extension giúp XAML viết ngắn gọn `{services:Translate Key}`
/// và tự động trả về một Binding kết nối đến LocalizationManager.
/// Nó cũng giúp bypass (lách bộ biên dịch) lỗi báo "Unsupported indexer index type" của MAUI.
/// </summary>
[ContentProperty(nameof(Text))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Text { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Text}]",
            Source = LocalizationManager.Instance
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

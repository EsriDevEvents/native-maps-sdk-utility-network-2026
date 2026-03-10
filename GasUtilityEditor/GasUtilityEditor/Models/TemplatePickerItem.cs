using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.Windows.Media;

namespace GasUtilityEditor;

public partial class TemplatePickerItem : ObservableObject
{
    public TemplatePickerItem(SharedTemplate template, long layerId)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        LayerId = layerId;
        TableName = Template.TemplateSource.GetTable(layerId)?.TableName ?? throw new ArgumentNullException(nameof(template));
        _ = SetImageSourceAsync();
    }

    public SharedTemplate Template { get; }

    public long LayerId { get; }

    [ObservableProperty]
    private ImageSource? _imageSource = null;

    private async Task SetImageSourceAsync()
    {
        try
        {
            var swatch = await Template.CreateSwatchAsync(LayerId);
            ImageSource = await swatch.ToImageSourceAsync();
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating template swatch: {ex.Message}");
        }

        #region

        var symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.White, null);
        var symbolSwatch = await symbol.CreateSwatchAsync();
        ImageSource = await symbolSwatch.ToImageSourceAsync();

        #endregion
    }

    [ObservableProperty]
    private string? _tableName = null;
}
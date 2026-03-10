using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System.Windows;

namespace GasUtilityEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    #region sensitive data

    public static string WebmapUrl = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=28c5d6e1c498445cbef96f82e7608e93";
    public static string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
    public static string Username = "editor01";
    public static string Password = "S7#i2LWmYH75";

    public static string SnapNetworkSource = "Pipeline Junction";
    public static string SnapAssetGroup = "Tee";
    public static string SnapAssetType = "Plastic 3-Way";

    public static Viewpoint PresetViewpoint = new Viewpoint(new Envelope(-9810612.052454052, 5123404.689818885, -9810597.530454624, 5123411.822238396, SpatialReferences.WebMercator));
    public static Viewpoint GroupViewpoint = new Viewpoint(new Envelope(-9810878.632410226, 5123437.1565792225, -9810692.123363564, 5123528.759720369, SpatialReferences.WebMercator));

    private void OnStartup(object sender, StartupEventArgs e)
    {
        //ArcGISRuntimeEnvironment.ApiKey = "<YOUR API KEY>";
        //ArcGISRuntimeEnvironment.SetLicense("<YOUR LICENSE KEY>",
        //    ["<YOUR EXTENSION KEY>"]);
    }

    #endregion sensitive data
}
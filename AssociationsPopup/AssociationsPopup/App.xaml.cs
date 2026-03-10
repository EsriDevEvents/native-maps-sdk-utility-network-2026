using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using System.Windows;

namespace AssociationsPopup;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    #region sensitive data

    public static readonly Viewpoint InitialViewpoint = new(new MapPoint(-9812698.108111804, 5131928.691597462, SpatialReferences.WebMercator),
                                                      scale: 15.073670440170595);

    public static string WebmapUrl = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=381de677abfe4c4d9304d1d896315faf";

    private static string HostName = "sampleserver7.arcgisonline.com";
    private static string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
    private static string Username = "editor01";
    private static string Password = "S7#i2LWmYH75";

    private void OnStartup(object sender, StartupEventArgs e)
    {
        //ArcGISRuntimeEnvironment.ApiKey = "<YOUR API KEY>";
        //ArcGISRuntimeEnvironment.SetLicense("<YOUR LICENSE KEY>",
        //    ["<YOUR EXTENSION KEY>"]);

        AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
        {
            if (info.ServiceUri?.Host.Equals(HostName) == true)
                return await AccessTokenCredential.CreateAsync(new Uri(PortalUrl), Username, Password);
            throw new InvalidOperationException();
        });
    }

    #endregion sensitive data
}
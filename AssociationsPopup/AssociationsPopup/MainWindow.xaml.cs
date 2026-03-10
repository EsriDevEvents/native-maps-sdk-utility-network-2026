using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

namespace AssociationsPopup;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Envelope? _previousExtent;
    private bool _showAssociations = false;
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();

        MyMapView.Map = new Map(new Uri(App.WebmapUrl))
        {
            InitialViewpoint = App.InitialViewpoint
        };

        _ = DisplayLegendAsync();
    }

    #region View associations on map

    private async Task DisplayLegendAsync()
    {
        try
        {
            IsBusy.Visibility = Visibility.Visible;
            var uniqueValueRenderer = new UniqueValueRenderer(["AssociationType"],
                                                              [ new ("Attachment",
                                                                     "Attachment",
                                                                     new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Blue, 5d),
                                                                     "Attachment"),
                                                                new ("Connectivity",
                                                                     "Connectivity",
                                                                     new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Red, 5d),
                                                                     "Connectivity"),
                                                                new ("Containment",
                                                                     "Containment",
                                                                     new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Color.Yellow, 5d),
                                                                     "Containment")
                                                              ],
                                                              string.Empty,
                                                              null);

            var symbolKey = new Dictionary<string, System.Windows.Media.ImageSource?>();
            foreach (var uniqueValue in uniqueValueRenderer.UniqueValues)
            {
                if (uniqueValue.Symbol is not Symbol symbol)
                    continue;
                var swatch = await symbol.CreateSwatchAsync();
                if (swatch is null)
                    continue;
                symbolKey[uniqueValue.Label] = await swatch.ToImageSourceAsync();
            }

            AssociationLegend.ItemsSource = symbolKey;
            MyMapView.GraphicsOverlays ??= [];
            MyMapView.GraphicsOverlays.Add(new GraphicsOverlay()
            {
                Renderer = uniqueValueRenderer
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().Name);
        }
        finally
        {
            IsBusy.Visibility = Visibility.Collapsed;
        }
    }

    private void OnAssociationsClick(object sender, RoutedEventArgs e)
    {
        _showAssociations = !_showAssociations;
        if (MyMapView.GraphicsOverlays?.FirstOrDefault() is GraphicsOverlay overlay)
            overlay.IsVisible = _showAssociations;
        if (_showAssociations)
        {
            _ = DisplayAttachmentAndConnectivitiesAsync();
            _ = DisplayContainmentsAsync();
        }
    }

    private void OnViewpointChanged(object sender, EventArgs e)
    {
        if (!_showAssociations)
        {
            return;
        }

        try
        {
            if (_timer == null || !_timer.IsEnabled)
            {
                if (_timer == null)
                {
                    _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (a, b) =>
                    {
                        _ = DisplayAttachmentAndConnectivitiesAsync();
                        _timer?.Stop();
                    }, Application.Current.Dispatcher);
                }
                _timer.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().Name);
        }
        finally
        {
            IsBusy.Visibility = Visibility.Collapsed;
        }
    }

    private async Task DisplayAttachmentAndConnectivitiesAsync()
    {
        try
        {
            if (MyMapView.Map?.UtilityNetworks?.FirstOrDefault() is not UtilityNetwork utilityNetwork ||
            MyMapView.GraphicsOverlays?.FirstOrDefault() is not GraphicsOverlay overlay)
            {
                return;
            }

            if (!_showAssociations)
            {
                return;
            }

            IsBusy.Visibility = Visibility.Visible;

            if (MyMapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry?.Extent is Envelope currentExtent &&
                (_previousExtent is null || _previousExtent != currentExtent))
            {
                var associations = await utilityNetwork.GetAssociationsAsync(currentExtent);

                foreach (var association in associations)
                {
                    if (overlay.Graphics.Any(g => g.Attributes.TryGetValue("GlobalId", out var id) &&
                                             id is Guid guid &&
                                             guid == association.GlobalId))
                    {
                        continue;
                    }

                    var graphic = new Graphic(association.Geometry);
                    graphic.Attributes["GlobalId"] = association.GlobalId;
                    graphic.Attributes["AssociationType"] = association.AssociationType.ToString();
                    overlay.Graphics.Add(graphic);
                }
            }
        }
        catch (TooManyAssociationsException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().Name);
        }
        finally
        {
            IsBusy.Visibility = Visibility.Collapsed;
        }
    }

    private async Task DisplayContainmentsAsync()
    {
        if (MyMapView.Map?.UtilityNetworks?.FirstOrDefault() is not UtilityNetwork utilityNetwork ||
            MyMapView.GraphicsOverlays?.FirstOrDefault() is not GraphicsOverlay overlay)
        {
            return;
        }

        for (int i = 0; i < overlay.Graphics.Count; i++)
        {
            var graphic = overlay.Graphics[i];

            if (graphic.Attributes.TryGetValue("AssociationType", out var type) &&
                type is string associationType &&
                associationType == "Containment")
            {
                overlay.Graphics.Remove(graphic);
            }
        }

        overlay.IsVisible = _showAssociations;

        if (MyPopupViewer.Popup?.GeoElement is ArcGISFeature feature)
        {
            await feature.LoadAsync();
            var element = utilityNetwork.CreateElement(feature);

            var associations = await utilityNetwork.GetAssociationsAsync(element, UtilityAssociationType.Containment);

            var elements = new List<UtilityElement>();

            foreach (var association in associations)
            {
                if (!elements.Any(element => element.GlobalId == association.FromElement.GlobalId))
                {
                    elements.Add(association.FromElement);
                }
                if (!elements.Any(element => element.GlobalId == association.ToElement.GlobalId))
                {
                    elements.Add(association.ToElement);
                }
            }

            if (elements.Count > 0)
            {
                var features = await utilityNetwork.GetFeaturesForElementsAsync(elements);
                var containmentExtent = GeometryEngine.CombineExtents(from f in features where f.Geometry is not null select f.Geometry);

                var containment = new Graphic(containmentExtent);
                containment.Attributes["AssociationType"] = "Containment";
                overlay.Graphics.Add(containment);
            }
        }
    }

    #endregion View associations on map

    #region View associations on popup

    private async void OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
    {
        try
        {
            IsBusy.Visibility = Visibility.Visible;
            PopupBorder.Visibility = Visibility.Collapsed;

            var layerResults = await MyMapView.IdentifyLayersAsync(e.Position,
                                                                   tolerance: 3,
                                                                   returnPopupsOnly: true);

            if (layerResults?.FirstOrDefault(r => r.Popups.Count > 0)?.Popups.FirstOrDefault() is not Popup popup)
            {
                return;
            }

            PopupBorder.Visibility = Visibility.Visible;
            MyPopupViewer.Popup = popup;
            if (popup.GeoElement is ArcGISFeature feature)
                SelectFeature(feature);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().Name);
        }
        finally
        {
            IsBusy.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSelectCurrent(object sender, RoutedEventArgs e)
    {
        if (MyPopupViewer.CurrentPopup?.GeoElement is not ArcGISFeature feature)
        {
            return;
        }

        SelectFeature(feature);
    }

    private void SelectFeature(ArcGISFeature feature)
    {
        if (MyMapView?.Map is null || feature.FeatureTable?.Layer is not FeatureLayer featureLayer)
            return;

        foreach (var layer in MyMapView.Map.OperationalLayers)
        {
            if (layer is FeatureLayer fl)
                fl.ClearSelection();
        }

        featureLayer.SelectFeature(feature);
    }

    #endregion View associations on popup
}
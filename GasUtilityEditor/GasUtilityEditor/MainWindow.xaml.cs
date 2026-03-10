using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI.Editing;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GasUtilityEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region members

    private CollectionViewSource _collectionViewSource = new();
    private ObservableCollection<TemplatePickerItem> _templateItems = [];
    private ObservableCollection<ToolItem> _toolItems = [];

    private readonly GeometryConstructionToolType[] NeutralConstructionTools = [
                GeometryConstructionToolType.Freehand,
                GeometryConstructionToolType.Trace,
                GeometryConstructionToolType.Circle,
                GeometryConstructionToolType.Ellipse,
                GeometryConstructionToolType.Rectangle
            ];

    private bool isOnPresetViewpoint = false;
    private CancellationTokenSource? cts = null;
    private TemplatePickerItem? _currentTemplatePickerItem = null;

    #endregion members

    public MainWindow()
    {
        InitializeComponent();

        #region collection setup

        _collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TemplatePickerItem.TableName)));
        _collectionViewSource.Source = _templateItems;

        TemplatePicker.SetBinding(ListView.ItemsSourceProperty, new Binding
        {
            Source = _collectionViewSource,
        });

        ToolPicker.ItemsSource = _toolItems;

        foreach (var toolType in Enum.GetValues<GeometryConstructionToolType>())
        {
            if (toolType == GeometryConstructionToolType.Unknown)
                continue;
            _toolItems.Add(new ToolItem(toolType));
        }

        #endregion collection setup

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsBusy.Visibility = Visibility.Visible;

            var credential = await AccessTokenCredential.CreateAsync(new Uri(App.PortalUrl), App.Username, App.Password);
            AuthenticationManager.Current.AddCredential(credential);

            var map = new Map(new Uri(App.WebmapUrl)) { InitialViewpoint = App.GroupViewpoint };
            map.LoadSettings.FeatureTilingMode = FeatureTilingMode.EnabledWithFullResolutionWhenSupported;
            MyMapView.Map = map;

            await map.LoadAsync();

            if (map.UtilityNetworks.FirstOrDefault() is not UtilityNetwork utilityNetwork
                || utilityNetwork.ServiceGeodatabase is not ServiceGeodatabase serviceGeodatabase)
            {
                return;
            }

            await utilityNetwork.LoadAsync();

            #region snapping

            var networkSource = utilityNetwork.Definition?.GetNetworkSource(App.SnapNetworkSource);
            var assetGroup = networkSource?.GetAssetGroup(App.SnapAssetGroup);
            var assetType = assetGroup?.GetAssetType(App.SnapAssetType);
            if (assetType is not null && MyMapView.GeometryEditor is GeometryEditor geometryEditor)
            {
                SnapRules snapRules = await SnapRules.CreateAsync(utilityNetwork, assetType);
                geometryEditor.SnapSettings.SyncSourceSettings(snapRules, SnapSourceEnablingBehavior.SetFromRules);
                geometryEditor.SnapSettings.IsEnabled = true;
            }

            #endregion snapping

            var sharedTemplates = await serviceGeodatabase.QuerySharedTemplatesAsync();

            foreach (var (serviceLayerId, layerTemplates) in sharedTemplates)
            {
                var table = serviceGeodatabase.GetTable(serviceLayerId);

                if (!table.HasGeometry)
                    continue;

                foreach (var template in layerTemplates)
                {
                    _templateItems.Add(new TemplatePickerItem(template, serviceLayerId));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            IsBusy.Visibility = Visibility.Collapsed;
            SearchText.IsEnabled = true;
        }
    }

    private void TryCancel()
    {
        _currentTemplatePickerItem = null;
        cts?.Cancel();
        cts = null;
    }

    private async Task ActivateToolAsync(ToolItem toolItem)
    {
        do
        {
            if (_currentTemplatePickerItem is null)
                break;

            cts = new CancellationTokenSource();
            _toolItems.ToList().ForEach(tool => tool.IsDefault = tool == toolItem);
            var templateSource = _currentTemplatePickerItem.Template.TemplateSource;

            Geometry? geometry = null;
            try
            {
                if (NeutralConstructionTools.Any(t => t == toolItem.ToolType))
                {
                    var table = templateSource.GetTable(_currentTemplatePickerItem.LayerId);
                    var geometryType = table.GeometryType;
                    geometry = await MyMapView.DrawAsync(toolItem.ToolType, geometryType, cts.Token);
                }
                else
                {
                    geometry = await MyMapView.DrawAsync(toolItem.ToolType, cts.Token);
                }

                var featureCreationSet = await templateSource.CreateFeaturesAsync(_currentTemplatePickerItem.Template, geometry);
                await templateSource.AddFeaturesAsync(featureCreationSet);
            }
            catch (TaskCanceledException) { }
        } while (cts is not null && !cts.Token.IsCancellationRequested);
    }

    private async void OnToolClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if ((sender as Button)?.DataContext is not ToolItem toolItem)
                return;
            await ActivateToolAsync(toolItem);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Add features failed: {ex}");
        }
    }

    private async void OnTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if ((sender as ListView)?.SelectedItem is not TemplatePickerItem currentTemplate)
                return;
            _currentTemplatePickerItem = currentTemplate;
            var template = _currentTemplatePickerItem.Template;

            var defaultTool = _currentTemplatePickerItem.Template.GetDefaultConstructionTool(_currentTemplatePickerItem.LayerId);
            var defaultToolType = defaultTool?.ToolType ?? GeometryConstructionToolType.Unknown;
            _toolItems.ToList().ForEach(tool => tool.IsDefault = tool.ToolType == defaultToolType);

            await template.LoadAsync();
            _toolItems.ToList().ForEach(tool => tool.IsEnabled = template.IsToolApplicable(tool.ToolType, _currentTemplatePickerItem.LayerId));

            if (_toolItems.FirstOrDefault(t => t.IsDefault) is ToolItem toolItem)
                await ActivateToolAsync(toolItem);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading selected template: {ex.Message}", $"{ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        TryCancel();
        if (SearchText.Text is string searchText && !string.IsNullOrEmpty(searchText))
        {
            _collectionViewSource.Source = _templateItems.Where(item => item.Template.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            _collectionViewSource.Source = _templateItems;
        }
    }

    private void OnClearSearch(object sender, MouseButtonEventArgs e)
    {
        SearchText.Text = string.Empty;
    }

    private void OnChangeViewpoint(object sender, RoutedEventArgs e)
    {
        var viewPoint = isOnPresetViewpoint ? App.GroupViewpoint : App.PresetViewpoint;
        isOnPresetViewpoint = !isOnPresetViewpoint;
        _ = MyMapView.SetViewpointAsync(viewPoint);
    }
}
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;

namespace GasUtilityEditor;

public static class Extensions
{
    #region prototype

    public static ArcGISFeatureTable GetTable(this ISharedTemplateSource templateSource, long layerId)
    {
        return templateSource switch
        {
            ServiceGeodatabase sgdb => sgdb.GetTable(layerId),
            Geodatabase gdb => gdb.GetGeodatabaseFeatureTable(layerId) ?? throw new ArgumentException(nameof(layerId)),
            _ => throw new NotSupportedException($"Unsupported template source type: {templateSource.GetType().FullName}"),
        };
    }

    public static async Task<IEnumerable<ISharedTemplateSource>> FetchSharedTemplateSourcesAsync(this GeoModel geoModel)
    {
        await geoModel.LoadAsync();

        var templateSources = new HashSet<ISharedTemplateSource>();

        var addTemplateSource = (ArcGISFeatureTable aft) =>
        {
            if (aft is ServiceFeatureTable sft && sft.ServiceGeodatabase is ISharedTemplateSource sgdb)
            {
                templateSources.Add(sgdb);
            }
            else if (aft is GeodatabaseFeatureTable gft && gft.Geodatabase is ISharedTemplateSource gdb)
            {
                templateSources.Add(gdb);
            }
        };

        foreach (var layer in geoModel.OperationalLayers.ToFeatureLayers())
        {
            if (layer.FeatureTable is not ArcGISFeatureTable aft)
            {
                continue;
            }
            await layer.LoadAsync();
            addTemplateSource(aft);
        }

        return templateSources;
    }

    #endregion prototype

    #region helper methods

    private static IEnumerable<FeatureLayer> ToFeatureLayers(this IEnumerable<Layer> layers)
    {
        foreach (var layer in layers)
        {
            if (layer is FeatureLayer featureLayer)
            {
                yield return featureLayer;
            }

            if (layer is GroupLayer groupLayer)
            {
                foreach (var childFeatureLayer in groupLayer.Layers.ToFeatureLayers())
                {
                    yield return childFeatureLayer;
                }
            }
        }
    }

    #endregion helper methods
}
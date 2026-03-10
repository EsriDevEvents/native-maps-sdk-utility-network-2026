using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;
using System.ComponentModel;
using System.Windows.Input;

namespace GasUtilityEditor;

public static class GeometryEditorExtensions
{
    private static Task<Geometry> DrawAsync(this MapView mapView, GeometryType geometryType, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<Geometry>();
        cancellationToken.Register(() =>
        {
            mapView?.GeometryEditor?.Stop();
            tcs.TrySetCanceled();
        });

        if (geometryType == GeometryType.Point)
        {
            EventHandler<GeoViewInputEventArgs>? tappedHandler = null;
            tappedHandler = (s, e) =>
            {
                if (mapView.GeometryEditor?.IsStarted == true)
                {
                    mapView.GeometryEditor.Stop();
                    mapView.GeoViewTapped -= tappedHandler;
                    tcs.TrySetResult(e.Location!);
                }
            };
            mapView.GeoViewTapped += tappedHandler;
        }
        else if (geometryType == GeometryType.Polyline)
        {
            var geometryEditor = mapView.GeometryEditor!;
            PropertyChangedEventHandler? changedHandler = null;
            changedHandler = (s, e) =>
            {
                if ((s as GeometryEditor)?.IsStarted == true &&
               (s as GeometryEditor)?.Geometry is Polyline line &&
                new PolylineBuilder(line).IsSketchValid)
                {
                    geometryEditor.PropertyChanged -= changedHandler;
                    var geometry = mapView.GeometryEditor?.Stop();
                    tcs.TrySetResult(geometry!);
                }
            };
            geometryEditor.PropertyChanged += changedHandler;
        }
        else if (geometryType == GeometryType.Polygon)
        {
            EventHandler<GeoViewInputEventArgs>? doubleTappedHandler = null;
            doubleTappedHandler = (s, e) =>
            {
                if (mapView.GeometryEditor?.IsStarted == true)
                {
                    e.Handled = true;
                    mapView.GeoViewDoubleTapped -= doubleTappedHandler;
                    var geometry = mapView.GeometryEditor?.Stop();
                    tcs.TrySetResult(geometry!);
                }
            };
            mapView.GeoViewDoubleTapped += doubleTappedHandler;
        }
        else
        {
            KeyEventHandler? keyEventHandler = null;
            keyEventHandler = (s, e) =>
            {
                if (e.Key == Key.Enter && mapView.GeometryEditor?.IsStarted == true)
                {
                    mapView.KeyUp -= keyEventHandler;
                    var geometry = mapView.GeometryEditor.Stop();
                    tcs.TrySetResult(geometry!);
                }
            };
            mapView.KeyUp += keyEventHandler;
        }
        try
        {
            ArgumentNullException.ThrowIfNull(mapView.GeometryEditor, nameof(mapView.GeometryEditor));
            mapView.GeometryEditor.Start(geometryType);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        return tcs.Task;
    }

    public static async Task<Geometry> DrawAsync(this MapView mapView, GeometryConstructionToolType constructionToolType, CancellationToken cancellationToken = default)
    {
        return await mapView.DrawAsync(constructionToolType, GeometryType.Unknown, cancellationToken);
    }

    public static async Task<Geometry> DrawAsync(this MapView mapView, GeometryConstructionToolType constructionToolType, GeometryType geometryType = GeometryType.Unknown, CancellationToken cancellationToken = default)
    {
        var geometryEditor = mapView.GeometryEditor;
        ArgumentNullException.ThrowIfNull(geometryEditor, nameof(mapView.GeometryEditor));
        var geomType = geometryType != GeometryType.Unknown ? geometryType : constructionToolType.ToString().EndsWith("Polygon") ? GeometryType.Polygon : GeometryType.Polyline;
        switch (constructionToolType)
        {
            case GeometryConstructionToolType.AutoCompleteFreehandPolygon:
            case GeometryConstructionToolType.Freehand:
            case GeometryConstructionToolType.StreamingPolyline:
            case GeometryConstructionToolType.StreamingPolygon:
                {
                    geometryEditor.Tool = new FreehandTool();
                    return await mapView.DrawAsync(geomType, cancellationToken);
                }
            case GeometryConstructionToolType.AutoCompletePolygon:
            case GeometryConstructionToolType.Polygon:
            case GeometryConstructionToolType.Trace:
                {
                    geometryEditor.Tool = new VertexTool();
                    return await mapView.DrawAsync(geomType, cancellationToken);
                }
            case GeometryConstructionToolType.Circle:
                {
                    var shapeTool = ShapeTool.Create(ShapeToolType.Ellipse);
                    shapeTool.Configuration.ScaleMode = GeometryEditorScaleMode.Uniform;
                    geometryEditor.Tool = shapeTool;
                    return await mapView.DrawAsync(geomType, cancellationToken);
                }
            case GeometryConstructionToolType.Ellipse:
                {
                    var shapeTool = ShapeTool.Create(ShapeToolType.Ellipse);
                    geometryEditor.Tool = shapeTool;
                    return await mapView.DrawAsync(geomType, cancellationToken);
                }
            case GeometryConstructionToolType.Line:
            case GeometryConstructionToolType.Radial:
            case GeometryConstructionToolType.RightAnglePolyline:
            case GeometryConstructionToolType.TwoPointLine:
            case GeometryConstructionToolType.Split:
                {
                    geometryEditor.Tool = new VertexTool();
                    return await mapView.DrawAsync(GeometryType.Polyline, cancellationToken);
                }
            case GeometryConstructionToolType.Point:
            case GeometryConstructionToolType.PointAndRotation:
            case GeometryConstructionToolType.PointAlongLine:
            case GeometryConstructionToolType.PointAtEndOfLine:
                {
                    geometryEditor.Tool = new VertexTool();
                    return await mapView.DrawAsync(GeometryType.Point, cancellationToken);
                }
            case GeometryConstructionToolType.Multipoint:
                {
                    geometryEditor.Tool = new VertexTool();
                    return await mapView.DrawAsync(GeometryType.Multipoint, cancellationToken);
                }
            case GeometryConstructionToolType.Rectangle:
            case GeometryConstructionToolType.RegularPolygon:
            case GeometryConstructionToolType.RegularPolyline:
            case GeometryConstructionToolType.RightAnglePolygon:
                {
                    var shapeTool = ShapeTool.Create(ShapeToolType.Rectangle);
                    geometryEditor.Tool = shapeTool;
                    return await mapView.DrawAsync(geomType, cancellationToken);
                }
        }
        throw new NotSupportedException($"Unsupported geometry construction tool type: {constructionToolType}");
    }
}
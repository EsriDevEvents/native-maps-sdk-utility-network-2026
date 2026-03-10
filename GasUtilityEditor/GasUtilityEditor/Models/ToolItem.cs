using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.Calcite.WPF;
using System.Windows.Media;

namespace GasUtilityEditor;

public partial class ToolItem : ObservableObject
{
    public ToolItem(GeometryConstructionToolType toolType)
    {
        ToolType = toolType;
    }

    public GeometryConstructionToolType ToolType { get; }

    private CalciteIcon GetCalciteIcon(GeometryConstructionToolType toolType)
    {
        return toolType switch
        {
            GeometryConstructionToolType.AutoCompleteFreehandPolygon => CalciteIcon.Lasso,
            GeometryConstructionToolType.AutoCompletePolygon => CalciteIcon.PolygonLineCheck,
            GeometryConstructionToolType.Circle => CalciteIcon.Circle,
            GeometryConstructionToolType.Ellipse => CalciteIcon.Ellipse,
            GeometryConstructionToolType.Freehand => CalciteIcon.Freehand,
            GeometryConstructionToolType.Line => CalciteIcon.Line,
            GeometryConstructionToolType.Multipoint => CalciteIcon.NodesUnlink,
            GeometryConstructionToolType.Point => CalciteIcon.Point,
            GeometryConstructionToolType.PointAndRotation => CalciteIcon.Rotate,
            GeometryConstructionToolType.PointAlongLine => CalciteIcon.ConnectionMiddle,
            GeometryConstructionToolType.PointAtEndOfLine => CalciteIcon.ConnectionEndRight,
            GeometryConstructionToolType.Polygon => CalciteIcon.PolygonVertices,
            GeometryConstructionToolType.Radial => CalciteIcon.RelativeDirection,
            GeometryConstructionToolType.Rectangle => CalciteIcon.Rectangle,
            GeometryConstructionToolType.RegularPolygon => CalciteIcon.HexagonInsetLarge,
            GeometryConstructionToolType.RegularPolyline => CalciteIcon.Hexagon,
            GeometryConstructionToolType.RightAnglePolygon => CalciteIcon.RectanglePlus,
            GeometryConstructionToolType.RightAnglePolyline => CalciteIcon.RightAngle,
            GeometryConstructionToolType.Split => CalciteIcon.SplitGeometry,
            GeometryConstructionToolType.StreamingPolyline => CalciteIcon.TracePath,
            GeometryConstructionToolType.StreamingPolygon => CalciteIcon.LassoSelect,
            GeometryConstructionToolType.Trace => CalciteIcon.Trace,
            GeometryConstructionToolType.TwoPointLine => CalciteIcon.ConnectionToConnection,
            _ => CalciteIcon.QuestionMark
        };
    }

    private ImageSource? _imageSource = null;

    public ImageSource? ImageSource
    {
        get
        {
            if (_imageSource is not null)
                return _imageSource;
            var iconImageExtension = new CalciteIconImageExtension
            {
                Icon = GetCalciteIcon(ToolType),
                SymbolSize = 35
            };
            _imageSource = iconImageExtension.ProvideValue(serviceProvider: null) as ImageSource;
            return _imageSource;
        }
    }

    [ObservableProperty]
    private bool isDefault;

    [ObservableProperty]
    private bool isEnabled = true;
}
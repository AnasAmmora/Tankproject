using UnityEngine;
using UnityEngine.UI;

// A three-vertex UI graphic. No texture, animation, or per-frame mesh rebuild.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UISelectionTriangle : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        vh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.center.y), color, Vector2.zero);
        vh.AddTriangle(0, 1, 2);
    }
}

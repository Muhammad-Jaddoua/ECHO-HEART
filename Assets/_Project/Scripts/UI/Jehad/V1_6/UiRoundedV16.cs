using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Original scalable rounded panel/border. Uses the default UI material and no texture asset.</summary>
    // Explicit: the panels are AddComponent'ed at runtime and were coming up without a CanvasRenderer (invisible, raycast errors).
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiRoundedV16 : MaskableGraphic
    {
        public float Radius { get; set; } = 16f;
        public float Border { get; set; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect rect = GetPixelAdjustedRect(); if (rect.width <= 0f || rect.height <= 0f) return;
            float radius = Mathf.Clamp(Radius, 0f, Mathf.Min(rect.width, rect.height) * .5f);
            const int perCorner = 8; const int count = (perCorner + 1) * 4;
            var outer = new Vector2[count]; var inner = new Vector2[count]; float inset = Mathf.Clamp(Border, 0f, radius * .9f);
            for (int corner = 0; corner < 4; corner++) for (int j = 0; j <= perCorner; j++)
            {
                float angle = (corner * 90f + j * 90f / perCorner) * Mathf.Deg2Rad;
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius, corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                int i = corner * (perCorner + 1) + j;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); outer[i] = center + direction * radius; inner[i] = center + direction * (radius - inset);
            }
            if (inset <= 0f)
            {
                vh.AddVert(rect.center, color, Vector2.zero);
                for (int i = 0; i < count; i++) vh.AddVert(outer[i], color, Vector2.zero);
                for (int i = 0; i < count; i++) vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
            }
            else
            {
                for (int i = 0; i < count; i++) { vh.AddVert(outer[i], color, Vector2.zero); vh.AddVert(inner[i], color, Vector2.zero); }
                for (int i = 0; i < count; i++) { int n = (i + 1) % count; vh.AddTriangle(i * 2, n * 2, i * 2 + 1); vh.AddTriangle(n * 2, n * 2 + 1, i * 2 + 1); }
            }
        }
    }
}

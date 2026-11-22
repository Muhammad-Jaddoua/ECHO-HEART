using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    public enum UiGlyphKindV16 { Heart, Sword, Bow, Fire, Relic, Cog, Pause, Arrow, Check, None }
    public sealed class UiGlyphV16 : MaskableGraphic
    {
        private UiGlyphKindV16 _kind;
        public UiGlyphKindV16 Kind { get => _kind; set { _kind = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_kind == UiGlyphKindV16.Heart)
            {
                var points = new Vector2[48];
                for (int i = 0; i < points.Length; i++) { float t = i * Mathf.PI * 2f / points.Length; points[i] = new Vector2(.5f + .028f * 16f * Mathf.Pow(Mathf.Sin(t), 3f), .49f + .029f * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2*t) - 2f * Mathf.Cos(3*t) - Mathf.Cos(4*t))); }
                Polygon(vh, points); return;
            }
            if (_kind == UiGlyphKindV16.Relic) { Polygon(vh, new[] { new Vector2(.5f,.06f),new Vector2(.88f,.5f),new Vector2(.5f,.94f),new Vector2(.12f,.5f) }); return; }
            if (_kind == UiGlyphKindV16.Fire) { Polygon(vh, new[] { new Vector2(.5f,.05f),new Vector2(.17f,.22f),new Vector2(.16f,.48f),new Vector2(.36f,.73f),new Vector2(.4f,.48f),new Vector2(.63f,.96f),new Vector2(.82f,.55f),new Vector2(.85f,.28f),new Vector2(.7f,.10f) }); return; }
            if (_kind == UiGlyphKindV16.Sword) { Line(vh,.22f,.18f,.77f,.82f,.12f); Line(vh,.14f,.39f,.47f,.10f,.09f); return; }
            if (_kind == UiGlyphKindV16.Bow) { for (int i=0;i<16;i++) { float a=-Mathf.PI*.5f+i*Mathf.PI/16,b=a+Mathf.PI/16; Line(vh,.32f+.40f*Mathf.Cos(a),.5f+.4f*Mathf.Sin(a),.32f+.40f*Mathf.Cos(b),.5f+.4f*Mathf.Sin(b),.065f); } Line(vh,.32f,.1f,.32f,.9f,.035f); Line(vh,.12f,.5f,.87f,.5f,.045f); Line(vh,.7f,.62f,.87f,.5f,.045f); Line(vh,.7f,.38f,.87f,.5f,.045f); return; }
            if (_kind == UiGlyphKindV16.Pause) { Line(vh,.35f,.2f,.35f,.8f,.14f); Line(vh,.65f,.2f,.65f,.8f,.14f); return; }
            if (_kind == UiGlyphKindV16.Arrow) { Line(vh,.35f,.2f,.65f,.5f,.075f); Line(vh,.65f,.5f,.35f,.8f,.075f); return; }
            if (_kind == UiGlyphKindV16.Check) { Line(vh,.18f,.48f,.42f,.25f,.10f); Line(vh,.42f,.25f,.84f,.78f,.10f); return; }
            int segments=24;
            for(int i=0;i<segments;i++) { float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments; Line(vh,.5f+.23f*Mathf.Cos(a),.5f+.23f*Mathf.Sin(a),.5f+.23f*Mathf.Cos(b),.5f+.23f*Mathf.Sin(b),.06f); }
            if (_kind == UiGlyphKindV16.Cog) for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Line(vh,.5f+.22f*Mathf.Cos(a),.5f+.22f*Mathf.Sin(a),.5f+.40f*Mathf.Cos(a),.5f+.40f*Mathf.Sin(a),.12f); }
        }
        private Vector2 Point(Vector2 p) { Rect r=GetPixelAdjustedRect(); return new Vector2(r.xMin+p.x*r.width,r.yMin+p.y*r.height); }
        private void Polygon(VertexHelper vh, Vector2[] points)
        {
            int start=vh.currentVertCount; Vector2 center=Vector2.zero; foreach(Vector2 p in points)center+=p; center/=points.Length;
            vh.AddVert(Point(center),color,Vector2.zero); foreach(Vector2 p in points)vh.AddVert(Point(p),color,Vector2.zero);
            for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);
        }
        private void Line(VertexHelper vh,float x1,float y1,float x2,float y2,float width)
        {
            Vector2 a=new Vector2(x1,y1),b=new Vector2(x2,y2),n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Polygon(vh,new[]{a+n,b+n,b-n,a-n});
        }
    }
}

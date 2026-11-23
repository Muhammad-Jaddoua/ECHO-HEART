using System;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool valid,string message){_checks++;if(!valid)throw new Exception(message);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.02f;
    private static bool Inside(float x,float y,float width,float height,float outerWidth,float outerHeight)=>x>=-.02f&&y>=-.02f&&x+width<=outerWidth+.02f&&y+height<=outerHeight+.02f;
    private static void Main()
    {
        var exact=UiLayoutV19.SafeArea(1920,1080,new UiSafeRectV19(100,70,1600,900));
        Check(exact.X==100&&exact.Y==70&&exact.Width==1600&&exact.Height==900,"Valid asymmetric insets changed.");
        var clipped=UiLayoutV19.SafeArea(1920,1080,new UiSafeRectV19(-40,-20,2000,1200));
        Check(clipped.X==0&&clipped.Y==0&&clipped.Width==1920&&clipped.Height==1080,"Out-of-screen safe area was not clipped.");
        foreach(var bad in new[]{new UiSafeRectV19(0,0,0,0),new UiSafeRectV19(2000,0,10,10),new UiSafeRectV19(0,0,-1,900),new UiSafeRectV19(float.NaN,0,1600,900),new UiSafeRectV19(0,0,float.PositiveInfinity,900)})
        {
            var fallback=UiLayoutV19.SafeArea(1920,1080,bad);
            Check(fallback.X==0&&fallback.Y==0&&fallback.Width==1920&&fallback.Height==1080,"Invalid/empty safe area did not fall back to the actual screen.");
        }
        Check(UiLayoutV19.CardScale(1600,900,1320,760)==1,"Normal title was needlessly scaled.");
        Check(UiLayoutV19.CardScale(4000,2000,660,432)==1,"Large displays enlarged UI above its designed size.");
        Check(Near(UiLayoutV19.CardScale(1000,900,1320,760),952f/1320f),"Width fitting ignored its gutters.");
        Check(Near(UiLayoutV19.CardScale(1600,600,1320,760),552f/760f),"Height fitting ignored its gutters.");
        foreach(float bad in new[]{0,-10,float.NaN,float.PositiveInfinity})
        {
            float factor=UiLayoutV19.CardScale(bad,bad,1320,760);UiSurfaceV19 surface=UiLayoutV19.Surface(bad,bad);
            Check(factor>0&&factor<=1&&!float.IsNaN(factor)&&surface.Scale>0&&surface.Scale<=1,"Invalid dimensions produced an invalid scale.");
        }
        var cases=new[]{
            (1920f,1080f,new UiSafeRectV19(0,0,1920,1080)),
            (1280f,720f,new UiSafeRectV19(0,0,1280,720)),
            (2560f,1080f,new UiSafeRectV19(0,0,2560,1080)),
            (1024f,768f,new UiSafeRectV19(0,0,1024,768)),
            (3840f,2160f,new UiSafeRectV19(0,0,3840,2160)),
            (1920f,1080f,new UiSafeRectV19(300,180,1000,600)),
            (1280f,720f,new UiSafeRectV19(180,100,900,420)),
            (1600f,900f,new UiSafeRectV19(500,50,600,700)),
            (900f,1600f,new UiSafeRectV19(80,120,760,1360)),
            (640f,360f,new UiSafeRectV19(15,25,600,310)),
            (1600f,900f,new UiSafeRectV19(700,380,200,140))};
        var cards=new[]{(1320f,760f),(620f,742f),(660f,644f),(760f,742f),(1100f,760f),(660f,432f)};
        foreach(var sample in cases)
        {
            float canvasScale=Math.Min(sample.Item1/1600f,sample.Item2/900f);
            UiSafeRectV19 safe=UiLayoutV19.SafeArea(sample.Item1,sample.Item2,sample.Item3);
            float width=safe.Width/canvasScale,height=safe.Height/canvasScale;
            UiSurfaceV19 surface=UiLayoutV19.Surface(width,height);
            Check(surface.Width>=1600&&surface.Height>=900,"HUD lost its minimum logical layout surface.");
            Check(Inside((width-surface.Width*surface.Scale)/2,(height-surface.Height*surface.Scale)/2,surface.Width*surface.Scale,surface.Height*surface.Scale,width,height),"HUD/toast surface exceeded the usable safe area.");
            foreach(var card in cards)
            {
                float factor=UiLayoutV19.CardScale(width,height,card.Item1,card.Item2);
                Check(factor>0&&factor<=1,"Card fitting is not a finite shrink-only operation.");
                float left=(width-card.Item1*factor)/2,bottom=(height-card.Item2*factor)/2;
                Check(Inside(left,bottom,card.Item1*factor,card.Item2*factor,width,height),"Centered menu card exceeded its safe area.");
                Check(left>=Math.Min(24f,Math.Min(width,height)*.25f)-.02f&&bottom>=Math.Min(24f,Math.Min(width,height)*.25f)-.02f,"Card lost its safe-area gutters.");
            }
            foreach(float scale in new[]{.85f,1f,1.15f})
            {
                // Actual HUD cluster anchors/sizes, checked on the common logical HUD/toast surface.
                float w=surface.Width,h=surface.Height,bottom=UiLayoutV19.ToastBottom(scale);
                var clusters=new[]{
                    (24f,h-24-92*scale,300*scale,92*scale),
                    (24f,h-38-182*scale,300*scale,90*scale),
                    (w-24-260*scale,h-24-82*scale,260*scale,82*scale),
                    (w-24-330*scale,h-40-186*scale,330*scale,104*scale),
                    (w-24-146*scale,h-56-236*scale,146*scale,50*scale),
                    (w/2-290*scale,h-24-82*scale,580*scale,82*scale),
                    (w/2-325*scale,96f,650*scale,66*scale),
                    (24f,24f,720*scale,42*scale),
                    (w/2-410*scale,bottom,820*scale,76*scale)};
                bool contained=true;
                foreach(var cluster in clusters)contained&=Inside(cluster.Item1,cluster.Item2,cluster.Item3,cluster.Item4,w,h);
                Check(contained,"HUD cluster or discovery toast escaped its logical layout surface.");
                Check(Near(bottom-(96+66*scale),22),"Discovery toast lost its prompt gap at a different HUD scale.");
                bool separate=true;
                for(int i=0;i<clusters.Length;i++)for(int j=i+1;j<clusters.Length;j++)
                {
                    var a=clusters[i];var b=clusters[j];
                    separate&=Math.Min(a.Item1+a.Item3,b.Item1+b.Item3)<=Math.Max(a.Item1,b.Item1)+.02f||Math.Min(a.Item2+a.Item4,b.Item2+b.Item4)<=Math.Max(a.Item2,b.Item2)+.02f;
                }
                Check(separate,"HUD clusters overlap at the supported HUD scale.");
            }
        }
        Check(UiLayoutV19.CardScale(1000,600,1320,760)<1&&1320>1000&&760>600,"Strong-inset clipping regression was not reproduced and fitted.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.9 safe-area, menu-fit and shared HUD/toast layout model (C# 9.0).");
    }
}

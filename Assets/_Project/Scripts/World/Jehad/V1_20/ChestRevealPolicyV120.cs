namespace Echoheart.World
{
    /// <summary>Optional presentation gate; opening and collection remain with their existing owners.</summary>
    public static class ChestRevealPolicyV120
    {
        public const float ClearAt=.95f;
        public static bool Visible(bool opened,bool collected,bool afterLid,float progress)
        {
            return opened&&!collected&&(!afterLid||(!float.IsNaN(progress)&&!float.IsInfinity(progress)&&progress>=ClearAt));
        }
    }
}

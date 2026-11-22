using System;

namespace Echoheart.Audio
{
    public enum MusicRegionV15 { Meadow, Amber, Lake, Pine, Alpine, Guardian }

    /// <summary>Engine-independent region choice and continuous music weights, including rapid reversals.</summary>
    public sealed class EnvironmentMusicV15
    {
        private readonly float[] _weights = { 1f, 0f, 0f, 0f, 0f, 0f };
        private MusicRegionV15 _candidate;
        private float _candidateTime;
        public MusicRegionV15 Region { get; private set; }
        public float Weight(int index) => _weights[index];

        public static MusicRegionV15 Select(float x, float y, float z, bool inArena)
        {
            if (inArena) return MusicRegionV15.Guardian;
            if (y > 25f) return MusicRegionV15.Alpine;
            float lakeX = (x - 52f) / 45f, lakeZ = (z + 3f) / 50f;
            if (lakeX * lakeX + lakeZ * lakeZ < 1.1f) return MusicRegionV15.Lake;
            if (x < -35f && z < 25f) return MusicRegionV15.Amber;
            if (z > 32f) return MusicRegionV15.Pine;
            return MusicRegionV15.Meadow;
        }

        public void Request(MusicRegionV15 region, float deltaTime, bool immediate = false)
        {
            if (region != _candidate) { _candidate = region; _candidateTime = 0f; }
            if (deltaTime > 0f && !float.IsInfinity(deltaTime) && !float.IsNaN(deltaTime)) _candidateTime += deltaTime;
            if (immediate || _candidateTime >= 0.65f) Region = region;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsInfinity(deltaTime) || float.IsNaN(deltaTime)) return;
            float keep = (float)Math.Exp(-deltaTime / 0.8f);
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = _weights[i] * keep + (i == (int)Region ? 1f - keep : 0f);
        }
    }
}

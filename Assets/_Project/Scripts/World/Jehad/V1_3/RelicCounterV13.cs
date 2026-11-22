using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Adapts the immutable V1.2 pickups to the existing HUD event contract.</summary>
    public sealed class RelicCounterV13 : MonoBehaviour
    {
        [SerializeField] private Transform _worldRoot;
        private HiddenRelicV12[] _relics;
        private int _published = -1;
        private float _nextRefresh;

        public int Collected { get; private set; }
        public int Total => _relics == null ? 0 : _relics.Length;
        public HiddenRelicV12[] Relics => _relics;

        private void Awake()
        {
            _relics = _worldRoot != null ? _worldRoot.GetComponentsInChildren<HiddenRelicV12>(true) : new HiddenRelicV12[0];
        }

        private void Start() => Refresh(true);

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.1f;
            Refresh(false);
        }

        public void Refresh(bool force)
        {
            int count = 0;
            if (_relics != null) foreach (var relic in _relics) if (relic != null && relic.Collected) count++;
            Collected = count;
            if (!force && count == _published) return;
            _published = count;
            GameEvents.RaiseCollectiblesChanged(count, Total);
        }
    }
}

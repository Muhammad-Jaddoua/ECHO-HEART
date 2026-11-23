using UnityEngine;
using UnityEngine.Events;
using Echoheart.Core;

namespace Echoheart.World
{
    /// <summary>A once-per-run hidden relic. Existing retry collection can reset inactive relics.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HiddenRelicV12 : MonoBehaviour, IResettable
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private UnityEvent _onDiscovered = new UnityEvent();
        private bool _collected;
        private Vector3 _home;

        public string Id => _id;
        public string DisplayName => _displayName;
        public bool Collected => _collected;

        private void Awake()
        {
            _home = transform.localPosition;
            GetComponent<SphereCollider>().isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
            transform.localPosition = _home + Vector3.up * (Mathf.Sin(Time.time * 1.8f) * 0.12f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected || other.gameObject.layer != Layers.Player) return;
            _collected = true;
            _onDiscovered.Invoke();
            gameObject.SetActive(false);
        }

        public void ResetFor(RetryScope scope)
        {
            if (scope != RetryScope.NewGame) return;
            _collected = false;
            transform.localPosition = _home;
            gameObject.SetActive(true);
        }
    }
}

using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Combat
{
    /// <summary>
    /// One shared projectile for Bow, Fire, and enemy/boss ranged attacks. Each frame it sweeps a sphere from
    /// the last position to the new one, so a fast arrow cannot tunnel through a thin target or wall.
    /// It resolves at most once: the closest valid hit wins, then it despawns.
    /// Queries ignore the physics collision matrix, so targets are chosen by the explicit hit mask.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private const int HitBufferSize = 8;

        [SerializeField, Min(0.01f)] private float _radius = 0.15f;
        [Tooltip("Safety net if range is huge or the projectile never resolves.")]
        [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;
        [SerializeField] private GameObject _impactVfx;
        [SerializeField] private UnityEvent _onHit;

        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        private Vector3 _direction;
        private float _speed;
        private float _range;
        private int _damage;
        private Team _team;
        private GameObject _owner;
        private int _hitMask;
        private int _attackId;

        private float _travelled;
        private float _age;
        private bool _launched;
        private bool _firstStep;
        // Guards the "hit once" rule: several colliders can be returned by one cast.
        private bool _resolved;

        public int AttackId => _attackId;
        public void Launch(Vector3 direction, float speed, float range, int damage, Team team, GameObject owner, LayerMask hitMask)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _speed = speed;
            _range = range;
            _damage = damage;
            _team = team;
            _owner = owner;
            _hitMask = hitMask.value | Layers.EnvironmentMask;
            _attackId = AttackIds.Next();

            _travelled = 0f;
            _age = 0f;
            _resolved = false;
            _firstStep = true;
            _launched = true;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        /// <summary>One simulation step. Separate from Update only so CombatSelfTest can drive it in edit mode.</summary>
        internal void Step(float deltaTime)
        {
            if (!_launched || _resolved) return;

            _age += deltaTime;
            if (_age >= _maxLifetime)
            {
                Despawn();
                return;
            }

            float step = Mathf.Min(_speed * deltaTime,_range - _travelled);
            Vector3 start = transform.position;

            // First step starts one radius behind the muzzle so a point-blank target (already
            // overlapping the spawn point, which SphereCast ignores) still gets hit.
            float padding = _firstStep ? _radius : 0f;
            _firstStep = false;

            int count = Physics.SphereCastNonAlloc(start - _direction * padding, _radius, _direction, _hits,
                step + padding, _hitMask, QueryTriggerInteraction.Ignore);

            if (TryFindClosestHit(count, out RaycastHit best))
            {
                transform.position = best.point;
                Resolve(best);
                return;
            }

            transform.position = start + _direction * step;
            _travelled += step;
            if (_travelled >= _range) Despawn();
        }

        private bool TryFindClosestHit(int count, out RaycastHit best)
        {
            best = default;
            float bestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (IsOwner(hit.collider)) continue;
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = hit;
                    found = true;
                }
            }
            return found;
        }

        // The muzzle is usually inside or next to the shooter's own colliders.
        private bool IsOwner(Collider col) =>
            _owner != null && col.transform.IsChildOf(_owner.transform);

        private void Resolve(RaycastHit hit)
        {
            if (_resolved) return;
            _resolved = true;

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                var info = new DamageInfo(_damage, hit.point, _direction, _team, _attackId, _owner);
                target.TryApplyDamage(info);
            }

            _onHit?.Invoke(); // null-safe: UnityEvent is not created when the component is added from code
            if (_impactVfx != null)
                Instantiate(_impactVfx, hit.point, Quaternion.LookRotation(-_direction));
            Despawn();
        }

        private void Despawn()
        {
            _resolved = true;
            // Destroy is illegal outside play mode; the edit-mode self test needs the immediate form.
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}

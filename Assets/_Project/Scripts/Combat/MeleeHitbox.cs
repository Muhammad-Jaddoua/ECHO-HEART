using System.Collections.Generic;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Combat
{
    /// <summary>
    /// A timed box in front of this transform. Swing() opens the window; every physics step inside it
    /// overlaps the box and damages each target once. One swing = one AttackId, and a per-swing set
    /// guarantees a target is never hit twice even if Health's id memory were bypassed.
    /// The box is drawn as a gizmo (yellow idle, red while active) so the hit window is visible in Scene view.
    /// </summary>
    public class MeleeHitbox : MonoBehaviour
    {
        private const int BufferSize = 16;

        [Tooltip("Seconds the hit window stays open after Swing().")]
        [SerializeField, Min(0.01f)] private float _activeSeconds = 0.15f;
        [Tooltip("Box size in local space: x = width, y = height, z = reach (forward).")]
        [SerializeField] private Vector3 _size = new Vector3(1.5f, 1f, 2f);
        [Tooltip("Box centre in local space. z is usually half of the reach so the box starts at the transform.")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, 1f);

        private readonly Collider[] _buffer = new Collider[BufferSize];
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        private int _damage;
        private Team _team;
        private GameObject _owner;
        private LayerMask _hitMask;
        private int _attackId;
        private float _endTime;

        public bool IsActive { get; private set; }

        /// <summary>Resize the box; reach is forward distance, the box then starts at the transform.</summary>
        public void Configure(float reach, float width)
        {
            reach = Mathf.Max(0.1f, reach);
            _size = new Vector3(Mathf.Max(0.1f, width), _size.y, reach);
            _offset = new Vector3(0f, _offset.y, reach * 0.5f);
        }

        /// <summary>
        /// Open the hit window. The mask is explicit because overlap queries ignore the collision matrix.
        /// Calling again while active starts a fresh swing (new id, fresh hit set).
        /// </summary>
        public void Swing(int damage, Team team, GameObject owner, LayerMask hitMask)
        {
            _damage = damage;
            _team = team;
            _owner = owner;
            _hitMask = hitMask;
            _attackId = AttackIds.Next();
            _hitThisSwing.Clear();
            _endTime = Time.time + _activeSeconds;
            IsActive = true;

            // Catch targets already inside the box so a window shorter than a physics step cannot miss them.
            ScanAndDamage();
        }

        /// <summary>Close the window early (owner defeated or pulled away mid-swing) so a dead attacker cannot still hit.</summary>
        public void Cancel()
        {
            IsActive = false;
            _hitThisSwing.Clear();
        }

        private void FixedUpdate()
        {
            if (!IsActive) return;
            ScanAndDamage();
            if (Time.time >= _endTime) IsActive = false;
        }

        private void OnDisable()
        {
            IsActive = false;
            _hitThisSwing.Clear();
        }

        private void ScanAndDamage()
        {
            Vector3 center = transform.TransformPoint(_offset);
            Vector3 halfExtents = Vector3.Scale(_size, transform.lossyScale) * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _buffer, transform.rotation,
                _hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider other = _buffer[i];
                _buffer[i] = null;

                IDamageable target = other.GetComponentInParent<IDamageable>();
                if (target == null || !_hitThisSwing.Add(target)) continue;
                if (target is Component component && _owner != null &&
                    component.transform.IsChildOf(_owner.transform)) continue;

                Vector3 point = other.ClosestPoint(center);
                Vector3 direction = other.bounds.center - (_owner != null ? _owner.transform.position : transform.position);
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

                target.TryApplyDamage(new DamageInfo(_damage, point, direction, _team, _attackId, _owner));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsActive ? new Color(1f, 0.1f, 0.1f, 0.9f) : new Color(1f, 0.9f, 0.1f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(_offset, _size);
            if (IsActive)
            {
                Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
                Gizmos.DrawCube(_offset, _size);
            }
        }
    }
}

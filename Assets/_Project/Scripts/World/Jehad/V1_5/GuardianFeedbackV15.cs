using Echoheart.AI;
using Echoheart.Audio;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoheart.World
{
    /// <summary>Presentation of the boss's public telegraph state and exact slam radius.</summary>
    public sealed class GuardianFeedbackV15 : MonoBehaviour
    {
        private BossBrain _boss;
        private WorldFeedbackV15 _feedback;
        private LineRenderer _line;
        private BossBrain.State _last;
        public void Bind(BossBrain boss, WorldFeedbackV15 feedback, Material material)
        {
            _boss = boss; _feedback = feedback;
            var go = new GameObject("Guardian attack warning"); go.transform.SetParent(transform, false);
            _line = go.AddComponent<LineRenderer>(); _line.sharedMaterial = material; _line.useWorldSpace = true;
            _line.shadowCastingMode = ShadowCastingMode.Off; _line.receiveShadows = false; _line.enabled = false;
            _line.numCornerVertices = 3;
        }
        private void LateUpdate()
        {
            if (_boss == null || _feedback == null || _line == null) return;
            bool active = _feedback.Playing && _boss.enabled && _boss.gameObject.activeInHierarchy;
            bool warning = active && _boss.Current == BossBrain.State.Telegraph;
            _line.enabled = warning;
            if (warning)
            {
                bool slam = _boss.CurrentAttack == BossBrain.AttackKind.Slam;
                _line.loop = slam; _line.positionCount = slam ? 64 : 2;
                Vector3 origin = transform.position + Vector3.up * 0.08f;
                if (slam) for (int i = 0; i < 64; i++)
                {
                    float a = i * Mathf.PI * 2f / 64f;
                    _line.SetPosition(i, origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _boss.Definition.SlamRadius);
                }
                else { _line.SetPosition(0, origin); _line.SetPosition(1, origin + transform.forward * 7f); }
                Color color = Color.Lerp(new Color(0.77f, 0.36f, 1f, 0.8f), new Color(1f, 0.68f, 0.25f), _boss.TelegraphProgress);
                _line.startColor = _line.endColor = color; _line.startWidth = _line.endWidth = Mathf.Lerp(0.055f, 0.15f, _boss.TelegraphProgress);
            }
            if (active && _boss.Current != _last)
            {
                if (_boss.Current == BossBrain.State.Telegraph) _feedback.Audio.PlayWorld(
                    _boss.CurrentAttack == BossBrain.AttackKind.Slam ? WorldCueV15.SlamWarning : WorldCueV15.RangedWarning, transform.position);
                if (_boss.Current == BossBrain.State.Attack)
                {
                    _feedback.Audio.PlayWorld(_boss.CurrentAttack == BossBrain.AttackKind.Slam ? WorldCueV15.Slam : WorldCueV15.RangedShot, transform.position);
                    _feedback.Particles.Burst(WorldBurstV15.Hit, transform.position + Vector3.up * 0.5f);
                }
            }
            _last = _boss.Current;
        }
        private void OnDisable() { if (_line != null) _line.enabled = false; }
    }
}

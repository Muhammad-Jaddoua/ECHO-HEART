using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Audio
{
    /// <summary>Opts this scene into regional music after the original boss-defeat event.</summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class GuardianMusicResolveV118 : MonoBehaviour
    {
        [SerializeField] private WorldAudioV15 _audio;
        private void Awake()
        {
            if(_audio==null){Debug.LogError("V1.18 Guardian music resolver binding missing.",this);enabled=false;}
        }
        private void OnEnable()=>GameEvents.BossDefeated+=Resolved;
        private void OnDisable()=>GameEvents.BossDefeated-=Resolved;
        private void Resolved(){if(_audio!=null)_audio.EndGuardianMusic();}
    }
}

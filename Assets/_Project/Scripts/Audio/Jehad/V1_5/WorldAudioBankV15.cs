using UnityEngine;

namespace Echoheart.Audio
{
    public enum WorldCueV15
    {
        Relic, Chest, Secret, Capture, Hit, Defeat, PlayerHurt, Death, Restore,
        EnemyWarning, SlamWarning, RangedWarning, Slam, RangedShot, Power,
        StepGrass, StepStone, StepWater, Jump, Land, Sword, Bow, Fire
    }

    [CreateAssetMenu(menuName = "Echoheart/Jehad/World Audio V1.5")]
    public sealed class WorldAudioBankV15 : ScriptableObject
    {
        [SerializeField] private AudioClip[] _music;
        [SerializeField] private AudioClip[] _cues;
        [SerializeField] private AudioClip _water;
        [SerializeField] private AudioClip _forest;
        [SerializeField] private AudioClip _alpine;
        public AudioClip Music(int index) => _music != null && index >= 0 && index < _music.Length ? _music[index] : null;
        public AudioClip Cue(WorldCueV15 cue) => _cues != null && (int)cue >= 0 && (int)cue < _cues.Length ? _cues[(int)cue] : null;
        public AudioClip Water => _water;
        public AudioClip Forest => _forest;
        public AudioClip Alpine => _alpine;
    }
}

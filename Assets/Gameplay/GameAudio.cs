using UnityEngine;

namespace BladeArena
{
    public enum SoundCue { Pickup, Hit, Hurt, Clash, Shield, Upgrade, Wave, Mana, Refill, RoundTable, Charge, Holy, Boss, Death }
    public sealed class GameAudio : MonoBehaviour
    {
        public AudioClip[] clips;
        public AudioSource[] voices;
        [Range(0, 1)] public float volume = .55f;
        readonly float[] nextAllowed = new float[14];
        int nextVoice;
        public void Play(SoundCue cue)
        {
            int i = (int)cue;
            if (voices == null || voices.Length == 0 || clips == null || i >= clips.Length || !clips[i] || Time.unscaledTime < nextAllowed[i]) return;
            nextAllowed[i] = Time.unscaledTime + (cue == SoundCue.Hit || cue == SoundCue.Wave || cue == SoundCue.Clash ? .075f : .12f);
            AudioSource voice = null;
            foreach (var candidate in voices) if (!candidate.isPlaying) { voice = candidate; break; }
            if (!voice) voice = voices[nextVoice++ % voices.Length];
            voice.Stop(); voice.clip = clips[i]; voice.volume = volume * (cue == SoundCue.Hit || cue == SoundCue.Wave ? .45f : 1);
            voice.Play();
        }
    }
}

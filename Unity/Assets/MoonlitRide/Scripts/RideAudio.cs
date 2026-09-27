using System;
using UnityEngine;

namespace MoonlitRide
{
    // Audio-thread synthesizer. No downloaded recordings, per-note clips or runtime allocations.
    [RequireComponent(typeof(AudioSource))]
    public sealed class RideAudio : MonoBehaviour
    {
        public const double Beat = .54;
        static readonly int[][] Chords = { new[] { 50, 57, 62, 65, 69 }, new[] { 46, 53, 58, 62, 65 }, new[] { 53, 60, 65, 69, 72 }, new[] { 48, 55, 60, 64, 67 } };
        struct Voice { public double Start, Duration, Frequency; public float Gain, Pan; }
        readonly Voice[] voices = new Voice[64];
        readonly object gate = new object();
        int nextVoice, sampleRate, beatIndex;
        double time, nextBeat;
        float master, wind;
        uint noise = 12345;
        float[] echo;
        int echoIndex;
        volatile float volume = .55f, speed;
        volatile bool enabledMusic = true, paused, started;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void MoonlitAudioConfigure(int playing, int paused, int sound, float volume, float speed);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void MoonlitAudioCollect(int count, int booster, int bonus);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void MoonlitAudioReset();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void MoonlitAudioDispose();
#endif
        public void Configure(bool playing, bool isPaused, bool enabledSound, float level, float velocity)
        {
            started = playing; paused = isPaused; enabledMusic = enabledSound; volume = level; speed = velocity;
#if UNITY_WEBGL && !UNITY_EDITOR
            MoonlitAudioConfigure(playing ? 1 : 0, isPaused ? 1 : 0, enabledSound ? 1 : 0, level, velocity);
#endif
        }
        void Awake()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            sampleRate = AudioSettings.outputSampleRate;
            echo = new float[Math.Max(1, sampleRate * 3 / 4)];
            var source = GetComponent<AudioSource>(); source.playOnAwake = false; source.loop = true; source.spatialBlend = 0;
            source.clip = AudioClip.Create("Synth driver", sampleRate, 1, sampleRate, false); source.Play();
#endif
        }
        public static int[] ChordAt(double seconds) => Chords[(int)Math.Floor(Math.Max(0, seconds) / (Beat * 16)) % 4];
        public static int PickupNote(double seconds, int count, int i) => ChordAt(seconds + i * Beat / 4)[((count - 1 + i * 2) % 5 + 5) % 5] + 12;
        void Note(int midi, double start, double duration, float gain, float pan = 0)
        {
            voices[nextVoice] = new Voice { Start = start, Duration = duration, Frequency = 440 * Math.Pow(2, (midi - 69) / 12.0), Gain = gain, Pan = pan };
            nextVoice = (nextVoice + 1) % voices.Length;
        }
        public void Collect(int count, bool booster, bool bonus)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MoonlitAudioCollect(count, booster ? 1 : 0, bonus ? 1 : 0);
#else
            lock (gate)
            {
                int length = booster || bonus ? 4 : 2;
                for (int i = 0; i < length; i++) Note(PickupNote(time, bonus ? 1 : count, i) + (bonus && i == 3 ? 12 : 0), time + i * Beat / 4, booster || bonus ? 1.4 : .8, i == 0 ? .085f : .045f, i % 2 == 0 ? -.15f : .15f);
            }
#endif
        }
        public void ResetScore()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MoonlitAudioReset();
#else
            lock (gate) { Array.Clear(voices, 0, voices.Length); Array.Clear(echo, 0, echo.Length); time = nextBeat = 0; beatIndex = nextVoice = echoIndex = 0; wind = 0; }
#endif
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        void OnAudioFilterRead(float[] data, int channels)
        {
            lock (gate)
            {
                double step = 1.0 / sampleRate;
                for (int frame = 0; frame < data.Length; frame += channels)
                {
                    if (started && time >= nextBeat)
                    {
                        var chord = ChordAt(nextBeat);
                        if (beatIndex % 16 == 0) { for (int i = 0; i < 4; i++) Note(chord[i], nextBeat, 9, .022f, (i - 1.5f) * .35f); Note(chord[0] - 12, nextBeat, 8, .035f); }
                        if (beatIndex % 2 == 0) { int pattern = (beatIndex / 2) % 8; int index = pattern == 0 || pattern == 5 ? 2 : pattern == 2 || pattern == 6 ? 3 : pattern == 3 ? 1 : 4; Note(chord[index] + 12, nextBeat, 3.8, .045f, (float)Math.Sin(beatIndex) * .5f); }
                        nextBeat += Beat; beatIndex++;
                    }
                    double left = 0, right = 0;
                    for (int i = 0; i < voices.Length; i++)
                    {
                        var v = voices[i]; double age = time - v.Start;
                        if (v.Duration <= 0 || age < 0 || age >= v.Duration) continue;
                        double envelope = Math.Min(1, age / .025) * Math.Pow(1 - age / v.Duration, 2);
                        double value = Math.Sin(age * v.Frequency * Math.PI * 2) * v.Gain * envelope;
                        left += value * (1 - v.Pan * .5); right += value * (1 + v.Pan * .5);
                    }
                    noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                    wind += (((noise / (float)uint.MaxValue) * 2 - 1) - wind) * .035f;
                    float windGain = Math.Min(speed / 20, 1); windGain = windGain * windGain * .12f;
                    float wet = echo[echoIndex]; echo[echoIndex] = (float)(left + right) * .16f + wet * .35f;
                    echoIndex = (echoIndex + 1) % echo.Length;
                    float target = started && enabledMusic ? volume * (paused ? .3f : 1) : 0;
                    master += (target - master) * .0001f;
                    for (int channel = 0; channel < channels; channel++) data[frame + channel] = (float)Math.Tanh(((channel % 2 == 0 ? left : right) + wet + wind * windGain) * master);
                    if (started) time += step;
                }
            }
        }
#endif
        void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MoonlitAudioDispose();
#else
            var source = GetComponent<AudioSource>(); if (source.clip) Destroy(source.clip);
#endif
        }
    }
}

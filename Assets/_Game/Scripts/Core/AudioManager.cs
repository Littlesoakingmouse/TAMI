using System.Collections.Generic;
using UnityEngine;

namespace Tami
{
    public enum Sfx { Click, Coin, Build, Upgrade, Gong, Chime, Error }

    /// <summary>
    /// Nhạc nền là các bài trong GameArt.musicTracks, phát lần lượt theo thứ tự xáo (thiếu thì dùng vòng nhạc
    /// tổng hợp). Hiệu ứng vẫn tổng hợp bằng code (GDD mục 8.5). Âm lượng chỉnh riêng theo từng loại trong Cài đặt.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // Mức gốc khi thanh trượt ở 100%: nhạc nền nhỏ hơn hiệu ứng để không át tiếng xu.
        const float MusicBase = 0.5f;
        const float SfxBase = 0.6f;

        AudioSource music, sfx;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        SettingsData settings;
        float lastCoinTime;
        AudioClip[] tracks;
        readonly List<int> playlist = new List<int>();
        int lastTrack = -1;

        public void Init(SettingsData s)
        {
            settings = s;
            // Save bản cũ chỉ có công tắc bật/tắt: tắt thì chuyển thành âm lượng 0.
            if (!s.music) { s.musicVolume = 0f; s.music = true; }
            if (!s.sfx) { s.effectVolume = s.coinVolume = s.uiVolume = 0f; s.sfx = true; }

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            clips[Sfx.Click] = Synth.Click();
            clips[Sfx.Coin] = Synth.Coin();
            clips[Sfx.Build] = Synth.Build();
            clips[Sfx.Upgrade] = Synth.Upgrade();
            clips[Sfx.Gong] = Synth.Gong();
            clips[Sfx.Chime] = Synth.Chime();
            clips[Sfx.Error] = Synth.Error();

            tracks = Game.I.art.musicTracks;
            if (tracks == null || tracks.Length == 0)
            {
                music.clip = Synth.Music();
                music.loop = true;
                music.Play();
            }
            else NextTrack();
            Apply();
        }

        /// <summary>Áp âm lượng hiện tại (gọi lại mỗi khi kéo thanh trượt).</summary>
        public void Apply()
        {
            if (music == null) return;
            music.volume = settings.masterVolume * settings.musicVolume * MusicBase;
        }

        void Update()
        {
            // Hết bài thì sang bài kế. Khi app bị ẩn (điện thoại) nhạc tạm dừng, không tính là hết bài.
            if (music == null || music.loop || music.isPlaying || !Application.isFocused) return;
            NextTrack();
        }

        void NextTrack()
        {
            // Phát hết lượt các bài theo thứ tự xáo rồi xáo lại; bài đầu lượt mới không trùng bài vừa nghe.
            if (playlist.Count == 0)
            {
                for (int i = 0; i < tracks.Length; i++) playlist.Add(i);
                for (int i = playlist.Count - 1; i > 0; i--)
                {
                    int k = Random.Range(0, i + 1);
                    (playlist[i], playlist[k]) = (playlist[k], playlist[i]);
                }
                if (playlist.Count > 1 && playlist[0] == lastTrack) (playlist[0], playlist[1]) = (playlist[1], playlist[0]);
            }
            lastTrack = playlist[0];
            playlist.RemoveAt(0);
            music.clip = tracks[lastTrack];
            music.Play();
        }

        float GroupVolume(Sfx s)
        {
            switch (s)
            {
                case Sfx.Coin: return settings.coinVolume;
                case Sfx.Click:
                case Sfx.Chime:
                case Sfx.Error: return settings.uiVolume;
                default: return settings.effectVolume; // dựng quầy, nâng cấp, chiêng
            }
        }

        public void Play(Sfx s)
        {
            if (settings == null || sfx == null) return;
            float volume = settings.masterVolume * GroupVolume(s) * SfxBase;
            if (volume <= 0.001f) return;
            if (s == Sfx.Coin)
            {
                if (Time.unscaledTime - lastCoinTime < 0.06f) return;
                lastCoinTime = Time.unscaledTime;
            }
            if (clips.TryGetValue(s, out var clip)) sfx.PlayOneShot(clip, volume);
        }
    }

    static class Synth
    {
        const int Rate = 22050;
        const float TwoPi = Mathf.PI * 2f;

        static float Sine(float hz, float t) => Mathf.Sin(TwoPi * hz * t);
        static float Env(float t, float attack, float decay) => t < 0f ? 0f : t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);

        static AudioClip Make(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static System.Func<float> Noise(int seed)
        {
            var rng = new System.Random(seed);
            return () => (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        public static AudioClip Click() => Make("click", 0.06f, t => Sine(880f, t) * Env(t, 0.002f, 0.015f) * 0.35f);

        public static AudioClip Coin() => Make("coin", 0.3f, t =>
            Sine(1318f, t) * Env(t, 0.003f, 0.03f) * 0.45f + Sine(1760f, t) * Env(t - 0.07f, 0.003f, 0.07f) * 0.45f);

        public static AudioClip Build()
        {
            var noise = Noise(1);
            return Make("build", 0.35f, t => noise() * Env(t, 0.001f, 0.02f) * 0.4f + Sine(196f, t) * Env(t, 0.002f, 0.09f) * 0.6f);
        }

        public static AudioClip Upgrade()
        {
            var noise = Noise(2);
            return Make("upgrade", 0.8f, t =>
                Sine(110f * (1f + Mathf.Exp(-t * 30f)), t) * Env(t, 0.002f, 0.12f) * 0.7f
                + noise() * Env(t, 0.001f, 0.03f) * 0.25f
                + Sine(523f, t) * Env(t - 0.12f, 0.005f, 0.08f) * 0.3f
                + Sine(659f, t) * Env(t - 0.22f, 0.005f, 0.08f) * 0.3f
                + Sine(784f, t) * Env(t - 0.32f, 0.005f, 0.15f) * 0.3f);
        }

        public static AudioClip Gong() => Make("gong", 2.6f, t =>
            (Sine(98f, t) + Sine(147.5f, t) * 0.7f + Sine(196.8f, t) * 0.5f + Sine(262f, t) * 0.3f) * Env(t, 0.01f, 0.9f) * 0.22f);

        public static AudioClip Chime() => Make("chime", 0.8f, t =>
            (Sine(784f, t) * Env(t, 0.002f, 0.15f) + Sine(880f, t) * Env(t - 0.08f, 0.002f, 0.15f)
             + Sine(1047f, t) * Env(t - 0.16f, 0.002f, 0.15f) + Sine(1175f, t) * Env(t - 0.24f, 0.002f, 0.25f)) * 0.3f);

        public static AudioClip Error() => Make("error", 0.25f, t => (Sine(220f, t) + Sine(233f, t)) * Env(t, 0.005f, 0.08f) * 0.3f);

        /// <summary>Vòng nhạc 30 giây: giai điệu ngũ cung luyến láy, bè trầm và nhịp gõ nhẹ.</summary>
        public static AudioClip Music()
        {
            const float beat = 0.625f; // 96 BPM
            const int beats = 48;
            float length = beat * beats;
            int n = Mathf.CeilToInt(length * Rate);
            var data = new float[n];
            var rng = new System.Random(7);
            float[] scale = { 261.6f, 293.7f, 329.6f, 392f, 440f, 523.3f, 587.3f, 659.3f };
            float[] bass = { 65.4f, 98f, 87.3f, 98f };

            int note = 3;
            for (int b = 0; b < beats; b++)
            {
                float start = b * beat;
                // Giai điệu: đi từng bậc, thỉnh thoảng nghỉ.
                if (rng.NextDouble() > 0.2)
                {
                    note = Mathf.Clamp(note + rng.Next(-2, 3), 0, scale.Length - 1);
                    float dur = rng.NextDouble() > 0.7 ? beat * 2f : beat;
                    AddNote(data, start, dur, scale[note], 0.16f, true);
                }
                // Bè trầm mỗi nửa ô nhịp.
                if (b % 2 == 0) AddNote(data, start, beat * 1.8f, bass[(b / 4) % bass.Length], 0.12f, false);
                // Nhịp gõ nhẹ.
                AddTick(data, start, b % 4 == 0 ? 0.08f : 0.04f, rng);
            }
            var clip = AudioClip.Create("music", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddNote(float[] data, float start, float dur, float hz, float volume, bool vibrato)
        {
            int s = Mathf.FloorToInt(start * Rate);
            int e = Mathf.Min(data.Length, Mathf.FloorToInt((start + dur + 0.3f) * Rate));
            float phase = 0f;
            for (int i = s; i < e; i++)
            {
                float t = (i - s) / (float)Rate;
                // Luyến láy: rung cao độ sau khi nốt vang lên, giống tay nhấn cần đàn bầu.
                float bend = vibrato && t > 0.15f ? 1f + 0.008f * Mathf.Sin(TwoPi * 5.5f * t) : 1f;
                phase += TwoPi * hz * bend / Rate;
                float env = t < 0.02f ? t / 0.02f : Mathf.Exp(-(t - 0.02f) / (dur * 0.6f));
                data[i] += (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * env * volume;
            }
        }

        static void AddTick(float[] data, float start, float volume, System.Random rng)
        {
            int s = Mathf.FloorToInt(start * Rate);
            int e = Mathf.Min(data.Length, s + Rate / 20);
            for (int i = s; i < e; i++)
            {
                float t = (i - s) / (float)Rate;
                data[i] += (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t / 0.01f) * volume;
            }
        }
    }
}

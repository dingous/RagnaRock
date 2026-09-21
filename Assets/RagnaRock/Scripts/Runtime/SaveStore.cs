using System;
using System.Security.Cryptography;
using System.Text;
using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock
{
    [Serializable]
    public sealed class GameOptions
    {
        public int version = 1;
        public float master = .7f, music = .48f, effects = .65f;
        public bool reducedMotion, mute, showDamage = true;
        public int quality = 1;
        public void Sanitize()
        {
            master = SafeVolume(master, .7f); music = SafeVolume(music, .48f); effects = SafeVolume(effects, .65f);
            quality = Mathf.Clamp(quality, 0, 2);
        }
        private static float SafeVolume(float value, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value); }
    }

    public sealed class SaveStore
    {
        private readonly string Slot, Backup, OptionsKey, BestScoreKey, BestWaveKey;
        public SaveStore(string keyNamespace = "RagnaRock")
        {
            if (string.IsNullOrWhiteSpace(keyNamespace)) throw new ArgumentException("Namespace de save vazio.");
            Slot = keyNamespace + ".Run.v1"; Backup = keyNamespace + ".Run.backup.v1";
            OptionsKey = keyNamespace + ".Options.v1";
            BestScoreKey = keyNamespace + ".BestScore"; BestWaveKey = keyNamespace + ".BestWave";
        }
        public string LastWarning { get; private set; }
        public int BestScore { get { return Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0)); } }
        public int BestWave { get { return Mathf.Max(0, PlayerPrefs.GetInt(BestWaveKey, 0)); } }
        public GameOptions LoadOptions()
        {
            try
            {
                var options = JsonUtility.FromJson<GameOptions>(PlayerPrefs.GetString(OptionsKey, "")) ?? new GameOptions();
                if (options.version != 1) return new GameOptions();
                options.Sanitize(); return options;
            }
            catch (Exception ex) { Warn("Preferências recuperadas com os valores padrão.", ex); return new GameOptions(); }
        }
        public void StoreOptions(GameOptions options)
        {
            options.Sanitize();
            try { PlayerPrefs.SetString(OptionsKey, JsonUtility.ToJson(options)); PlayerPrefs.Save(); }
            catch (Exception ex) { Warn("Não foi possível salvar as preferências.", ex); }
        }
        public RunCheckpoint Load(int totalWaves)
        {
            var result = Decode(PlayerPrefs.GetString(Slot, ""), totalWaves);
            if (result != null) return result;
            result = Decode(PlayerPrefs.GetString(Backup, ""), totalWaves);
            if (result != null) LastWarning = "O último checkpoint estava indisponível. Backup recuperado.";
            return result;
        }
        public void Save(RunCheckpoint checkpoint, int totalWaves)
        {
            if (checkpoint == null || !checkpoint.IsValid(totalWaves))
                throw new ArgumentException("Checkpoint inválido: salvamento recusado.");
            try
            {
                string existing = PlayerPrefs.GetString(Slot, "");
                if (Decode(existing, totalWaves) != null) PlayerPrefs.SetString(Backup, existing);
                string json = JsonUtility.ToJson(checkpoint);
                PlayerPrefs.SetString(Slot, Digest(json) + "\n" + json); PlayerPrefs.Save();
            }
            catch (Exception ex) { Warn("O checkpoint não pôde ser salvo neste dispositivo.", ex); }
        }
        public void Complete(int score, int wave)
        {
            try
            {
                PlayerPrefs.SetInt(BestScoreKey, Mathf.Max(BestScore, score));
                PlayerPrefs.SetInt(BestWaveKey, Mathf.Max(BestWave, wave));
                Clear();
            }
            catch (Exception ex) { Warn("O recorde não pôde ser salvo.", ex); }
        }
        public void Clear()
        {
            try { PlayerPrefs.DeleteKey(Slot); PlayerPrefs.DeleteKey(Backup); PlayerPrefs.Save(); }
            catch (Exception ex) { Warn("Não foi possível limpar o checkpoint.", ex); }
        }
        private RunCheckpoint Decode(string payload, int totalWaves)
        {
            if (string.IsNullOrEmpty(payload)) return null;
            if (payload.Length > 65536) { LastWarning = "Checkpoint inválido ignorado."; return null; }
            try
            {
                int separator = payload.IndexOf('\n');
                if (separator != 64) return null;
                string json = payload.Substring(separator + 1);
                if (!string.Equals(Digest(json), payload.Substring(0, separator), StringComparison.Ordinal))
                { LastWarning = "Checkpoint corrompido ignorado."; return null; }
                var data = JsonUtility.FromJson<RunCheckpoint>(json);
                return data != null && data.IsValid(totalWaves) ? data : null;
            }
            catch (Exception ex) { Warn("Checkpoint inválido ignorado.", ex); return null; }
        }
        private static string Digest(string text)
        {
            using (var hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                var output = new StringBuilder(64);
                foreach (byte b in bytes) output.Append(b.ToString("x2"));
                return output.ToString();
            }
        }
        private void Warn(string message, Exception ex)
        { LastWarning = message; Debug.LogWarning(message + " " + ex.GetType().Name); }
    }
}

using System;
using System.Collections.Generic;

namespace RagnaRock.Core
{
    public enum BandRole { Voice, Guitar, Bass, Drums }
    public enum EnemyKind { Shambler, Rusher, Brute, Spitter, Shielded, Splitter, Summoner, Boss }
    public enum RunMode { Menu, Intermission, Playing, Upgrade, Paused, Defeat, Victory }
    public enum UpgradeKind { Voice, Guitar, Bass, Drums, Amplifier, Tempo, Range, Armor, Vitality, Magnet, Critical, Regeneration }

    [Serializable]
    public sealed class CampaignDefinition
    {
        public int schemaVersion = 1;
        public int wavesPerChapter = 3;
        public ChapterDefinition[] chapters;

        public int TotalWaves { get { return chapters == null ? 0 : chapters.Length * wavesPerChapter; } }
        public void Validate()
        {
            if (schemaVersion != 1 || wavesPerChapter < 1 || wavesPerChapter > 10)
                throw new InvalidOperationException("Versão ou tamanho de campanha inválido.");
            if (chapters == null || chapters.Length == 0 || chapters.Length > 100)
                throw new InvalidOperationException("A campanha precisa conter atos.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chapter in chapters)
            {
                if (chapter == null || string.IsNullOrWhiteSpace(chapter.id) || !ids.Add(chapter.id))
                    throw new InvalidOperationException("Ato nulo ou ID repetido.");
                if (string.IsNullOrWhiteSpace(chapter.title) || string.IsNullOrWhiteSpace(chapter.style))
                    throw new InvalidOperationException("Ato sem título ou vertente.");
                if (chapter.bpm < 50 || chapter.bpm > 300 || chapter.enemyFamily < 0 || chapter.enemyFamily > 6)
                    throw new InvalidOperationException("BPM ou família de inimigos inválidos.");
                if (chapter.colorHex == null || chapter.colorHex.Length != 7 || chapter.colorHex[0] != '#')
                    throw new InvalidOperationException("Cor do ato inválida.");
                for (int i = 1; i < chapter.colorHex.Length; i++)
                    if (!Uri.IsHexDigit(chapter.colorHex[i])) throw new InvalidOperationException("Cor hexadecimal inválida.");
            }
        }
    }

    [Serializable]
    public sealed class ChapterDefinition
    {
        public string id, title, style, period, colorHex, environment, lore, tribute, bossName, songReference;
        public int bpm, enemyFamily;
    }

    public readonly struct WavePlan
    {
        public readonly int Number, ChapterIndex, Quota;
        public readonly bool HasBoss;
        public readonly float HealthScale, SpeedScale, SpawnInterval, DamageScale;
        public WavePlan(int index, CampaignDefinition campaign)
        {
            if (campaign == null || index < 0 || index >= campaign.TotalWaves)
                throw new ArgumentOutOfRangeException(nameof(index));
            Number = index + 1;
            ChapterIndex = index / campaign.wavesPerChapter;
            Quota = Math.Min(132, 10 + index * 2 + ChapterIndex * 2);
            HasBoss = Number % campaign.wavesPerChapter == 0;
            HealthScale = 1f + index * .063f + (float)Math.Pow(index, 1.45) * .003f;
            SpeedScale = Math.Min(2f, 1f + index * .017f);
            SpawnInterval = Math.Max(.15f, .85f - index * .0135f);
            DamageScale = 1f + index * .012f;
        }
    }

    /// <summary>Small explicit PRNG: predictable seeded runs; independent from scene/VFX randomness.</summary>
    public sealed class RunRandom
    {
        public uint State { get; private set; }
        public RunRandom(uint seed) { State = seed == 0 ? 0x6D2B79F5u : seed; }
        public uint NextUInt()
        {
            uint x = State;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return State = x;
        }
        public float Value() { return (NextUInt() >> 8) * (1f / 16777216f); }
        public int Range(int min, int max)
        {
            if (max <= min) throw new ArgumentOutOfRangeException(nameof(max));
            return min + (int)(NextUInt() % (uint)(max - min));
        }
    }

    [Serializable]
    public sealed class BandStats
    {
        public int[] ranks = new int[12];
        public int level = 1, experience, pendingChoices;
        public BandStats() { for (int i = 0; i < 4; i++) ranks[i] = 1; }
        public int Rank(UpgradeKind kind) { return ranks[(int)kind]; }
        public int WeaponRank(BandRole role) { return ranks[(int)role]; }
        public float MaxHealth { get { return 180f + Rank(UpgradeKind.Vitality) * 32f; } }
        public float DamageMultiplier { get { return 1f + Rank(UpgradeKind.Amplifier) * .14f; } }
        public float TempoMultiplier { get { return 1f + Rank(UpgradeKind.Tempo) * .085f; } }
        public float RangeBonus { get { return Rank(UpgradeKind.Range) * .9f; } }
        public float DamageReduction { get { return Rank(UpgradeKind.Armor) * .065f; } }
        public float CriticalChance { get { return .06f + Rank(UpgradeKind.Critical) * .045f; } }
        public float MagnetSpeed { get { return 9f + Rank(UpgradeKind.Magnet) * 2.2f; } }
        public int RequiredExperience { get { return ExperienceForLevel(level); } }
        public static int ExperienceForLevel(int level) { return 22 + Math.Max(0, level - 1) * 11; }
        public static int MaxRank(UpgradeKind kind) { return (int)kind < 4 ? 8 : 5; }
        public bool CanUpgrade(UpgradeKind kind) { return ranks[(int)kind] < MaxRank(kind); }
        public bool Apply(UpgradeKind kind)
        {
            if (!CanUpgrade(kind)) return false;
            ranks[(int)kind]++;
            return true;
        }
        public int AddExperience(int amount)
        {
            if (amount <= 0) return 0;
            experience = (int)Math.Min(1000000L, (long)experience + amount);
            int gained = 0;
            while (level < 200 && experience >= RequiredExperience)
            {
                experience -= RequiredExperience; level++; gained++; pendingChoices++;
            }
            return gained;
        }
        public bool Validate()
        {
            if (ranks == null || ranks.Length != 12 || level < 1 || level > 200 || experience < 0 ||
                experience > 1000000 || pendingChoices < 0 || pendingChoices > 200) return false;
            for (int i = 0; i < ranks.Length; i++)
                if (ranks[i] < (i < 4 ? 1 : 0) || ranks[i] > MaxRank((UpgradeKind)i)) return false;
            return true;
        }
        public BandStats Copy()
        {
            return new BandStats { ranks = (int[])ranks.Clone(), level = level,
                experience = experience, pendingChoices = pendingChoices };
        }
    }

    public static class UpgradeDraft
    {
        public static UpgradeKind[] Draw(BandStats stats, RunRandom random, int count = 3)
        {
            if (stats == null || random == null || count < 1) throw new ArgumentException("Draft inválido.");
            var candidates = new List<UpgradeKind>(12);
            for (int i = 0; i < 12; i++) if (stats.CanUpgrade((UpgradeKind)i)) candidates.Add((UpgradeKind)i);
            int length = Math.Min(count, candidates.Count);
            var result = new UpgradeKind[length];
            for (int i = 0; i < length; i++)
            {
                int index = random.Range(0, candidates.Count);
                result[i] = candidates[index]; candidates.RemoveAt(index);
            }
            return result;
        }
    }

    public static class CombatMath
    {
        public static float BaseHealth(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Rusher: return 13;
                case EnemyKind.Brute: return 60;
                case EnemyKind.Spitter: return 25;
                case EnemyKind.Shielded: return 44;
                case EnemyKind.Splitter: return 32;
                case EnemyKind.Summoner: return 62;
                case EnemyKind.Boss: return 340;
                default: return 19;
            }
        }
        public static float Speed(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Rusher: return 2.4f;
                case EnemyKind.Brute: return .83f;
                case EnemyKind.Boss: return .65f;
                case EnemyKind.Spitter: return 1.05f;
                default: return 1.25f;
            }
        }
        public static float WeaponDamage(BandRole role, int rank)
        {
            float basis = role == BandRole.Guitar ? 17f : role == BandRole.Bass ? 13f : role == BandRole.Drums ? 28f : 16f;
            return basis * (1f + (rank - 1) * .32f) * (rank == 8 ? 1.3f : 1f);
        }
        public static float WeaponCooldown(BandRole role, int rank)
        {
            float basis = role == BandRole.Guitar ? .62f : role == BandRole.Bass ? 2.3f : role == BandRole.Drums ? 1.55f : 1.25f;
            return basis / (1f + (rank - 1) * .085f);
        }
        public static float ApplyArmor(float incoming, float reduction)
        {
            return Math.Max(0f, incoming) * (1f - Math.Min(.75f, Math.Max(0f, reduction)));
        }
        public static EnemyKind ChooseEnemy(int waveIndex, int family, RunRandom random)
        {
            if (waveIndex < 2) return random.Value() < .16f ? EnemyKind.Rusher : EnemyKind.Shambler;
            float roll = random.Value();
            if (roll < .32f) return EnemyKind.Shambler;
            if (roll < .51f) return EnemyKind.Rusher;
            if (roll < .65f) return EnemyKind.Brute;
            if (waveIndex < 6) return EnemyKind.Shielded;
            if (roll < .77f) return EnemyKind.Spitter;
            if (waveIndex < 12) return EnemyKind.Shielded;
            if (roll < .91f) return (EnemyKind)Math.Max(2, Math.Min(6, family));
            return waveIndex < 21 ? EnemyKind.Splitter : EnemyKind.Summoner;
        }
        public static float SegmentDistanceSquared(float px, float pz, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az, denominator = dx * dx + dz * dz;
            float t = denominator < .00001f ? 0f : Math.Max(0f, Math.Min(1f, ((px - ax) * dx + (pz - az) * dz) / denominator));
            float ex = px - ax - dx * t, ez = pz - az - dz * t;
            return ex * ex + ez * ez;
        }
    }

    [Serializable]
    public sealed class RunCheckpoint
    {
        public int version = 1;
        public int waveIndex, kills, score;
        public uint seed;
        public float health, fury, elapsed;
        public BandStats stats;
        public bool IsValid(int totalWaves)
        {
            return version == 1 && waveIndex >= 0 && waveIndex < totalWaves && seed != 0 && stats != null &&
                stats.Validate() && IsFinite(health) && health > 0 && health <= stats.MaxHealth &&
                IsFinite(fury) && fury >= 0 && fury <= 100 && IsFinite(elapsed) && elapsed >= 0 &&
                elapsed <= 10000000 && kills >= 0 && score >= 0;
        }
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}

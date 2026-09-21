using System;
using System.Linq;
using NUnit.Framework;
using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock.Tests
{
    public sealed class RulesTests
    {
        private static CampaignDefinition Campaign()
        {
            return JsonUtility.FromJson<CampaignDefinition>(Resources.Load<TextAsset>("Campaign").text);
        }
        [Test] public void CampaignHasEighteenActsAndFiftyFourWaves()
        {
            var campaign = Campaign(); campaign.Validate();
            Assert.That(campaign.chapters.Length, Is.EqualTo(18));
            Assert.That(campaign.TotalWaves, Is.EqualTo(54));
            Assert.That(campaign.chapters[0].id, Is.EqualTo("roots"));
            Assert.That(campaign.chapters[17].id, Is.EqualTo("brutal-death"));
        }
        [Test] public void EveryThirdWaveHasABossAndDifficultyNeverDecreases()
        {
            var campaign = Campaign(); var previous = new WavePlan(0,campaign);
            for (int index=0;index<campaign.TotalWaves;index++)
            {
                var current = new WavePlan(index,campaign);
                Assert.That(current.HasBoss, Is.EqualTo((index+1)%3==0));
                Assert.That(current.Quota, Is.InRange(previous.Quota,132));
                Assert.That(current.HealthScale, Is.GreaterThanOrEqualTo(previous.HealthScale));
                Assert.That(current.SpeedScale, Is.InRange(1f,2f));
                Assert.That(current.SpawnInterval, Is.InRange(.15f,previous.SpawnInterval));
                previous=current;
            }
        }
        [Test] public void SameSeedProducesSameRandomSequence()
        {
            var a=new RunRandom(123);var b=new RunRandom(123);
            for(int i=0;i<10000;i++)Assert.That(a.NextUInt(),Is.EqualTo(b.NextUInt()));
            Assert.That(new RunRandom(0).State,Is.Not.EqualTo(0));
        }
        [Test] public void RandomStateCanBeRestored()
        {
            var a=new RunRandom(775);for(int i=0;i<87;i++)a.NextUInt();
            var b=new RunRandom(a.State);
            for(int i=0;i<1000;i++)Assert.That(a.NextUInt(),Is.EqualTo(b.NextUInt()));
        }
        [Test] public void DraftNeverRepeatsOrOffersMaxedEquipment()
        {
            var stats=new BandStats();var random=new RunRandom(553);
            while(true)
            {
                var draft=UpgradeDraft.Draw(stats,random);
                Assert.That(draft.Distinct().Count(),Is.EqualTo(draft.Length));
                foreach(var item in draft)Assert.That(stats.CanUpgrade(item),Is.True);
                if(draft.Length==0)break;
                Assert.That(stats.Apply(draft[0]),Is.True);
            }
            Assert.That(stats.Validate(),Is.True);
            for(int i=0;i<12;i++)Assert.That(stats.ranks[i],Is.EqualTo(BandStats.MaxRank((UpgradeKind)i)));
        }
        [Test] public void ExcessExperienceQueuesAllChoicesAndCopiesAreIndependent()
        {
            var stats=new BandStats();Assert.That(stats.AddExperience(55),Is.EqualTo(2));
            Assert.That(stats.level,Is.EqualTo(3));Assert.That(stats.experience,Is.EqualTo(0));
            Assert.That(stats.pendingChoices,Is.EqualTo(2));
            var copy=stats.Copy();copy.ranks[0]++;
            Assert.That(stats.ranks[0],Is.EqualTo(1));
            Assert.That(stats.AddExperience(-10),Is.EqualTo(0));
        }
        [Test] public void WeaponDamageIncreasesAtEveryRank()
        {
            foreach(BandRole role in Enum.GetValues(typeof(BandRole)))
                for(int rank=2;rank<=8;rank++)
                {
                    Assert.That(CombatMath.WeaponDamage(role,rank),Is.GreaterThan(CombatMath.WeaponDamage(role,rank-1)));
                    Assert.That(CombatMath.WeaponCooldown(role,rank),Is.LessThan(CombatMath.WeaponCooldown(role,rank-1)));
                }
        }
        [Test] public void ArmorAndZeroLengthSegmentsAreSafe()
        {
            Assert.That(CombatMath.ApplyArmor(100,99),Is.EqualTo(25));
            Assert.That(CombatMath.ApplyArmor(-10,.4f),Is.EqualTo(0));
            Assert.That(CombatMath.SegmentDistanceSquared(3,4,0,0,0,0),Is.EqualTo(25));
            Assert.That(CombatMath.SegmentDistanceSquared(4,2,0,0,10,0),Is.EqualTo(4));
        }
        [Test] public void CheckpointRejectsNonFiniteAndOutOfRangeValues()
        {
            var data=new RunCheckpoint{seed=17,stats=new BandStats(),health=180};
            Assert.That(data.IsValid(54),Is.True);
            data.health=float.NaN;Assert.That(data.IsValid(54),Is.False);
            data.health=180;data.waveIndex=54;Assert.That(data.IsValid(54),Is.False);
            data.waveIndex=0;data.stats.ranks[0]=9;Assert.That(data.IsValid(54),Is.False);
        }
        [Test] public void InvalidCampaignIsRejected()
        {
            var c=Campaign();c.chapters[1].id=c.chapters[0].id;
            Assert.Throws<InvalidOperationException>(()=>c.Validate());
            c=Campaign();c.chapters[0].colorHex="#GGGGGG";
            Assert.Throws<InvalidOperationException>(()=>c.Validate());
        }
        [Test] public void EnemySelectionNeverReturnsABoss()
        {
            var rng=new RunRandom(413);
            for(int wave=0;wave<54;wave++)for(int i=0;i<200;i++)
                Assert.That((int)CombatMath.ChooseEnemy(wave,6,rng),Is.InRange(0,6));
        }
    }
}

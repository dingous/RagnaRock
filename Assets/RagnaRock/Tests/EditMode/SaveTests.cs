using System;
using NUnit.Framework;
using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock.Tests
{
    public sealed class SaveTests
    {
        private string prefix;
        private SaveStore store;
        [SetUp] public void Setup(){prefix="RagnaRock.Tests."+Guid.NewGuid().ToString("N");store=new SaveStore(prefix);}
        [TearDown] public void Cleanup()
        {
            foreach(var suffix in new[]{".Run.v1",".Run.backup.v1",".Options.v1",".BestScore",".BestWave"})
                PlayerPrefs.DeleteKey(prefix+suffix);
            PlayerPrefs.Save();
        }
        private static RunCheckpoint Data(int wave)
        {return new RunCheckpoint{seed=71,stats=new BandStats(),health=180,waveIndex=wave};}
        [Test] public void CorruptPrimaryRestoresPreviousCheckpoint()
        {
            store.Save(Data(2),54);store.Save(Data(3),54);
            PlayerPrefs.SetString(prefix+".Run.v1","not a valid save");
            Assert.That(store.Load(54).waveIndex,Is.EqualTo(2));
        }
        [Test] public void InvalidCheckpointDoesNotReplaceGoodSave()
        {
            store.Save(Data(2),54);var bad=Data(3);bad.health=float.PositiveInfinity;
            Assert.Throws<ArgumentException>(()=>store.Save(bad,54));
            Assert.That(store.Load(54).waveIndex,Is.EqualTo(2));
        }
        [Test] public void CompleteKeepsBestAndClearsRun()
        {
            store.Save(Data(1),54);store.Complete(700,9);store.Complete(120,2);
            Assert.That(store.BestScore,Is.EqualTo(700));Assert.That(store.BestWave,Is.EqualTo(9));
            Assert.That(store.Load(54),Is.Null);
        }
        [Test] public void OptionVolumesAreSanitized()
        {
            var options=new GameOptions{master=float.NaN,music=4,effects=-1,quality=12};options.Sanitize();
            Assert.That(options.master,Is.EqualTo(.7f));Assert.That(options.music,Is.EqualTo(1));
            Assert.That(options.effects,Is.EqualTo(0));Assert.That(options.quality,Is.EqualTo(2));
        }
    }
}

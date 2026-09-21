using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RagnaRock.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RagnaRock.Tests
{
    public sealed class SessionTests
    {
        private GameSession game;
        private GameObject root;
        private string prefix;
        [UnitySetUp] public IEnumerator Setup()
        {
            // Unity Test Runner opens an isolated test scene. Never load the player's saved game.
            Assert.That(GameSession.Instance,Is.Null,"Feche a sessão em Play antes de executar os testes.");
            prefix="RagnaRock.Tests."+Guid.NewGuid().ToString("N");
            root=new GameObject("RagnaRock isolated play test");
            game=root.AddComponent<GameSession>();game.Initialize(prefix);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null)Object.Destroy(root);
            yield return null;
            foreach(var suffix in new[]{".Run.v1",".Run.backup.v1",".Options.v1",".BestScore",".BestWave"})
                PlayerPrefs.DeleteKey(prefix+suffix);
            PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator BootsAndKeepsFourMusiciansStationary()
        {
            Assert.That(game.Mode,Is.EqualTo(RunMode.Menu));Assert.That(game.Art.Band.Length,Is.EqualTo(4));
            var positions=new Vector3[4];for(int i=0;i<4;i++)positions[i]=game.Art.Band[i].Root.position;
            game.StartNewRun();game.BeginWave();yield return new WaitForSecondsRealtime(.8f);
            Assert.That(game.Spawned,Is.GreaterThan(0));
            for(int i=0;i<4;i++)Assert.That(Vector3.Distance(positions[i],game.Art.Band[i].Root.position),Is.LessThan(.0001f));
        }
        [UnityTest] public IEnumerator PauseFreezesSimulationAndCanResume()
        {
            game.StartNewRun();game.BeginWave();yield return null;
            game.TogglePause();float clock=game.Clock;int spawned=game.Spawned;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(game.Clock,Is.EqualTo(clock));Assert.That(game.Spawned,Is.EqualTo(spawned));
            game.TogglePause();yield return null;Assert.That(game.Mode,Is.EqualTo(RunMode.Playing));
        }
        [UnityTest] public IEnumerator SuperProtectsStageAndConsumesFury()
        {
            game.StartNewRun();game.BeginWave();game.Collect(PickupKind.Fury,100);game.ActivateSuper();
            float hp=game.Health;game.DamageStage(1000);yield return null;
            Assert.That(game.Health,Is.EqualTo(hp));Assert.That(game.Fury,Is.EqualTo(0));
            Assert.That(game.InvulnerableUntil,Is.GreaterThan(game.Clock));
        }
        [UnityTest] public IEnumerator RestartClearsWorldAndStartsFirstWave()
        {
            game.StartNewRun();game.BeginWave();yield return new WaitForSecondsRealtime(.9f);
            Assert.That(game.Enemies.Count,Is.GreaterThan(0));
            game.StartNewRun();yield return null;
            Assert.That(game.Mode,Is.EqualTo(RunMode.Intermission));Assert.That(game.WaveIndex,Is.EqualTo(0));
            Assert.That(game.Enemies.Count,Is.EqualTo(0));Assert.That(game.Health,Is.EqualTo(180));
        }
        [UnityTest] public IEnumerator MaxEquipmentDoesNotTrapDraftScreen()
        {
            game.StartNewRun();game.BeginWave();
            for(int i=0;i<12;i++)game.Stats.ranks[i]=BandStats.MaxRank((UpgradeKind)i);
            game.Stats.pendingChoices=2;
            var show=typeof(GameSession).GetMethod("ShowDraft",BindingFlags.NonPublic|BindingFlags.Instance);
            Assert.That(show,Is.Not.Null);show.Invoke(game,null);yield return null;
            Assert.That(game.Mode,Is.EqualTo(RunMode.Playing));Assert.That(game.Stats.pendingChoices,Is.EqualTo(0));
        }
        [UnityTest] public IEnumerator PoolRejectsEnemiesBeyondCap()
        {
            game.StartNewRun();game.BeginWave();
            for(int i=0;i<EnemyWorld.MaximumAlive;i++)Assert.That(game.Enemies.Spawn(EnemyKind.Shambler,new Vector3(15,0,0)),Is.True);
            Assert.That(game.Enemies.Spawn(EnemyKind.Shambler,new Vector3(15,0,0)),Is.False);
            Assert.That(game.Enemies.Count,Is.EqualTo(EnemyWorld.MaximumAlive));yield return null;
        }
    }
}

using System;
using RagnaRock.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RagnaRock
{
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public CampaignDefinition Campaign { get; private set; }
        public BandStats Stats { get; private set; }
        public RunRandom Random { get; private set; }
        public SaveStore Saves { get; private set; }
        public GameOptions Options { get; private set; }
        public StageArt Art { get; private set; }
        public EnemyWorld Enemies { get; private set; }
        public WeaponSystem Weapons { get; private set; }
        public PickupSystem Pickups { get; private set; }
        public EffectPool Effects { get; private set; }
        public MetalAudio Audio { get; private set; }
        public GameHud Hud { get; private set; }
        public RunMode Mode { get; private set; }
        public WavePlan Wave { get; private set; }
        public UpgradeKind[] Draft { get; private set; } = new UpgradeKind[0];
        public EnemyActor FocusTarget { get; private set; }
        public int WaveIndex { get; private set; }
        public int Spawned { get; private set; }
        public int Kills { get; private set; }
        public int Score { get; private set; }
        public float Health { get; private set; }
        public float Fury { get; private set; }
        public float Elapsed { get; private set; }
        public float Clock { get; private set; }
        public float IntermissionRemaining { get; private set; }
        public float InvulnerableUntil { get; private set; }
        public string Announcement { get; private set; }
        public float AnnouncementRemaining { get; private set; }
        public bool DangerousAnnouncement { get; private set; }
        public bool Initialized { get; private set; }
        public bool HasCheckpoint { get { return Saves.Load(Campaign.TotalWaves)!=null; } }
        public ChapterDefinition Chapter { get { return Campaign.chapters[Wave.ChapterIndex]; } }
        private bool bossSpawned,betweenWaves;
        private float spawnTimer,focusUntil,focusRingTimer,hudTimer;
        private RunMode beforePause;
        private int appliedChapter=-1;

        public void Initialize(string saveNamespace = "RagnaRock")
        {
            if(Initialized)return;
            if(Instance!=null&&Instance!=this)throw new InvalidOperationException("Já existe uma sessão RagnaRock ativa.");
            Instance=this;
            var asset=Resources.Load<TextAsset>("Campaign");
            if(asset==null)throw new InvalidOperationException("Assets/RagnaRock/Resources/Campaign.json está ausente.");
            Campaign=JsonUtility.FromJson<CampaignDefinition>(asset.text);Campaign.Validate();
            Saves=new SaveStore(saveNamespace);Options=Saves.LoadOptions();Stats=new BandStats();Random=new RunRandom(7719);
            Wave=new WavePlan(0,Campaign);Health=Stats.MaxHealth;
            Art=new StageArt(transform);Art.SetQuality(Options.quality);
            Effects=new EffectPool(transform,Art);
            Audio=gameObject.AddComponent<MetalAudio>();Audio.Initialize(Options);
            Enemies=new EnemyWorld(this,transform);Weapons=new WeaponSystem(this,transform);Pickups=new PickupSystem(this,transform);
            Hud=new GameHud(this,transform);
            Mode=RunMode.Menu;ApplyChapter(0);Initialized=true;Hud.RefreshState();
            if(!string.IsNullOrEmpty(Saves.LastWarning))Alert(Saves.LastWarning,5);
        }
        private void Update()
        {
            if(!Initialized)return;
            HandleInput();
            float delta=Mathf.Min(Time.unscaledDeltaTime,.05f);
            bool moving=Mode==RunMode.Playing||Mode==RunMode.Intermission||Mode==RunMode.Menu;
            if(moving)
            {
                if(Mode!=RunMode.Menu)Clock+=delta;
                Effects.ShowDamage=Options.showDamage;Effects.ReducedMotion=Options.reducedMotion;
                Effects.Tick(delta);
                if(AnnouncementRemaining>0)AnnouncementRemaining=Mathf.Max(0,AnnouncementRemaining-delta);
            }
            if(Mode==RunMode.Intermission)
            {
                IntermissionRemaining-=delta;
                if(IntermissionRemaining<=0)BeginWave();
            }
            else if(Mode==RunMode.Playing)TickRun(delta);
            float visualTime=Mode==RunMode.Menu?Time.unscaledTime:Clock;
            Art.Tick(visualTime,moving?delta:0,Options,Stats);
            hudTimer-=delta;
            if(hudTimer<=0){Hud.RefreshValues();hudTimer=.08f;}
            Hud.UpdateSafeArea();
        }
        private void TickRun(float delta)
        {
            Elapsed+=delta;
            SpawnEnemies(delta);
            Enemies.Tick(delta);
            if(Mode!=RunMode.Playing)return;
            Weapons.Tick(delta);Enemies.SweepDead();Pickups.Tick(delta);
            if(FocusTarget!=null&&(!FocusTarget.Active||Clock>=focusUntil))FocusTarget=null;
            focusRingTimer-=delta;
            if(FocusTarget!=null&&focusRingTimer<=0)
            {
                Effects.Ring(FocusTarget.Position+Vector3.up*.09f,FocusTarget.IsBoss?2.1f:.75f,new Color(1,.83f,.35f),.65f);
                focusRingTimer=.6f;
            }
            if(Spawned>=Wave.Quota&&(!Wave.HasBoss||bossSpawned)&&Enemies.Count==0){FinishWave();return;}
            if(Stats.pendingChoices>0)ShowDraft();
        }
        private void SpawnEnemies(float delta)
        {
            spawnTimer-=delta;int catchUp=0;
            while(spawnTimer<=0&&Spawned<Wave.Quota&&Enemies.Count<EnemyWorld.MaximumAlive&&catchUp++<4)
            {
                EnemyKind kind=CombatMath.ChooseEnemy(WaveIndex,Chapter.enemyFamily,Random);
                if(!Enemies.Spawn(kind,SpawnPosition()))break;
                Spawned++;spawnTimer+=Wave.SpawnInterval;
            }
            if(Enemies.Count>=EnemyWorld.MaximumAlive)spawnTimer=Mathf.Max(spawnTimer,0);
            if(Wave.HasBoss&&!bossSpawned&&Spawned>=Mathf.Max(1,Wave.Quota*2/3)&&Enemies.Count<EnemyWorld.MaximumAlive)
            {
                bossSpawned=Enemies.Spawn(EnemyKind.Boss,new Vector3(0,0,20.4f));
                if(bossSpawned){Alert(Chapter.bossName+" • CHEFE DO ATO",4,true);Art.Shake(.45f);Audio.Super();}
            }
        }
        private Vector3 SpawnPosition()
        {
            float a=Random.Value()*Mathf.PI*2;
            return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(20.4f+Random.Value()*.3f);
        }
        public void StartNewRun()
        {
            ResetWorld();Saves.Clear();Stats=new BandStats();
            Random=new RunRandom(unchecked((uint)Environment.TickCount*747796405u+2891336453u));
            WaveIndex=0;Health=Stats.MaxHealth;Fury=35;Kills=0;Score=0;Elapsed=0;Clock=0;InvulnerableUntil=0;
            PrepareWave();
        }
        public void ContinueRun()
        {
            var checkpoint=Saves.Load(Campaign.TotalWaves);
            if(checkpoint==null){Alert("Nenhum checkpoint válido encontrado.",4);Hud.RefreshState();return;}
            ResetWorld();Stats=checkpoint.stats.Copy();Random=new RunRandom(checkpoint.seed);
            WaveIndex=checkpoint.waveIndex;Health=checkpoint.health;Fury=checkpoint.fury;
            Kills=checkpoint.kills;Score=checkpoint.score;Elapsed=checkpoint.elapsed;Clock=0;InvulnerableUntil=0;
            PrepareWave();Alert("CHECKPOINT RESTAURADO • início da onda "+(WaveIndex+1),4);
        }
        private void PrepareWave()
        {
            Wave=new WavePlan(WaveIndex,Campaign);Spawned=0;bossSpawned=false;betweenWaves=true;
            ApplyChapter(Wave.ChapterIndex);
            IntermissionRemaining=WaveIndex==0?12f:8f;
            SetMode(RunMode.Intermission);
        }
        public void BeginWave()
        {
            if(Mode!=RunMode.Intermission)return;
            betweenWaves=false;spawnTimer=.35f;Spawned=0;bossSpawned=false;Enemies.BeginWave();Weapons.Reset();
            FocusTarget=null;InvulnerableUntil=Clock+1.2f;
            Saves.Save(new RunCheckpoint{waveIndex=WaveIndex,kills=Kills,score=Score,seed=Random.State,
                health=Health,fury=Fury,elapsed=Elapsed,stats=Stats.Copy()},Campaign.TotalWaves);
            SetMode(RunMode.Playing);
            Alert("ONDA "+Wave.Number+" / "+Campaign.TotalWaves+" • "+(Wave.HasBoss?"CHEFE NESTA ONDA":"AUMENTE O VOLUME"),3);
        }
        private void FinishWave()
        {
            Pickups.CollectAll();Score+=120+Wave.Number*15;Health=Mathf.Min(Stats.MaxHealth,Health+8+Stats.Rank(UpgradeKind.Regeneration)*3);
            Fury=Mathf.Min(100,Fury+7);FocusTarget=null;Weapons.Reset();
            if(WaveIndex+1>=Campaign.TotalWaves){Complete(true);return;}
            WaveIndex++;betweenWaves=true;
            if(Stats.pendingChoices>0)ShowDraft();else PrepareWave();
        }
        private void ShowDraft()
        {
            Draft=UpgradeDraft.Draw(Stats,Random);
            if(Draft.Length==0)
            {
                Health=Mathf.Min(Stats.MaxHealth,Health+Stats.pendingChoices*18);Stats.pendingChoices=0;
                Alert("BANDA NO MÁXIMO • EXCEDENTE CONVERTIDO EM REPAROS",3);
                if(betweenWaves)PrepareWave();else SetMode(RunMode.Playing);return;
            }
            SetMode(RunMode.Upgrade);Audio.Upgrade();
        }
        public void ChooseUpgrade(int index)
        {
            if(Mode!=RunMode.Upgrade||index<0||index>=Draft.Length||Stats.pendingChoices<=0)return;
            float previousMaximum=Stats.MaxHealth;
            if(!Stats.Apply(Draft[index]))return;
            Stats.pendingChoices--;
            if(Stats.MaxHealth>previousMaximum)Health=Mathf.Min(Stats.MaxHealth,Health+Stats.MaxHealth-previousMaximum);
            Audio.Upgrade();Effects.Ring(new Vector3(0,.8f,0),5.5f,StageArt.MemberColors[index%4],.6f);
            if(Stats.pendingChoices>0)ShowDraft();
            else if(betweenWaves)PrepareWave();else SetMode(RunMode.Playing);
        }
        public void DamageStage(float amount)
        {
            if(Mode!=RunMode.Playing||Clock<InvulnerableUntil)return;
            Health=Mathf.Max(0,Health-CombatMath.ApplyArmor(amount,Stats.DamageReduction));
            Art.Shake(.3f);Hud.StageHit();
            if(Health<=0)Complete(false);
        }
        public void ActivateSuper()
        {
            if(Mode!=RunMode.Playing)return;
            if(Fury<100){Alert("MURALHA DE SOM: carregue a fúria derrotando inimigos.",2);return;}
            Fury=0;InvulnerableUntil=Clock+3.5f;
            float damage=(95+Wave.Number*3.8f)*Stats.DamageMultiplier;
            Enemies.Area(Vector3.zero,24f,damage,.32f,2.6f);
            Effects.Ring(new Vector3(0,.8f,0),22f,new Color(.63f,.86f,1),1.1f);
            Art.Shake(.85f);Audio.Super();Alert("MURALHA DE SOM • PALCO PROTEGIDO POR 3,5s",3.5f);
        }
        public void OnEnemyKilled(EnemyActor actor)
        {
            Kills++;Score+=actor.IsBoss?500:actor.Kind==EnemyKind.Brute?32:16;
            Fury=Mathf.Min(100,Fury+(actor.IsBoss?18:1.5f));
            Pickups.Drop(actor.Position,PickupKind.Experience,actor.IsBoss?100+Wave.Number*3:7+Wave.Number/7);
            if(actor.IsBoss)Pickups.Drop(actor.Position+Vector3.right*.7f,PickupKind.InstrumentCore,1);
            else
            {
                float drop=Random.Value();
                if(drop<.036f)Pickups.Drop(actor.Position+Vector3.right*.3f,PickupKind.Health,10);
                else if(drop<.071f)Pickups.Drop(actor.Position+Vector3.left*.3f,PickupKind.Fury,7);
            }
        }
        public void Collect(PickupKind kind,int amount)
        {
            switch(kind)
            {
                case PickupKind.Experience:Stats.AddExperience(amount);break;
                case PickupKind.Health:Health=Mathf.Min(Stats.MaxHealth,Health+amount);break;
                case PickupKind.Fury:Fury=Mathf.Min(100,Fury+amount);break;
                case PickupKind.InstrumentCore:
                    int lowest=-1;
                    for(int i=0;i<4;i++)if(Stats.CanUpgrade((UpgradeKind)i)&&(lowest<0||Stats.ranks[i]<Stats.ranks[lowest]))lowest=i;
                    if(lowest>=0)
                    {
                        Stats.Apply((UpgradeKind)lowest);Audio.Upgrade();
                        Alert("NÚCLEO COLETADO • "+StageArt.MemberNames[lowest]+": INSTRUMENTO NÍVEL "+Stats.ranks[lowest],4);
                    }
                    else{Health=Mathf.Min(Stats.MaxHealth,Health+25);Fury=Mathf.Min(100,Fury+20);}
                    break;
            }
        }
        public void TogglePause()
        {
            if(Mode==RunMode.Paused){SetMode(beforePause);return;}
            if(Mode!=RunMode.Playing&&Mode!=RunMode.Intermission&&Mode!=RunMode.Upgrade)return;
            beforePause=Mode;SetMode(RunMode.Paused);
        }
        public void PauseForOverlay()
        {if(Mode==RunMode.Playing||Mode==RunMode.Intermission||Mode==RunMode.Upgrade)TogglePause();}
        public void ReturnToMenu()
        {
            ResetWorld();SetMode(RunMode.Menu);ApplyChapter(0);Saves.StoreOptions(Options);
        }
        public void ApplyOptions(){Options.Sanitize();Art.SetQuality(Options.quality);Saves.StoreOptions(Options);}
        public void Quit()
        {
            Saves.StoreOptions(Options);
            if(Application.isEditor){ReturnToMenu();return;}
            Application.Quit();
        }
        public void Alert(string message,float seconds=3,bool danger=false)
        {
            if(DangerousAnnouncement&&AnnouncementRemaining>0&&!danger)return;
            Announcement=message;AnnouncementRemaining=seconds;DangerousAnnouncement=danger;
        }
        private void Complete(bool victory)
        {
            if(Mode==RunMode.Defeat||Mode==RunMode.Victory)return;
            Saves.Complete(Score,Wave.Number);SetMode(victory?RunMode.Victory:RunMode.Defeat);
            if(victory)Audio.Super();else Audio.Impact();
        }
        private void SetMode(RunMode next)
        {
            Mode=next;Audio.SetPaused(next==RunMode.Paused||next==RunMode.Upgrade||next==RunMode.Defeat);
            Hud.RefreshState();
        }
        private void ApplyChapter(int index)
        {
            if(appliedChapter==index)return;appliedChapter=index;
            Art.SetChapter(Campaign.chapters[index]);Audio.SetChapter(Campaign.chapters[index],index);
        }
        private void ResetWorld()
        {
            Enemies.Clear();Weapons.Reset();Pickups.Clear();Effects.Clear();
            FocusTarget=null;AnnouncementRemaining=0;DangerousAnnouncement=false;Draft=new UpgradeKind[0];
        }
        private void HandleInput()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(!Hud.HandleBack())TogglePause();return;}
            if(Input.GetKeyDown(KeyCode.F1)){Hud.ShowCodex();return;}
            if(Hud.BlocksGameplayInput)return;
            if(Mode==RunMode.Upgrade)
            {
                if(Input.GetKeyDown(KeyCode.Alpha1)||Input.GetKeyDown(KeyCode.Keypad1))ChooseUpgrade(0);
                else if(Input.GetKeyDown(KeyCode.Alpha2)||Input.GetKeyDown(KeyCode.Keypad2))ChooseUpgrade(1);
                else if(Input.GetKeyDown(KeyCode.Alpha3)||Input.GetKeyDown(KeyCode.Keypad3))ChooseUpgrade(2);
                return;
            }
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))
            {if(Mode==RunMode.Intermission)BeginWave();else if(Mode==RunMode.Menu)Hud.RequestNewRun();}
            if(Mode!=RunMode.Playing)return;
            if(Input.GetKeyDown(KeyCode.Space))ActivateSuper();
            if(Input.GetKeyDown(KeyCode.P))TogglePause();
            if(Mode!=RunMode.Playing)return;
            bool overUi=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            Vector2 pointer=Input.mousePosition;
            bool clicked=Input.GetMouseButtonDown(0);
            if(Input.touchCount>0)
            {
                Touch touch=Input.GetTouch(0);pointer=touch.position;clicked=touch.phase==TouchPhase.Began;
                overUi=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            }
            if(overUi)return;
            Ray ray=Art.Camera.ScreenPointToRay(pointer);float enter;
            if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter))return;
            Vector3 point=ray.GetPoint(enter);Pickups.AttractNear(point);
            if(!clicked)return;
            var actor=Enemies.FindAt(point,2.15f);
            FocusTarget=actor;focusUntil=Clock+8f;focusRingTimer=0;
        }
        private void OnApplicationFocus(bool focus)
        {
            if(!Initialized)return;
            if(!focus)
            {
                if(Mode==RunMode.Playing||Mode==RunMode.Intermission)TogglePause();
                Audio.SetPaused(true);
            }
            else Audio.SetPaused(Mode==RunMode.Paused||Mode==RunMode.Upgrade||Mode==RunMode.Defeat);
        }
        private void OnApplicationPause(bool pause)
        {
            if(!Initialized||!pause)return;
            if(Mode==RunMode.Playing||Mode==RunMode.Intermission)TogglePause();
            Saves.StoreOptions(Options);
        }
        private void OnDestroy()
        {
            if(Instance==this)Instance=null;
            if(Initialized)Saves.StoreOptions(Options);
            if(Hud!=null)Hud.Dispose();if(Effects!=null)Effects.Dispose();if(Art!=null)Art.Dispose();
        }
    }
}

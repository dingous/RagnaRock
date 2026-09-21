using System.Collections.Generic;
using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock
{
    public sealed class EnemyActor
    {
        public GameObject Object;
        public Transform Transform;
        public EnemyKind Kind;
        public Vector3 Position;
        public float Health,MaxHealth,Speed,DamageScale,AttackTimer,WarningTimer,SlowUntil,SlowFactor,Phase,Scale;
        public bool Active,IsChild;
        public int SummonsLeft;
        public bool IsBoss { get { return Kind==EnemyKind.Boss; } }
    }

    public sealed class EnemyWorld
    {
        public const int MaximumAlive=96;
        public readonly List<EnemyActor> Actors=new List<EnemyActor>(MaximumAlive);
        private readonly Stack<EnemyActor>[] pools=new Stack<EnemyActor>[8];
        private readonly Transform root;
        private readonly GameSession game;
        private int serial,extraBudget;
        public int Count { get; private set; }
        public EnemyActor Boss { get; private set; }
        public EnemyWorld(GameSession game,Transform parent)
        {
            this.game=game;root=new GameObject("Enemies • bounded object pools").transform;root.SetParent(parent,false);
            for(int k=0;k<8;k++)
            {
                pools[k]=new Stack<EnemyActor>();int warm=k==0?16:k==1?12:k==7?1:5;
                for(int i=0;i<warm;i++)pools[k].Push(Create((EnemyKind)k));
            }
        }
        public void BeginWave(){extraBudget=28;Boss=null;}
        public bool Spawn(EnemyKind kind,Vector3 position,bool child=false)
        {
            if(Count>=MaximumAlive)return false;
            if(child&&extraBudget<=0)return false;
            if(child)extraBudget--;
            var pool=pools[(int)kind];var actor=pool.Count>0?pool.Pop():Create(kind);
            WavePlan wave=game.Wave;
            actor.Active=true;actor.IsChild=child;actor.Position=position;
            actor.MaxHealth=CombatMath.BaseHealth(kind)*wave.HealthScale;
            if(kind==EnemyKind.Boss)actor.MaxHealth*=1f+wave.ChapterIndex*.065f;
            if(child)actor.MaxHealth*=.72f;
            actor.Health=actor.MaxHealth;actor.Speed=CombatMath.Speed(kind)*wave.SpeedScale;
            actor.DamageScale=wave.DamageScale;actor.AttackTimer=kind==EnemyKind.Boss?4f:kind==EnemyKind.Summoner?6f:1.1f;
            actor.WarningTimer=0;actor.SlowUntil=0;actor.SlowFactor=1;actor.SummonsLeft=4;
            actor.Scale=kind==EnemyKind.Boss?2.12f:kind==EnemyKind.Brute?1.32f:kind==EnemyKind.Rusher?.82f:1f;
            actor.Transform.localScale=Vector3.one*actor.Scale;actor.Transform.position=position;actor.Object.SetActive(true);
            Actors.Add(actor);Count++;if(kind==EnemyKind.Boss)Boss=actor;return true;
        }
        public void Tick(float delta)
        {
            int initialCount=Actors.Count;
            for(int i=0;i<initialCount;i++)
            {
                if(game.Mode!=RunMode.Playing)break;
                var actor=Actors[i];if(!actor.Active)continue;
                float distance=actor.Position.magnitude;
                bool ranged=actor.Kind==EnemyKind.Spitter||actor.Kind==EnemyKind.Summoner||actor.IsBoss;
                float stop=ranged?actor.IsBoss?9.4f:10.6f:3.95f;
                if(distance>stop)
                {
                    float slow=game.Clock<actor.SlowUntil?actor.SlowFactor:1f;
                    float enrage=actor.IsBoss&&actor.Health<actor.MaxHealth*.35f?1.2f:1f;
                    actor.Position=Vector3.MoveTowards(actor.Position,Vector3.zero,actor.Speed*slow*enrage*delta);
                }
                else Attack(actor,delta);
                Vector3 visual=actor.Position;
                float bob=game.Options.reducedMotion?0:Mathf.Sin(game.Clock*6f+actor.Phase)*.055f;
                visual.y=Mathf.Abs(bob);actor.Transform.position=visual;
                Vector3 facing=-actor.Position;facing.y=0;
                if(facing.sqrMagnitude>.01f)actor.Transform.rotation=Quaternion.LookRotation(facing)*Quaternion.Euler(0,0,bob*18f);
            }
        }
        private void Attack(EnemyActor actor,float delta)
        {
            if(actor.WarningTimer>0)
            {
                actor.WarningTimer-=delta;
                if(actor.WarningTimer>0)return;
                if(actor.Kind==EnemyKind.Summoner)
                {
                    if(actor.SummonsLeft>0){Summon(actor,2);actor.SummonsLeft--;}
                    else game.DamageStage(3f*actor.DamageScale);
                    actor.AttackTimer=8f;
                }
                else if(actor.IsBoss)
                {
                    game.DamageStage((12f+game.Wave.ChapterIndex*.4f)*actor.DamageScale);
                    game.Effects.Beam(actor.Position+Vector3.up*2.6f,new Vector3(0,.9f,0),new Color(1f,.28f,.25f),.32f,.35f);
                    if(game.Wave.ChapterIndex>=5&&actor.SummonsLeft>0){Summon(actor,3);actor.SummonsLeft--;}
                    actor.AttackTimer=actor.Health<actor.MaxHealth*.35f?4.6f:6.2f;
                }
                else
                {
                    game.DamageStage(3.5f*actor.DamageScale);
                    game.Effects.Beam(actor.Position+Vector3.up*1.1f,new Vector3(0,.8f,0),new Color(.65f,.85f,.32f),.09f,.24f);
                    actor.AttackTimer=3.6f;
                }
                return;
            }
            actor.AttackTimer-=delta;if(actor.AttackTimer>0)return;
            if(actor.IsBoss)
            {
                actor.WarningTimer=2.4f;
                game.Effects.Ring(new Vector3(0,.74f,0),4.65f,new Color(1f,.35f,.17f),2.4f,true);
                game.Alert("GOLPE DO CHEFE EM 2,4s • MURALHA DE SOM PROTEGE O PALCO",2.4f,true);
            }
            else if(actor.Kind==EnemyKind.Summoner)
            {
                actor.WarningTimer=1.7f;
                game.Effects.Ring(actor.Position+Vector3.up*.12f,2f,new Color(.68f,.38f,1f),1.7f,true);
            }
            else if(actor.Kind==EnemyKind.Spitter)
            {
                actor.WarningTimer=1.15f;
                game.Effects.Ring(actor.Position+Vector3.up*.1f,1.4f,new Color(.65f,.85f,.32f),1.15f,true);
            }
            else
            {
                float damage=actor.Kind==EnemyKind.Brute?7f:actor.Kind==EnemyKind.Rusher?2.4f:3.7f;
                game.DamageStage(damage*actor.DamageScale);
                game.Effects.Beam(actor.Position+Vector3.up*.9f,actor.Position.normalized*3.6f+Vector3.up*.8f,new Color(1,.28f,.24f),.1f,.15f);
                actor.AttackTimer=actor.Kind==EnemyKind.Rusher?.95f:1.5f;
            }
        }
        private void Summon(EnemyActor actor,int amount)
        {
            for(int i=0;i<amount;i++)
            {
                float a=game.Random.Value()*Mathf.PI*2;
                Vector3 p=actor.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.8f;
                if(p.magnitude<5f)p=p.normalized*5f;
                Spawn(EnemyKind.Rusher,p,true);
            }
        }
        public void Hit(EnemyActor actor,float damage,bool critical=false,bool ignoreArmor=false,float knockback=0)
        {
            if(actor==null||!actor.Active||damage<=0)return;
            if(actor.Kind==EnemyKind.Shielded&&!ignoreArmor)damage*=.65f;
            actor.Health-=damage;game.Effects.NumberAt(actor.Position,damage,critical);
            if(knockback>0&&!actor.IsBoss)
                actor.Position=Vector3.ClampMagnitude(actor.Position+actor.Position.normalized*knockback,20.7f);
            if(actor.Health>0)return;
            actor.Active=false;Count--;actor.Object.SetActive(false);
            if(actor==Boss)Boss=null;
            game.OnEnemyKilled(actor);
            if(actor.Kind==EnemyKind.Splitter&&!actor.IsChild)Summon(actor,2);
        }
        public void SweepDead()
        {
            for(int i=Actors.Count-1;i>=0;i--)
            {
                var actor=Actors[i];if(actor.Active)continue;
                pools[(int)actor.Kind].Push(actor);Actors.RemoveAt(i);
            }
        }
        public EnemyActor FindTarget(Vector3 from,float range,EnemyActor focus=null)
        {
            float max=range*range;
            if(focus!=null&&focus.Active&&(focus.Position-from).sqrMagnitude<=max)return focus;
            EnemyActor nearest=null;float score=float.MaxValue;
            foreach(var actor in Actors)
            {
                if(!actor.Active||(actor.Position-from).sqrMagnitude>max)continue;
                float candidate=actor.Position.sqrMagnitude*(actor.IsBoss?.86f:actor.Kind==EnemyKind.Summoner?.8f:1f);
                if(candidate<score){score=candidate;nearest=actor;}
            }
            return nearest;
        }
        public EnemyActor FindAt(Vector3 point,float radius)
        {
            EnemyActor found=null;float best=radius*radius;
            foreach(var actor in Actors)
            {
                if(!actor.Active)continue;float d=(actor.Position-point).sqrMagnitude;
                if(d<best){best=d;found=actor;}
            }
            return found;
        }
        public void Area(Vector3 center,float radius,float damage,float slow=1,float knockback=0)
        {
            int count=Actors.Count;float rr=radius*radius;
            for(int i=0;i<count;i++)
            {
                var actor=Actors[i];if(!actor.Active||(actor.Position-center).sqrMagnitude>rr)continue;
                if(slow<1){actor.SlowFactor=slow;actor.SlowUntil=game.Clock+1.6f;}
                Hit(actor,damage,false,true,knockback);
            }
        }
        public void Clear()
        {
            foreach(var actor in Actors){actor.Active=false;actor.Object.SetActive(false);pools[(int)actor.Kind].Push(actor);}
            Actors.Clear();Count=0;Boss=null;
        }
        private EnemyActor Create(EnemyKind kind)
        {
            var go=game.Art.EnemyObject(kind,root);go.SetActive(false);
            return new EnemyActor{Object=go,Transform=go.transform,Kind=kind,Phase=(serial++)*1.17f};
        }
    }
}

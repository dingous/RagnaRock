using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock
{
    public sealed class WeaponSystem
    {
        private sealed class Mortar
        {
            public GameObject Object;
            public Vector3 Start,End;
            public float Age,Duration,Damage,Radius;
            public bool Active,Ultimate;
        }
        private readonly GameSession game;
        private readonly float[] timers=new float[4];
        private readonly EnemyActor[] candidates=new EnemyActor[EnemyWorld.MaximumAlive];
        private readonly float[] distances=new float[EnemyWorld.MaximumAlive];
        private readonly Mortar[] mortars=new Mortar[24];
        public WeaponSystem(GameSession game,Transform parent)
        {
            this.game=game;
            var mesh=new MeshCraft();mesh.Octahedron(Vector3.zero,new Vector3(.18f,.28f,.18f),StageArt.MemberColors[3]);
            var template=game.Art.Make("Pooled drum mortar",mesh,parent,game.Art.Glow);template.SetActive(false);
            for(int i=0;i<mortars.Length;i++)
            {
                var go=i==0?template:Object.Instantiate(template,parent);
                go.name="Drum mortar "+i;go.SetActive(false);mortars[i]=new Mortar{Object=go};
            }
            Reset();
        }
        public void Reset()
        {
            for(int i=0;i<4;i++)timers[i]=i*.13f;
            foreach(var mortar in mortars){mortar.Active=false;mortar.Object.SetActive(false);}
        }
        public void Tick(float delta)
        {
            TickMortars(delta);
            for(int i=0;i<4;i++)
            {
                timers[i]-=delta;if(timers[i]>0)continue;
                var role=(BandRole)i;int rank=game.Stats.WeaponRank(role);
                Vector3 origin=game.Art.Band[i].Root.position;origin.y=0;
                float range=(role==BandRole.Bass?10.4f:role==BandRole.Voice?15f:19f)+game.Stats.RangeBonus+rank*.15f;
                var target=game.Enemies.FindTarget(origin,range,game.FocusTarget);
                if(target==null){timers[i]=.08f;continue;}
                bool critical=game.Random.Value()<game.Stats.CriticalChance;
                float damage=CombatMath.WeaponDamage(role,rank)*game.Stats.DamageMultiplier*(critical?1.8f:1f);
                game.Art.Band[i].Aim(target.Position);game.Art.Band[i].Recoil=1;
                switch(role)
                {
                    case BandRole.Guitar: Guitar(origin,target,range,damage,critical,rank);break;
                    case BandRole.Voice: Voice(origin,target,range,damage,critical,rank);break;
                    case BandRole.Bass:
                        game.Enemies.Area(origin,range,damage,rank>=4?.5f:.72f,rank>=8?.9f:.28f);
                        game.Effects.Ring(origin+Vector3.up*.25f,range,StageArt.MemberColors[i],.65f);
                        break;
                    case BandRole.Drums:
                        Throw(origin,target.Position,damage,rank);
                        if(rank>=4)
                        {
                            var extra=game.Enemies.FindTarget(-origin,range);
                            Throw(origin,extra!=null?extra.Position:target.Position+Vector3.right*1.5f,damage*.72f,rank);
                        }
                        if(rank==8)Throw(origin,target.Position+Vector3.forward*2f,damage*.72f,rank);
                        break;
                }
                game.Audio.Shot(role,rank);
                timers[i]=CombatMath.WeaponCooldown(role,rank)/game.Stats.TempoMultiplier;
            }
        }
        private void Guitar(Vector3 origin,EnemyActor target,float range,float damage,bool critical,int rank)
        {
            Vector3 direction=(target.Position-origin).normalized;
            Vector3 end=origin+direction*range;int count=0;
            foreach(var actor in game.Enemies.Actors)
            {
                if(!actor.Active)continue;
                float distance=CombatMath.SegmentDistanceSquared(actor.Position.x,actor.Position.z,origin.x,origin.z,end.x,end.z);
                float width=actor.IsBoss?1.2f:.48f+rank*.055f;
                if(distance>width*width)continue;
                float along=Vector3.Dot(actor.Position-origin,direction);if(along<0||along>range)continue;
                if(count>=candidates.Length)break;
                int j=count;
                while(j>0&&distances[j-1]>along){candidates[j]=candidates[j-1];distances[j]=distances[j-1];j--;}
                candidates[j]=actor;distances[j]=along;count++;
            }
            int pierce=1+rank/2;
            for(int i=0;i<Mathf.Min(count,pierce);i++)game.Enemies.Hit(candidates[i],damage,critical,rank==8);
            game.Effects.Beam(origin+Vector3.up*1.7f,end+Vector3.up*.8f,StageArt.MemberColors[1],rank==8?.23f:.12f,.17f);
            if(rank==8)
            {
                game.Enemies.Area(target.Position,2.5f,damage*.38f);
                game.Effects.Ring(target.Position+Vector3.up*.35f,2.5f,StageArt.MemberColors[1],.35f);
            }
        }
        private void Voice(Vector3 origin,EnemyActor target,float range,float damage,bool critical,int rank)
        {
            Vector3 direction=(target.Position-origin).normalized;
            float cosine=rank==8?-1f:Mathf.Cos((34f+rank*2.7f)*Mathf.Deg2Rad);
            int count=game.Enemies.Actors.Count;
            for(int i=0;i<count;i++)
            {
                var actor=game.Enemies.Actors[i];if(!actor.Active)continue;
                Vector3 to=actor.Position-origin;float d=to.magnitude;
                if(d>range||(d>.01f&&Vector3.Dot(to/d,direction)<cosine))continue;
                game.Enemies.Hit(actor,damage,critical,true,rank>=4?.75f:.30f);
            }
            if(rank==8)game.Effects.Ring(origin+Vector3.up*.8f,range,StageArt.MemberColors[0],.65f);
            else
            {
                Vector3 tip=origin+direction*range;
                game.Effects.Beam(origin+Vector3.up*1.8f,tip+Vector3.up*.5f,StageArt.MemberColors[0],.24f,.2f);
                game.Effects.Ring(origin+direction*(range*.6f)+Vector3.up*.6f,range*.4f,StageArt.MemberColors[0],.5f);
            }
        }
        private void Throw(Vector3 origin,Vector3 target,float damage,int rank)
        {
            foreach(var m in mortars)
            {
                if(m.Active)continue;
                m.Active=true;m.Start=origin+Vector3.up*1.4f;m.End=target+Vector3.up*.3f;
                m.Age=0;m.Duration=.65f;m.Damage=damage;m.Radius=2.15f+rank*.15f;m.Ultimate=rank==8;
                m.Object.transform.position=m.Start;m.Object.SetActive(true);return;
            }
            // Capacity fallback still delivers combat damage rather than silently losing a shot.
            game.Enemies.Area(target,2.15f+rank*.15f,damage);
        }
        private void TickMortars(float delta)
        {
            foreach(var m in mortars)
            {
                if(!m.Active)continue;m.Age+=delta;float t=Mathf.Clamp01(m.Age/m.Duration);
                m.Object.transform.position=Vector3.Lerp(m.Start,m.End,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*4.2f);
                m.Object.transform.Rotate(new Vector3(0,delta*220f,delta*90f));
                if(t<1)continue;m.Active=false;m.Object.SetActive(false);
                Vector3 point=m.End;point.y=0;
                game.Enemies.Area(point,m.Radius,m.Damage,m.Ultimate?.6f:1f,m.Ultimate?.4f:0);
                game.Effects.Ring(point+Vector3.up*.25f,m.Radius,StageArt.MemberColors[3],.45f);
                game.Audio.Impact();
            }
        }
    }
}

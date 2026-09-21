using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RagnaRock
{
    public sealed class EffectPool : IDisposable
    {
        private sealed class Effect
        {
            public LineRenderer Line;
            public Vector3 A, B;
            public float Life, Duration, Radius, Width;
            public Color Color;
            public bool Ring, Active, Warning;
        }
        private sealed class Number
        {
            public TextMesh Text;
            public Vector3 Position;
            public float Life;
            public Color Color;
        }
        private readonly Effect[] effects = new Effect[112];
        private readonly Number[] numbers = new Number[40];
        private readonly Material material;
        private readonly Transform root;
        private int effectCursor, numberCursor;
        public bool ShowDamage = true, ReducedMotion;
        public EffectPool(Transform parent, StageArt art)
        {
            root = new GameObject("Pooled sound waves and hit feedback").transform; root.SetParent(parent,false);
            Shader shader=Resources.Load<Shader>("SonicFX");
            if(shader==null)throw new InvalidOperationException("Shader SonicFX ausente.");
            material=new Material(shader){name="RagnaRock • sonic lines"};
            for(int i=0;i<effects.Length;i++)
            {
                var go=new GameObject("Sonic effect "+i,typeof(LineRenderer));go.transform.SetParent(root,false);
                var line=go.GetComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
                line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.numCapVertices=2;
                line.enabled=false;effects[i]=new Effect{Line=line};
            }
            for(int i=0;i<numbers.Length;i++)
            {
                var text=art.WorldText("",Vector3.zero,.72f,Color.white,root);text.gameObject.SetActive(false);
                numbers[i]=new Number{Text=text};
            }
        }
        public void Beam(Vector3 from,Vector3 to,Color color,float width=.13f,float duration=.14f)
        {
            var e=Next(false);if(e==null)return;
            e.A=from;e.B=to;e.Ring=false;e.Width=width;e.Duration=e.Life=duration;e.Color=color;e.Warning=false;
            e.Line.loop=false;e.Line.positionCount=2;e.Line.SetPosition(0,from);e.Line.SetPosition(1,to);
            e.Line.startWidth=e.Line.endWidth=width;e.Line.startColor=e.Line.endColor=color;
        }
        public void Ring(Vector3 center,float radius,Color color,float duration=.5f,bool warning=false)
        {
            var e=Next(warning);if(e==null)return;
            e.A=center;e.Radius=radius;e.Ring=true;e.Duration=e.Life=duration;e.Color=color;e.Warning=warning;
            e.Width=warning?.105f:.18f;e.Line.loop=true;e.Line.positionCount=40;
            e.Line.startWidth=e.Line.endWidth=e.Width;e.Line.startColor=e.Line.endColor=color;
            DrawRing(e,warning?radius:radius*.16f);
        }
        public void NumberAt(Vector3 point,float damage,bool critical)
        {
            if(!ShowDamage)return;
            var n=numbers[numberCursor++%numbers.Length];n.Position=point+Vector3.up*1.8f;
            n.Color=critical?new Color(1f,.82f,.35f):new Color(.88f,.94f,1f);n.Life=.72f;
            n.Text.text=Mathf.CeilToInt(damage).ToString()+(critical?"!":"");n.Text.color=n.Color;
            n.Text.transform.position=n.Position;n.Text.gameObject.SetActive(true);
        }
        public void Tick(float delta)
        {
            foreach(var e in effects)
            {
                if(!e.Active)continue;
                e.Life-=delta;if(e.Life<=0){e.Active=false;e.Line.enabled=false;continue;}
                float t=1-e.Life/e.Duration;Color color=e.Color;
                color.a=e.Warning?(.5f+t*.45f):Mathf.Clamp01((1-t)*1.5f);
                if(ReducedMotion&&!e.Warning)color.a*=.55f;
                e.Line.startColor=e.Line.endColor=color;
                if(e.Ring)DrawRing(e,e.Warning?e.Radius*(1f-.12f*t):e.Radius*Mathf.Lerp(.16f,1,t));
                else e.Line.startWidth=e.Line.endWidth=e.Width*(1-t*.7f);
            }
            foreach(var n in numbers)
            {
                if(n.Life<=0)continue;n.Life-=delta;
                if(n.Life<=0){n.Text.gameObject.SetActive(false);continue;}
                if(!ReducedMotion)n.Position+=Vector3.up*delta*1.9f;
                n.Text.transform.position=n.Position;Color c=n.Color;c.a=Mathf.Min(1,n.Life*3f);n.Text.color=c;
            }
        }
        public void Clear()
        {
            foreach(var e in effects){e.Active=false;e.Line.enabled=false;}
            foreach(var n in numbers){n.Life=0;n.Text.gameObject.SetActive(false);}
        }
        private Effect Next(bool warning)
        {
            for(int i=0;i<effects.Length;i++)
            {
                var e=effects[(effectCursor+i)%effects.Length];
                if(e.Active)continue;
                effectCursor=(effectCursor+i+1)%effects.Length;e.Active=true;e.Line.enabled=true;return e;
            }
            // Never steal an active attack warning just to show decorative feedback.
            if(warning)foreach(var e in effects)if(!e.Warning){e.Active=true;e.Line.enabled=true;return e;}
            return null;
        }
        private static void DrawRing(Effect e,float radius)
        {
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40;
                e.Line.SetPosition(i,e.A+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));
            }
        }
        public void Dispose(){if(material!=null)Object.Destroy(material);if(root!=null)Object.Destroy(root.gameObject);}
    }
}

using UnityEngine;

namespace RagnaRock
{
    public enum PickupKind { Experience, Health, Fury, InstrumentCore }
    public sealed class PickupSystem
    {
        private sealed class Pickup
        {
            public GameObject Object;
            public Renderer Renderer;
            public Vector3 Position;
            public PickupKind Kind;
            public int Amount;
            public float Age,Delay;
            public bool Active;
        }
        private readonly Pickup[] items=new Pickup[240];
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private readonly GameSession game;
        private static readonly Color[] Colors={new Color(.22f,.95f,.79f),new Color(.40f,1f,.45f),new Color(.77f,.45f,1),new Color(1f,.76f,.25f)};
        public int ActiveCount{get;private set;}
        public PickupSystem(GameSession game,Transform parent)
        {
            this.game=game;
            var root=new GameObject("Loot • shared mesh pool").transform;root.SetParent(parent,false);
            var mesh=new MeshCraft();mesh.Octahedron(Vector3.zero,new Vector3(.15f,.24f,.15f),Color.white);
            var template=game.Art.Make("Pickup",mesh,root,game.Art.Glow);template.SetActive(false);
            for(int i=0;i<items.Length;i++)
            {
                var go=i==0?template:Object.Instantiate(template,root);go.name="Pickup "+i;go.SetActive(false);
                items[i]=new Pickup{Object=go,Renderer=go.GetComponent<Renderer>()};
            }
        }
        public void Drop(Vector3 point,PickupKind kind,int amount)
        {
            foreach(var item in items)
            {
                if(item.Active)continue;
                item.Active=true;item.Position=point+Vector3.up*.4f;item.Kind=kind;item.Amount=amount;item.Age=0;
                item.Delay=kind==PickupKind.InstrumentCore?.7f:1.6f;
                item.Object.transform.position=item.Position;
                item.Object.transform.localScale=Vector3.one*(kind==PickupKind.InstrumentCore?2.4f:1f);
                properties.SetColor("_Color",Colors[(int)kind]);item.Renderer.SetPropertyBlock(properties);
                item.Object.SetActive(true);ActiveCount++;return;
            }
            // Pool pressure must not erase progression, healing, or a boss reward.
            game.Collect(kind,amount);
        }
        public void AttractNear(Vector3 point)
        {
            foreach(var item in items)
                if(item.Active&&(item.Position-point).sqrMagnitude<6.25f)item.Delay=0;
        }
        public void Tick(float delta)
        {
            foreach(var item in items)
            {
                if(!item.Active)continue;item.Age+=delta;
                if(item.Age>=item.Delay)
                    item.Position=Vector3.MoveTowards(item.Position,new Vector3(0,1.2f,0),game.Stats.MagnetSpeed*delta*(1+item.Age*.14f));
                else item.Position.y=.4f+(game.Options.reducedMotion?0:Mathf.Sin(item.Age*4f)*.06f);
                item.Object.transform.position=item.Position;
                if(!game.Options.reducedMotion)item.Object.transform.Rotate(0,delta*105f,0);
                if(item.Position.x*item.Position.x+item.Position.z*item.Position.z<2.5f||item.Age>9f)Collect(item);
            }
        }
        public void CollectAll(){foreach(var item in items)if(item.Active)Collect(item);}
        public void Clear()
        {foreach(var item in items){item.Active=false;item.Object.SetActive(false);}ActiveCount=0;}
        private void Collect(Pickup item)
        {
            if(!item.Active)return;item.Active=false;item.Object.SetActive(false);ActiveCount--;
            game.Collect(item.Kind,item.Amount);
        }
    }
}

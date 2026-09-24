using System;
using System.Collections.Generic;
using RagnaRock.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RagnaRock
{
    public sealed class MusicianRig
    {
        public Transform Root, Head, RightArm, LeftArm, Instrument;
        public Transform[] Evolution;
        public Vector3 Anchor;
        public float Recoil, Phase;
        public BandRole Role;
        public void Tick(float time, float delta, bool reducedMotion, int rank)
        {
            Recoil = Mathf.MoveTowards(Recoil, 0f, delta * 5f);
            float beat = reducedMotion ? 0f : Mathf.Sin(time * 7f + Phase);
            Root.localPosition = Anchor;
            if (Head != null) Head.localRotation = Quaternion.Euler(beat * 9f - Recoil * 20f, beat * 4f, 0);
            if (RightArm != null) RightArm.localRotation = Quaternion.Euler(-15f + beat * (Role == BandRole.Drums ? 34f : 9f) - Recoil * 24f, 0, -9);
            if (LeftArm != null) LeftArm.localRotation = Quaternion.Euler(-25f - beat * (Role == BandRole.Drums ? 34f : 8f), 0, 12);
            for (int i = 0; i < Evolution.Length; i++) Evolution[i].gameObject.SetActive(rank >= (i == 0 ? 4 : 8));
        }
        public void Aim(Vector3 position)
        {
            if (Role == BandRole.Drums) return;
            Vector3 direction = position - Root.position; direction.y = 0;
            if (direction.sqrMagnitude > .01f) Root.rotation = Quaternion.LookRotation(direction);
        }
    }

    public sealed class StageArt : IDisposable
    {
        // Identity follows the four visual archetypes approved for the band while preserving
        // the existing combat roles: Voice, Guitar, Bass and Drums.
        public static readonly Color[] MemberColors = {
            Hex("#B84DFF"), Hex("#D28A45"), Hex("#B7353D"), Hex("#9AA8BA") };
        public static readonly string[] MemberNames = { "ROXY VANE", "JACK IRON", "MORTEN GRAVES", "VARG NOCTURNE" };
        public static readonly string[] MemberStyles = { "GLAM METAL", "METAL CLÁSSICO", "DEATH METAL", "BLACK METAL" };
        public readonly Transform Root;
        public readonly Camera Camera;
        public readonly MusicianRig[] Band = new MusicianRig[4];
        public readonly Material Solid, Glow;
        public readonly Font Font;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Mesh[] enemyMeshes = new Mesh[8];
        private readonly TextMesh title, style;
        private readonly Transform record, drone;
        private readonly Light keyLight, rimLight;
        private readonly Vector3 cameraHome;
        private GameObject chapterRoot;
        private Mesh chapterMesh;
        private float shake;
        private readonly System.Random decorationRandom = new System.Random(7731);
        private static Color Steel { get { return Hex("#303B4B"); } }
        private static Color Dark { get { return Hex("#121924"); } }
        private static Color Bone { get { return Hex("#DFD6BE"); } }

        public StageArt(Transform parent)
        {
            Root = new GameObject("Arena • original procedural art").transform;
            Root.SetParent(parent, false);
            Shader shader = Resources.Load<Shader>("StageLit");
            if (shader == null) throw new InvalidOperationException("Shader StageLit não encontrado em Resources.");
            Solid = new Material(shader) { name = "RagnaRock • vertex-color metal" };
            Solid.SetFloat("_Metallic", .32f); Solid.SetFloat("_Smoothness", .26f);
            Glow = new Material(shader) { name = "RagnaRock • illuminated trim" };
            Glow.SetFloat("_Emission", 1.1f); Glow.SetFloat("_Metallic", .1f);
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Font == null) throw new InvalidOperationException("Fonte interna LegacyRuntime não encontrada.");

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(Root, false);
            Camera = cameraObject.GetComponent<Camera>(); Camera.tag = "MainCamera";
            cameraHome = new Vector3(0, 31, -23);
            Camera.transform.position = cameraHome;
            Camera.transform.LookAt(new Vector3(0, 0, 1));
            Camera.orthographic = true; Camera.orthographicSize = 18.6f;
            Camera.nearClipPlane = .1f; Camera.farClipPlane = 110f;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = Hex("#09101B");
            Camera.allowHDR = true; Camera.allowMSAA = true;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#7187A4");
            RenderSettings.ambientEquatorColor = Hex("#374052");
            RenderSettings.ambientGroundColor = Hex("#1B202B");
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Hex("#0D1420"); RenderSettings.fogDensity = .009f;
            keyLight = Light("Key • warm spotlight", new Vector3(4, 15, -8), new Vector3(54, -22, 0), Hex("#FFE7C2"), 1.6f);
            keyLight.shadows = LightShadows.Soft;
            rimLight = Light("Rim • blue", new Vector3(-9, 9, 8), new Vector3(37, 145, 0), Hex("#6BBEE7"), .85f);

            var world = new MeshCraft();
            world.Box(new Vector3(0,-.4f,0), new Vector3(72,.4f,72), Hex("#0D131F"));
            world.Cylinder(new Vector3(0,-.11f,0), 21.5f, .22f, Hex("#25303B"), 96);
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                var p = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                world.Box(p * 16f + Vector3.up * .015f, new Vector3(.55f,.04f,2.9f),
                    i % 4 == 0 ? Hex("#536071") : Hex("#353F4D"), Quaternion.Euler(0,-angle * Mathf.Rad2Deg,0));
                world.Cylinder(p * 18.3f, .12f, .12f, Hex("#968263"), 6);
            }
            for (int ring = 0; ring < 4; ring++) world.Torus(Vector3.up * .028f, 6.3f + ring * 3.6f, .035f, Hex("#546173"), 96);
            for (int i = 0; i < 48; i++)
            {
                float x = (float)decorationRandom.NextDouble() * 34 - 17;
                float z = (float)decorationRandom.NextDouble() * 34 - 17;
                if (x*x + z*z < 30 || x*x+z*z > 390) continue;
                world.Box(new Vector3(x,.035f,z), new Vector3(.45f,.035f,.13f), Hex("#6B6260"), Quaternion.Euler(0,i * 43,0));
            }
            Make("Arena ground • engraved steel", world, Root);

            var stage = new MeshCraft();
            stage.Cylinder(new Vector3(0,.28f,0), 4.45f, .6f, Hex("#131B29"), 64);
            stage.Cylinder(new Vector3(0,.61f,0), 4.2f, .1f, Hex("#242D3B"), 64);
            stage.Torus(new Vector3(0,.69f,0), 4.18f, .07f, Hex("#B19464"));
            stage.Torus(new Vector3(0,.35f,0), 4.43f, .08f, Hex("#473547"));
            for (int i=0;i<8;i++)
            {
                float a = i * Mathf.PI / 4;
                stage.Box(new Vector3(Mathf.Cos(a)*4.28f,.36f,Mathf.Sin(a)*4.28f), new Vector3(.32f,.45f,.32f), Steel);
            }
            Make("Central stage • vinyl fortress", stage, Root);
            var vinyl = new MeshCraft();
            for(int i=0;i<6;i++) vinyl.Torus(Vector3.zero, .5f + i*.21f, .021f, Hex("#65646B"));
            record = Make("Vinyl grooves", vinyl, Root, Glow).transform; record.localPosition = new Vector3(0,.685f,0);
            var titleRecord = new MeshCraft(); titleRecord.Cylinder(Vector3.zero, .45f, .015f, Hex("#BA4C4B"),24);
            Make("Original record label",titleRecord,record);

            for (int i=0;i<4;i++)
            {
                float x = i % 2 == 0 ? -3.7f : 3.7f;
                float z = i < 2 ? -2.35f : 2.35f;
                Speaker(new Vector3(x,.7f,z), i >= 2 ? .8f : .6f, Root);
            }
            Band[0] = Musician(BandRole.Voice, new Vector3(0,.72f,-2.35f));
            Band[1] = Musician(BandRole.Guitar, new Vector3(-2.2f,.72f,-.25f));
            Band[2] = Musician(BandRole.Bass, new Vector3(2.2f,.72f,-.25f));
            Band[3] = Musician(BandRole.Drums, new Vector3(0,.72f,1.8f));
            Drums(new Vector3(0,.72f,1.8f));
            title = WorldText("R A G N A R O C K", new Vector3(0,.12f,6.2f), 1.03f, Hex("#DAC294"));
            style = WorldText("DEFENDA O ÚLTIMO PALCO", new Vector3(0,.11f,7.45f), .42f, Hex("#8FA6C0"));
            var droneMesh = new MeshCraft();
            droneMesh.Octahedron(Vector3.zero, new Vector3(.32f,.15f,.28f), Hex("#66D6D3"));
            droneMesh.Box(new Vector3(0,.03f,0), new Vector3(.85f,.06f,.09f), Steel);
            droneMesh.Cylinder(new Vector3(-.4f,.04f,0), .19f,.03f,Hex("#ABD6DD"),10);
            droneMesh.Cylinder(new Vector3(.4f,.04f,0), .19f,.03f,Hex("#ABD6DD"),10);
            drone = Make("Roadie • pickup drone",droneMesh,Root,Glow).transform;
            for (int i=0;i<8;i++) enemyMeshes[i] = BuildEnemy((EnemyKind)i);
        }

        public void SetChapter(ChapterDefinition chapter)
        {
            if (chapterRoot != null) Object.Destroy(chapterRoot);
            if (chapterMesh != null) { meshes.Remove(chapterMesh); Object.Destroy(chapterMesh); }
            Color accent = Hex(chapter.colorHex);
            chapterRoot = new GameObject("Act scenery • " + chapter.id); chapterRoot.transform.SetParent(Root,false);
            var mesh = new MeshCraft();
            bool trees = chapter.environment == "forest" || chapter.environment == "frozen";
            bool spires = chapter.environment == "cathedral" || chapter.environment == "castle" || chapter.environment == "crypt";
            for (int i=0;i<14;i++)
            {
                float a = (i+.15f)*Mathf.PI*2/14;
                Vector3 p = new Vector3(Mathf.Cos(a)*22f,0,Mathf.Sin(a)*22f);
                float h = 3f + (i % 3)*.8f;
                mesh.Cylinder(p+Vector3.up*.2f,1.1f,.4f,Steel,8);
                if(trees)
                {
                    mesh.Cylinder(p+Vector3.up*h*.5f,.22f,h,Hex("#423E43"),6);
                    for(int layer=0;layer<3;layer++) mesh.Cone(p+Vector3.up*(1f+layer*.9f),1.6f-layer*.32f,2f,
                        chapter.environment=="frozen" ? Color.Lerp(accent,Color.white,.18f+layer*.1f) : Color.Lerp(accent,Dark,.4f),7);
                }
                else if (spires)
                {
                    mesh.Box(p+Vector3.up*h*.5f,new Vector3(1.25f,h,1.25f),Steel);
                    mesh.Cone(p+Vector3.up*h,1f,1.2f,accent,4,Quaternion.Euler(0,45,0));
                    mesh.Box(p+Vector3.up*1.8f,new Vector3(1.28f,.25f,1.28f),accent*.65f);
                }
                else if(chapter.environment=="observatory")
                {
                    mesh.Cylinder(p+Vector3.up*1.3f,.55f,2.6f,Steel,8);
                    mesh.Octahedron(p+Vector3.up*3.2f,new Vector3(.9f,1.4f,.9f),accent);
                    mesh.Torus(p+Vector3.up*2.6f,1.3f,.08f,Bone,20);
                }
                else if(chapter.environment=="abyss")
                {
                    mesh.Cone(p,1.1f,h,Color.Lerp(accent,Dark,.35f),5,Quaternion.Euler(14*Mathf.Sin(a),0,9*Mathf.Cos(a)));
                    mesh.Octahedron(p+Vector3.up*.45f,new Vector3(.7f,.8f,.7f),Bone);
                }
                else
                {
                    mesh.Box(p+Vector3.up*h*.5f,new Vector3(1.5f,h,1.1f),Dark);
                    for(int k=0;k<3;k++)
                    {
                        mesh.Cylinder(p+new Vector3(0,.7f+k*.85f,-.56f),.45f,.06f,Steel,12,Quaternion.Euler(90,0,0));
                        mesh.Box(p+new Vector3(.82f,.7f+k*.85f,0),new Vector3(.08f,.36f,1.05f),accent);
                    }
                }
                mesh.Cone(p+Vector3.up*.45f,.25f,.8f,accent,5);
            }
            // Original abstract iron gate, chains, and bell: visual references, not copied album art.
            for(int s=-1;s<=1;s+=2)
            {
                mesh.Box(new Vector3(s*7,2.2f,20),new Vector3(.65f,4.4f,.65f),Steel);
                for(int k=0;k<7;k++) mesh.Box(new Vector3(s*7,1+k*.45f,19.6f),new Vector3(.32f,.15f,.25f),accent);
            }
            mesh.Box(new Vector3(0,4.25f,20),new Vector3(14.5f,.35f,.5f),Steel);
            mesh.Cylinder(new Vector3(0,3.2f,20),.7f,1f,Hex("#AC8250"),12);
            mesh.Cylinder(new Vector3(0,2.65f,20),.9f,.13f,Hex("#D5AD6C"),12);
            // Small original sculptures, not reproductions of covers or stage designs.
            if (chapter.id == "roots")
            {
                Vector3 p = new Vector3(-10, 0, 16);
                mesh.Box(p + Vector3.up * .3f, new Vector3(3,.6f,2), Steel);
                mesh.Box(p + Vector3.up * 2.1f, new Vector3(1.3f,1.8f,.7f), Hex("#788491"));
                mesh.Box(p + Vector3.up * 3.3f, new Vector3(.85f,.85f,.65f), Hex("#A6AFB8"));
                for (int sign=-1; sign<=1; sign+=2)
                {
                    mesh.Box(p + new Vector3(sign*.42f,.95f,0), new Vector3(.42f,1.3f,.55f), Steel);
                    mesh.Box(p + new Vector3(sign*.88f,2.15f,0), new Vector3(.4f,1.5f,.5f), Steel);
                }
            }
            else if (chapter.id == "thrash")
            {
                Vector3 p = new Vector3(-10, 0, 16);
                mesh.Box(p + Vector3.up * 4.6f, new Vector3(3,.16f,1.4f), Bone);
                for (int sign=-1; sign<=1; sign+=2)
                {
                    mesh.Cylinder(p + new Vector3(sign*.65f,3.35f,0), .022f,2.5f, Bone,6);
                    mesh.Box(p + new Vector3(sign*.42f,1.05f,0), new Vector3(.3f,1.3f,.4f), Steel);
                    mesh.Box(p + new Vector3(sign*.65f,2.05f,0), new Vector3(.3f,.9f,.4f), Steel);
                }
                mesh.Box(p + Vector3.up * 1.8f, new Vector3(.85f,1.05f,.5f), accent);
                mesh.Octahedron(p + Vector3.up * 2.65f, Vector3.one * .42f, Bone);
            }
            var deco = Make("Act architecture",mesh,chapterRoot.transform);
            chapterMesh = deco.GetComponent<MeshFilter>().sharedMesh;
            title.text = chapter.title;
            title.color = Color.Lerp(accent,Color.white,.25f);
            style.text = chapter.style.ToUpperInvariant();
            rimLight.color = Color.Lerp(accent,Color.white,.35f);
            WorldText(chapter.tribute, new Vector3(0,.2f,19f), .43f, accent, chapterRoot.transform);
            if (!string.IsNullOrEmpty(chapter.songReference))
                WorldText(chapter.songReference.Split('·')[0].Trim(), new Vector3(-10,.3f,13.7f), .43f,
                    Color.Lerp(accent,Color.white,.3f), chapterRoot.transform);
        }

        public void Tick(float time, float delta, GameOptions options, BandStats stats)
        {
            for(int i=0;i<4;i++) Band[i].Tick(time,delta,options.reducedMotion,stats.WeaponRank((BandRole)i));
            if(!options.reducedMotion) record.Rotate(Vector3.up,delta*9f,Space.Self);
            drone.localPosition = new Vector3(Mathf.Cos(time*.45f)*4.9f,2.1f+Mathf.Sin(time*1.6f)*.12f,Mathf.Sin(time*.45f)*4.9f);
            if(options.reducedMotion) drone.localPosition = new Vector3(4.9f,2.1f,0);
            drone.localRotation = Quaternion.Euler(0,-time*25f,0);
            shake = Mathf.MoveTowards(shake,0,delta*1.4f);
            Vector3 offset = options.reducedMotion ? Vector3.zero : new Vector3(Mathf.Sin(time*68f),Mathf.Sin(time*57f),0)*shake*.2f;
            Camera.transform.position = cameraHome + offset;
            Camera.orthographicSize = Mathf.Max(18.6f, 24f / Mathf.Max(.5f,Camera.aspect));
        }
        public void Shake(float strength) { shake = Mathf.Max(shake,Mathf.Clamp01(strength)); }
        public void SetQuality(int quality)
        {
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = quality == 0 ? 0 : quality == 1 ? 2 : 4;
            QualitySettings.shadowDistance = quality == 0 ? 0 : 55;
            keyLight.shadows = quality == 0 ? LightShadows.None : LightShadows.Soft;
        }
        public GameObject EnemyObject(EnemyKind kind, Transform parent)
        {
            return MeshObject("Enemy • "+kind,enemyMeshes[(int)kind],parent,Solid);
        }
        public GameObject Make(string name, MeshCraft craft, Transform parent, Material material = null)
        {
            Mesh mesh=craft.Build(name); meshes.Add(mesh);
            return MeshObject(name,mesh,parent,material??Solid);
        }
        public TextMesh WorldText(string value, Vector3 position, float size, Color color, Transform parent = null)
        {
            var go = new GameObject("Inscription • "+value); go.transform.SetParent(parent??Root,false);
            go.transform.localPosition=position; go.transform.rotation=Camera.transform.rotation;
            var text=go.AddComponent<TextMesh>(); text.font=Font; text.fontSize=60;
            text.characterSize=size/6f; text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center;
            text.text=value; text.color=color;
            go.GetComponent<MeshRenderer>().sharedMaterial=Font.material;
            return text;
        }
        private static GameObject MeshObject(string name, Mesh mesh, Transform parent, Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true;
            return go;
        }
        private Light Light(string name, Vector3 position, Vector3 angles, Color color, float intensity)
        {
            var go=new GameObject(name);go.transform.SetParent(Root,false);go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(angles);
            var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;
            light.shadows=LightShadows.None;return light;
        }
        private void Speaker(Vector3 position,float scale,Transform parent)
        {
            var mesh=new MeshCraft();
            mesh.Box(new Vector3(0,.8f,0),new Vector3(.95f,1.6f,.65f),Dark);
            for(int i=0;i<2;i++)
            {
                mesh.Cylinder(new Vector3(0,.42f+i*.74f,-.34f),.33f,.06f,Steel,14,Quaternion.Euler(90,0,0));
                mesh.Cylinder(new Vector3(0,.42f+i*.74f,-.38f),.13f,.04f,Hex("#8692A0"),10,Quaternion.Euler(90,0,0));
            }
            mesh.Box(new Vector3(0,1.58f,-.36f),new Vector3(.6f,.07f,.04f),Hex("#C6AC76"));
            var go=Make("Amplifier • unbranded",mesh,parent);go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;
        }
        private MusicianRig Musician(BandRole role,Vector3 position)
        {
            int index=(int)role;
            Color accent=MemberColors[index];
            bool roxy=role==BandRole.Voice;
            bool jack=role==BandRole.Guitar;
            bool morten=role==BandRole.Bass;
            bool varg=role==BandRole.Drums;

            var root=new GameObject(MemberNames[index]+" • "+MemberStyles[index]+" • "+role).transform;
            root.SetParent(Root,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,180,0);

            // Distinct silhouettes make every musician readable at gameplay distance.
            float torsoWidth=roxy?.57f:morten?.78f:varg?.64f:.67f;
            Color skin=varg?Hex("#E3E2DE"):morten?Hex("#A76E52"):jack?Hex("#C08E72"):Hex("#C69077");
            Color leather=Hex("#11141A");
            Color cloth=Hex("#252A33");
            var body=new MeshCraft();

            body.Box(new Vector3(0,1.08f,0),new Vector3(torsoWidth,.72f,.38f),leather);
            body.Box(new Vector3(0,.77f,-.01f),new Vector3(torsoWidth+.02f,.14f,.42f),Steel);
            body.Box(new Vector3(0,.79f,.225f),new Vector3(.19f,.105f,.04f),accent);

            // Jacket lapels / vest identity.
            body.Box(new Vector3(-.12f,1.15f,.215f),new Vector3(.12f,.48f,.035f),jack?Hex("#2D3138"):accent*.48f,Quaternion.Euler(0,0,-12));
            body.Box(new Vector3(.12f,1.15f,.215f),new Vector3(.12f,.48f,.035f),jack?Hex("#2D3138"):accent*.48f,Quaternion.Euler(0,0,12));

            for(int s=-1;s<=1;s+=2)
            {
                Color trousers=roxy?(s<0?Hex("#4E2478"):leather):cloth;
                body.Box(new Vector3(s*.19f,.4f,0),new Vector3(morten?.27f:.23f,.69f,.24f),trousers,Quaternion.Euler(0,0,s*-5));
                body.Box(new Vector3(s*.23f,.085f,.11f),new Vector3(morten?.32f:.29f,.18f,.48f),leather);
                body.Box(new Vector3(s*.3f,1.2f,.23f),new Vector3(.07f,.35f,.04f),accent);
                for(int stud=0;stud<3;stud++)
                    body.Octahedron(new Vector3(s*(.27f+stud*.06f),1.47f,0),new Vector3(.045f,.08f,.045f),Bone);
            }

            // Individual costume signatures from the approved concepts.
            if(roxy)
            {
                body.Box(new Vector3(-.30f,.70f,.18f),new Vector3(.08f,.86f,.05f),Hex("#D22542"),Quaternion.Euler(0,0,-6));
                body.Box(new Vector3(.29f,.56f,.19f),new Vector3(.075f,.72f,.05f),Hex("#EEE0C6"),Quaternion.Euler(0,0,7));
                for(int i=0;i<5;i++)
                    body.Box(new Vector3(.25f+(i%2)*.045f,.28f+i*.11f,.142f),new Vector3(.038f,.035f,.02f),i%2==0?Dark:Hex("#C7A78A"));
            }
            else if(jack)
            {
                body.Box(new Vector3(-.19f,1.22f,.235f),new Vector3(.18f,.13f,.025f),Hex("#8D322B"));
                body.Box(new Vector3(.20f,1.03f,.235f),new Vector3(.16f,.11f,.025f),Hex("#C8B177"));
                for(int i=0;i<6;i++)
                    body.Octahedron(new Vector3(-.34f+i*.055f,.70f,.225f),new Vector3(.025f,.025f,.018f),Steel);
            }
            else if(morten)
            {
                body.Box(new Vector3(-.20f,1.26f,.235f),new Vector3(.18f,.14f,.025f),Hex("#D8D1C0"));
                body.Box(new Vector3(.20f,1.10f,.235f),new Vector3(.18f,.14f,.025f),Hex("#6F2025"));
                body.Box(new Vector3(0,.96f,.238f),new Vector3(.15f,.12f,.025f),Hex("#C4B894"));
            }
            else if(varg)
            {
                for(int strip=-2;strip<=2;strip++)
                    body.Box(new Vector3(strip*.12f,.68f,-.15f),new Vector3(.075f,1.15f,.07f),Hex("#0A0C10"),Quaternion.Euler(0,0,strip*3));
                for(int s=-1;s<=1;s+=2)
                    body.Cone(new Vector3(s*.42f,1.40f,-.03f),.10f,.38f,Steel,4,Quaternion.Euler(0,0,s*-58));
            }
            Make("Signature leather, vest and stagewear • "+MemberStyles[index],body,root);

            var headRoot=new GameObject("Headbang pivot • "+MemberNames[index]).transform;
            headRoot.SetParent(root,false);headRoot.localPosition=new Vector3(0,1.55f,0);
            var headMesh=new MeshCraft();
            headMesh.Box(new Vector3(0,.19f,0),new Vector3(.4f,.44f,.36f),skin);

            if(roxy)
            {
                // Platinum teased glam hair, purple bandana and stage make-up.
                Color blonde=Hex("#E8D9AD");
                headMesh.Box(new Vector3(0,.49f,-.04f),new Vector3(.57f,.22f,.48f),blonde);
                headMesh.Box(new Vector3(-.28f,.17f,-.02f),new Vector3(.17f,.78f,.39f),blonde,Quaternion.Euler(0,0,-8));
                headMesh.Box(new Vector3(.28f,.17f,-.02f),new Vector3(.17f,.78f,.39f),blonde,Quaternion.Euler(0,0,8));
                for(int i=0;i<5;i++)
                    headMesh.Octahedron(new Vector3(-.28f+i*.14f,.56f+(i%2)*.06f,-.04f),new Vector3(.13f,.17f,.13f),blonde);
                headMesh.Box(new Vector3(0,.39f,.205f),new Vector3(.46f,.075f,.035f),accent);
                for(int s=-1;s<=1;s+=2)
                    headMesh.Box(new Vector3(s*.12f,.245f,.194f),new Vector3(.12f,.035f,.025f),Hex("#342040"));
                headMesh.Box(new Vector3(0,.08f,.19f),new Vector3(.18f,.045f,.025f),Hex("#B63862"));
            }
            else if(jack)
            {
                Color hair=Hex("#15161A");
                headMesh.Box(new Vector3(0,.45f,-.05f),new Vector3(.48f,.18f,.43f),hair);
                headMesh.Box(new Vector3(-.24f,.12f,-.03f),new Vector3(.15f,.82f,.36f),hair,Quaternion.Euler(0,0,-4));
                headMesh.Box(new Vector3(.24f,.12f,-.03f),new Vector3(.15f,.82f,.36f),hair,Quaternion.Euler(0,0,4));
                headMesh.Box(new Vector3(0,.015f,.17f),new Vector3(.28f,.16f,.08f),Hex("#252126"));
                for(int s=-1;s<=1;s+=2)
                    headMesh.Box(new Vector3(s*.12f,.25f,.191f),new Vector3(.11f,.05f,.025f),Dark);
            }
            else if(morten)
            {
                Color hair=Hex("#2B211E");
                headMesh.Box(new Vector3(0,.45f,-.05f),new Vector3(.50f,.18f,.43f),hair);
                headMesh.Box(new Vector3(-.25f,.10f,-.02f),new Vector3(.16f,.86f,.36f),hair);
                headMesh.Box(new Vector3(.25f,.10f,-.02f),new Vector3(.16f,.86f,.36f),hair);
                headMesh.Box(new Vector3(0,-.02f,.13f),new Vector3(.34f,.34f,.18f),hair);
                headMesh.Box(new Vector3(0,-.18f,.11f),new Vector3(.28f,.22f,.16f),hair);
                for(int s=-1;s<=1;s+=2)
                    headMesh.Box(new Vector3(s*.12f,.25f,.191f),new Vector3(.11f,.05f,.025f),Dark);
            }
            else
            {
                // Black metal corpse paint: pale base, dark eyes and vertical paint tears.
                Color hair=Hex("#090B0E");
                headMesh.Box(new Vector3(0,.46f,-.05f),new Vector3(.48f,.17f,.43f),hair);
                headMesh.Box(new Vector3(-.25f,.06f,-.03f),new Vector3(.15f,.94f,.38f),hair);
                headMesh.Box(new Vector3(.25f,.06f,-.03f),new Vector3(.15f,.94f,.38f),hair);
                for(int s=-1;s<=1;s+=2)
                {
                    headMesh.Box(new Vector3(s*.12f,.255f,.194f),new Vector3(.14f,.08f,.026f),Hex("#090A0C"));
                    headMesh.Box(new Vector3(s*.12f,.14f,.195f),new Vector3(.045f,.20f,.027f),Hex("#090A0C"));
                }
                headMesh.Box(new Vector3(0,.065f,.19f),new Vector3(.18f,.06f,.027f),Hex("#0A0A0C"));
            }
            Make("Face, hair and make-up • "+MemberStyles[index],headMesh,headRoot);

            Transform[] arms=new Transform[2];
            for(int i=0;i<2;i++)
            {
                int s=i==0?-1:1;
                arms[i]=new GameObject(i==0?"Left arm":"Right arm").transform;
                arms[i].SetParent(root,false);arms[i].localPosition=new Vector3(s*(morten?.46f:.4f),1.36f,0);
                var arm=new MeshCraft();
                Color upper=morten?skin:leather;
                arm.Box(new Vector3(0,-.15f,0),new Vector3(morten?.25f:.22f,.33f,.23f),upper);
                arm.Box(new Vector3(0,-.41f,.05f),new Vector3(morten?.21f:.18f,.3f,.19f),skin);
                arm.Box(new Vector3(0,-.45f,.06f),new Vector3(.20f,.11f,.21f),accent*.65f);
                if(morten)
                {
                    arm.Box(new Vector3(0,-.19f,.122f),new Vector3(.055f,.18f,.018f),Dark,Quaternion.Euler(0,0,28));
                    arm.Box(new Vector3(0,-.38f,.148f),new Vector3(.06f,.16f,.018f),Dark,Quaternion.Euler(0,0,-28));
                }
                if(varg)
                    for(int stud=0;stud<3;stud++)arm.Cone(new Vector3((stud-1)*.055f,-.46f,.08f),.035f,.13f,Steel,4,Quaternion.Euler(90,0,0));
                if(role==BandRole.Drums)
                    arm.Box(new Vector3(0,-.56f,.27f),new Vector3(.035f,.035f,.52f),Bone);
                Make("Arm • "+MemberStyles[index],arm,arms[i]);
            }

            var instrument=new GameObject("Instrument • "+MemberNames[index]).transform;
            instrument.SetParent(root,false);
            var instrumentMesh=new MeshCraft();
            if(jack)
            {
                // Classic-metal Flying-V silhouette.
                instrument.localPosition=new Vector3(.05f,.91f,.4f);instrument.localRotation=Quaternion.Euler(0,0,-31);
                instrumentMesh.Box(new Vector3(-.17f,-.18f,0),new Vector3(.20f,.78f,.15f),accent,Quaternion.Euler(0,0,-31));
                instrumentMesh.Box(new Vector3(.17f,-.18f,0),new Vector3(.20f,.78f,.15f),accent,Quaternion.Euler(0,0,31));
                instrumentMesh.Box(new Vector3(0,.43f,0),new Vector3(.12f,1.00f,.095f),Hex("#A37B4A"));
                instrumentMesh.Box(new Vector3(0,.92f,0),new Vector3(.22f,.31f,.12f),Hex("#17191E"));
                instrumentMesh.Box(new Vector3(0,.02f,.088f),new Vector3(.14f,.18f,.018f),Bone);
                for(int k=0;k<6;k++)instrumentMesh.Box(new Vector3(-.04f+k*.016f,.44f,.063f),new Vector3(.005f,.88f,.006f),Bone);
            }
            else if(morten)
            {
                instrument.localPosition=new Vector3(.05f,.91f,.4f);instrument.localRotation=Quaternion.Euler(0,0,-31);
                instrumentMesh.Box(new Vector3(-.18f,-.06f,0),new Vector3(.33f,.52f,.16f),Hex("#332D2B"),Quaternion.Euler(0,0,-26));
                instrumentMesh.Box(new Vector3(.17f,-.02f,0),new Vector3(.31f,.48f,.16f),Hex("#332D2B"),Quaternion.Euler(0,0,24));
                for(int s=-1;s<=1;s+=2)instrumentMesh.Cone(new Vector3(s*.30f,-.22f,0),.11f,.42f,Hex("#76675A"),5,Quaternion.Euler(0,0,s*-48));
                instrumentMesh.Box(new Vector3(0,.52f,0),new Vector3(.13f,1.24f,.10f),Hex("#6F4A33"));
                instrumentMesh.Box(new Vector3(0,1.13f,0),new Vector3(.24f,.34f,.13f),Hex("#17191E"));
                for(int k=0;k<4;k++)instrumentMesh.Box(new Vector3(-.03f+k*.02f,.50f,.066f),new Vector3(.006f,1.05f,.006f),Bone);
            }
            else if(roxy)
            {
                // Chrome microphone with purple head and long glam scarves.
                instrumentMesh.Cylinder(new Vector3(.32f,.78f,.6f),.032f,1.55f,Steel,8);
                instrumentMesh.Cylinder(new Vector3(.32f,.025f,.6f),.32f,.05f,Steel,12);
                instrumentMesh.Cylinder(new Vector3(.32f,1.52f,.55f),.062f,.24f,accent,8,Quaternion.Euler(80,0,0));
                instrumentMesh.Octahedron(new Vector3(.32f,1.53f,.69f),Vector3.one*.10f,Bone);
                instrumentMesh.Box(new Vector3(.39f,.60f,.59f),new Vector3(.055f,1.10f,.035f),Hex("#7D2FA3"),Quaternion.Euler(0,0,-3));
                instrumentMesh.Box(new Vector3(.47f,.52f,.59f),new Vector3(.045f,.95f,.035f),Hex("#C72D4B"),Quaternion.Euler(0,0,5));
            }
            Make("Signature instrument • "+MemberStyles[index],instrumentMesh,instrument);

            var evo=new Transform[2];
            for(int tier=0;tier<2;tier++)
            {
                var m=new MeshCraft();
                for(int s=-1;s<=1;s+=2)
                    m.Cone(new Vector3(s*(.48f+tier*.15f),1.15f,-.15f),.15f,.65f+tier*.2f,accent,4,Quaternion.Euler(0,0,s*-42));
                evo[tier]=Make(tier==0?"Evolution I • resonance fins":"Evolution II • ultimate rig",m,root,Glow).transform;
                evo[tier].gameObject.SetActive(false);
            }
            return new MusicianRig{Root=root,Anchor=position,Head=headRoot,LeftArm=arms[0],RightArm=arms[1],Instrument=instrument,Evolution=evo,Role=role,Phase=index*1.7f};
        }

        private void Drums(Vector3 position)
        {
            var m=new MeshCraft();Color amber=MemberColors[3];
            m.Cylinder(new Vector3(0,.5f,-.78f),.59f,.65f,amber,16,Quaternion.Euler(90,0,0));
            m.Cylinder(new Vector3(0,.5f,-1.12f),.53f,.04f,Bone,16,Quaternion.Euler(90,0,0));
            m.Octahedron(new Vector3(0,.5f,-1.16f),new Vector3(.22f,.27f,.025f),Dark);
            for(int s=-1;s<=1;s+=2)
            {
                m.Cylinder(new Vector3(s*.7f,.7f,-.25f),.35f,.33f,amber,12);
                m.Cylinder(new Vector3(s*.7f,.88f,-.25f),.35f,.025f,Bone,12);
                m.Cylinder(new Vector3(s*1.08f,.64f,.13f),.028f,1.3f,Steel,6);
                m.Cylinder(new Vector3(s*1.08f,1.32f,.13f),.49f,.035f,Hex("#CBB164"),16);
                m.Cone(new Vector3(s*1.08f,1.35f,.13f),.14f,.07f,Hex("#E6CB7A"),12);
                m.Box(new Vector3(s*.67f,.38f,-.27f),new Vector3(.04f,.65f,.04f),Steel);
            }
            Make("Seven-piece original drum kit",m,Root).transform.localPosition=position;
        }
        private Mesh BuildEnemy(EnemyKind kind)
        {
            var m=new MeshCraft();int k=(int)kind;
            Color[] c={Hex("#828B7D"),Hex("#B28582"),Hex("#777F89"),Hex("#8DA56D"),Hex("#939DBA"),Hex("#A683AD"),Hex("#657D9A"),Hex("#B56B61")};
            Color flesh=c[k],cloth=Color.Lerp(flesh,Dark,.70f);
            m.Box(new Vector3(0,.82f,0),new Vector3(.55f,.65f,.36f),cloth);
            m.Octahedron(new Vector3(0,1.37f,0),new Vector3(.32f,.35f,.28f),Bone);
            for(int s=-1;s<=1;s+=2)
            {
                m.Box(new Vector3(s*.17f,.26f,0),new Vector3(.17f,.5f,.19f),cloth,Quaternion.Euler(0,0,s*-8));
                m.Box(new Vector3(s*.2f,.07f,.13f),new Vector3(.22f,.14f,.36f),Dark);
                m.Box(new Vector3(s*.39f,.78f,.12f),new Vector3(.15f,.67f,.17f),flesh,Quaternion.Euler(-22,0,s*15));
                m.Box(new Vector3(s*.11f,1.42f,.225f),new Vector3(.085f,.09f,.035f),Hex("#F34D51"));
            }
            m.Box(new Vector3(0,1.2f,.23f),new Vector3(.2f,.075f,.035f),Dark);
            if(kind==EnemyKind.Brute || kind==EnemyKind.Boss)
            {
                for(int s=-1;s<=1;s+=2)
                {
                    m.Box(new Vector3(s*.4f,1.08f,0),new Vector3(.35f,.35f,.5f),Steel);
                    m.Cone(new Vector3(s*.42f,1.22f,0),.15f,.58f,Bone,5,Quaternion.Euler(0,0,s*-23));
                    m.Cone(new Vector3(s*.23f,1.55f,0),.13f,.7f,flesh,5,Quaternion.Euler(0,0,s*-25));
                }
            }
            if(kind==EnemyKind.Shielded)m.Box(new Vector3(.29f,.8f,.49f),new Vector3(.61f,.92f,.15f),Hex("#74889F"),Quaternion.Euler(0,0,-13));
            if(kind==EnemyKind.Spitter)m.Octahedron(new Vector3(0,.82f,-.33f),new Vector3(.4f,.43f,.33f),Hex("#A8BC58"));
            if(kind==EnemyKind.Rusher)
                for(int i=0;i<4;i++)m.Cone(new Vector3(0,1.62f,-.15f+i*.1f),.09f,.38f,Hex("#E0995A"),4);
            if(kind==EnemyKind.Splitter)
                for(int s=-1;s<=1;s+=2)m.Octahedron(new Vector3(s*.35f,1.02f,-.22f),new Vector3(.23f,.32f,.23f),Hex("#C187CC"));
            if(kind==EnemyKind.Summoner || kind==EnemyKind.Boss)
            {
                m.Cone(new Vector3(0,.13f,-.04f),.55f,1.2f,cloth,8);
                for(int s=-1;s<=1;s+=2)m.Cylinder(new Vector3(s*.65f,.95f,0),.04f,1.9f,Steel,6);
                m.Octahedron(new Vector3(-.65f,1.97f,0),new Vector3(.24f,.4f,.24f),Hex("#B176D6"));
            }
            Mesh mesh=m.Build("Original creature • "+kind);meshes.Add(mesh);return mesh;
        }
        public static Color Hex(string value)
        { Color color; return ColorUtility.TryParseHtmlString(value,out color)?color:Color.white; }
        public void Dispose()
        {
            foreach(Mesh mesh in meshes)if(mesh!=null)Object.Destroy(mesh);
            meshes.Clear();if(Solid!=null)Object.Destroy(Solid);if(Glow!=null)Object.Destroy(Glow);
            if(Root!=null)Object.Destroy(Root.gameObject);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace RagnaRock
{
    public sealed class UiKit : IDisposable
    {
        public static readonly Color Ink=StageArt.Hex("#0D1420");
        public static readonly Color Panel=StageArt.Hex("#172230");
        public static readonly Color TextColor=StageArt.Hex("#F0EADF");
        public static readonly Color Muted=StageArt.Hex("#A4B4C8");
        public static readonly Color Gold=StageArt.Hex("#E0BA75");
        private readonly Font font;
        private readonly Texture2D texture;
        private readonly Sprite rounded;
        public UiKit(Font font)
        {
            this.font=font;
            texture=new Texture2D(48,48,TextureFormat.RGBA32,false){name="Original rounded UI mask",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[48*48];
            for(int y=0;y<48;y++)for(int x=0;x<48;x++)
            {
                float dx=Mathf.Max(12-x,x-35),dy=Mathf.Max(12-y,y-35);
                float distance=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;
                pixels[y*48+x]=new Color(1,1,1,Mathf.Clamp01(12.5f-distance));
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            rounded=Sprite.Create(texture,new Rect(0,0,48,48),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
        }
        public RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
            return rect;
        }
        public Image Image(Transform parent,string name,Color color,Vector2 min,Vector2 max,bool round=true)
        {
            var rect=Rect(parent,name,min,max);var image=rect.gameObject.AddComponent<Image>();
            image.color=color;if(round){image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;}
            return image;
        }
        public Text Label(Transform parent,string name,string value,int size,Color color,Vector2 min,Vector2 max,
            TextAnchor align=TextAnchor.MiddleLeft,FontStyle style=FontStyle.Normal)
        {
            var rect=Rect(parent,name,min,max);var label=rect.gameObject.AddComponent<Text>();
            label.font=font;label.text=value;label.fontSize=size;label.fontStyle=style;label.color=color;
            label.alignment=align;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            if(name=="Instructions"||name=="References"||name=="Lore"||name=="Effect"||name=="Question"||name=="Act context")
            {
                label.resizeTextForBestFit=true;label.resizeTextMaxSize=size;
                label.resizeTextMinSize=Mathf.Max(14,Mathf.FloorToInt(size*.78f));
            }
            return label;
        }
        public Button Button(Transform parent,string name,string text,Vector2 min,Vector2 max,UnityAction action,Color? color=null,int size=24)
        {
            var image=Image(parent,name,color??Panel,min,max);var button=image.gameObject.AddComponent<Button>();
            button.targetGraphic=image;
            ColorBlock palette=button.colors;palette.normalColor=Color.white;palette.highlightedColor=new Color(1.18f,1.18f,1.18f);
            palette.pressedColor=new Color(.8f,.84f,.9f);palette.selectedColor=new Color(1.12f,1.12f,1.12f);
            palette.disabledColor=new Color(.42f,.45f,.5f,.6f);palette.fadeDuration=.10f;button.colors=palette;
            Label(image.transform,"Caption",text,size,TextColor,new Vector2(.04f,.05f),new Vector2(.96f,.95f),TextAnchor.MiddleCenter,FontStyle.Bold);
            if(action!=null)button.onClick.AddListener(action);return button;
        }
        public Image Bar(Transform parent,string name,Color color,Vector2 min,Vector2 max)
        {
            var background=Image(parent,name+" track",new Color(.05f,.08f,.12f,.95f),min,max);
            var fill=Image(background.transform,name+" fill",color,Vector2.zero,Vector2.one);
            return fill;
        }
        public static void Fill(Image bar,float amount)
        {
            float value=Mathf.Clamp01(amount);bar.rectTransform.anchorMax=new Vector2(value,1);
            bar.gameObject.SetActive(value>.0001f);
        }
        public void Dispose(){Object.Destroy(rounded);Object.Destroy(texture);}
    }
}

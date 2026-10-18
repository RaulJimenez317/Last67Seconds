using TMPro;
using Text = TMPro.TextMeshProUGUI;
using Codice.Characters;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Codice.UI
{
    public static class UIWidgets
    {
        static TMP_FontAsset font;
        public static void ConfigureCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Keep the entire reference layout visible on 4:3, 16:10 and ultrawide displays.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,string shape="solid")
        {var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=ProceduralUISprites.Shape(shape);i.color=color;if(shape=="frame")i.type=UnityEngine.UI.Image.Type.Sliced;i.raycastTarget=false;return i;}
        public static Text Label(Transform parent,string name,float x,float y,float w,float h,int size=20,Color? color=null)
        {
            if (!font) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var t = Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color ?? new Color(.95f,.94f,.91f);
            t.raycastTarget = false; t.richText = true; t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }
        public static Button Button(Transform parent,string text,float x,float y,float w,float h,UnityAction action)
        {var image=Image(parent,text,x,y,w,h,ProceduralUISprites.Cyan,"frame");image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);var label=Label(image.transform,"Label",10,8,w-20,h-10,19);label.text=text;label.alignment=TextAlignmentOptions.Center;return b;}
        public static Image Filled(Transform parent,string name,float x,float y,float w,float h,Color color,bool radial=false)
        {var i=Image(parent,name,x,y,w,h,color,radial?"ring":"solid");i.type=UnityEngine.UI.Image.Type.Filled;i.fillMethod=radial?UnityEngine.UI.Image.FillMethod.Radial360:UnityEngine.UI.Image.FillMethod.Horizontal;i.fillOrigin=0;return i;}
        public static string WeaponName(WeaponKind k)=>new[]{"Espada","Hacha","Arco","Daga","Martillo","Lanza","Guadaña","Escudo","Báculo","Mosquete"}[(int)k];
        public static string Special(WeaponKind k)=>new[]{"Danza de Acero","Hendidura Sísmica","Lluvia Perforante","Paso Sombrío","Pulso Gravitatorio","Empalamiento Imperial","Tajo Sangriento","Bastión Inmortal","Singularidad Arcana","Disparo Perforatitanes"}[(int)k];
        public static string Passive(PassiveKind k)=>new[]{"Frenesí","Corazón Vampírico","Sobrecarga","Botas de Mercurio","Coraza de Espinas"}[(int)k];
        public static string Clock(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}.{(int)(seconds*100)%100:00}";
    }
    public sealed class GhostBar : MonoBehaviour
    {
        public Image Fill {get;private set;}
        public Image Ghost {get;private set;}
        public Text Caption {get;private set;}
        float previous=-1,delay;
        public static GhostBar Create(Transform parent,string name,float x,float y,float width,float height)
        {
            var frame=UIWidgets.Image(parent,name,x,y,width,height,ProceduralUISprites.Ink,"frame");var bar=frame.gameObject.AddComponent<GhostBar>();
            UIWidgets.Image(frame.transform,"Well",5,5,width-10,height-10,ProceduralUISprites.Abyss);
            bar.Ghost=UIWidgets.Filled(frame.transform,"Rally",5,5,width-10,height-10,ProceduralUISprites.Gold);
            bar.Fill=UIWidgets.Filled(frame.transform,"Health",5,5,width-10,height-10,ProceduralUISprites.Crimson);
            bar.Caption=UIWidgets.Label(frame.transform,"Value",9,3,width-18,height-3,Mathf.Min(20,(int)height-6));bar.Caption.alignment=TextAlignmentOptions.Center;return bar;
        }
        public void Set(float health,float maximum,string caption=null)
        {
            float value=maximum>0?Mathf.Clamp01(health/maximum):0;
            if(previous<0||value>previous) Ghost.fillAmount=value;
            else if(value<previous)delay=Time.unscaledTime+.35f;
            Fill.fillAmount=value;previous=value;Caption.text=caption??$"{Mathf.CeilToInt(health)} / {maximum:0}";
        }
        void Update(){if(Time.unscaledTime>=delay)Ghost.fillAmount=Mathf.Lerp(Ghost.fillAmount,Fill.fillAmount,1-Mathf.Exp(-7*Time.unscaledDeltaTime));}
    }
}

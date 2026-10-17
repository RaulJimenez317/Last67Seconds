using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace Codice.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        RectTransform home,options,classes;
        Text values,displayMode;
        static readonly Color Gold=new Color(.88f,.71f,.43f), Cream=new Color(.95f,.92f,.85f), Muted=new Color(.57f,.61f,.66f);
        void Start()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(PlayerPrefs.HasKey("Codice.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Codice.Fullscreen")==1;
            AudioListener.volume=PlayerPrefs.GetFloat("Codice.Volume",1);
            var root=new GameObject("Main menu canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            var bg=UIWidgets.Image(root.transform,"Background",0,0,1920,1080,new Color(.035f,.043f,.065f)).rectTransform;bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;
            home=Centered(root.transform,"Main Menu",1680,900);
            options=Centered(root.transform,"Opciones",760,900);options.gameObject.SetActive(false);
            Label(home,"Edition",80,35,700,30,18,Gold,"CÓDICE DE CENIZA     /     UN CICLO SIN FINAL");
            Line(home,80,85,1520,new Color(.3f,.28f,.23f));
            Label(home,"Chapter",80,138,700,30,18,Muted,"ROGUELIKE  •  ACCIÓN EN PRIMERA PERSONA");
            Label(home,"Title",74,185,800,230,82,Cream,"CÓDICE\nDE CENIZA");
            UIWidgets.Image(home,"Title accent",80,422,72,4,Gold);
            Label(home,"Subtitle",80,463,650,78,25,Muted,"Desciende a las ruinas. Reclama su poder.\nQue las cenizas cuenten tu historia.");
            var first=Button(home,"INICIAR JUEGO",80,580,620,78,()=>ShowClasses(true),true);
            Button(home,"OPCIONES",80,686,295,62,()=>ShowOptions(true));
            Button(home,"SALIR",405,686,295,62,RunNavigation.Quit);
            var art=UIWidgets.Rect(home,"Relics",920,140,620,620).gameObject.AddComponent<RawImage>();
            art.gameObject.AddComponent<MenuRelics>().Initialize(art);
            var caption=Label(home,"Relic caption",920,740,620,32,20,Gold,"RELIQUIAS DEL CÓDICE");caption.alignment=TextAnchor.MiddleCenter;
            var sub=Label(home,"Relic hint",920,782,620,30,17,Muted,"Recoge sus sellos. Despierta su poder con T / Y / U.");sub.alignment=TextAnchor.MiddleCenter;
            Line(home,80,835,1520,new Color(.3f,.28f,.23f));
            Label(home,"Footer",80,863,1000,28,17,Muted,"WASD  Mover     •     Clic  Atacar     •     E  Interactuar     •     Esc  Pausa");
            Label(home,"Navigation",1220,863,400,28,17,Gold,"↑ ↓  Elegir     /     Enter  Confirmar");
            Label(options,"Eyebrow",40,30,680,30,18,Gold,"CÓDICE DE CENIZA    /    AJUSTES");
            Label(options,"Heading",35,100,690,85,64,Cream,"A tu manera.");
            Label(options,"Description",40,195,680,38,21,Muted,"Prepara tu próximo descenso.");
            Line(options,40,262,680,new Color(.3f,.28f,.23f));
            values=Label(options,"Values",40,303,680,140,25,Cream,"");
            Button(options,"VOLUMEN −",40,480,325,60,()=>Volume(-.1f));Button(options,"VOLUMEN +",395,480,325,60,()=>Volume(.1f));
            Button(options,"SENSIBILIDAD −",40,562,325,60,()=>Sensitivity(-.01f));Button(options,"SENSIBILIDAD +",395,562,325,60,()=>Sensitivity(.01f));
            var display=Button(options,"CAMBIAR PANTALLA COMPLETA",40,644,680,60,()=>{bool full=!Screen.fullScreen;Screen.fullScreen=full;PlayerPrefs.SetInt("Codice.Fullscreen",full?1:0);PlayerPrefs.Save();});
            displayMode=display.GetComponentInChildren<Text>();
            Button(options,"VOLVER",40,757,680,68,()=>ShowOptions(false),true);
            Label(options,"Saved",40,854,680,30,17,Muted,"Los cambios se guardan automáticamente.  •  Esc para volver");
            classes=Centered(root.transform,"Elegir clase",1680,900);
            Label(classes,"Chapter",80,35,1520,35,18,Gold,"CÓDICE DE CENIZA  /  NUEVO DESCENSO");
            Label(classes,"Title",75,115,1520,90,60,Cream,"Elige quién desafía las cenizas.");
            Label(classes,"Hint",80,220,1520,55,24,Muted,"Cada clase empieza con su propia arma. Podrás encontrar y comprar otras durante la partida.");
            string[] names={"CABALLERO","ARQUERO","TANQUE"};
            string[] stats={"120 VIDA   /   VELOCIDAD 6.5\n\nEspada • Daño equilibrado\nClic izquierdo: atacar\nMantén clic derecho: escudo\nBloquea el 50% del daño frontal.","100 VIDA   /   VELOCIDAD 8\n\nArco • Alto daño por flecha\nClic izquierdo: disparar\nCada disparo consume munición.\nLas flechas se regeneran." ,"180 VIDA   /   VELOCIDAD 4.8\n\nMazo • Golpe en área frontal\nClic izquierdo: atacar\nGolpea a varios enemigos cercanos.\nMás resistencia, menos movilidad."};
            var kinds=new[]{Codice.Characters.PlayerClass.Warrior,Codice.Characters.PlayerClass.Archer,Codice.Characters.PlayerClass.Tank};
            for(int i=0;i<3;i++)
            {
                int index=i;float x=80+i*515;
                var card=UIWidgets.Image(classes,names[i]+" card",x,320,490,385,new Color(.07f,.085f,.12f));
                Label(card.transform,"Name",25,25,440,50,32,Gold,names[i]);
                Label(card.transform,"Stats",25,100,440,260,23,Cream,stats[i]);
                Button(classes,"JUGAR COMO "+names[i],x,730,490,65,()=>RunNavigation.StartGame(kinds[index]),true);
            }
            Button(classes,"VOLVER",80,825,260,55,()=>ShowClasses(false));
            Label(classes,"Controls",385,827,1200,50,20,Muted,"WASD: moverte   •   E: interactuar   •   Q: especial   •   T / Y / U: pasivas");
            classes.gameObject.SetActive(false);
            first.Select();RefreshOptions();
        }
        static RectTransform Centered(Transform parent,string name,float w,float h)
        {var r=UIWidgets.Rect(parent,name,0,0,w,h);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Color color,string text)
        {var t=UIWidgets.Label(parent,name,x,y,w,h,size,color);t.text=text;return t;}
        static void Line(Transform parent,float x,float y,float w,Color color)=>UIWidgets.Image(parent,"Divider",x,y,w,1,color);
        static Button Button(Transform parent,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=false)
        {
            var image=UIWidgets.Image(parent,name,x,y,w,h,Color.white);image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            var colors=b.colors;colors.normalColor=primary?Gold:new Color(.09f,.105f,.14f);colors.highlightedColor=primary?new Color(1,.84f,.56f):new Color(.22f,.24f,.29f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.5f,.38f,.21f);colors.fadeDuration=.14f;b.colors=colors;
            var label=Label(image.transform,"Label",24,0,w-48,h,22,primary?new Color(.08f,.07f,.06f):Cream,name);label.alignment=TextAnchor.MiddleCenter;
            if(!primary)UIWidgets.Image(image.transform,"Accent",0,0,3,h,Gold);
            return b;
        }
        void Update()
        {
            if(classes && classes.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowClasses(false);
            if(options && options.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowOptions(false);
            if(displayMode)displayMode.text=Screen.fullScreen?"PANTALLA COMPLETA  •  CAMBIAR":"MODO VENTANA  •  CAMBIAR";
        }
        void ShowClasses(bool show){home.gameObject.SetActive(!show);classes.gameObject.SetActive(show);(show?classes:home).GetComponentInChildren<Button>().Select();}
        void ShowOptions(bool show){home.gameObject.SetActive(!show);options.gameObject.SetActive(show);(show?options:home).GetComponentInChildren<Button>().Select();RefreshOptions();}
        void Volume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("Codice.Volume",AudioListener.volume);PlayerPrefs.Save();RefreshOptions();}
        void Sensitivity(float delta){PlayerPrefs.SetFloat("Codice.Sensitivity",Mathf.Clamp(PlayerPrefs.GetFloat("Codice.Sensitivity",.09f)+delta,.02f,.3f));PlayerPrefs.Save();RefreshOptions();}
        void RefreshOptions(){values.text=$"VOLUMEN GENERAL                         {AudioListener.volume:P0}\n\nSENSIBILIDAD DEL RATÓN                 {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}";}
    }
}

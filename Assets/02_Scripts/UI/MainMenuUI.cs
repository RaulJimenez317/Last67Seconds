using TMPro;
using Text = TMPro.TextMeshProUGUI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace Codice.UI
{
    public sealed partial class MainMenuUI : MonoBehaviour
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
            UIWidgets.ConfigureCanvas(root.GetComponent<Canvas>());
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            var bg=UIWidgets.Image(root.transform,"Background",0,0,1920,1080,new Color(.035f,.043f,.065f)).rectTransform;bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;
            home=Centered(root.transform,"Main Menu",1680,900);
            options=Centered(root.transform,"Opciones",760,900);options.gameObject.SetActive(false);
            Label(home,"Edition",80,35,700,30,18,Gold,"LAST67SECONDS     /     UN CICLO SIN FINAL");
            Line(home,80,85,1520,new Color(.3f,.28f,.23f));
            Label(home,"Chapter",80,138,700,30,18,Muted,"ROGUELIKE  •  ACCIÓN EN PRIMERA PERSONA");
            var title=Label(home,"Title",74,225,850,130,72,Cream,"Last<color=#FFBA68>67</color>Seconds");
            title.fontStyle=FontStyles.Bold;
            Label(home,"Title detail",80,370,800,30,18,Gold,"CENIZA  /  ACERO  /  SUPERVIVENCIA");
            UIWidgets.Image(home,"Title accent",80,422,72,4,Gold);
            Label(home,"Subtitle",80,463,650,78,25,Muted,"Desciende a las ruinas. Reclama su poder.\nQue las cenizas cuenten tu historia.");
            var first=Button(home,"INICIAR JUEGO",80,580,620,78,()=>ShowClasses(true),true);
            Button(home,"OPCIONES",80,686,295,62,()=>ShowOptions(true));
            Button(home,"SALIR",405,686,295,62,RunNavigation.Quit);
            Button(home,"TRUCOS",80,765,620,52,()=>ShowCheats(true));
            BuildCheats(root.transform);
            Button(home,"COOPERATIVO LAN",820,765,620,52,Codice.Multiplayer.LanSession.Open);
            var art=UIWidgets.Rect(home,"Relics",920,140,620,620).gameObject.AddComponent<RawImage>();
            art.gameObject.AddComponent<MenuRelics>().Initialize(art);
            var caption=Label(home,"Relic caption",920,740,620,32,20,Gold,"RELIQUIAS DE LAS CENIZAS");caption.alignment=TextAlignmentOptions.Center;
            var sub=Label(home,"Relic hint",920,782,620,30,17,Muted,"Recoge sus sellos. Despierta su poder con T / Y / U.");sub.alignment=TextAlignmentOptions.Center;
            Line(home,80,835,1520,new Color(.3f,.28f,.23f));
            Label(home,"Footer",80,863,1000,28,17,Muted,"WASD  Mover     •     Clic  Atacar     •     E  Interactuar     •     Esc  Pausa");
            Label(home,"Navigation",1220,863,400,28,17,Gold,"↑ ↓  Elegir     /     Enter  Confirmar");
            Label(options,"Eyebrow",40,30,680,30,18,Gold,"LAST67SECONDS    /    AJUSTES");
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
            Label(classes,"Chapter",80,35,1520,35,18,Gold,"LAST67SECONDS  /  NUEVO DESCENSO");
            Label(classes,"Title",75,115,1520,90,60,Cream,"Elige quién desafía las cenizas.");
            Label(classes,"Hint",80,220,1520,55,24,Muted,"Cada clase empieza con su propia arma. Podrás encontrar y comprar otras durante la partida.");
            string[] names={"CABALLERO","ARQUERO","TANQUE","MAGO","SHINOBI"};
            string[] stats={"120 VIDA   /   VELOCIDAD 6.5\n\nEspada • Daño equilibrado\nClic izquierdo: atacar\nMantén clic derecho: escudo\nBloquea el 50% del daño frontal.","100 VIDA   /   VELOCIDAD 8\n\nArco • Alto daño por flecha\nClic izquierdo: disparar\nCada disparo consume munición.\nLas flechas se regeneran." ,"180 VIDA   /   VELOCIDAD 4.8\n\nMazo • Golpe en área frontal\nClic izquierdo: atacar\nGolpea a varios enemigos cercanos.\nMás resistencia, menos movilidad.","100 VIDA / VELOCIDAD 6.5\n\nBastón • Magia a distancia\n100 MANÁ (otras clases: 60)\nDisparo: 20 • Especial: 40\nRegenera 8/s tras 1.5 s.","120 VIDA / VELOCIDAD 6.5\n\nKatana y shuriken\nClic derecho: parry\nPerfecto: sin daño ni gasto\nGuardia: reduce daño,\ngasta postura. Q: tajos rojos."};
            var kinds=new[]{Codice.Characters.PlayerClass.Warrior,Codice.Characters.PlayerClass.Archer,Codice.Characters.PlayerClass.Tank,Codice.Characters.PlayerClass.Mage,Codice.Characters.PlayerClass.Shinobi};
            for(int i=0;i<5;i++)
            {
                int index=i;float x=80+i*304;
                var card=UIWidgets.Image(classes,names[i]+" card",x,320,288,385,new Color(.07f,.085f,.12f));
                Label(card.transform,"Name",16,25,256,50,25,Gold,names[i]);
                Label(card.transform,"Stats",16,100,256,260,18,Cream,stats[i]);
                Button(classes,"JUGAR COMO "+names[i],x,730,288,75,()=>RunNavigation.StartGame(kinds[index],CheatRound),true);
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
            var rect=UIWidgets.Rect(parent,name,x,y,w,h);
            var face=rect.gameObject.AddComponent<RelicButtonFace>();face.primary=primary;face.raycastTarget=true;
            var b=rect.gameObject.AddComponent<RelicMenuButton>();b.targetGraphic=face;b.onClick.AddListener(action);
            b.transition=Selectable.Transition.None;
            var label=Label(rect,"Label",66,0,w-112,h,w<350?18:21,Cream,name);label.alignment=TextAlignmentOptions.MidlineLeft;label.fontStyle=FontStyles.Bold;
            return b;
        }
        void Update()
        {
            if(cheats&&cheats.gameObject.activeSelf&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){ShowCheats(false);return;}
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

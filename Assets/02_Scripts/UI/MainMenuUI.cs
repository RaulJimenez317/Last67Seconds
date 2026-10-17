using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Codice.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        RectTransform home,options;Text values;
        void Start()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(PlayerPrefs.HasKey("Codice.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Codice.Fullscreen")==1;
            AudioListener.volume=PlayerPrefs.GetFloat("Codice.Volume",1);
            var root=new GameObject("Main menu canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            var bg=UIWidgets.Image(root.transform,"Background",0,0,1920,1080,ProceduralUISprites.Abyss).rectTransform;bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;
            home=Panel(root.transform,"Main Menu");options=Panel(root.transform,"Opciones");options.gameObject.SetActive(false);
            UIWidgets.Label(home,"Eyebrow",60,45,600,30,21,ProceduralUISprites.Cyan).text="UN VIAJE ENTRE RUINAS Y CENIZAS";
            UIWidgets.Label(home,"Title",60,105,650,130,58,ProceduralUISprites.Gold).text="CÓDICE\nDE CENIZA";
            UIWidgets.Label(home,"Subtitle",60,265,610,80,22).text="Sobrevive al Calabozo. Desafía a los señores del Coliseo. Vuelve a intentarlo.";
            var first=UIWidgets.Button(home,"INICIAR JUEGO",60,380,600,65,RunNavigation.StartGame);
            UIWidgets.Button(home,"OPCIONES",60,470,600,65,()=>ShowOptions(true));
            UIWidgets.Button(home,"SALIR",60,560,600,65,RunNavigation.Quit);
            UIWidgets.Label(home,"Footer",60,675,600,40,17).text="WASD · Mover     E · Interactuar     Escape · Pausa";
            UIWidgets.Label(options,"Heading",60,55,600,65,42,ProceduralUISprites.Gold).text="OPCIONES";
            values=UIWidgets.Label(options,"Values",60,155,600,160,24);
            UIWidgets.Button(options,"VOLUMEN −",60,330,285,55,()=>Volume(-.1f));UIWidgets.Button(options,"VOLUMEN +",375,330,285,55,()=>Volume(.1f));
            UIWidgets.Button(options,"SENSIBILIDAD −",60,415,285,55,()=>Sensitivity(-.01f));UIWidgets.Button(options,"SENSIBILIDAD +",375,415,285,55,()=>Sensitivity(.01f));
            UIWidgets.Button(options,"CAMBIAR PANTALLA COMPLETA",60,500,600,55,()=>{bool full=!Screen.fullScreen;Screen.fullScreen=full;PlayerPrefs.SetInt("Codice.Fullscreen",full?1:0);PlayerPrefs.Save();});
            UIWidgets.Button(options,"VOLVER",60,620,600,60,()=>ShowOptions(false));
            first.Select();RefreshOptions();
        }
        RectTransform Panel(Transform parent,string name){var r=UIWidgets.Image(parent,name,0,0,720,770,ProceduralUISprites.Ink,"frame").rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
        void ShowOptions(bool show){home.gameObject.SetActive(!show);options.gameObject.SetActive(show);(show?options:home).GetComponentInChildren<Button>().Select();RefreshOptions();}
        void Volume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("Codice.Volume",AudioListener.volume);PlayerPrefs.Save();RefreshOptions();}
        void Sensitivity(float delta){PlayerPrefs.SetFloat("Codice.Sensitivity",Mathf.Clamp(PlayerPrefs.GetFloat("Codice.Sensitivity",.09f)+delta,.02f,.3f));PlayerPrefs.Save();RefreshOptions();}
        void RefreshOptions(){values.text=$"Volumen general: {AudioListener.volume:P0}\n\nSensibilidad del ratón: {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}\n\nLos ajustes se guardan automáticamente.";}
    }
}

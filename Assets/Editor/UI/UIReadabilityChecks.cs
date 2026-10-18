using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Codice.UI;
using Codice.Mechanics;
using Codice.Characters;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UIReadabilityChecks
{
    const string Report = "Library/UIReadability.txt";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += s => {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("UIReadability.Play", false))
            { SessionState.SetBool("UIReadability.Play", false); Run(); }
        };
    }
    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Library/UIReadability.request")) return;
        File.Delete("Library/UIReadability.request");
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
        { File.WriteAllText(Report,"FAILED: save open scenes first"); return; }
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
        {
            string package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).Single();
            AssetDatabase.ImportPackage(package, false);
        }
        EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
        File.WriteAllText(Report,"UI readability regression\n");
        SessionState.SetBool("UIReadability.Play",true); EditorApplication.isPlaying = true;
    }
    static void Log(string s) => File.AppendAllText(Report,s+"\n");
    static void Pump() => EditorApplication.QueuePlayerLoopUpdate();
    static async Task Until(Func<bool> ready)
    { double end=EditorApplication.timeSinceStartup+60; while(!ready()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(50);} }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static readonly Vector2Int[] sizes = { new(1280,720),new(1920,1080),new(2560,1440),new(3840,2160),new(1280,1024),new(2560,1080),new(1024,768) };
    static EditorWindow gameView;
    static int originalSize;
    static object sizeGroup;
    static int layoutIssues;
    static void SetSize(int width,int height)
    {
        var assembly=typeof(EditorWindow).Assembly;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(instance,new object[]{0});
        sizeGroup=group;
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),width,height,"UI check"});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView,count-1);
        gameView.Repaint();
    }
    static async Task Inspect(string name,Transform root)
    {
        foreach(var size in sizes)
        {
            SetSize(size.x,size.y);
            await Until(()=>Screen.width==size.x&&Screen.height==size.y);
            await Task.Delay(150); Canvas.ForceUpdateCanvases();
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) {layoutIssues++;Log("OVERFLOW "+name+" "+size+" "+text.name+" : "+text.text.Replace('\n',' '));}
                var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                if(corners.Any(p=>p.x < -1 || p.x > Screen.width+1 || p.y < -1 || p.y > Screen.height+1)) {layoutIssues++;Log("OUTSIDE "+name+" "+size+" "+text.name);}
            }
            var canvas=root.GetComponentInParent<Canvas>();if(!canvas)canvas=root.GetComponentInChildren<Canvas>();
            Require(canvas.GetComponent<CanvasScaler>().screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,name+" fits "+size);
            if(size==sizes[0]||size==sizes[4]||size==sizes[5])
            { ScreenCapture.CaptureScreenshot("Library/UI-"+name+"-"+size.x+"x"+size.y+".png"); await Task.Delay(250); }
        }
    }
    static async void Run()
    {
        EditorApplication.update+=Pump;
        try
        {
            gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            originalSize=(int)gameView.GetType().GetProperty("selectedSizeIndex").GetValue(gameView);
            await Until(()=>Object.FindAnyObjectByType<MainMenuUI>()&&GameObject.Find("Main Menu"));
            var menu=Object.FindAnyObjectByType<MainMenuUI>();
            layoutIssues=0;
            var font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            string glyphs=new string(Directory.GetFiles("Assets/02_Scripts/UI","*.cs").SelectMany(File.ReadAllText).Where(c=>c>127).Distinct().ToArray());
            bool supported=font.HasCharacters(glyphs,out uint[] missing,true,true);
            Require(supported,"all Spanish characters and UI symbols supported"+(missing==null?"":": "+string.Join(",",missing.Select(c=>"U+"+c.ToString("X4")))));
            await Inspect("menu",GameObject.Find("Main Menu").transform);
            typeof(MainMenuUI).GetMethod("ShowOptions",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,new object[]{true});
            await Inspect("options",GameObject.Find("Opciones").transform);
            typeof(MainMenuUI).GetMethod("ShowOptions",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,new object[]{false});
            typeof(MainMenuUI).GetMethod("ShowClasses",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,new object[]{true});
            await Inspect("classes",GameObject.Find("Elegir clase").transform);
            RunNavigation.StartGame(PlayerClass.Warrior,1);
            await Until(()=>!RunNavigation.Loading&&RunHUD.Instance&&RunDirector.Instance.Player);
            var hud=RunHUD.Instance;var player=RunDirector.Instance.Player;
            foreach(var enemy in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
            player.enabled=false;player.GetComponent<PlayerCombat>().enabled=false;Time.timeScale=0;
            player.State.EquipPassive(PassiveKind.VampiricHeart);player.State.EquipPassive(PassiveKind.MercuryBoots);player.State.EquipPassive(PassiveKind.ThornArmor);
            await Inspect("hud",hud.transform);
            hud.SetPaused(true);await Task.Delay(100);await Inspect("pause",GameObject.Find("Pause").transform);hud.SetPaused(false);
            var merchant=hud.Context.dungeon.merchant;merchant.BeginIntermission(1);
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            player.transform.position=merchant.transform.position+Vector3.forward;controller.enabled=true;
            merchant.Interact(player);
            await Until(()=>hud.ShopVisible);
            player.State.AddGold(1000);
            await Inspect("shop",GameObject.Find("Mercader Errante").transform);
            int gold=player.State.Gold;var offer=merchant.GetOffer(0);
            var buy=GameObject.Find("Mercader Errante").GetComponentsInChildren<Button>().First(b=>b.name=="COMPRAR [1]");
            Require(buy.interactable,"shop purchase button enabled with sufficient gold");
            buy.onClick.Invoke();
            Require(offer.sold&&player.State.Gold==gold-offer.price,"shop button buys offer and updates balance");
            merchant.CloseShop();
            player.ReceiveDamage(new DamageInfo{amount=999999,ignoreGuard=true});
            await Until(()=>hud.ResultsVisible);
            await Inspect("defeat",GameObject.Find("Defeat").transform);
            RunNavigation.StartGame(PlayerClass.Warrior,20);
            await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Level==20&&RunHUD.Instance.Context.arena.ActiveBoss);
            hud=RunHUD.Instance;hud.Context.arena.ActiveBoss.ReceiveDamage(new DamageInfo{amount=999999,source=hud.Player.gameObject});
            await Until(()=>hud.ResultsVisible);
            await Inspect("victory",GameObject.Find("Victory").transform);
            Require(layoutIssues==0,"no clipped text or off-screen labels across all layouts");
            Log("UI_CHECKS_FINISHED");
        }
        catch(Exception e){Log("FAILED "+e);Debug.LogException(e);}
        finally
        {
            if(gameView)gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView,originalSize);
            if(sizeGroup!=null)
            {
                var type=sizeGroup.GetType();int builtin=(int)type.GetMethod("GetBuiltinCount").Invoke(sizeGroup,null);
                var names=(string[])type.GetMethod("GetDisplayTexts").Invoke(sizeGroup,null);
                for(int i=names.Length-1;i>=builtin;i--)if(names[i].Contains("UI check"))type.GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{i-builtin});
            }
            EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;
        }
    }
}

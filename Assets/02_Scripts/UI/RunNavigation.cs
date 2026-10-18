using Codice.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Codice.UI
{
    public static class RunNavigation
    {
        static AsyncOperation loading;
        static bool transitioning;
        public static Codice.Characters.PlayerClass SelectedClass { get; private set; }
        internal static bool SceneLoading=>loading!=null&&!loading.isDone;
        public static bool Loading=>transitioning||SceneLoading;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){loading=null;transitioning=false;SelectedClass=Codice.Characters.PlayerClass.Warrior;}
        public static void StartGame()=>StartGame(SelectedClass,1);
        public static void StartGame(Codice.Characters.PlayerClass selected)
        { StartGame(selected,1); }
        public static void StartGame(Codice.Characters.PlayerClass selected,int round)
        { if(Loading)return;SelectedClass=selected;round=Mathf.Clamp(round,1,RoundRules.FinalRound);PlayerPrefs.SetInt("Codice.TestRound",round);PlayerPrefs.Save();Load(RoundRules.SceneForRound(round)); }
        public static void MainMenu()=>Load("MainMenu");
        static void Load(string scene)
        {
            if(Loading)return;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Missing scene: "+scene);return;}
            transitioning=true; SceneTransition.Begin(scene);
        }
        internal static void FinishTransition(){transitioning=false;}
        internal static void LoadSceneNow(string scene)
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(RunDirector.Instance){RunDirector.Instance.StopAllCoroutines();RunDirector.Instance.gameObject.SetActive(false);Object.Destroy(RunDirector.Instance.gameObject);}
            loading=SceneManager.LoadSceneAsync(scene);
        }
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}

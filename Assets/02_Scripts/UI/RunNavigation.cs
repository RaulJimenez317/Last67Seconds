using Codice.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Codice.UI
{
    public static class RunNavigation
    {
        static AsyncOperation loading;
        public static bool Loading=>loading!=null&&!loading.isDone;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>loading=null;
        public static void StartGame()=>Load("Calabozo");
        public static void MainMenu()=>Load("MainMenu");
        static void Load(string scene)
        {
            if(Loading)return;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Missing scene: "+scene);return;}
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

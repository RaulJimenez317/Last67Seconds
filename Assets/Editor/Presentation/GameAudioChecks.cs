using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class GameAudioChecks
{
    public static string Result="Not run";
    static int count;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);count++;}
    static async Task Until(Func<bool> ready)
    {float end=Time.realtimeSinceStartup+30;while(!ready()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(50);}}
    public static async void Run()
    {
        if(!Application.isPlaying||Result=="Running")return;
        Result="Running";count=0;Application.runInBackground=true;
        try
        {
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&Object.FindFirstObjectByType<MainMenuUI>());await Task.Delay(500);
            var audio=GameAudio.Instance;var library=audio.Library;
            Check(library.menu&&library.dungeon&&library.arena&&library.menu!=library.dungeon&&library.dungeon!=library.arena,"Three distinct tracks");
            Check(library.effects.Length==Enum.GetValues(typeof(SoundCue)).Length&&library.effects.All(e=>e.clips.Length>0&&e.clips.All(c=>c)),"All cues have clips");
            Check(audio.CurrentMusic==library.menu,"Menu selects menu music");
            Check(Mathf.Abs(library.musicVolume-.85f)<.001f,"Background music reduced 15 percent");
            await Task.Delay(1000);
            Check(audio.GetComponentsInChildren<AudioSource>().Any(s=>s.clip==library.menu&&s.isPlaying&&s.loop&&s.volume>0),"Menu music playing in loop");
            Check(audio.GetComponentsInChildren<AudioSource>().Any(s=>s.clip==library.menu&&Mathf.Abs(s.volume-.32f*.85f)<.005f),"Music gain applies to source without changing master");
            int before=audio.PlayedCount;
            Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b=>b.name=="INICIAR JUEGO").onClick.Invoke();
            Check(audio.PlayedCount==before+1&&audio.LastCue==SoundCue.Click,"Button click plays sound");
            RunNavigation.StartGame(PlayerClass.Archer);await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player);
            var p=RunDirector.Instance.Player;
            // Exercise actions directly, without a physical mouse click from the editor firing first.
            p.GetComponent<PlayerCombat>().enabled=false;
            foreach(var enemy in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            Check(GameAudio.Instance==audio&&audio.CurrentMusic==library.dungeon,"Persistent manager switches to dungeon");
            await Task.Delay(2000);
            Check(!audio.GetComponentsInChildren<AudioSource>().Any(s=>s.clip==library.menu&&s.isPlaying),"Old music fades out");
            before=audio.PlayedCount;Check(p.GetComponent<PlayerCombat>().TryAttack()&&audio.PlayedCount>before&&audio.LastCue==SoundCue.Bow,"Bow action triggers audio");
            await Task.Delay(200);before=audio.PlayedCount;
            GoldPickup.Drop(p.transform.position,40,p).Collect(p);
            Check(audio.PlayedCount>before&&audio.LastCue==SoundCue.Coin,"Gold collection triggers audio");
            await Task.Delay(100);
            RunHUD.Instance.SetPaused(true);await Task.Delay(100);before=audio.PlayedCount;
            GameAudio.Play(SoundCue.Swing);Check(audio.GameplayPaused&&audio.PlayedCount==before,"Pause suppresses combat sounds");
            GameAudio.Play(SoundCue.Click);Check(audio.PlayedCount==before+1,"UI sound works while paused");
            RunHUD.Instance.SetPaused(false);await Task.Delay(100);Check(!audio.GameplayPaused,"Audio resumes after pause");
            float volume=AudioListener.volume;AudioListener.volume=0;GameAudio.Play(SoundCue.Pickup);Check(AudioListener.volume==0,"Master mute retained");AudioListener.volume=volume;
            foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {await Task.Delay(220);GameAudio.Play(cue);}
            Check(audio.GetComponentsInChildren<AudioSource>().Length==26,"Bounded audio source pool");
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&SceneManager.GetActiveScene().name=="MainMenu");
            SceneManager.LoadScene("Coliseo");await Until(()=>RunDirector.Instance&&RunDirector.Instance.Player&&Object.FindFirstObjectByType<Codice.Bosses.BossCharacter>());
            Check(audio.CurrentMusic==library.arena,"Arena music selected");
            var boss=Object.FindFirstObjectByType<Codice.Bosses.BossCharacter>();
            boss.ReceiveDamage(new DamageInfo{amount=100000,source=RunDirector.Instance.Player.gameObject});await Task.Delay(300);
            Check(audio.GetComponentsInChildren<AudioSource>().Any(s=>s.clip==library.effects.Single(e=>e.cue==SoundCue.Victory).clips[0]&&s.isPlaying),"Victory cue plays");
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&SceneManager.GetActiveScene().name=="MainMenu");
            Check(Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length==1&&audio.CurrentMusic==library.menu,"Return to menu has one audio manager");
            Result="AUDIO_CHECKS_PASSED: "+count;Debug.Log(Result);
        }
        catch(Exception error){Result="FAILED: "+error;Debug.LogError(Result);}
    }
}

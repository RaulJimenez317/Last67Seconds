using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Presentation;
using UnityEditor;
using UnityEngine;
public static class GameAudioSetup
{
    [MenuItem("Codice/Audio/Install library")]
    public static void Install()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play first");
        var paths=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/05_Music"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        AudioClip Clip(string name,bool streaming=false)
        {
            string path=paths.Single(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            settings.loadType=streaming?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;
            importer.defaultSampleSettings=settings;importer.forceToMono=!streaming;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        const string output="Assets/Resources/GameAudioLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(output);
        if(!library){library=ScriptableObject.CreateInstance<GameAudioLibrary>();AssetDatabase.CreateAsset(library,output);}
        library.menu=Clip("34724807-castle-of-intrigue-288660",true);
        library.dungeon=Clip("freesound_community-medieval-75892",true);
        library.arena=Clip("tatamusic-battle-warrior-fighting-drums-478210",true);
        var entries=new List<SoundEntry>();
        void Add(SoundCue cue,float volume,float cooldown,params string[] names)
        {entries.Add(new SoundEntry{cue=cue,volume=volume,cooldown=cooldown,clips=names.Select(n=>Clip(n)).ToArray()});}
        Add(SoundCue.Click,.3f,.08f,"select_1");
        Add(SoundCue.Swing,.5f,.07f,"floraphonic-swing-whoosh-weapon-4-189820");
        Add(SoundCue.Bow,.55f,.08f,"gargamel10-shooting-arrow-game-sound-effect-379234");
        Add(SoundCue.Gun,.65f,.08f,"shot_muffled");
        Add(SoundCue.Magic,.35f,.1f,"synth_bass_mystery");
        Add(SoundCue.Heavy,.6f,.1f,"sword_light");
        Add(SoundCue.Hit,.45f,.07f,"harsh_thud");
        Add(SoundCue.Block,.5f,.1f,"sword_clash");
        Add(SoundCue.Hurt,.4f,.3f,"dragon-studio-armor-impact-from-sword-393843");
        Add(SoundCue.Death,.3f,.6f,"applehillstudios-hurt-amp-death-pain-gurgle-543682");
        Add(SoundCue.Footstep,.2f,.09f,"foley_footstep_concrete_1","foley_footstep_concrete_2","foley_footstep_concrete_3","foley_footstep_concrete_4");
        Add(SoundCue.Coin,.4f,.12f,"freesound_community-coin-bag-pickup-drop-94617");
        Add(SoundCue.Pickup,.4f,.12f,"weapon_equip_short");
        Add(SoundCue.Chest,.5f,.2f,"creaky_door_short");
        Add(SoundCue.Purchase,.45f,.12f,"synth_confirmation");
        Add(SoundCue.Error,.3f,.3f,"synth_error");
        Add(SoundCue.Reload,.45f,.2f,"weapon_equip");
        Add(SoundCue.Power,.45f,.2f,"vibraphone_chime_positive");
        Add(SoundCue.Dash,.3f,.15f,"floraphonic-swing-whoosh-weapon-4-189820");
        Add(SoundCue.Explosion,.5f,.1f,"explosion_medium");
        Add(SoundCue.Victory,.55f,1,"vibraphone_level_complete");
        Add(SoundCue.Defeat,.5f,1,"synth_bass_defeated");
        Add(SoundCue.Boss,.5f,1.5f,"freesound_community-dragon-shout-roar-98277");
        library.effects=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        Debug.Log("AUDIO_LIBRARY_INSTALLED: 3 music tracks / "+entries.Count+" cues");
    }
}

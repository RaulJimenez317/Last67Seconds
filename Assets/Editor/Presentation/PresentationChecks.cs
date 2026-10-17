using System;
using System.IO;
using System.Linq;
using Codice.Characters;
using Codice.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PresentationChecks
{
    static int count;
    static void Check(bool condition, string message)
    { count++; if(!condition) throw new InvalidOperationException(message); }

    [MenuItem("Codice/Presentation/Validate generated assets")]
    public static void Assets()
    {
        count=0;
        var library=AssetDatabase.LoadAssetAtPath<PresentationLibrary>("Assets/Resources/Presentation/Library.asset");
        Check(library,"Missing presentation library");
        string[] names={"Idle","Ready","Movement","Patrol","Chase","Melee","HeavyAttack","Thrust","Shoot","ShootBow","ShootMagic","BowIdle","RifleIdle","BlockIdle","Hit","Death","Dash"};
        foreach(var controller in new[]{library.medium,library.goblin,library.demon})
        {
            Check(controller,"Missing controller");
            var states=((AnimatorController)controller).layers[0].stateMachine.states.Select(s=>s.state).ToArray();
            foreach(var name in names)
            {
                var state=states.SingleOrDefault(s=>s.name==name);
                Check(state && state.motion is AnimationClip && ((AnimationClip)state.motion).length>0,controller.name+" missing "+name);
                Check(AnimationUtility.GetCurveBindings((AnimationClip)state.motion).Length>0,"Empty animation "+name);
            }
        }
        foreach(var effect in new[]{library.impact,library.dust,library.explosion,library.muzzle})
        {
            Check(effect,"Missing VFX prefab");
            Check(effect.GetComponentsInChildren<ParticleSystem>(true).Length>0,"No particles");
            foreach(var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
                foreach(var material in renderer.sharedMaterials)
                    Check(material && material.shader && material.shader.name.StartsWith("Universal Render Pipeline/"),"Non URP particle material");
        }
        Check(EditorBuildSettings.scenes[0].path=="Assets/00_Scenes/MainMenu.unity" && EditorBuildSettings.scenes[0].enabled,"Main menu must start build");
        Check(File.Exists("Assets/00_Scenes/MainMenu.unity"),"Menu scene not saved");
        Debug.Log("PRESENTATION_ASSETS_PASSED: "+count);
    }

    public static void Player()
    {
        count=0;
        var player=Object.FindFirstObjectByType<PlayerCharacter>();
        Check(player,"No player");
        var actor=player.GetComponent<ActorPresentation>();
        Check(actor && actor.Body && actor.Arms,"Body and first person arms required");
        Check(actor.WeaponSocket,"Right hand socket missing");
        foreach(var weapon in player.GetComponent<PlayerCombat>().weaponModels)
        {
            Check(weapon && weapon.transform.IsChildOf(actor.Arms.transform),"Weapon not on first person rig");
            Check(weapon.transform.parent.name.StartsWith("handslot."),"Weapon floating outside hand socket");
        }
        foreach(var animator in new[]{actor.Body,actor.Arms})
            foreach(string state in new[]{"Idle","Movement","Melee","ShootBow","Hit","Death"})
                Check(animator.HasState(0,Animator.StringToHash(state)),"Unplayable state "+state);
        actor.Play("Melee",.5f,true);actor.Arms.Update(.2f);
        Check(actor.Arms.GetCurrentAnimatorStateInfo(0).length>0,"Arms animation not playing");
        Check(actor.Arms.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"No visible arms");
        Debug.Log("PRESENTATION_PLAYER_PASSED: "+count);
    }
}

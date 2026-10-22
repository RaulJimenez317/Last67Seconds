using UnityEngine;
using Codice.Characters;
using VolumetricLines;
namespace Codice.Presentation
{
    public sealed class StaffBeamVisual:MonoBehaviour
    {
        PlayerCharacter player;VolumetricLineBehavior beam;Light glow;
        public static StaffBeamVisual Create(PlayerCharacter owner)
        {
            var go=new GameObject("Staff volumetric laser");var visual=go.AddComponent<StaffBeamVisual>();visual.player=owner;
            visual.beam=go.AddComponent<VolumetricLineBehavior>();visual.beam.TemplateMaterial=Resources.Load<Material>("StaffLaser");visual.beam.LineColor=new Color(.12f,.55f,1);visual.beam.LineWidth=.28f;visual.beam.LightSaberFactor=.85f;
            visual.glow=go.AddComponent<Light>();visual.glow.type=LightType.Point;visual.glow.color=Color.cyan;visual.glow.range=4;visual.glow.intensity=2;return visual;
        }
        void LateUpdate()
        {
            if(!player||!player.State.IsAlive){Destroy(gameObject);return;}
            Vector3 origin=player.view.transform.position,forward=player.view.transform.forward;float length=28;
            foreach(var hit in Physics.RaycastAll(origin,forward,length,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.GetComponentInParent<PlayerCharacter>()&&!Codice.Mechanics.CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component))length=Mathf.Min(length,hit.distance);
            var staff=player.GetComponent<PlayerCombat>().weaponModels[(int)WeaponKind.Staff];
            Vector3 start=staff?staff.transform.TransformPoint(Vector3.up*1.1f):origin+forward*.7f;
            if(staff)foreach(var child in staff.GetComponentsInChildren<Transform>())if(child.name=="Muzzle"){start=child.position;break;}
            transform.position=start;beam.SetStartAndEndPoints(Vector3.zero,origin+forward*length-start);beam.LineWidth=.28f+Mathf.Sin(Time.time*32)*.015f;
        }
    }
}

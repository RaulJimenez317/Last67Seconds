using UnityEngine;
namespace Codice.Presentation
{
    public static class SpecialVisuals
    {
        static Material material;
        static Material Material {get{if(!material)material=Resources.Load<Material>("SpecialGlow");return material;}}
        static LineRenderer Line(string name,Color color,float width,float lifetime)
        {
            var go=new GameObject(name);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Material;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.numCapVertices=4;Object.Destroy(go,lifetime);return line;
        }
        public static void Beam(Vector3 from,Vector3 to,float lifetime,bool bullet=false)
        {
            var outer=Line(bullet?"Bullet tracer":"Arcane beam",bullet?new Color(1,.7f,.2f):new Color(.1f,.65f,1),bullet?.035f:.75f,lifetime);outer.positionCount=2;outer.SetPositions(new[]{from,to});
            if(!bullet){var core=Line("Beam core",Color.white,.22f,lifetime);core.positionCount=2;core.SetPositions(new[]{from,to});}
        }
        public static void Slash(Vector3 origin,Vector3 forward,float radius,bool circle,bool blood)
        {
            var line=Line(blood?"Blood slash":"Steel slash",blood?new Color(1,.035f,.07f):new Color(1,.8f,.45f),circle?.18f:.25f,.45f);
            line.positionCount=49;Vector3 flat=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;if(flat.sqrMagnitude<.01f)flat=Vector3.forward;
            for(int i=0;i<49;i++){float angle=circle?i*360f/48:Mathf.Lerp(-65,65,i/48f);line.SetPosition(i,origin+Quaternion.AngleAxis(angle,Vector3.up)*flat*radius);}
            GameAudio.Play(SoundCue.Swing);
        }
    }
}

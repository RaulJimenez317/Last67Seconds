using UnityEngine;
namespace Codice.Presentation
{
    // The dungeon skeleton demo has no death clip. Its visual falls without a live collider.
    public sealed class CorpseCollapse : MonoBehaviour
    {
        Quaternion rest; Vector3 position; float elapsed;
        void Awake(){rest=transform.rotation;position=transform.position;}
        void Update(){elapsed+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,elapsed/.7f);transform.rotation=rest*Quaternion.Euler(85*t,0,0);transform.position=position+Vector3.down*Mathf.Max(0,elapsed-2)*.3f;}
    }
}

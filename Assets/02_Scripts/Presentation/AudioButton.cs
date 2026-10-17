using UnityEngine;
using UnityEngine.UI;
namespace Codice.Presentation
{
    [RequireComponent(typeof(Button))]
    public sealed class AudioButton : MonoBehaviour
    {
        Button button;
        void Awake(){button=GetComponent<Button>();button.onClick.AddListener(Click);}
        void Click(){GameAudio.Play(SoundCue.Click);}
        void OnDestroy(){if(button)button.onClick.RemoveListener(Click);}
    }
}

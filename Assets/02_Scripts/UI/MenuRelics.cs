using Codice.Presentation;
using UnityEngine;

namespace Codice.UI
{
    // A separate, scene-owned display of the same models used by pickups.
    public sealed class MenuRelics : MonoBehaviour
    {
        GameObject stage;
        RenderTexture texture;
        readonly Transform[] relics = new Transform[5];
        Camera displayCamera;
        UnityEngine.UI.RawImage targetImage;
        public void Initialize(UnityEngine.UI.RawImage image)
        {
            var library = PresentationLibrary.Load();
            if (!library || library.passiveModels == null) return;
            stage = new GameObject("Menu relic stage"); stage.transform.position = new Vector3(1000, 1000, 1000);
            targetImage = image; image.raycastTarget = false;
            var cameraObject = new GameObject("Relic display camera"); cameraObject.transform.SetParent(stage.transform, false);
            displayCamera = cameraObject.AddComponent<Camera>(); displayCamera.orthographic = true; displayCamera.orthographicSize = 1.8f;
            displayCamera.clearFlags = CameraClearFlags.SolidColor; displayCamera.backgroundColor = new Color(.035f,.043f,.065f,1);
            displayCamera.cullingMask = 1 << 31; displayCamera.targetTexture = texture;
            displayCamera.nearClipPlane = .1f; displayCamera.farClipPlane = 15;
            ResizeTexture();
            cameraObject.transform.localPosition = new Vector3(0,0,-5);
            var lightObject = new GameObject("Relic key light"); lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(30,-25,0);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2; light.color = new Color(1,.85f,.65f); light.cullingMask = 1 << 31;
            for(int i=0;i<5;i++)
            {
                if(i>=library.passiveModels.Length || !library.passiveModels[i])continue;
                var model=Instantiate(library.passiveModels[i],stage.transform,false); relics[i]=model.transform;
                foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                model.transform.localScale=Vector3.one*(i==2?2.1f:.9f);
            }
        }
        void Update()
        {
            ResizeTexture();
            for(int i=0;i<relics.Length;i++)
            {
                if(!relics[i])continue;
                float angle=(i<2?i:i-1)*Mathf.PI/2+Mathf.PI/4;
                relics[i].localPosition=i==2?Vector3.up*(Mathf.Sin(Time.unscaledTime*.7f)*.09f):new Vector3(Mathf.Cos(angle)*1.15f,Mathf.Sin(angle)*1.15f,0);
                relics[i].localRotation=Quaternion.Euler(0,Mathf.Sin(Time.unscaledTime*.5f+i)*22,0);
            }
        }
        void ResizeTexture()
        {
            if (!targetImage || !displayCamera) return;
            float scale = targetImage.canvas ? targetImage.canvas.scaleFactor : 1;
            int size = Mathf.Clamp(Mathf.CeilToInt(targetImage.rectTransform.rect.width * scale / 64) * 64, 512, 2048);
            if (texture && texture.width == size) return;
            displayCamera.targetTexture = null;
            if (texture) { texture.Release(); Destroy(texture); }
            texture = new RenderTexture(size, size, 24) { name = "Menu relics", antiAliasing = 4, filterMode = FilterMode.Bilinear };
            targetImage.texture = texture; displayCamera.targetTexture = texture;
        }
        void OnEnable(){if(stage)stage.SetActive(true);}
        void OnDisable(){if(stage)stage.SetActive(false);}
        void OnDestroy(){if(stage)Destroy(stage);if(texture){texture.Release();Destroy(texture);}}
    }
}

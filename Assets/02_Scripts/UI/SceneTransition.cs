using System.Collections;
using UnityEngine;

namespace Codice.UI
{
    // Survives the load; blocks duplicate clicks while fading using unscaled time.
    public sealed class SceneTransition : MonoBehaviour
    {
        public static void Begin(string scene)
        {
            var root = new GameObject("Scene transition", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var transition = root.AddComponent<SceneTransition>();
            transition.StartCoroutine(transition.FadeAndLoad(scene));
        }
        IEnumerator FadeAndLoad(string scene)
        {
            var self = this;
            var canvas = self.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            self.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var image = new GameObject("Fade", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
            image.transform.SetParent(self.transform, false);
            var rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; image.raycastTarget = true;
            Time.timeScale = 0;
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
            { image.color = new Color(0, 0, 0, t / .2f); yield return null; }
            image.color = Color.black;
            RunNavigation.LoadSceneNow(scene);
            while (RunNavigation.SceneLoading) yield return null;
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
            { image.color = new Color(0, 0, 0, 1 - t / .2f); yield return null; }
            RunNavigation.FinishTransition();
            Destroy(self.gameObject);
        }
    }
}

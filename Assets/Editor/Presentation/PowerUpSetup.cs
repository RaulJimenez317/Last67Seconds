using System;
using System.Linq;
using Codice.Presentation;
using UnityEditor;
using UnityEngine;

public static class PowerUpSetup
{
    const string Source = "Assets/03_Models/Power Ups/BTM_Items_Gems/";
    const string Output = "Assets/Presentation/PowerUps";
    [MenuItem("Codice/Presentation/Install power-up models")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play before installing models.");
        System.IO.Directory.CreateDirectory(Output); AssetDatabase.Refresh();
        var library = AssetDatabase.LoadAssetAtPath<PresentationLibrary>("Assets/Resources/Presentation/Library.asset");
        if (!library) throw new InvalidOperationException("Presentation library missing");
        string[] models = { "Skull", "HeartGem", "Thunder", "SpeedChev", "Shield", "Battery" };
        string[] names = { "Frenzy", "VampiricHeart", "Overload", "MercuryBoots", "ThornArmor", "Ammunition" };
        string[] colors = { "EF7B46", "EC4568", "F7CD65", "66DCC1", "9DABEF", "D6D5BE" };
        library.passiveModels = new GameObject[5];
        for (int i = 0; i < models.Length; i++)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Prefabs/" + (i == 3 ? "SpeedChevBlue" : models[i]) + ".prefab");
            if (!source) source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Models/" + models[i] + ".fbx");
            if (!source) throw new InvalidOperationException("Missing power-up " + models[i]);
            var root = new GameObject(names[i]);
            try
            {
                var model = UnityEngine.Object.Instantiate(source, root.transform); model.name = models[i];
                foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = .65f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                model.transform.localScale *= scale; model.transform.localPosition -= bounds.center * scale;
                string materialPath = Output + "/" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                ColorUtility.TryParseHtmlString("#" + colors[i], out var color);
                var originalMaterial = renderers[0].sharedMaterial;
                if (originalMaterial && originalMaterial.mainTexture) material.SetTexture("_BaseMap", originalMaterial.mainTexture);
                material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", .35f); material.SetFloat("_Smoothness", .55f);
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .25f); EditorUtility.SetDirty(material);
                foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + names[i] + ".prefab");
                if (i < 5) library.passiveModels[i] = prefab; else library.ammunitionModel = prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets(); Debug.Log("POWERUP_MODELS_INSTALLED: 6");
    }
}

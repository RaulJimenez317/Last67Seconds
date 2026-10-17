using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EconomySetup
{
    [MenuItem("Codice/Presentation/Install gold coin")]
    public static void Install()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/Power Ups/BTM_Items_Gems/Models/Coin.fbx");
        if(!source)throw new System.InvalidOperationException("Coin model missing");
        var root=new GameObject("GoldCoin");
        try
        {
            var model=Object.Instantiate(source,root.transform);
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=.5f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            model.transform.localScale*=scale;model.transform.localPosition-=bounds.center*scale;
            foreach(var collider in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            const string path="Assets/Resources/GoldCoin.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",new Color(1,.65f,.12f));material.SetFloat("_Metallic",.7f);
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.45f,.22f,.02f));
            foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
            EditorUtility.SetDirty(material);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/GoldCoin.prefab");AssetDatabase.SaveAssets();
        }
        finally{Object.DestroyImmediate(root);}
    }
}

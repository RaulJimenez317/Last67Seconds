using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PlayerWeaponRepairSetup
{
    const string Pack="Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/";
    [MenuItem("Codice/Presentation/Repair player bow and arrow")]
    public static void Install()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
        string path="Assets/01_Prefabs/Characters/Player.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bow=root.GetComponent<Codice.Characters.PlayerCombat>().weaponModels[(int)Codice.Characters.WeaponKind.Bow];
            foreach(Transform child in bow.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Pack+"bow.fbx"),bow.transform);
            model.transform.localPosition=Vector3.zero; model.transform.localRotation=Quaternion.identity;
            Normalize(model,.8f,false);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var arrow=new GameObject("PlayerArrow");
        try
        {
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Pack+"arrow_bow.fbx"),arrow.transform);
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            Normalize(model,.7f,true);
            PrefabUtility.SaveAsPrefabAsset(arrow,"Assets/Resources/PlayerArrow.prefab");
        }
        finally{Object.DestroyImmediate(arrow);}
        AssetDatabase.SaveAssets();
    }
    static void Normalize(GameObject model,float length,bool arrow)
    {
        var renderers=model.GetComponentsInChildren<Renderer>();
        Bounds Bounds(){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        var bounds=Bounds();
        Vector3 axis=bounds.size.x>bounds.size.y&&bounds.size.x>bounds.size.z?Vector3.right:bounds.size.y>bounds.size.z?Vector3.up:Vector3.forward;
        model.transform.localRotation=Quaternion.FromToRotation(axis,arrow?Vector3.forward:Vector3.up)*model.transform.localRotation;
        bounds=Bounds();model.transform.localScale*=length/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
        bounds=Bounds();model.transform.position+=model.transform.parent.position-bounds.center;
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/01_Prefabs/Characters/ranger.mat");
        foreach(var r in renderers){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    }
}

using System.Collections.Generic;
using Codice.Characters;
using UnityEngine;

namespace Codice.UI
{
    public static class ProceduralUISprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static readonly Color Abyss = Hex("12131A", .90f), Crimson = Hex("D92B4B"), Gold = Hex("F5D76E"), Cyan = Hex("57DBDB"), Ink = Hex("777C91");
        public static Color Hex(string hex, float alpha = 1) { ColorUtility.TryParseHtmlString("#" + hex, out var c); c.a = alpha; return c; }
        public static Color Tier(int tier) => Hex(new[] { "8C8C8C", "2D7FF9", "9B4DFF", "FFB800" }[Mathf.Clamp(tier - 1, 0, 3)]);
        public static Sprite Shape(string kind)
        {
            if (cache.TryGetValue(kind, out var saved) && saved) return saved;
            const int n = 256;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "UI procedural " + kind, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) / n * 2 - 1, v = (y + .5f) / n * 2 - 1;
                float edge = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v), (Mathf.Abs(u) + Mathf.Abs(v)) / 1.75f);
                if (kind == "diamond") edge = Mathf.Abs(u) + Mathf.Abs(v);
                if (kind == "hex") edge = Mathf.Max(Mathf.Abs(v), Mathf.Abs(u) * .866f + Mathf.Abs(v) * .5f);
                Color c = Color.clear;
                if (kind == "solid") c = Color.white;
                else if (kind == "ring") { float r = Mathf.Sqrt(u*u+v*v); if (r < .98f && r > .78f) c = Color.white; }
                else if (kind == "vignette") c = new Color(1, 1, 1, Mathf.Pow(Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)), 6));
                else if (edge < 1) c = edge > .95f ? new Color(.12f,.13f,.17f) : edge > .90f ? Color.white : edge > .85f ? new Color(.12f,.13f,.17f) : new Color(.20f,.21f,.28f,.96f);
                pixels[y*n+x] = c;
            }
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0,0,n,n), new Vector2(.5f,.5f), 200, 0, SpriteMeshType.FullRect, kind == "frame" ? new Vector4(36,36,36,36) : Vector4.zero); sprite.name = kind; sprite.hideFlags = HideFlags.DontSave;
            cache[kind] = sprite; return sprite;
        }
        public static Sprite Weapon(WeaponInstance weapon)
        {
            var catalog=Codice.Presentation.WeaponVisualCatalog.Load();var icon=catalog?catalog.Icon(weapon):null;
            return icon?icon:Weapon(weapon.kind);
        }
        public static Sprite Weapon(WeaponKind kind)
        {
            string key = "weapon" + kind;
            if (cache.TryGetValue(key, out var saved) && saved) return saved;
            const int n = 256; var pixels = new Color[n*n];
            void Line(float ax,float ay,float bx,float by,float width)
            {
                var a = new Vector2(ax,ay); var b = new Vector2(bx,by); var d=b-a;
                for(int y=0;y<n;y++) for(int x=0;x<n;x++)
                { var p = new Vector2((x+.5f)*64/n,(y+.5f)*64/n); float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude)); float alpha=Mathf.Clamp01((width-Vector2.Distance(p,a+d*t))*4+.5f); if(alpha>pixels[y*n+x].a) pixels[y*n+x]=new Color(1,1,1,alpha); }
            }
            if(kind == WeaponKind.Bow) { for(int i=0;i<24;i++){float a=-1.25f+i*.1f,b=a+.1f;Line(24+23*Mathf.Cos(a),32+25*Mathf.Sin(a),24+23*Mathf.Cos(b),32+25*Mathf.Sin(b),2);} Line(31,8,31,56,1);Line(15,32,53,32,1.5f); }
            else if(kind == WeaponKind.Shield) {Line(12,49,32,57,3);Line(32,57,52,49,3);Line(12,49,17,23,4);Line(52,49,47,23,4);Line(17,23,32,8,4);Line(47,23,32,8,4);Line(32,15,32,49,3);}
            else if(kind == WeaponKind.Musket) {Line(9,13,49,53,4);Line(8,13,21,20,6);Line(26,25,33,20,2);Line(35,41,42,48,2);}
            else {Line(30,7,30,49,2.5f); switch(kind) {
                case WeaponKind.Sword: Line(30,28,30,58,5);Line(19,26,42,26,2);break;
                case WeaponKind.Dagger: Line(30,29,30,49,4);Line(22,27,38,27,2);break;
                case WeaponKind.Axe: Line(30,46,48,49,8);Line(46,41,46,56,3);break;
                case WeaponKind.Hammer: Line(16,49,46,49,8);break;
                case WeaponKind.Polearm: Line(30,58,23,44,3);Line(30,58,37,44,3);break;
                case WeaponKind.Scythe: Line(30,53,46,54,4);Line(46,54,55,39,3);break;
                case WeaponKind.Staff: Line(30,57,19,47,3);Line(19,47,30,37,3);Line(30,37,41,47,3);Line(41,47,30,57,3);break;
            }}
            var texture = new Texture2D(n,n,TextureFormat.RGBA32,false){name=key,hideFlags=HideFlags.DontSave,filterMode=FilterMode.Bilinear};texture.SetPixels(pixels);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100);sprite.hideFlags=HideFlags.DontSave;cache[key]=sprite;return sprite;
        }
        public static void Release()
        { foreach(var sprite in cache.Values) if(sprite) { Object.Destroy(sprite.texture);Object.Destroy(sprite); } cache.Clear(); }
    }
}

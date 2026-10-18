using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    // Resolution-independent cut metal, etched time marks and hourglass insignia.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RelicButtonFace : MaskableGraphic
    {
        public bool primary, highlighted, pressed;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect;
            Color bronze=new Color(.57f,.39f,.20f), light=new Color(1,.73f,.36f);
            Color edge=highlighted?light:bronze;
            void Poly(Vector2[] points,Color tint)
            {
                int start=vh.currentVertCount;
                foreach(var point in points)vh.AddVert(point,tint*color,Vector2.zero);
                for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
            }
            void Plate(float inset,Color tint)
            {
                float x=r.xMin+inset,y=r.yMin+inset,w=r.width-2*inset,h=r.height-2*inset,c=10;
                Poly(new[]{new Vector2(x+c,y),new Vector2(x+w-c,y),new Vector2(x+w,y+c),new Vector2(x+w,y+h-c),new Vector2(x+w-c,y+h),new Vector2(x+c,y+h),new Vector2(x,y+h-c),new Vector2(x,y+c)},tint);
            }
            void Stroke(Vector2 a,Vector2 b,float width,Color tint)
            {
                Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
                Poly(new[]{a+n,b+n,b-n,a-n},tint);
            }
            Plate(0,new Color(.025f,.026f,.035f));Plate(2,edge);
            Plate(4,pressed?new Color(.18f,.11f,.07f):primary?new Color(.19f,.105f,.065f):new Color(.065f,.078f,.10f));
            float mid=r.center.y;Vector2 emblem=new Vector2(r.xMin+35,mid);
            Poly(new[]{emblem+Vector2.up*20,emblem+Vector2.right*20,emblem+Vector2.down*20,emblem+Vector2.left*20},edge);
            Poly(new[]{emblem+Vector2.up*17,emblem+Vector2.right*17,emblem+Vector2.down*17,emblem+Vector2.left*17},new Color(.04f,.045f,.055f));
            Stroke(emblem+new Vector2(-7,9),emblem+new Vector2(7,9),1.5f,light);
            Stroke(emblem+new Vector2(-7,-9),emblem+new Vector2(7,-9),1.5f,light);
            Stroke(emblem+new Vector2(-6,8),emblem+new Vector2(6,-8),1.5f,light);
            Stroke(emblem+new Vector2(6,8),emblem+new Vector2(-6,-8),1.5f,light);
            for(int i=0;i<7;i++)
            {
                float x=r.xMax-90+i*6;
                Stroke(new Vector2(x,r.yMin+7),new Vector2(x+2,r.yMin+10+(i==3?3:0)),1,edge);
            }
            Stroke(new Vector2(r.xMin+65,r.yMax-7),new Vector2(r.xMax-24,r.yMax-7),1,edge);
            float arrow=r.xMax-(highlighted?23:29);
            Stroke(new Vector2(arrow-6,mid+6),new Vector2(arrow,mid),2,light);
            Stroke(new Vector2(arrow,mid),new Vector2(arrow-6,mid-6),2,light);
        }
    }
}

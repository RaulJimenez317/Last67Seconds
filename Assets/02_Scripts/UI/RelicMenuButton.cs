using UnityEngine.UI;

namespace Codice.UI
{
    public sealed class RelicMenuButton : Button
    {
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            var face=targetGraphic as RelicButtonFace;
            if(!face)return;
            face.highlighted=state==SelectionState.Highlighted||state==SelectionState.Selected||state==SelectionState.Pressed;
            face.pressed=state==SelectionState.Pressed;
            face.SetVerticesDirty();
        }
    }
}

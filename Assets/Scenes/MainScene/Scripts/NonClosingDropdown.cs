using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TMP_Dropdown variant whose Blocker ignores raycasts, so the open list
// only closes when the user picks an item (or the dropdown is hidden in code).
// Solves intermittent VR auto-close caused by stray controller raycasts.
public class NonClosingDropdown : TMP_Dropdown
{
    protected override GameObject CreateBlocker(Canvas rootCanvas)
    {
        var blocker = base.CreateBlocker(rootCanvas);

        var graphic = blocker.GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = false;

        return blocker;
    }
}

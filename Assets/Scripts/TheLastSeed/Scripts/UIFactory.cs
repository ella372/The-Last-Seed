using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helpers that build the cozy "paper" UI in code (cream panels with a dark brown border).
/// </summary>
public static class UIFactory
{
    public static readonly Color Cream = new Color(0.96f, 0.92f, 0.80f);
    public static readonly Color Brown = new Color(0.33f, 0.20f, 0.11f);
    public static readonly Color Ink   = new Color(0.25f, 0.15f, 0.08f);

    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Stretch/position a rect with anchors and offsets.</summary>
    public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    /// <summary>
    /// Creates a bordered panel. 'corner' is both the anchor and the pivot (e.g. (0,0) = bottom-left).
    /// Returns the INNER (cream) rect where you put the content.
    /// </summary>
    public static RectTransform Panel(string name, Transform parent, Vector2 corner, Vector2 pos, Vector2 size)
    {
        var outer = NewRect(name, parent);
        outer.gameObject.AddComponent<Image>().color = Brown;
        outer.anchorMin = outer.anchorMax = outer.pivot = corner;
        outer.anchoredPosition = pos;
        outer.sizeDelta = size;

        var inner = NewRect("Inner", outer);
        inner.gameObject.AddComponent<Image>().color = Cream;
        Place(inner, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
        return inner;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
        FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = Ink;
        t.raycastTarget = false;
        return t;
    }
}

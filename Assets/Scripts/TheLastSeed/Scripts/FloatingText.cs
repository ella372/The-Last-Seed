using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Floating damage / heal numbers above units.</summary>
public class FloatingText : MonoBehaviour
{
    public static void Spawn(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = worldPos + new Vector3(0f, 1.3f, 0f);
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = 5f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.sortingOrder = 9000;
        t.rectTransform.sizeDelta = new Vector2(5f, 1f);
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(45, 28, 12, 255);
        go.AddComponent<FloatingText>().StartCoroutine(go.GetComponent<FloatingText>().Animate(t));
    }

    IEnumerator Animate(TextMeshPro t)
    {
        float time = 0f;
        Vector3 start = transform.position;
        Color c = t.color;
        while (time < 1.1f)
        {
            time += Time.deltaTime;
            transform.position = start + new Vector3(0f, time * 0.7f, 0f);
            c.a = time < 0.6f ? 1f : Mathf.Clamp01(1f - (time - 0.6f) / 0.5f);
            t.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}

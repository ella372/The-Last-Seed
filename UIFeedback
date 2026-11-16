
using System.Collections;
using UnityEngine;
using TMPro;
using LastSeed.Domain;

public class UIFeedback : MonoBehaviour, IFeedbackSink
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float showSeconds = 2.5f;

    private Coroutine running;

    public void Show(string message)
    {
        if (label == null) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        label.text = message;
        yield return new WaitForSeconds(showSeconds);
        label.text = "";
    }
}


using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the whole HUD in code (Canvas 1920x1080, cozy paper style) and exposes simple methods/events.
/// </summary>
public class UIManager : MonoBehaviour
{
    class BarUI
    {
        public RectTransform fill;
        public TextMeshProUGUI label;
        public void Set(float frac, string text)
        {
            fill.gameObject.SetActive(frac > 0.001f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
            label.text = text;
        }
    }

    class Btn { public Button button; public Image face; public TextMeshProUGUI label; }

    public event Action MovePressed, AttackPressed, SkillPressed, WaitPressed, EndTurnPressed;

    Canvas canvas;
    TextMeshProUGUI tileTitle, tileBody, turnText, objectiveText, messageText, resultTitle, resultSub, resultButtonLabel;
    Image tilePreview, portrait;
    BarUI hpBar, mpBar, seedBar;
    Btn bMove, bAttack, bSkill, bWait, bEnd;
    GameObject messageRoot, resultRoot;
    Action resultAction;
    Coroutine msgRoutine;

    // =============== BUILD ===============
    public void Build()
    {
        var cgo = new GameObject("Canvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var vg = UIFactory.NewRect("Vignette", canvas.transform);
        var vi = vg.gameObject.AddComponent<Image>();
        vi.sprite = IsoArt.Vignette();
        vi.raycastTarget = false;
        UIFactory.Place(vg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildTopLeft();
        BuildTopRight();
        BuildActionBar();
        BuildEndTurn();
        BuildTileInfo();
        BuildMessage();
        BuildResult();
        ShowTileInfo(null);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var m = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        m.AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    BarUI MakeBar(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var bg = UIFactory.NewRect(name, parent);
        bg.gameObject.AddComponent<Image>().color = new Color(.25f, .16f, .10f);
        UIFactory.Place(bg, aMin, aMax, offMin, offMax);

        var fill = UIFactory.NewRect("Fill", bg);
        fill.gameObject.AddComponent<Image>().color = color;
        UIFactory.Place(fill, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));

        var label = UIFactory.Text(bg, "Label", "", 26, FontStyles.Bold, TextAlignmentOptions.Center);
        label.color = Color.white;
        label.outlineWidth = 0.25f;
        label.outlineColor = (Color32)UIFactory.Brown;
        UIFactory.Place(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return new BarUI { fill = fill, label = label };
    }

    void BuildTopLeft()
    {
        var inner = UIFactory.Panel("MiraPanel", canvas.transform, new Vector2(0, 1), new Vector2(30, -30), new Vector2(580, 230));

        var frame = UIFactory.NewRect("PortraitFrame", inner);
        frame.gameObject.AddComponent<Image>().color = UIFactory.Brown;
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, .5f);
        frame.anchoredPosition = new Vector2(16, 0);
        frame.sizeDelta = new Vector2(160, 160);

        var pr = UIFactory.NewRect("Portrait", frame);
        portrait = pr.gameObject.AddComponent<Image>();
        portrait.color = new Color(.86f, .80f, .62f);
        portrait.preserveAspect = true;
        UIFactory.Place(pr, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));

        var name = UIFactory.Text(inner, "Name", "Mira", 46, FontStyles.Bold);
        UIFactory.Place(name.rectTransform, new Vector2(0, .68f), new Vector2(1, 1), new Vector2(196, 0), new Vector2(-14, -6));
        var lv = UIFactory.Text(inner, "Level", "Lv. 1  ·  Wanderer", 28);
        UIFactory.Place(lv.rectTransform, new Vector2(0, .50f), new Vector2(1, .70f), new Vector2(196, 0), new Vector2(-14, 0));

        hpBar = MakeBar(inner, "HP", new Color(.40f, .78f, .32f), new Vector2(0, .27f), new Vector2(1, .48f), new Vector2(196, 0), new Vector2(-14, 0));
        mpBar = MakeBar(inner, "MP", new Color(.60f, .40f, .85f), new Vector2(0, .05f), new Vector2(1, .26f), new Vector2(196, 0), new Vector2(-14, 0));
    }

    void BuildTopRight()
    {
        var inner = UIFactory.Panel("InfoPanel", canvas.transform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(620, 230));
        var title = UIFactory.Text(inner, "Title", "Victory Conditions", 40, FontStyles.Bold);
        UIFactory.Place(title.rectTransform, new Vector2(0, .72f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, -8));
        objectiveText = UIFactory.Text(inner, "Objective", "Protect the seed until it blooms", 30);
        UIFactory.Place(objectiveText.rectTransform, new Vector2(0, .48f), new Vector2(1, .74f), new Vector2(20, 0), new Vector2(-20, 0));
        turnText = UIFactory.Text(inner, "Turn", "Turn: 1", 38, FontStyles.Bold);
        UIFactory.Place(turnText.rectTransform, new Vector2(0, .26f), new Vector2(1, .48f), new Vector2(20, 0), new Vector2(-20, 0));
        seedBar = MakeBar(inner, "Seed", new Color(.95f, .78f, .25f), new Vector2(0, .05f), new Vector2(1, .25f), new Vector2(20, 0), new Vector2(-20, 0));
    }

    Btn MakeButton(Transform parent, string text, Action onClick, int fontSize = 38)
    {
        var rt = UIFactory.NewRect(text, parent);
        var face = rt.gameObject.AddComponent<Image>();
        face.color = UIFactory.Cream;
        var ol = rt.gameObject.AddComponent<Outline>();
        ol.effectColor = UIFactory.Brown;
        ol.effectDistance = new Vector2(4, -4);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = face;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, .96f, .82f);
        cb.pressedColor = new Color(.82f, .78f, .62f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(.62f, .60f, .56f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());
        var label = UIFactory.Text(rt, "Label", text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Place(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return new Btn { button = btn, face = face, label = label };
    }

    void BuildActionBar()
    {
        var inner = UIFactory.Panel("ActionBar", canvas.transform, new Vector2(.5f, 0), new Vector2(0, 30), new Vector2(880, 140));
        var lg = inner.gameObject.AddComponent<HorizontalLayoutGroup>();
        lg.padding = new RectOffset(12, 12, 12, 12);
        lg.spacing = 12;
        lg.childControlWidth = lg.childControlHeight = true;
        lg.childForceExpandWidth = lg.childForceExpandHeight = true;
        bMove = MakeButton(inner, "Move", () => MovePressed?.Invoke());
        bAttack = MakeButton(inner, "Attack", () => AttackPressed?.Invoke());
        bSkill = MakeButton(inner, "Skill", () => SkillPressed?.Invoke());
        bWait = MakeButton(inner, "Wait", () => WaitPressed?.Invoke());
    }

    void BuildEndTurn()
    {
        var inner = UIFactory.Panel("EndTurnPanel", canvas.transform, new Vector2(1, 0), new Vector2(-30, 30), new Vector2(320, 140));
        bEnd = MakeButton(inner, "End Turn", () => EndTurnPressed?.Invoke(), 42);
        var rt = bEnd.button.GetComponent<RectTransform>();
        UIFactory.Place(rt, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
    }

    void BuildTileInfo()
    {
        var inner = UIFactory.Panel("TileInfoBox", canvas.transform, new Vector2(0, 0), new Vector2(30, 30), new Vector2(600, 280));

        var frame = UIFactory.NewRect("PreviewFrame", inner);
        frame.gameObject.AddComponent<Image>().color = UIFactory.Brown;
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, .5f);
        frame.anchoredPosition = new Vector2(18, 0);
        frame.sizeDelta = new Vector2(130, 130);

        var pv = UIFactory.NewRect("Preview", frame);
        tilePreview = pv.gameObject.AddComponent<Image>();
        UIFactory.Place(pv, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));

        tileTitle = UIFactory.Text(inner, "Title", "", 40, FontStyles.Bold);
        UIFactory.Place(tileTitle.rectTransform, new Vector2(0, .78f), new Vector2(1, 1), new Vector2(170, 0), new Vector2(-14, -8));
        tileBody = UIFactory.Text(inner, "Body", "", 25);
        UIFactory.Place(tileBody.rectTransform, new Vector2(0, 0), new Vector2(1, .78f), new Vector2(170, 10), new Vector2(-14, 0));
    }

    void BuildMessage()
    {
        var inner = UIFactory.Panel("Message", canvas.transform, new Vector2(.5f, 1), new Vector2(0, -40), new Vector2(640, 80));
        messageRoot = inner.parent.gameObject;
        messageText = UIFactory.Text(inner, "Text", "", 36, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Place(messageText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        messageRoot.SetActive(false);
    }

    void BuildResult()
    {
        resultRoot = new GameObject("Result", typeof(RectTransform), typeof(Image));
        resultRoot.transform.SetParent(canvas.transform, false);
        resultRoot.GetComponent<Image>().color = new Color(0, 0, 0, .55f);
        UIFactory.Place(resultRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var inner = UIFactory.Panel("ResultPanel", resultRoot.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(820, 420));
        resultTitle = UIFactory.Text(inner, "Title", "", 70, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Place(resultTitle.rectTransform, new Vector2(0, .58f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -20));
        resultSub = UIFactory.Text(inner, "Sub", "", 34, FontStyles.Normal, TextAlignmentOptions.Center);
        UIFactory.Place(resultSub.rectTransform, new Vector2(0, .38f), new Vector2(1, .60f), new Vector2(30, 0), new Vector2(-30, 0));

        var b = MakeButton(inner, "Button", () => resultAction?.Invoke(), 40);
        var rt = b.button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, 0);
        rt.anchoredPosition = new Vector2(0, 30);
        rt.sizeDelta = new Vector2(420, 110);
        resultButtonLabel = b.label;
        resultRoot.SetActive(false);
    }

    // =============== UPDATE THE HUD ===============
    public void SetPortrait(Sprite s) { portrait.sprite = s; portrait.color = Color.white; }

    public void SetMira(int hp, int maxHp, int mp, int maxMp)
    {
        hpBar.Set(hp / (float)Mathf.Max(1, maxHp), $"HP {hp}/{maxHp}");
        mpBar.Set(mp / (float)Mathf.Max(1, maxMp), $"MP {mp}/{maxMp}");
    }

    public void SetTurn(int turn) { turnText.text = "Turn: " + turn; }
    public void SetObjective(string text) { objectiveText.text = text; }
    public void SetSeed(int stage, int max) { seedBar.Set(stage / (float)Mathf.Max(1, max), $"Seed growth {stage}/{max}"); }

    /// <summary>Enable/grey out the buttons. active: 0 none, 1 Move, 2 Attack, 3 Skill (highlighted).</summary>
    public void SetButtons(bool move, bool attack, bool skill, bool wait, bool end, int active)
    {
        Apply(bMove, move, active == 1);
        Apply(bAttack, attack, active == 2);
        Apply(bSkill, skill, active == 3);
        Apply(bWait, wait, false);
        Apply(bEnd, end, false);
    }

    void Apply(Btn b, bool enabled, bool active)
    {
        b.button.interactable = enabled;
        b.face.color = active ? new Color(.78f, .92f, .62f) : UIFactory.Cream;
        b.label.color = enabled || active ? UIFactory.Ink : new Color(.50f, .45f, .40f);
    }

    /// <summary>Tile name + effect, and the unit standing on it (if any). null = hint text.</summary>
    public void ShowTileInfo(Tile t)
    {
        if (t == null)
        {
            tileTitle.text = "Tile info";
            tileBody.text = "Click a tile or a unit to see what it does.";
            tilePreview.sprite = null;
            tilePreview.preserveAspect = true;
            tilePreview.color = new Color(0, 0, 0, 0.08f);
            return;
        }
        var d = t.Def;
        tileTitle.text = d.displayName;
        string text = d.description + (d.walkable ? $"\nMove cost: {d.moveCost}" : "");
        if (t.Occupant != null) text += "\n" + t.Occupant.InfoLine();
        tileBody.text = text;
        tilePreview.sprite = t.PreviewSprite;
        tilePreview.preserveAspect = true;
        tilePreview.color = Color.white;
    }

    public void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageRoot.SetActive(true);
        if (msgRoutine != null) StopCoroutine(msgRoutine);
        msgRoutine = StartCoroutine(HideMessageLater());
    }

    IEnumerator HideMessageLater()
    {
        yield return new WaitForSeconds(1.8f);
        messageRoot.SetActive(false);
    }

    public void ShowResult(string title, string subtitle, string buttonText, Action onClick)
    {
        resultTitle.text = title;
        resultSub.text = subtitle;
        resultButtonLabel.text = buttonText;
        resultAction = onClick;
        resultRoot.SetActive(true);
    }

    public void HideResult() { if (resultRoot != null) resultRoot.SetActive(false); }
}

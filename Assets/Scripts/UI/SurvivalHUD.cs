using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI HUD built in code at startup (no prefabs needed): four stat bars, clock, items, missions,
/// place/fire info, interaction prompt, action preview card, messages, rain and the end screen.
/// </summary>
public class SurvivalHUD : MonoBehaviour
{
    const float BarWidth = 300f;
    const int RainDrops = 70;

    static readonly string[] Labels = { "Energy", "Water", "Food", "Warmth" };
    static readonly Color[] BarColors =
    {
        new Color(0.95f, 0.85f, 0.25f),
        new Color(0.30f, 0.60f, 0.95f),
        new Color(0.95f, 0.60f, 0.20f),
        new Color(0.90f, 0.35f, 0.35f),
    };

    SurvivalStats stats;
    Inventory inventory;
    ActionSystem actions;
    PlayerController player;
    ObjectiveManager objectives;

    Font font;
    RectTransform[] fills = new RectTransform[4];
    Image[] fillImages = new Image[4];
    Text[] values = new Text[4];
    Text clockText, itemsText, placeText, promptText, toastText, missionsText;
    Text cardTitle, cardDesc, cardCost, cardNeeds, cardHint, cardControls;
    Text workingText, endTitle, endDetail, endHint, endQuip, endTip;
    GameObject card, working, endPanel, rainLayer;
    RectTransform[] drops = new RectTransform[RainDrops];
    float[] dropSpeed = new float[RainDrops];
    float toastUntil;

    // Dialogue box (the character's thoughts), typed out letter by letter
    const float CharsPerSecond = 45f;
    Text bubbleText;
    GameObject bubble;
    Image arrowImage;
    readonly Queue<string> dialogueQueue = new Queue<string>();
    string dialogueText = "";
    float typedChars, dialogueHoldUntil;
    bool typing;
    bool missionsDirty = true;

    void Start()
    {
        stats = FindFirstObjectByType<SurvivalStats>();
        inventory = FindFirstObjectByType<Inventory>();
        actions = FindFirstObjectByType<ActionSystem>();
        player = FindFirstObjectByType<PlayerController>();
        objectives = ObjectiveManager.Instance;

        // Pixelify Sans (OFL, Resources/Pixel/Ui): a pixel font that stays readable; the plain built-in font if it is missing
        font = Resources.Load<Font>("Pixel/Ui/PixelifySans");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();

        if (actions != null)
        {
            actions.PreviewOpened += OnPreviewOpened;
            actions.PreviewClosed += OnPreviewClosed;
            actions.ActionStarted += OnActionStarted;
            actions.Message += Toast;
            actions.Thought += ShowThought;
        }
        if (stats != null) stats.LevelChanged += OnLevelChanged;
        if (objectives != null)
        {
            objectives.Changed += OnObjectivesChanged;
            objectives.Completed += OnObjectiveCompleted;
        }
    }

    void OnDestroy()
    {
        if (actions != null)
        {
            actions.PreviewOpened -= OnPreviewOpened;
            actions.PreviewClosed -= OnPreviewClosed;
            actions.ActionStarted -= OnActionStarted;
            actions.Message -= Toast;
            actions.Thought -= ShowThought;
        }
        if (stats != null) stats.LevelChanged -= OnLevelChanged;
        if (objectives != null)
        {
            objectives.Changed -= OnObjectivesChanged;
            objectives.Completed -= OnObjectiveCompleted;
        }
    }

    // ------------------------------------------------------------------ events

    void OnPreviewOpened(Interactable target, string hint)
    {
        var a = target.ActiveAction;
        cardTitle.text = a.name;
        cardDesc.text = a.description;
        cardCost.text = a.DescribeCost();

        string needs = a.DescribeNeeds();
        cardNeeds.text = string.IsNullOrEmpty(a.reason) ? needs : (needs.Length > 0 ? needs + "\n" : "") + a.reason;

        cardHint.text = hint ?? "";
        cardControls.text = hint == null ? "[E] Do it      [Esc] Cancel" : "[E] / [Esc] Back";
        card.SetActive(true);
    }

    void OnPreviewClosed()
    {
        card.SetActive(false);
        working.SetActive(false);
    }

    void OnActionStarted(Interactable target)
    {
        card.SetActive(false);
        workingText.text = target.ActiveAction.name + "...";
        working.SetActive(true);
    }

    void OnLevelChanged(StatType type, int level)
    {
        if (level == 0) return;
        string what = level == 2 ? "very low" : "getting low";
        string tip = "";
        if (GameHints.Enabled) switch (type)
        {
            case StatType.Energy: tip = " Sleep at a camp to recover."; break;
            case StatType.Water: tip = " Boil stream water at a fire."; break;
            case StatType.Food: tip = " Eat the airline snack."; break;
            case StatType.Warmth: tip = " Fire, shelter, more layers (look in trees)."; break;
        }
        Toast($"{Labels[(int)type]} is {what}!{tip}");
    }

    void ShowThought(string text)
    {
        // At most two thoughts wait in line; the oldest one is dropped.
        dialogueQueue.Enqueue(text);
        while (dialogueQueue.Count > 2) dialogueQueue.Dequeue();
    }

    void StartNextDialogue()
    {
        dialogueText = dialogueQueue.Dequeue();
        typedChars = 0f;
        typing = true;
        bubbleText.text = "";
        arrowImage.enabled = false;
        bubble.SetActive(true);
    }

    void UpdateDialogue()
    {
        if (!bubble.activeSelf)
        {
            if (dialogueQueue.Count > 0) StartNextDialogue();
            return;
        }
        if (typing)
        {
            typedChars += Time.unscaledDeltaTime * CharsPerSecond;
            int n = Mathf.Min((int)typedChars, dialogueText.Length);
            bubbleText.text = dialogueText.Substring(0, n);
            if (n >= dialogueText.Length)
            {
                typing = false;
                dialogueHoldUntil = Time.time + Mathf.Max(2.5f, dialogueText.Length * 0.04f);
            }
            return;
        }
        arrowImage.enabled = ((int)(Time.time * 2f) % 2) == 0;   // blinking marker: "done reading"
        if (Time.time >= dialogueHoldUntil)
        {
            if (dialogueQueue.Count > 0) StartNextDialogue();
            else bubble.SetActive(false);
        }
    }

    void OnObjectivesChanged() => missionsDirty = true;

    void OnObjectiveCompleted(Objective o) => Toast("Mission complete: " + o.title);

    void Toast(string message)
    {
        toastText.text = message;
        toastUntil = Time.time + 4f;
    }

    // ------------------------------------------------------------------ per-frame

    void Update()
    {
        if (stats == null) return;
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
        {
            GameHints.Enabled = !GameHints.Enabled;
            missionsDirty = true;
            Toast(GameHints.Enabled ? "Hints on" : "Hints off");
        }
        var clock = GameClock.Instance;
        var gm = GameManager.Instance;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        for (int i = 0; i < 4; i++)
        {
            var type = (StatType)i;
            float v = stats.Get(type);
            fills[i].sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(v / stats.max), fills[i].sizeDelta.y);
            int level = stats.GetLevel(type);
            fillImages[i].color = level == 0 ? BarColors[i] : Color.Lerp(BarColors[i], Color.red, level == 2 ? pulse : 0.4f);
            values[i].text = Mathf.CeilToInt(v).ToString();
        }

        if (clock != null) clockText.text = $"Day {clock.DayCount}   {clock.TimeText}";
        itemsText.text = ItemsLine();
        placeText.text = PlaceLine();

        if (missionsDirty && objectives != null)
        {
            missionsText.text = MissionsString();
            missionsDirty = false;
        }

        bool playing = gm == null || gm.State == GameState.Playing;
        bool showPrompt = playing && actions != null && !actions.IsBusy && player != null && player.Nearest != null;
        promptText.text = showPrompt ? $"[E]  {player.Nearest.ActiveAction.name}" : "";

        toastText.gameObject.SetActive(Time.time < toastUntil);
        UpdateDialogue();
        UpdateRain();

        if (gm != null && gm.State != GameState.Playing)
        {
            if (!endPanel.activeSelf)
            {
                card.SetActive(false);
                working.SetActive(false);
                endPanel.SetActive(true);
            }
            endTitle.text = gm.EndTitle;
            endDetail.text = gm.EndDetail;
            endQuip.text = gm.EndQuip;
            endTip.text = string.IsNullOrEmpty(gm.EndTip) ? "" : "<b>Real survival tip:</b> " + gm.EndTip;
        }
    }

    void UpdateRain()
    {
        bool raining = Weather.Instance != null && Weather.Instance.IsRaining;
        if (rainLayer.activeSelf != raining) rainLayer.SetActive(raining);
        if (!raining) return;

        for (int i = 0; i < RainDrops; i++)
        {
            var p = drops[i].anchoredPosition;
            p.y -= dropSpeed[i] * Time.deltaTime;
            p.x -= dropSpeed[i] * 0.08f * Time.deltaTime;
            if (p.y < -60f)
            {
                p.y = 1140f;
                p.x = Random.Range(0f, 1980f);
            }
            drops[i].anchoredPosition = p;
        }
    }

    string ItemsLine()
    {
        if (inventory == null) return "";
        var sb = new StringBuilder("Items: ");
        bool any = false;
        int junk = 0;
        foreach (var kv in inventory.All)
        {
            if (kv.Key.IsJunk()) { junk += kv.Value; continue; }
            if (any) sb.Append(", ");
            sb.Append(kv.Key.Pretty());
            if (kv.Value > 1) sb.Append(" x").Append(kv.Value);
            any = true;
        }
        if (junk > 0)
        {
            if (any) sb.Append(", ");
            sb.Append("Junk x").Append(junk);
            any = true;
        }
        if (!any) sb.Append("-");
        return sb.ToString();
    }

    string PlaceLine()
    {
        var sb = new StringBuilder();
        if (Weather.Instance != null && Weather.Instance.IsRaining) sb.Append("RAIN   ");
        if (stats.IsWet) sb.Append("WET CLOTHES   ");
        if (stats.gearFactor < 0.999f) sb.Append($"Clothing: warmth loss x{stats.gearFactor:0.00}   ");
        if (player != null && player.CurrentTerrain != null) sb.Append(player.CurrentTerrain.label).Append("   ");
        if (!string.IsNullOrEmpty(stats.ZoneLabel)) sb.Append(stats.ZoneLabel);
        if (inventory != null && inventory.Has(ItemType.Map) && RescuePoint.Instance != null)
        {
            if (sb.Length > 0) sb.Append("   ");
            sb.Append("Map: rescue spot ").Append(RescuePoint.Instance.DirectionFrom(stats.transform.position));
        }
        var fire = Campfire.NearestLit(stats.transform.position, 8f);
        if (fire != null)
        {
            if (sb.Length > 0) sb.Append("   ");
            sb.Append($"Fire: {Mathf.CeilToInt(fire.TimeLeft)} min left");
        }
        return sb.ToString();
    }

    string MissionsString()
    {
        var sb = new StringBuilder("<b>MISSIONS</b>\n");
        var current = objectives.Current;
        foreach (var o in objectives.Objectives)
        {
            if (o.Done)
            {
                sb.Append("<color=#7fd67f>[x] ").Append(o.title).Append("</color>\n");
            }
            else if (o == current)
            {
                sb.Append("<color=#fff2a0>[ ] ").Append(o.title).Append("</color>\n");
                if (GameHints.Enabled) sb.Append("<color=#d0d0d0>      ").Append(o.hint).Append("</color>\n");
            }
            else
            {
                sb.Append("<color=#8a8a8a>[ ] ").Append(o.title).Append("</color>\n");
            }
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ UI building

    void BuildUI()
    {
        var canvasGo = new GameObject("HUD Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        BuildRain(root);

        // Stat bars (top-left)
        for (int i = 0; i < 4; i++)
        {
            float y = -24f - i * 50f;
            MakeText("Label" + i, root, Labels[i], 28, TextAnchor.MiddleLeft, Vector2.up, Vector2.up,
                     new Vector2(24, y), new Vector2(150, 36), Color.white);
            MakeImage("BarBg" + i, root, new Color(0, 0, 0, 0.6f), Vector2.up, Vector2.up,
                      new Vector2(180, y - 2), new Vector2(BarWidth + 8, 36));
            var fillImg = MakeImage("BarFill" + i, root, BarColors[i], Vector2.up, new Vector2(0, 1),
                                    new Vector2(184, y - 6), new Vector2(BarWidth, 28));
            fillImages[i] = fillImg;
            fills[i] = fillImg.rectTransform;
            values[i] = MakeText("Value" + i, root, "100", 24, TextAnchor.MiddleLeft, Vector2.up, Vector2.up,
                                 new Vector2(500, y), new Vector2(80, 36), Color.white);
        }

        // Missions (top-left, below the bars)
        missionsText = MakeText("Missions", root, "", 24, TextAnchor.UpperLeft, Vector2.up, Vector2.up,
                                new Vector2(24, -240), new Vector2(720, 520), Color.white);

        // Clock, items, place (top-right)
        clockText = MakeText("Clock", root, "", 42, TextAnchor.MiddleRight, Vector2.one, Vector2.one,
                             new Vector2(-30, -20), new Vector2(600, 56), Color.white);
        itemsText = MakeText("Items", root, "", 26, TextAnchor.MiddleRight, Vector2.one, Vector2.one,
                             new Vector2(-30, -80), new Vector2(900, 40), new Color(0.9f, 0.9f, 0.9f));
        placeText = MakeText("Place", root, "", 24, TextAnchor.MiddleRight, Vector2.one, Vector2.one,
                             new Vector2(-30, -122), new Vector2(1100, 40), new Color(0.75f, 0.9f, 1f));

        // Interaction prompt (bottom-center)
        var bottom = new Vector2(0.5f, 0f);
        promptText = MakeText("Prompt", root, "", 34, TextAnchor.MiddleCenter, bottom, bottom,
                              new Vector2(0, 270), new Vector2(1200, 56), new Color(1f, 0.95f, 0.6f));

        MakeText("Controls", root, "WASD / Arrows: move     E: interact     Esc: cancel     H: hints on/off     R: restart (after the end)",
                 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero, new Vector2(24, 16), new Vector2(1400, 30),
                 new Color(1, 1, 1, 0.7f));

        // Toast (top-center)
        var top = new Vector2(0.5f, 1f);
        toastText = MakeText("Toast", root, "", 34, TextAnchor.MiddleCenter, top, top,
                             new Vector2(0, -150), new Vector2(1400, 60), new Color(1f, 0.95f, 0.6f));
        toastText.gameObject.SetActive(false);

        // Dialogue box (bottom of the screen, like a classic 2D RPG): the character's inner voice, typed out letter by letter
        var bubbleImg = MakeImage("Dialogue", root, Color.white, bottom, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(1560, 210));
        bubbleImg.sprite = PixelUI.Box();
        bubbleImg.type = Image.Type.Sliced;
        bubbleImg.pixelsPerUnitMultiplier = 6f;
        bubble = bubbleImg.gameObject;
        bubbleText = MakeText("DialogueText", bubble.transform, "", 40, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0, 4), new Vector2(1470, 150), PixelUI.Dark);
        Destroy(bubbleText.GetComponent<Outline>());   // dark text on a light box needs no outline
        var arrowGo = NewRect("DialogueArrow", bubble.transform);
        Place(arrowGo, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36, 26), new Vector2(34, 34));
        arrowImage = arrowGo.gameObject.AddComponent<Image>();
        arrowImage.sprite = PixelUI.Arrow();
        arrowImage.raycastTarget = false;
        bubble.SetActive(false);

        // Action preview card (center)
        var mid = new Vector2(0.5f, 0.5f);
        var cardImg = MakeImage("Card", root, new Color(0, 0, 0, 0.88f), mid, mid, Vector2.zero, new Vector2(1000, 500));
        card = cardImg.gameObject;
        var ct = card.transform;
        var topC = new Vector2(0.5f, 1f);
        cardTitle = MakeText("Title", ct, "", 46, TextAnchor.MiddleCenter, topC, topC, new Vector2(0, -20), new Vector2(940, 64), Color.white);
        cardDesc = MakeText("Desc", ct, "", 28, TextAnchor.UpperCenter, topC, topC, new Vector2(0, -95), new Vector2(900, 100), new Color(0.9f, 0.9f, 0.9f));
        cardCost = MakeText("Cost", ct, "", 32, TextAnchor.MiddleCenter, topC, topC, new Vector2(0, -205), new Vector2(940, 50), Color.white);
        cardNeeds = MakeText("Needs", ct, "", 24, TextAnchor.UpperCenter, topC, topC, new Vector2(0, -265), new Vector2(940, 90), new Color(0.78f, 0.78f, 0.78f));
        cardHint = MakeText("Hint", ct, "", 30, TextAnchor.MiddleCenter, topC, topC, new Vector2(0, -365), new Vector2(940, 70), new Color(1f, 0.5f, 0.5f));
        cardControls = MakeText("Controls", ct, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                new Vector2(0, 18), new Vector2(940, 44), new Color(1f, 0.95f, 0.6f));
        card.SetActive(false);

        // "Time passes" label
        workingText = MakeText("Working", root, "", 40, TextAnchor.MiddleCenter, mid, mid, Vector2.zero, new Vector2(1200, 70), Color.white);
        working = workingText.gameObject;
        working.SetActive(false);

        // End screen
        var end = MakeImage("End", root, new Color(0, 0, 0, 0.75f), Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(end.rectTransform);
        endPanel = end.gameObject;
        endTitle = MakeText("EndTitle", endPanel.transform, "", 64, TextAnchor.MiddleCenter, mid, mid, new Vector2(0, 60), new Vector2(1500, 100), Color.white);
        endDetail = MakeText("EndDetail", endPanel.transform, "", 34, TextAnchor.MiddleCenter, mid, mid, new Vector2(0, -20), new Vector2(1500, 60), new Color(0.9f, 0.9f, 0.9f));
        endQuip = MakeText("EndQuip", endPanel.transform, "", 34, TextAnchor.MiddleCenter, mid, mid, new Vector2(0, -105), new Vector2(1400, 110), new Color(0.85f, 0.88f, 1f));
        endQuip.fontStyle = FontStyle.Italic;
        endTip = MakeText("EndTip", endPanel.transform, "", 28, TextAnchor.MiddleCenter, mid, mid, new Vector2(0, -215), new Vector2(1400, 110), new Color(0.6f, 0.95f, 0.6f));
        endHint = MakeText("EndHint", endPanel.transform, "Press R to try again", 30, TextAnchor.MiddleCenter, mid, mid, new Vector2(0, -330), new Vector2(1500, 50), new Color(1f, 0.95f, 0.6f));
        endPanel.SetActive(false);
    }

    void BuildRain(Transform root)
    {
        var layer = NewRect("Rain", root);
        Stretch(layer);
        rainLayer = layer.gameObject;

        var tint = MakeImage("RainTint", layer, new Color(0.10f, 0.20f, 0.45f, 0.22f), Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(tint.rectTransform);

        for (int i = 0; i < RainDrops; i++)
        {
            var drop = MakeImage("Drop" + i, layer, new Color(0.75f, 0.85f, 1f, 0.35f), Vector2.zero, new Vector2(0.5f, 0.5f),
                                 new Vector2(Random.Range(0f, 1980f), Random.Range(0f, 1140f)), new Vector2(3f, 46f));
            drops[i] = drop.rectTransform;
            dropSpeed[i] = Random.Range(900f, 1500f);
        }
        rainLayer.SetActive(false);
    }

    RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    Image MakeImage(string name, Transform parent, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var r = NewRect(name, parent);
        Place(r, anchor, pivot, pos, size);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text MakeText(string name, Transform parent, string content, int fontSize, TextAnchor align,
                  Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        var r = NewRect(name, parent);
        Place(r, anchor, pivot, pos, size);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = Mathf.RoundToInt(fontSize * 1.12f);   // Pixelify Sans looks a bit smaller than the plain font
        t.alignment = align;
        t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var outline = r.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);
        return t;
    }
}

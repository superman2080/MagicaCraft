using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Assets > MagicaCraft > Create SpellEditorUI Prefab 메뉴로 실행
// 씬에 SpellEditorCanvas 오브젝트를 완성된 계층으로 생성하고
// Assets/03. Datas/Prefabs/SpellEditorUI.prefab 으로 저장한다.
public static class SpellEditorUIBuilder
{
    private const string PrefabSavePath = "Assets/03. Datas/Prefabs/SpellEditorUI.prefab";

    [MenuItem("MagicaCraft/Create SpellEditorUI Prefab")]
    public static void Build()
    {
        // 기존 씬 오브젝트 제거
        var existing = GameObject.Find("SpellEditorCanvas");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        // ── Canvas Root ───────────────────────────────────────
        var root = new GameObject("SpellEditorCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        var editorUI = root.AddComponent<SpellEditorUI>();

        // ── Panel ─────────────────────────────────────────────
        var panel = MakeRect("Panel", root.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRect.pivot            = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta        = new Vector2(700, 560);
        panelRect.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = Hex("282C34F0");
        var vLayout = panel.AddComponent<VerticalLayoutGroup>();
        vLayout.childControlWidth      = true;
        vLayout.childControlHeight     = true;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing                = 0;

        // ── Header ────────────────────────────────────────────
        var header = MakeRect("Header", panel.transform);
        SetLayoutHeight(header, 32);
        header.AddComponent<Image>().color = Hex("21252BF0");

        var titleText = MakeText("TitleText", header.transform, "Spell Editor", 13, Hex("ABB2BFFF"));
        StretchRect(titleText.rectTransform, new Vector2(10, 0), new Vector2(-40, 0));

        var closeBtn = MakeButton("CloseButton", header.transform, "✕", 12, Color.white, new Color(0.8f, 0.3f, 0.3f));
        var closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin        = new Vector2(1, 0);
        closeBtnRect.anchorMax        = new Vector2(1, 1);
        closeBtnRect.pivot            = new Vector2(1, 0.5f);
        closeBtnRect.anchoredPosition = Vector2.zero;
        closeBtnRect.sizeDelta        = new Vector2(32, -8);

        // ── ContextBar ────────────────────────────────────────
        var contextBar = MakeRect("ContextBar", panel.transform);
        SetLayoutHeight(contextBar, 26);
        contextBar.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.19f, 0.9f);
        var hLayout1 = contextBar.AddComponent<HorizontalLayoutGroup>();
        hLayout1.childControlHeight     = true;
        hLayout1.childControlWidth      = true;
        hLayout1.childForceExpandHeight = true;
        hLayout1.childForceExpandWidth  = false;
        hLayout1.padding = new RectOffset(8, 8, 4, 4);
        hLayout1.spacing = 0;

        var casterLabel = MakeText("CasterLabel", contextBar.transform, "<color=#5C6370>Caster: (없음)</color>", 11, Hex("ABB2BFFF"));
        SetLayoutWidth(casterLabel.gameObject, 220);
        var targetLabel = MakeText("TargetLabel", contextBar.transform, "<color=#5C6370>Target: (없음)</color>", 11, Hex("ABB2BFFF"));
        SetLayoutWidth(targetLabel.gameObject, 220);
        MakeRect("Spacer", contextBar.transform).AddComponent<LayoutElement>().flexibleWidth = 1;
        var manaLabel = MakeText("ManaLabel", contextBar.transform, "Mana: 500/500", 11, new Color(0.78f, 0.67f, 0.94f));
        manaLabel.alignment = TextAlignmentOptions.Right;
        SetLayoutWidth(manaLabel.gameObject, 140);

        // ── CodeEditorWrapper ─────────────────────────────────
        var codeWrapper = MakeRect("CodeEditorWrapper", panel.transform);
        var codeWrapperEl = codeWrapper.AddComponent<LayoutElement>();
        codeWrapperEl.preferredHeight = 270;
        codeWrapperEl.flexibleHeight  = 1;
        codeWrapper.AddComponent<Image>().color = Hex("1E2127FF");

        // TMP_InputField
        var inputRoot = MakeRect("CodeInput", codeWrapper.transform);
        var inputRect = inputRoot.GetComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = new Vector2(4, 4);
        inputRect.offsetMax = new Vector2(-4, -4);
        var inputField = inputRoot.AddComponent<TMP_InputField>();

        var textArea = MakeRect("Text Area", inputRoot.transform);
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(4, 4);
        taRect.offsetMax = new Vector2(-4, -4);
        textArea.AddComponent<RectMask2D>();
        inputField.textViewport = taRect;

        var placeholder = MakeRect("Placeholder", textArea.transform);
        StretchRect(placeholder.GetComponent<RectTransform>());
        var phText = placeholder.AddComponent<TextMeshProUGUI>();
        phText.text               = "// DSL 코드를 입력하세요...\n// 예: Fire(basicBolt, self.position, Vec2(1,0), 10)";
        phText.fontSize           = 13;
        phText.color              = Hex("5C6370FF");
        phText.enableWordWrapping = false;
        inputField.placeholder    = phText;

        var inputText = MakeRect("Text", textArea.transform);
        StretchRect(inputText.GetComponent<RectTransform>());
        var inputTmp = inputText.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize           = 13;
        inputTmp.color              = new Color(0.84f, 0.88f, 0.96f);
        inputTmp.enableWordWrapping = false;
        inputTmp.overflowMode       = TextOverflowModes.Overflow;
        inputField.textComponent    = inputTmp;

        inputField.lineType          = TMP_InputField.LineType.MultiLineNewline;
        inputField.richText          = false;
        inputField.scrollSensitivity = 20f;

        // ── ButtonBar ─────────────────────────────────────────
        var buttonBar = MakeRect("ButtonBar", panel.transform);
        SetLayoutHeight(buttonBar, 34);
        buttonBar.AddComponent<Image>().color = Hex("21252BF0");
        var hLayout2 = buttonBar.AddComponent<HorizontalLayoutGroup>();
        hLayout2.childControlHeight     = true;
        hLayout2.childControlWidth      = true;
        hLayout2.childForceExpandHeight = true;
        hLayout2.childForceExpandWidth  = false;
        hLayout2.spacing = 6;
        hLayout2.padding = new RectOffset(8, 8, 4, 4);

        var runBtn   = MakeButton("RunButton",   buttonBar.transform, "▶  Run  (Ctrl+Enter)", 12, Color.black, Hex("98C379FF"));
        SetLayoutWidth(runBtn, 170);
        var clearBtn = MakeButton("ClearButton", buttonBar.transform, "Clear Log", 12, Color.white, Hex("E06C75FF"));
        SetLayoutWidth(clearBtn, 90);
        MakeRect("Spacer", buttonBar.transform).AddComponent<LayoutElement>().flexibleWidth = 1;
        var hintText = MakeText("HintText", buttonBar.transform, "Ctrl+Enter", 10, Hex("5C6370FF"));
        hintText.alignment = TextAlignmentOptions.Right;
        SetLayoutWidth(hintText.gameObject, 80);

        // ── LogWrapper ────────────────────────────────────────
        var logWrapper = MakeRect("LogWrapper", panel.transform);
        var logWrapperEl = logWrapper.AddComponent<LayoutElement>();
        logWrapperEl.preferredHeight = 190;
        logWrapperEl.flexibleHeight  = 1;
        logWrapper.AddComponent<Image>().color = Hex("1A1D23FF");

        var scrollRect = logWrapper.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;

        var viewport = MakeRect("Viewport", logWrapper.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = new Vector2(6, 4);
        vpRect.offsetMax = new Vector2(-6, -4);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRect;

        var content = MakeRect("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0, 1);
        contentRect.sizeDelta = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vl2 = content.AddComponent<VerticalLayoutGroup>();
        vl2.childControlWidth      = true;
        vl2.childControlHeight     = true;
        vl2.childForceExpandWidth  = true;
        vl2.childForceExpandHeight = false;
        scrollRect.content = contentRect;

        var logTextGo = MakeRect("LogText", content.transform);
        var logTmp = logTextGo.AddComponent<TextMeshProUGUI>();
        logTmp.fontSize           = 12;
        logTmp.color              = Hex("ABB2BFFF");
        logTmp.richText           = true;
        logTmp.enableWordWrapping = false;
        logTmp.overflowMode       = TextOverflowModes.Overflow;

        // ── SpellEditorUI 참조 연결 ───────────────────────────
        var so = new SerializedObject(editorUI);
        so.FindProperty("codeInput").objectReferenceValue   = inputField;
        so.FindProperty("logText").objectReferenceValue     = logTmp;
        so.FindProperty("logScroll").objectReferenceValue   = scrollRect;
        so.FindProperty("manaLabel").objectReferenceValue   = manaLabel;
        so.FindProperty("casterLabel").objectReferenceValue = casterLabel;
        so.FindProperty("targetLabel").objectReferenceValue = targetLabel;
        so.FindProperty("runButton").objectReferenceValue   = runBtn.GetComponent<Button>();
        so.FindProperty("clearButton").objectReferenceValue = clearBtn.GetComponent<Button>();
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Prefab 저장 ───────────────────────────────────────
        System.IO.Directory.CreateDirectory("Assets/03. Datas/Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabSavePath, InteractionMode.AutomatedAction);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        AssetDatabase.Refresh();

        Debug.Log($"[SpellEditorUIBuilder] 프리펩 저장 완료: {PrefabSavePath}");
    }

    // ─── 헬퍼 ────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color)
    {
        var go  = MakeRect(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    private static GameObject MakeButton(string name, Transform parent, string label, float fontSize, Color textColor, Color bgColor)
    {
        var go = MakeRect(name, parent);
        go.AddComponent<Image>().color = bgColor;
        go.AddComponent<Button>();

        var txt = MakeText("Label", go.transform, label, fontSize, textColor);
        StretchRect(txt.rectTransform);
        txt.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static void SetLayoutHeight(GameObject go, float h)
    {
        var el = go.AddComponent<LayoutElement>();
        el.minHeight       = h;
        el.preferredHeight = h;
    }

    private static void SetLayoutWidth(GameObject go, float w)
    {
        var el = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        el.minWidth       = w;
        el.preferredWidth = w;
    }

    private static void StretchRect(RectTransform rt, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}

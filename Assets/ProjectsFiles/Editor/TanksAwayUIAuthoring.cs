using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit editor authoring only. Never invoked by a player/build or on scene load.
public static class TanksAwayUIAuthoring
{
    public static readonly Color Cream = new Color(1f, .945f, .82f);
    public static readonly Color Orange = new Color(1f, .52f, .1f);
    public static readonly Color Ink = new Color(.045f, .065f, .055f, .42f);
    public static readonly Color Blue = new Color(.38f, .66f, .89f);
    public static readonly Color Red = new Color(.94f, .39f, .31f);
    const string UI = "Assets/ProjectsFiles/Prefabes/UI/";
    const string MaterialPath = "Assets/ProjectsFiles/Models/UI/TanksAwayText.mat";
    static Material textMaterial;

    static void Prepare()
    {
        if (Application.isPlaying) throw new InvalidOperationException("UI authoring requires Edit Mode.");
        textMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (textMaterial == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UnityTechnologies/ParticlePack/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            textMaterial = new Material(font.material) { name = "Tanks Away Cream Shadow" };
            textMaterial.EnableKeyword("UNDERLAY_ON");
            textMaterial.SetColor("_UnderlayColor", new Color(.025f, .03f, .02f, .8f));
            textMaterial.SetFloat("_UnderlayOffsetX", .3f);
            textMaterial.SetFloat("_UnderlayOffsetY", -.4f);
            textMaterial.SetFloat("_UnderlayDilate", .05f);
            textMaterial.SetFloat("_UnderlaySoftness", .15f);
            AssetDatabase.CreateAsset(textMaterial, MaterialPath);
        }
    }
    static void Text(TMP_Text t)
    {
        t.color = Cream;
        t.fontSharedMaterial = textMaterial;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        t.extraPadding = true;
        t.characterSpacing = 0;
        // Preserve world-space text size and all gameplay-driven values.
        if (t.fontSize >= 12) { t.enableAutoSizing = true; t.fontSizeMin = Mathf.Min(t.fontSize, Mathf.Max(14, t.fontSize * .7f)); t.fontSizeMax = t.fontSize; }
        EditorUtility.SetDirty(t);
    }
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static void SetColor(Component c, string field, Color value)
    {
        var so = new SerializedObject(c); var p = so.FindProperty(field);
        if (p != null) { p.colorValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
    static void Rect(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
    }
    static void Off(Transform root, params string[] names)
    { foreach (var name in names) { var t = root.Find(name); if (t != null) t.gameObject.SetActive(false); } }
    static void Button(Button b, float fontSize = 0)
    {
        var label = b.GetComponentInChildren<TMP_Text>(true); if (label == null) return;
        var image = b.GetComponent<Image>();
        if (image != null) { image.sprite = null; image.color = Color.white; b.targetGraphic = image; }
        var colors = b.colors;
        colors.normalColor = new Color(.08f, .1f, .075f, 0f);
        colors.highlightedColor = new Color(1f, .52f, .1f, .035f);
        colors.selectedColor = new Color(1f, .52f, .1f, .025f);
        colors.pressedColor = new Color(1f, .52f, .1f, .065f);
        colors.disabledColor = new Color(.08f, .1f, .075f, .02f);
        colors.colorMultiplier = 1; colors.fadeDuration = .06f; b.colors = colors;
        b.transition = Selectable.Transition.ColorTint;
        Text(label); if (fontSize > 0) { label.fontSize = label.fontSizeMax = fontSize; label.fontSizeMin = fontSize * .75f; }
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(42, 2); lr.offsetMax = new Vector2(-12, -2);
        var arrow = b.transform.Find("SelectionTriangle");
        if (arrow == null) arrow = new GameObject("SelectionTriangle", typeof(RectTransform), typeof(UISelectionTriangle)).transform;
        arrow.SetParent(b.transform, false);
        if(arrow.GetComponent<CanvasRenderer>()==null)arrow.gameObject.AddComponent<CanvasRenderer>();
        var ar = (RectTransform)arrow; ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(0, .5f);
        ar.anchoredPosition = new Vector2(10, 0); ar.sizeDelta = new Vector2(fontSize >= 50 ? 26 : 15, fontSize >= 50 ? 34 : 20);
        var graphic = arrow.GetComponent<UISelectionTriangle>(); graphic.color = Orange; graphic.raycastTarget = false;
        arrow.gameObject.SetActive(false);
        var feedback = b.GetComponent<UISelectionFeedback>() ?? b.gameObject.AddComponent<UISelectionFeedback>();
        var so = new SerializedObject(feedback); so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("indicator").objectReferenceValue = arrow.gameObject; so.ApplyModifiedPropertiesWithoutUndo();
        b.navigation = new Navigation { mode = Navigation.Mode.Automatic };
    }
    static void Skin(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) Text(t);
        foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            if (scaler.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace)
            { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f; }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string path = Path(image.transform).ToLowerInvariant(), n = image.name.ToLowerInvariant();
            if (image.GetComponent<Selectable>() != null) continue;
            if (n == "reloadtrack") { image.color = new Color(Cream.r,Cream.g,Cream.b,.18f); continue; }
            if (n.Contains("crosshair") || n.Contains("reload") || n.Contains("hitmarker") || n.Contains("damagearrow")) { image.color = Cream; continue; }
            if (n.Contains("fill") || n.Contains("healthbar")) { image.color = new Color(.73f,.8f,.4f); continue; }
            if (n.Contains("accent") || n.Contains("localmarker") || n.Contains("checkmark") || n == "arrow") { image.color = Orange; continue; }
            if (n.Contains("warning") || path.Contains("lowhealth") || n.Contains("killmarker")) { image.color = Red; continue; }
            if (image.sprite != null && image.sprite.name != "UISprite" && image.sprite.name != "Background" && image.sprite.name != "InputFieldBackground") continue;
            if (n == "results") image.color = new Color(.02f,.035f,.025f,.67f);
            else if (n == "teamheader") image.color = path.Contains("redtable") ? new Color(Red.r,Red.g,Red.b,.25f) : new Color(Blue.r,Blue.g,Blue.b,.25f);
            else if (n.Contains("table") || n.Contains("columnheaders")) image.color = Color.clear;
            else if (n.Contains("background") || n.Contains("panel") || n.Contains("card") || n.Contains("notice") || n=="playerhealth" || n=="specialabilityunavailable") image.color = new Color(Ink.r,Ink.g,Ink.b,.30f);
            else if(n.Contains("roomitem")||n=="killfeedrow"||n=="teammaterow") image.color=new Color(Ink.r,Ink.g,Ink.b,.22f);
            else if (n.Contains("track")) image.color = new Color(.04f,.06f,.035f,.45f);
            else if (n=="abilityicon") image.color = new Color(Cream.r,Cream.g,Cream.b,.4f);
            else if (n.Contains("handle")) image.color = Orange;
        }
        foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            input.caretColor = Orange; input.customCaretColor = true; input.selectionColor = new Color(1f,.52f,.1f,.3f);
            var image = input.GetComponent<Image>(); if (image != null) { image.sprite = null; image.color = new Color(.035f,.05f,.03f,.22f); }
            var colors = input.colors; colors.normalColor = Color.white; colors.selectedColor = new Color(1,.85f,.65f); colors.highlightedColor = Color.white; input.colors = colors;
        }
        foreach (var b in root.GetComponentsInChildren<Button>(true)) if(b.name!="SpecialAbilityUnavailable") Button(b);
        foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (c is GameplayHud) { SetColor(c,"normalTime",Cream); SetColor(c,"lowTime",Red); SetColor(c,"overtimeTime",Orange); }
            if (c is KillFeedRow) { SetColor(c,"blue",Blue); SetColor(c,"red",Red); }
            if (c is FloatingDamageNumber) SetColor(c,"color",Cream);
        }
    }
    public static string Prefabs()
    {
        Prepare(); int count = 0;
        var paths = AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ProjectsFiles/Prefabes"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset.GetComponentsInChildren<TMP_Text>(true).Length == 0) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Skin(root);
                if(path.EndsWith("/Player.prefab"))
                {
                    var special=root.transform.Find("PlayerCanvas/SpecialAbilityUnavailable");
                    var feedback=special.GetComponent<UISelectionFeedback>();if(feedback!=null)UnityEngine.Object.DestroyImmediate(feedback);
                    var arrow=special.Find("SelectionTriangle");if(arrow!=null)UnityEngine.Object.DestroyImmediate(arrow.gameObject);
                    Rect(special.Find("AbilityLabel"),12,-8,126,22);
                    special.Find("AbilityLabel").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
                }
                if (path.EndsWith("LoadingCanvas.prefab"))
                {
                    root.transform.Find("Background").GetComponent<Image>().color = new Color(.025f,.04f,.025f,.94f);
                    var art = root.transform.Find("Background/LoadingArtwork"); if (art != null) art.gameObject.SetActive(false);
                    var status=root.transform.Find("Background/Status");var sr=(RectTransform)status;sr.anchorMin=new Vector2(.08f,.4f);sr.anchorMax=new Vector2(.92f,.6f);sr.pivot=new Vector2(.5f,.5f);sr.offsetMin=sr.offsetMax=Vector2.zero;
                    var t=status.GetComponent<TMP_Text>(); t.fontSize=t.fontSizeMax=42; t.fontSizeMin=28;t.alignment=TextAlignmentOptions.Center;
                    var back=root.transform.Find("Background/ReturnToMenu");var br=(RectTransform)back;br.anchorMin=br.anchorMax=new Vector2(.5f,0);br.pivot=new Vector2(.5f,0);br.anchoredPosition=new Vector2(0,110);br.sizeDelta=new Vector2(400,70);
                }
                if (path.EndsWith("MatchCanvas.prefab"))
                {
                    foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
                        if(t.name=="TeamName") t.color=Path(t.transform).Contains("RedTable")?Red:Blue;
                }
                if (path.EndsWith("ResultRow.prefab"))
                {
                    foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)) {t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;}
                }
                if (path.EndsWith("MainMenuRoomItem.prefab") || path.EndsWith("RoomItem.prefab"))
                {
                    foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
                        if(t.name=="RoomNameText") {t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;}
                }
                PrefabUtility.SaveAsPrefabAsset(root,path); count++;
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return "Styled "+count+" authored UI prefabs";
    }
    public static string MainMenu()
    {
        Prepare();
        if (EditorSceneManager.GetActiveScene().name != "MainMenuScene") throw new InvalidOperationException("Open MainMenuScene first.");
        var canvas=GameObject.Find("Canvas"); Skin(canvas);
        var brand=canvas.transform.Find("MenuBrand"); Rect(brand,100,-70,720,310);
        Off(brand,"BlueAccent","BrandOverline","GameTagline"); Off(canvas.transform,"MenuFooter","MenuBackgroundShade");
        var title=brand.Find("GameTitle").GetComponent<TMP_Text>();Rect(title.transform,0,0,710,300);title.text="TANKS\nAWAY!";title.fontStyle=FontStyles.Bold|FontStyles.Italic;title.fontSize=title.fontSizeMax=148;title.fontSizeMin=115;title.lineSpacing=-22;title.characterSpacing=-4;title.alignment=TextAlignmentOptions.TopLeft;
        const string titleMaterialPath="Assets/ProjectsFiles/Models/UI/TanksAwayTitle.mat";
        var titleMaterial=AssetDatabase.LoadAssetAtPath<Material>(titleMaterialPath);
        if(titleMaterial==null){titleMaterial=new Material(textMaterial);titleMaterial.name="Tanks Away Bold Title";titleMaterial.SetFloat("_FaceDilate",.16f);AssetDatabase.CreateAsset(titleMaterial,titleMaterialPath);}
        title.fontSharedMaterial=titleMaterial;
        var dock=canvas.transform.Find("MenuDock");Rect(dock,0,0,820,1080);
        const string scrimPath="Assets/ProjectsFiles/Models/UI/MainMenu/ReadabilityGradient.png";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(scrimPath)==null)
        {
            var texture=new Texture2D(256,4,TextureFormat.RGBA32,false);
            for(int x=0;x<256;x++)for(int y=0;y<4;y++)texture.SetPixel(x,y,new Color(.015f,.025f,.015f,.6f*Mathf.Pow(Mathf.Clamp01(1-x/160f),.65f)));
            texture.Apply();System.IO.File.WriteAllBytes(scrimPath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(scrimPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(scrimPath);importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        canvas.transform.Find("MenuLeftScrim").GetComponent<RawImage>().texture=AssetDatabase.LoadAssetAtPath<Texture2D>(scrimPath);
        foreach(var n in new[]{"LoginPanel","RoomBrowserPanel","WaitingRoomPanel"})
        {
            var p=dock.Find(n);p.GetComponent<Image>().color=Color.clear;
            Off(p,"TopRule","SectionKicker","Introduction","LoginFooter","FooterRule","BrowserFooter","ReadyHelp");
        }
        var login=dock.Find("LoginPanel");Off(login,"SectionHeading");
        var label=login.Find("CallsignLabel").GetComponent<TMP_Text>();label.text="NAME:";label.fontSize=label.fontSizeMax=30;Rect(label.transform,110,-390,120,50);
        var input=login.Find("PlayerNameInput").GetComponent<TMP_InputField>();Rect(input.transform,230,-388,430,56);input.GetComponent<Image>().color=Color.clear;
        foreach(var t in input.GetComponentsInChildren<TMP_Text>(true)){t.fontSize=t.fontSizeMax=30;t.fontSizeMin=23;}
        ((TMP_Text)input.placeholder).text="Your name";
        var area=input.transform.Find("Text Area") as RectTransform;area.offsetMin=new Vector2(4,4);area.offsetMax=new Vector2(-4,-4);
        var underline=login.Find("NameUnderline");if(underline==null)underline=new GameObject("NameUnderline",typeof(RectTransform),typeof(Image)).transform;
        underline.SetParent(login,false);Rect(underline,110,-448,500,3);underline.GetComponent<Image>().color=new Color(.75f,.8f,.37f,.85f);underline.GetComponent<Image>().raycastTarget=false;
        var play=login.Find("Button").GetComponent<Button>();Rect(play.transform,102,-510,590,90);Button(play,64);play.GetComponentInChildren<TMP_Text>().text="PLAY";
        int offset=1;
        foreach(var name in new[]{"Settings","Quit"})
        {
            var t=login.Find(name);if(t==null){t=UnityEngine.Object.Instantiate(play.gameObject,login).transform;t.name=name;}
            var b=t.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();Rect(t,102,-510-offset*116,590,90);Button(b,56);b.GetComponentInChildren<TMP_Text>().text=name.ToUpperInvariant();offset++;
        }
        Rect(login.Find("StatusText"),110,-895,650,115);
        var focus=login.GetComponent<MenuPanelFocus>();if(focus!=null){var so=new SerializedObject(focus);so.FindProperty("initialSelection").objectReferenceValue=play;so.ApplyModifiedPropertiesWithoutUndo();}
        UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>().firstSelectedGameObject=play.gameObject;
        var browser=dock.Find("RoomBrowserPanel");Rect(browser.Find("SectionHeading"),110,-380,650,65);browser.Find("SectionHeading").GetComponent<TMP_Text>().text="MULTIPLAYER";
        Off(browser,"CreateRoomSection","CreateRoomLabel");Rect(browser.Find("MaxPlayersDropdown"),110,-482,190,58);Rect(browser.Find("CreateRoomButton"),325,-482,380,58);
        var dropdown=browser.Find("MaxPlayersDropdown").GetComponent<TMP_Dropdown>();dropdown.GetComponent<Image>().sprite=null;dropdown.GetComponent<Image>().color=new Color(.04f,.06f,.035f,.42f);var dc=dropdown.colors;dc.normalColor=Color.white;dc.highlightedColor=new Color(1,.75f,.4f);dc.selectedColor=Orange;dropdown.colors=dc;
        Rect(browser.Find("OpenRoomsLabel"),110,-590,330,40);Rect(browser.Find("RefreshButton"),510,-580,200,58);Rect(browser.Find("Scroll View"),110,-650,600,210);
        var scrollBackground=browser.Find("Scroll View").GetComponent<Image>();scrollBackground.sprite=null;scrollBackground.color=new Color(.025f,.04f,.025f,.22f);
        var waiting=dock.Find("WaitingRoomPanel");Rect(waiting.Find("SectionHeading"),110,-380,650,65);waiting.Find("SectionHeading").GetComponent<TMP_Text>().text="YOUR SQUAD";
        Off(waiting,"RosterBackground");Rect(waiting.Find("RosterLabel"),110,-475,650,32);Rect(waiting.Find("PlayersListText"),110,-525,650,250);
        Rect(waiting.Find("ReadyButton"),100,-800,305,68);Rect(waiting.Find("StartGameButton"),420,-800,330,68);Rect(waiting.Find("BackButton"),100,-990,400,60);
        Rect(dock.Find("LobbyOperationStatusText (TMP)"),110,-890,640,86);
        // Keep the persistent scene instance's status at the source prefab position.
        var loading=GameObject.Find("LoadingCanvas");
        if(loading!=null){var status=loading.transform.Find("Background/Status");var props=PrefabUtility.GetPropertyModifications(loading);PrefabUtility.SetPropertyModifications(loading,props.Where(p=>!(p.target!=null&&p.target.name=="Status"&&p.propertyPath=="m_AnchoredPosition.y")).ToArray());}
        var tank=GameObject.Find("TankModel");var cam=Camera.main;
        // Stable relative to the existing display model; no mesh/material or environment edits.
        cam.transform.position=new Vector3(4.43f,-1.66f,-.10f);cam.transform.eulerAngles=new Vector3(8,289,0);
        cam.transform.position-=cam.transform.right*.55f;cam.transform.position-=cam.transform.forward*.20f;
        tank.transform.eulerAngles=new Vector3(0,90,0);
        tank.transform.localScale=Vector3.one*.135f;tank.transform.position=new Vector3(3.39f,-2.06f,-.14f)+cam.transform.right*.13f;
        if(cam.GetComponent<MenuCameraFraming>()==null)cam.gameObject.AddComponent<MenuCameraFraming>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "Authored Tanks Away main menu and existing browser/waiting screens";
    }
}

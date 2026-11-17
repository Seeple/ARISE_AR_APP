using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class KeyboardPrefabBuilder
{
    public const string PrefabPath="Assets/Resources/Prefab/Keyboard.prefab";
    public const string BackupPath="Assets/Backups/Keyboard/Keyboard-before-redesign-20261026.prefab";
    static readonly Color Ink=new Color(.82f,.92f,.98f,1);
    static readonly Color Cyan=new Color(.24f,.85f,.95f,1);
    static readonly Color Muted=new Color(.40f,.58f,.68f,1);
    static TMP_FontAsset font;

    [MenuItem("Tools/ARISE/Keyboard/Rebuild Console Prefab")]
    public static void Build()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(BackupPath)==null)throw new InvalidOperationException("Create the original keyboard backup before rebuilding.");
        font=ConsoleFont();
        GameObject root=PrefabUtility.LoadPrefabContents(PrefabPath);
        GameObject original=PrefabUtility.LoadPrefabContents(BackupPath);
        try
        {
            var keyboard=root.GetComponent<MyKeyboard>();
            var source=original.transform.Find("r");
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var surface=new GameObject("Console Surface").transform;surface.SetParent(root.transform,false);surface.localRotation=Quaternion.Euler(0,180,0);
            var template=source.Find("MenuPanel").gameObject;
            Panel(template,surface,"Host IP Panel",new Vector2(-.20f,.075f),new Vector2(.37f,.41f));
            Panel(template,surface,"Monitor Panel",new Vector2(-.20f,-.235f),new Vector2(.37f,.18f));
            Panel(template,surface,"Instruction Panel",new Vector2(.20f,-.0225f),new Vector2(.38f,.605f));

            var canvasGo=new GameObject("Console Text",typeof(RectTransform),typeof(Canvas));canvasGo.transform.SetParent(surface,false);
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvasGo.transform.localScale=Vector3.one*.001f;canvasGo.transform.localPosition=new Vector3(0,0,-.018f);
            canvasGo.GetComponent<RectTransform>().sizeDelta=new Vector2(800,660);
            var ct=canvasGo.transform;
            Text(ct,"Host IP Title","HOST IP",-365,252,310,25,20,Cyan,FontStyles.Bold);
            Text(ct,"IP Label","WORKSTATION ADDRESS",-365,216,310,18,10,Muted);
            keyboard.text=Text(ct,"Host IP Value","10.128.1.140",-357,187,310,28,20,Ink);
            keyboard.text.enableAutoSizing=true;keyboard.text.fontSizeMin=10;keyboard.text.fontSizeMax=20;keyboard.text.enableWordWrapping=false;
            Rule(ct,"Input Underline",-365,168,330,1.3f,Cyan);
            string[] numeric={"1","2","3","4","5","6","7","8","9",".","0",":"};
            for(int i=0;i<numeric.Length;i++)Button(source,surface,keyboard,numeric[i],-.33f+(i%3)*.055f,.105f-(i/3)*.06f,false);
            Button(source,surface,keyboard,"Backspace",-.115f,.105f,true);
            Button(source,surface,keyboard,"RefreshIp",-.115f,.045f,true);
            Button(source,surface,keyboard,"SwitchLRController",-.115f,-.015f,true);
            Text(ct,"IP Hint","Enter address, then tap RefreshIP.",-365,-110,330,15,10,Muted);

            Text(ct,"Monitor Title","MONITOR",-365,-161,310,24,17,Cyan,FontStyles.Bold);
            Rule(ct,"Monitor Divider",-365,-180,330,1,Cyan*.55f);
            keyboard.showText=Text(ct,"Monitor Status","",-365,-196,330,115,11.5f,Ink);
            keyboard.instructionText=Text(ct,"Instructions",MyKeyboard.ControlHelp,35,200,330,375,12,Ink);
            Text(ct,"Instruction Title","INSTRUCTION",35,252,320,25,20,Cyan,FontStyles.Bold);
            Rule(ct,"Instruction Divider",35,225,330,1,Cyan*.55f);
            Rule(ct,"Calibration Divider",35,-188,330,1,Cyan*.55f);
            keyboard.calibrationTitle=Text(ct,"Calibration Title","CALIBRATION YAW / 1 DEG STEP",35,-202,330,18,11,Muted);
            keyboard.calibrationYawText=Text(ct,"Calibration Yaw","0.0°",155,-248,100,32,24,Muted);
            keyboard.calibrationYawText.alignment=TextAlignmentOptions.Center;
            keyboard.yawIncreaseButton=YawButton(source,surface,keyboard,true,.09f,-.265f);
            keyboard.yawDecreaseButton=YawButton(source,surface,keyboard,false,.31f,-.265f);
            Text(ct,"Instruction Footer","LEFT STICK CLICK / SHOW · HIDE",35,-307,330,14,9,Muted);
            BuildToolTaskControls(source, surface, keyboard);
            // Populate a useful edit-time placeholder without simulating any tracking data.
            keyboard.showText.text="<color=#52D9EF>FINGERTIP / WORLD [m]</color>\nX --         Y --         Z --\nPoint  None     Hover  None\nMode  Direct     Editing  Idle\nEdit XYZ  --\nHost  Ready";
            keyboard.Client=null;keyboard.summonDistance=.45f;keyboard.summonDrop=.22f;
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            root.SetActive(false);
            ConfigureStableRendering(root);
            keyboard.RefreshCalibrationControls();
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(original);PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.ImportAsset(PrefabPath,ImportAssetOptions.ForceUpdate);
    }

    static TMP_FontAsset ConsoleFont()
    {
        // FindAssets also returns the dynamic fallback atlas. Pin the baked font explicitly.
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }

    public static void ConfigureStableRendering(GameObject root)
    {
        var bakedFont=ConsoleFont();
        if(bakedFont==null)throw new InvalidOperationException("The baked console font is missing.");
        // These shaders blend without writing depth. Distance sorting can put a large
        // background over its children as the eye moves, so specify every visual layer.
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingLayerID=0;
            renderer.sortingOrder=renderer.GetComponent<TMP_Text>()!=null?2:
                renderer.name=="ButtonPanel"?1:0;
        }
        foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            canvas.overrideSorting=true;
            canvas.sortingLayerID=0;
            canvas.sortingOrder=3;
        }
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font=bakedFont;
            text.fontSharedMaterial=bakedFont.material;
        }
    }

    [MenuItem("Tools/ARISE/Keyboard/Repair Console Rendering")]
    public static void RepairRendering()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureStableRendering(root);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.ImportAsset(PrefabPath,ImportAssetOptions.ForceUpdate);
    }

    static void Panel(GameObject template,Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=UnityEngine.Object.Instantiate(template,parent,false);go.name=name;go.SetActive(true);
        go.transform.localPosition=new Vector3(position.x,position.y,.005f);
        var rounded=go.GetComponent<RoundedBoxProperties>();
        rounded.Color=new Color(.018f,.037f,.061f,.98f);rounded.BorderColor=new Color(.13f,.48f,.61f,.80f);
        rounded.BorderInnerRadius=0;rounded.BorderOuterRadius=.0012f;
        rounded.RadiusTopLeft=rounded.RadiusTopRight=rounded.RadiusBottomLeft=rounded.RadiusBottomRight=.012f;
        rounded.Width=size.x;rounded.Height=size.y;
    }

    static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float width,float height,float size,Color color,FontStyles style=FontStyles.Normal)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(width,height);
        var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=font.material;text.text=value;text.fontSize=size;text.fontStyle=style;text.color=color;text.alignment=TextAlignmentOptions.TopLeft;text.raycastTarget=false;text.enableWordWrapping=true;text.overflowMode=TextOverflowModes.Overflow;
        return text;
    }

    static void Rule(Transform parent,string name,float x,float y,float width,float height,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(width,height);
        go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;
    }

    static GameObject Button(Transform original,Transform parent,MyKeyboard keyboard,string name,float x,float y,bool utility)
    {
        var go=UnityEngine.Object.Instantiate(original.Find(name).gameObject,parent,false);go.name=name;go.SetActive(true);
        go.transform.localPosition=new Vector3(x,y,0);go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one*.05f;
        var label=go.GetComponentInChildren<TMP_Text>(true);label.font=font;label.fontSharedMaterial=font.material;label.text=name=="SwitchLRController"?"SwitchL-R":name=="RefreshIp"?"RefreshIP":name;label.fontSize=utility?30:36;label.color=Ink;
        // Keep glyphs in front of the solid key face, including while the face moves on poke.
        label.transform.localPosition+=Vector3.back*.025f;
        label.GetComponent<Renderer>().sortingOrder=2;
        foreach(var rounded in go.GetComponentsInChildren<RoundedBoxProperties>(true))
        {
            rounded.Color=new Color(.045f,.11f,.17f,1);rounded.BorderColor=new Color(.15f,.40f,.52f,1);rounded.BorderOuterRadius=.018f;rounded.BorderInnerRadius=0;
            // Width assignment immediately updates the edit-time material property block.
            rounded.Width=rounded.Width;
        }
        foreach(var visual in go.GetComponentsInChildren<InteractableColorVisual>(true))
        {
            var so=new SerializedObject(visual);
            so.FindProperty("_normalColorState.Color").colorValue=new Color(.045f,.11f,.17f,1);
            so.FindProperty("_hoverColorState.Color").colorValue=new Color(.06f,.30f,.40f,1);
            so.FindProperty("_selectColorState.Color").colorValue=new Color(.07f,.65f,.76f,1);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Keep the original poke surface and visual wiring, but bind every key to the new root.
        var wrapper=go.GetComponent<PointableUnityEventWrapper>();
        var ws=new SerializedObject(wrapper);
        foreach(string evt in new[]{"_whenSelect","_whenRelease","_whenUnselect","_whenCancel"})
            ws.FindProperty(evt+".m_PersistentCalls.m_Calls").ClearArray();
        ws.ApplyModifiedPropertiesWithoutUndo();
        var calls=new SerializedObject(wrapper);var array=calls.FindProperty("_whenRelease.m_PersistentCalls.m_Calls");array.arraySize=1;
        var entry=array.GetArrayElementAtIndex(0);entry.FindPropertyRelative("m_Target").objectReferenceValue=keyboard;
        entry.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue=typeof(MyKeyboard).AssemblyQualifiedName;
        entry.FindPropertyRelative("m_MethodName").stringValue=name=="Backspace"?"Remove":name=="RefreshIp"?"RefreshIP":name=="SwitchLRController"?"SwitchLRController":"Add";
        entry.FindPropertyRelative("m_Mode").enumValueIndex=utility?1:5;
        entry.FindPropertyRelative("m_CallState").enumValueIndex=2;
        entry.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue=name;
        calls.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    static GameObject YawButton(Transform source,Transform parent,MyKeyboard keyboard,bool increase,float x,float y)
    {
        string name=increase?"Yaw Increase":"Yaw Decrease";
        // Reuse the proven numeric-key poke geometry and bind release to the keyboard root.
        var go=Button(source,parent,keyboard,"1",x,y,false);
        go.name=name;
        var label=go.GetComponentInChildren<TMP_Text>(true);
        label.text=increase?"+":"-";
        var wrapper=go.GetComponent<PointableUnityEventWrapper>();
        var so=new SerializedObject(wrapper);
        var call=so.FindProperty("_whenRelease.m_PersistentCalls.m_Calls").GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_MethodName").stringValue=increase?"IncreaseCalibrationYaw":"DecreaseCalibrationYaw";
        call.FindPropertyRelative("m_Mode").enumValueIndex=1;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // Install the task selector in an existing prefab without regenerating its other keys.
    public static void UpdateToolTaskControls()
    {
        font = ConsoleFont();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var original = PrefabUtility.LoadPrefabContents(BackupPath);
        try
        {
            BuildToolTaskControls(original.transform.Find("r"), root.transform.Find("Console Surface"),
                root.GetComponent<MyKeyboard>());
            ConfigureStableRendering(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(original);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BuildToolTaskControls(Transform source, Transform surface, MyKeyboard keyboard)
    {
        if (keyboard.defaultTaskButton != null) return;
        var canvas = surface.Find("Console Text");
        var statusRect = keyboard.showText.rectTransform;
        statusRect.sizeDelta = new Vector2(214f, statusRect.sizeDelta.y);
        Text(canvas, "Tool Task Title", "TOOL TASK", -145, -193, 111, 18, 12, Cyan, FontStyles.Bold);
        keyboard.defaultTaskButton = TaskButton(source, surface, keyboard, "Default", -.119f,
            nameof(MyKeyboard.SelectDefaultTask));
        keyboard.knotTaskButton = TaskButton(source, surface, keyboard, "Knot", -.064f,
            nameof(MyKeyboard.SelectKnotTask));
        keyboard.toolStatusText = Text(canvas, "Tool Status", "Off", -145, -279, 115, 32, 11, Ink);
        keyboard.RefreshToolControls();
    }

    static GameObject TaskButton(Transform source, Transform parent, MyKeyboard keyboard,
        string label, float x, string method)
    {
        var button = Button(source, parent, keyboard, "1", x, -.239f, false);
        button.name = label + " Task";
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.fontSize = 23f;
        var wrapper = new SerializedObject(button.GetComponent<PointableUnityEventWrapper>());
        var call = wrapper.FindProperty("_whenRelease.m_PersistentCalls.m_Calls").GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_MethodName").stringValue = method;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 1;
        wrapper.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    public static void ApplyToCurrentScene()
    {
        var vr=UnityEngine.Object.FindObjectOfType<VRController>();
        if(vr==null||vr.keyboard==null)throw new InvalidOperationException("The main controller and keyboard must be present.");
        var root=vr.keyboard.gameObject;
        PrefabUtility.RevertPrefabInstance(root,InteractionMode.AutomatedAction);
        var k=root.GetComponent<MyKeyboard>();k.Client=vr;k.InitializeHostIP(vr.ip);root.SetActive(false);
        vr.keyboard=k;vr.showText=k.showText;
        EditorUtility.SetDirty(k);EditorUtility.SetDirty(vr);
        var scene=root.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/ARISE/Keyboard/Render Console Preview")]
    public static string RenderPreview()
    {
        var scene=EditorSceneManager.NewPreviewScene();Camera camera=null;RenderTexture target=null;Texture2D image=null;var previous=RenderTexture.active;
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);root.SetActive(true);
            root.GetComponent<MyKeyboard>().RefreshCalibrationControls();
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate(true,true);
            // Use the existing isolated root for the camera to avoid creating anything in the main scene.
            var view=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(view,scene);camera=view.AddComponent<Camera>();camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.015f,.024f);camera.orthographic=true;camera.orthographicSize=.35f;
            camera.transform.position=new Vector3(0,-.0225f,1);camera.transform.LookAt(new Vector3(0,-.0225f,0));camera.nearClipPlane=.01f;camera.farClipPlane=3;
            var light=view.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;
            target=new RenderTexture(1800,1450,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;target.Create();camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image=new Texture2D(1800,1450,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,1450),0,0);image.Apply();
            Directory.CreateDirectory("Docs/Images");string path="Docs/Images/Keyboard-Console.png";File.WriteAllBytes(path,image.EncodeToPNG());return path;
        }
        finally {RenderTexture.active=previous;if(camera!=null)camera.targetTexture=null;if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}EditorSceneManager.ClosePreviewScene(scene);}
    }
}

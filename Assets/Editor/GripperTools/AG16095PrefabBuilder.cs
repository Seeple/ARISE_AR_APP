using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Deterministic, texture-free source geometry for the Quest gripper visual.</summary>
public static class AG16095PrefabBuilder
{
    public const string AssetFolder = "Assets/Resources/GripperModels/AG16095";
    public const string PrefabPath = "Assets/Resources/Prefab/AG16095SoftGripper.prefab";
    static readonly Color Housing = new Color(.12f, .13f, .14f, .18f);
    static readonly Color Panel = new Color(.16f, .18f, .20f, .22f);
    static readonly Color Edge = new Color(.24f, .26f, .28f, .38f);
    static readonly Color Steel = new Color(.56f, .61f, .65f, .9f);
    static readonly Color BrightSteel = new Color(.76f, .80f, .82f, .95f);
    static readonly Color Recess = new Color(.018f, .024f, .029f, .06f);
    static readonly Color Silicone = new Color(1f, .205f, .035f, .04f);
    static readonly Color SiliconeRib = new Color(1f, .30f, .065f, .03f);
    static readonly Color Contact = new Color(.10f, .095f, .083f, .015f);

    [MenuItem("Tools/ARISE/Gripper/Rebuild AG-160-95 Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(AssetFolder);
        AssetDatabase.Refresh();
        Shader shader = Shader.Find("ARISE/Gripper Vertex Color");
        if (shader == null) throw new InvalidOperationException("Gripper shader has not finished importing.");
        string materialPath = AssetFolder + "/AG16095Surface.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        material.shader = shader;
        material.color = Color.white;
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.SetFloat("_GhostEffect", 0f);
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);

        Mesh body = SaveMesh(BuildHousing(), "Housing");
        Mesh rightJaw = SaveMesh(BuildJaw(), "RightSoftFinger");
        MeshDraft leftDraft = BuildJaw(); leftDraft.MirrorX();
        Mesh leftJaw = SaveMesh(leftDraft, "LeftSoftFinger");
        Mesh link = SaveMesh(BuildLink(), "ParallelLinkPair");
        var root = new GameObject("AG16095SoftGripper");
        try
        {
            var geometry = new GameObject("Geometry_TCP_200mm_below_flange").transform;
            geometry.SetParent(root.transform, false);
            geometry.localPosition = Vector3.up * AG16095GripperVisual.GeometryOffsetY;
            var flangeDatum = new GameObject("FlangeMountDatum").transform;
            flangeDatum.SetParent(geometry, false);
            flangeDatum.localPosition = Vector3.up * AG16095GripperVisual.SourceFlangeMountY;
            MeshObject(geometry, "AG16095_Housing", body, material);
            Transform left = MeshObject(geometry, "Left_SoftFinger_91_6mm", leftJaw, material).transform;
            Transform right = MeshObject(geometry, "Right_SoftFinger_91_6mm", rightJaw, material).transform;
            var controller = root.AddComponent<GripperJawController>();
            controller.jawLeft = left; controller.jawRight = right;
            controller.robotClosedWidth = 0f; controller.robotOpenWidth = AG16095GripperVisual.MaximumOpening;
            controller.leftClosedLocalX = 0f; controller.leftOpenLocalX = -.0475f;
            controller.rightClosedLocalX = 0f; controller.rightOpenLocalX = .0475f;
            controller.jawWidth = .085f;
            var visual = root.AddComponent<AG16095GripperVisual>();
            visual.widthController = controller;
            visual.leftInnerLink = MeshObject(geometry, "Left_Inner_ParallelLinks", link, material).transform;
            visual.leftOuterLink = MeshObject(geometry, "Left_Outer_ParallelLinks", link, material).transform;
            visual.rightInnerLink = MeshObject(geometry, "Right_Inner_ParallelLinks", link, material).transform;
            visual.rightOuterLink = MeshObject(geometry, "Right_Outer_ParallelLinks", link, material).transform;
            visual.ApplyWidthNow();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssetIfDirty(material);
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("[ARISE Gripper] Rebuilt AG16095SoftGripper with metre geometry and 0–95 mm aperture.");
    }

    static Mesh SaveMesh(MeshDraft draft, string name)
    {
        string path = AssetFolder + "/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Mesh source = draft.ToMesh(name);
        if (mesh == null) AssetDatabase.CreateAsset(source, path);
        else
        {
            // Update through Mesh APIs so both saved data and the Editor GPU buffers change.
            mesh.Clear(); mesh.indexFormat=source.indexFormat;
            mesh.vertices=source.vertices; mesh.normals=source.normals;
            mesh.colors=source.colors; mesh.triangles=source.triangles;
            mesh.RecalculateBounds(); mesh.UploadMeshData(false);
            UnityEngine.Object.DestroyImmediate(source);
            EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
        }
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go;
    }

    static MeshDraft BuildHousing()
    {
        var d = new MeshDraft();
        // Hourglass housing and nested front/back covers, following the supplied photographs.
        Vector2[] outline = {
            new Vector2(-.0335f,.196f), new Vector2(-.038f,.188f),
            new Vector2(-.038f,.163f), new Vector2(-.034f,.151f),
            new Vector2(-.025f,.137f), new Vector2(-.023f,.112f),
            new Vector2(-.015f,.097f), new Vector2(0f,.090f),
            new Vector2(.015f,.097f), new Vector2(.023f,.112f),
            new Vector2(.025f,.137f), new Vector2(.034f,.151f),
            new Vector2(.038f,.163f), new Vector2(.038f,.188f), new Vector2(.0335f,.196f)
        };
        d.Extrude(outline, .067f, Vector3.zero, Housing, .002f);
        Vector2[] panel = {
            new Vector2(-.029f,.188f), new Vector2(-.031f,.178f),
            new Vector2(-.029f,.158f), new Vector2(-.021f,.143f),
            new Vector2(-.018f,.115f), new Vector2(0f,.101f),
            new Vector2(.018f,.115f), new Vector2(.021f,.143f),
            new Vector2(.029f,.158f), new Vector2(.031f,.178f), new Vector2(.029f,.188f)
        };
        d.Extrude(panel, .0682f, Vector3.zero, Panel, .001f);
        // Machined shoulder support and cylindrical spindle showing in the neck.
        d.Box(new Vector3(0,.101f,0), new Vector3(.113f,.009f,.046f), Steel, .0013f);
        d.Box(new Vector3(0,.086f,0), new Vector3(.032f,.030f,.026f), Housing, .0018f);
        d.Box(new Vector3(0,.119f,-.0348f),new Vector3(.019f,.029f,.0015f), Recess, .001f);
        d.Box(new Vector3(0,.116f,-.036f),new Vector3(.011f,.022f,.0016f), Steel, .0007f);
        foreach (float z in new[]{-.035f,.035f})
        {
            foreach(float x in new[]{-.028f,.028f}) Bolt(d,new Vector3(x,.183f,z),.0026f);
            // Fine split seam and inset grooves on the case.
            d.Box(new Vector3(-.0205f,.156f,z),new Vector3(.0008f,.047f,.0006f),Recess,.0001f);
            d.Box(new Vector3(.0205f,.156f,z),new Vector3(.0008f,.047f,.0006f),Recess,.0001f);
            for(int k=0;k<4;k++) d.Box(new Vector3(.027f,.172f-k*.0038f,z),new Vector3(.005f,.00065f,.0005f),Edge,.0001f);
        }
        // DH-inspired geometric name mark, without a texture/font dependency.
        LetterD(d,new Vector3(-.008f,.163f,-.0352f),.010f);
        LetterH(d,new Vector3(.005f,.163f,-.0352f),.010f);
        d.Box(new Vector3(0,.153f,-.0353f),new Vector3(.020f,.0007f,.0004f),Steel,.0001f);
        // Wrist flange: machined aluminium ring and recessed socket.
        var flange=new MeshDraft();
        flange.CylinderZ(Vector3.zero,.026f,.026f,.012f,Steel,32);
        flange.CylinderZ(new Vector3(0,0,-.0065f),.020f,.020f,.0015f,Edge,24);
        flange.CylinderZ(new Vector3(0,0,-.0075f),.011f,.011f,.0015f,Recess,24);
        for(int i=0;i<4;i++)
        {
            float a=(45+i*90)*Mathf.Deg2Rad;
            Bolt(flange,new Vector3(Mathf.Cos(a)*.020f,Mathf.Sin(a)*.020f,-.0075f),.0025f);
        }
        d.Append(flange,new Vector3(0,.202f,0),Quaternion.Euler(90,0,0),Vector3.one);
        // Side power connector and short elbow cable visible in the user's side photograph.
        var plug=new MeshDraft();
        plug.CylinderZ(Vector3.zero,.007f,.007f,.013f,Steel,16);
        plug.CylinderZ(new Vector3(0,0,-.008f),.0065f,.0065f,.010f,Recess,16);
        d.Append(plug,new Vector3(-.042f,.170f,0),Quaternion.Euler(0,90,0),Vector3.one);
        d.Tube(new[]{new Vector3(-.055f,.170f,0),new Vector3(-.062f,.170f,0),new Vector3(-.066f,.178f,0),new Vector3(-.067f,.195f,0),new Vector3(-.061f,.213f,0)},.0032f,Recess,10);
        return d;
    }

    static MeshDraft BuildJaw()
    {
        var d=new MeshDraft();
        // Black replaceable-finger holder; the upper 8 mm of silicone is inside it.
        d.Box(new Vector3(.017f,.012f,0),new Vector3(.034f,.026f,.047f),Housing,.0024f);
        d.Box(new Vector3(.017f,.026f,0),new Vector3(.032f,.004f,.043f),Edge,.0008f);
        foreach(float z in new[]{-.024f,.024f})
        {
            Bolt(d,new Vector3(.009f,.012f,z),.0025f);
            Bolt(d,new Vector3(.028f,.012f,z),.0025f);
        }
        float top=AG16095GripperVisual.EmbeddedFingerLength, bottom=top-AG16095GripperVisual.FingerLength;
        const float skin=.00135f;
        // Hollow truncated triangular wedge: full dimensional envelope is 91.6 x 39.55 x 27 mm.
        // Continuous contact/outer skins and discrete webs leave real openings on both sides.
        d.Wedge(top,0f,0,skin,0,skin,.019775f,DepthAt(0f),Silicone);
        d.Wedge(0f,bottom,.0006f,skin,.0006f,skin,DepthAt(0f),.015775f,Silicone);
        d.Wedge(top,bottom,.027f-skin,.027f,.0024f-skin,.0024f,.019775f,.015775f,Silicone);
        for(int i=0;i<=8;i++)
        {
            float y=top-i*(AG16095GripperVisual.FingerLength/8f);
            float a=Mathf.Min(top,y+.0009f),b=Mathf.Max(bottom,y-.0009f);
            if(ThicknessAt(b)>skin*2f)
                d.Wedge(a,b,skin,ThicknessAt(a)-skin,skin,ThicknessAt(b)-skin,DepthAt(a),DepthAt(b),SiliconeRib);
        }
        // Dark, thin contact facing as seen between the two orange fingers.
        d.Wedge(0,bottom,0f,.0008f,0f,.0008f,.015f,.0125f,Contact);
        // Small terminal pad reinforces the truncated tip without changing the nominal aperture plane.
        d.Wedge(bottom+.002f,bottom,0,ThicknessAt(bottom+.002f),0,ThicknessAt(bottom),DepthAt(bottom+.002f),DepthAt(bottom),Contact);
        return d;
    }

    static float ThicknessAt(float y)
    {
        float t=Mathf.Clamp01((AG16095GripperVisual.EmbeddedFingerLength-y)/AG16095GripperVisual.FingerLength);
        return Mathf.Lerp(.027f,.0024f,t);
    }
    static float DepthAt(float y)
    {
        float t=Mathf.Clamp01((AG16095GripperVisual.EmbeddedFingerLength-y)/AG16095GripperVisual.FingerLength);
        return Mathf.Lerp(.019775f,.015775f,t);
    }

    static MeshDraft BuildLink()
    {
        var d=new MeshDraft();
        // Unit-length linkage; the runtime scales only its longitudinal axis to 64 mm.
        foreach(float z in new[]{-.0275f,.0275f})
        {
            const int n=8; var profile=new List<Vector2>();
            for(int i=0;i<=n;i++){float a=(180+i*180f/n)*Mathf.Deg2Rad;profile.Add(new Vector2(Mathf.Cos(a)*.006f,Mathf.Sin(a)*.075f));}
            for(int i=0;i<=n;i++){float a=(i*180f/n)*Mathf.Deg2Rad;profile.Add(new Vector2(Mathf.Cos(a)*.006f,1f+Mathf.Sin(a)*.075f));}
            d.Extrude(profile.ToArray(),.0045f,new Vector3(0,0,z),Housing,0f);
            foreach(float y in new[]{0f,1f})
            {
                d.CylinderZ(new Vector3(0,y,z+Mathf.Sign(z)*.0034f),.0046f,.071f,.0024f,Steel,12);
                d.CylinderZ(new Vector3(0,y,z+Mathf.Sign(z)*.0048f),.0032f,.050f,.0013f,BrightSteel,6);
                d.CylinderZ(new Vector3(0,y,z+Mathf.Sign(z)*.0056f),.00155f,.024f,.0007f,Recess,6);
            }
        }
        return d;
    }

    static void Bolt(MeshDraft d,Vector3 at,float radius)
    {
        float sign=at.z<0?-1:1;
        d.CylinderZ(at,radius,radius,.0015f,Steel,12);
        d.CylinderZ(at+new Vector3(0,0,sign*.001f),radius*.76f,radius*.76f,.0012f,BrightSteel,6);
        d.CylinderZ(at+new Vector3(0,0,sign*.0018f),radius*.36f,radius*.36f,.0005f,Recess,6);
    }
    static void LetterD(MeshDraft d,Vector3 at,float h)
    {
        d.Box(at+new Vector3(-h*.28f,0,0),new Vector3(h*.16f,h,.00055f),BrightSteel,0f);
        d.Box(at+new Vector3(0,h*.42f,0),new Vector3(h*.55f,h*.16f,.00055f),BrightSteel,0f);
        d.Box(at+new Vector3(0,-h*.42f,0),new Vector3(h*.55f,h*.16f,.00055f),BrightSteel,0f);
        d.Box(at+new Vector3(h*.27f,0,0),new Vector3(h*.16f,h*.68f,.00055f),BrightSteel,0f);
    }
    static void LetterH(MeshDraft d,Vector3 at,float h)
    {
        d.Box(at+new Vector3(-h*.25f,0,0),new Vector3(h*.16f,h,.00055f),BrightSteel,0f);
        d.Box(at+new Vector3(h*.25f,0,0),new Vector3(h*.16f,h,.00055f),BrightSteel,0f);
        d.Box(at,new Vector3(h*.55f,h*.16f,.00055f),BrightSteel,0f);
    }

    [MenuItem("Tools/ARISE/Gripper/Render Reference Views")]
    public static void RenderReferenceViews()
    {
        GameObject p=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(p==null)throw new InvalidOperationException("Build the gripper first.");
        Directory.CreateDirectory("Docs/Images");
        RenderView(p,.085f,"Docs/Images/AG16095SoftGripper.png",new Vector3(.7f,.30f,-1f));
        RenderView(p,0f,"Docs/Images/AG16095SoftGripper-closed.png",new Vector3(.18f,.08f,-1f));
        RenderView(p,.0475f,"Docs/Images/AG16095SoftGripper-half.png",new Vector3(.18f,.08f,-1f));
        RenderView(p,.095f,"Docs/Images/AG16095SoftGripper-open.png",new Vector3(.18f,.08f,-1f));
        RenderView(p,.085f,"Docs/Images/AG16095SoftGripper-side.png",new Vector3(1f,.12f,-.15f));
    }

    static void RenderView(GameObject prefab,float width,string path,Vector3 direction)
    {
        Scene scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null; Texture2D image=null;
        Camera camera=null;
        RenderTexture previous=RenderTexture.active;
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;
            Material opaque=AssetDatabase.LoadAssetAtPath<Material>(AssetFolder+"/AG16095Surface.mat");
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=opaque;
            go.GetComponent<AG16095GripperVisual>().SetJawWidth(width);
            var cameraGo=new GameObject("Gripper documentation camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);
            camera=cameraGo.AddComponent<Camera>();camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.071f,.091f,1);
            camera.orthographic=true;camera.orthographicSize=.158f;
            Vector3 focus=new Vector3(0,.083f,0);
            camera.transform.position=focus+direction.normalized*.65f;camera.transform.LookAt(focus);
            camera.nearClipPlane=.01f;camera.farClipPlane=3f;
            AddLight(scene,"Key",new Vector3(35,-30,0),1.4f,new Color(1f,.94f,.87f));
            AddLight(scene,"Fill",new Vector3(25,135,0),.95f,new Color(.70f,.83f,1f));
            target=new RenderTexture(1200,1200,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;target.Create();
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image=new Texture2D(1200,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void AddLight(Scene scene,string name,Vector3 rotation,float intensity,Color color)
    {
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);
        var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;go.transform.eulerAngles=rotation;
    }

    [MenuItem("Tools/ARISE/Gripper/Create Preview Scene")]
    public static void CreatePreviewScene()
    {
        Scene active=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            for(int i=0;i<2;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                go.name=i==0?"AG16095_Closed_0mm":"AG16095_Open_95mm";
                go.transform.position=new Vector3(i==0?-.11f:.11f,0,0);
                go.GetComponent<AG16095GripperVisual>().SetJawWidth(i==0?0:.095f);
            }
            AddLight(scene,"Key",new Vector3(35,-30,0),1.4f,new Color(1,.94f,.87f));
            AddLight(scene,"Fill",new Vector3(25,135,0),.95f,new Color(.7f,.83f,1));
            var camGo=new GameObject("Preview Camera");SceneManager.MoveGameObjectToScene(camGo,scene);
            var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.071f,.091f);
            cam.orthographic=true;cam.orthographicSize=.185f;cam.transform.position=new Vector3(.15f,.19f,-.65f);cam.transform.LookAt(new Vector3(0,.08f,0));
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/GripperPreview.unity");
        }
        finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
    }

    [Serializable] public sealed class WidthSample
    {
        public float requestedWidth,appliedWidth,contactGap,innerLinkLength,outerLinkLength;
        public float[] leftPosition,rightPosition;
        public bool passed;
    }

    public static object Validate()
    {
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(prefab==null)throw new InvalidOperationException("Missing AG16095 prefab.");
        Scene scene=EditorSceneManager.NewPreviewScene();
        var samples=new List<WidthSample>();
        int triangles=0,renderers=0,missing=0;
        Vector3 softFingerSize=Vector3.zero;
        bool noClosedFingerOverlap=false;
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            var visual=go.GetComponent<AG16095GripperVisual>();
            var controller=go.GetComponent<GripperJawController>();
            Mesh rightMesh=controller.jawRight.GetComponent<MeshFilter>().sharedMesh;
            Mesh leftMesh=controller.jawLeft.GetComponent<MeshFilter>().sharedMesh;
            noClosedFingerOverlap=rightMesh.bounds.min.x>=-.000001f&&leftMesh.bounds.max.x<=.000001f;
            Vector3 lo=Vector3.one*float.PositiveInfinity,hi=Vector3.one*float.NegativeInfinity;
            var meshVertices=rightMesh.vertices;var meshColors=rightMesh.colors;
            for(int i=0;i<meshVertices.Length;i++) if(meshColors[i].r>.95f&&meshColors[i].g>.1f&&meshColors[i].g<.4f){lo=Vector3.Min(lo,meshVertices[i]);hi=Vector3.Max(hi,meshVertices[i]);}
            softFingerSize=hi-lo;
            foreach(float width in new[]{-.01f,0f,.025f,.0475f,.085f,.095f,.120f,float.NaN,float.PositiveInfinity})
            {
                visual.SetJawWidth(width);
                float expected=float.IsNaN(width)||float.IsInfinity(width)?0f:Mathf.Clamp(width,0,.095f);
                float gap=controller.jawRight.localPosition.x-controller.jawLeft.localPosition.x;
                float a=visual.rightInnerLink.localScale.y,b=visual.rightOuterLink.localScale.y;
                Vector3 lp=controller.jawLeft.localPosition,rp=controller.jawRight.localPosition;
                samples.Add(new WidthSample{requestedWidth=width,appliedWidth=controller.jawWidth,contactGap=gap,leftPosition=new[]{lp.x,lp.y,lp.z},rightPosition=new[]{rp.x,rp.y,rp.z},innerLinkLength=a,outerLinkLength=b,passed=Mathf.Abs(gap-expected)<.000001f&&Mathf.Abs(a-.064f)<.000001f&&Mathf.Abs(b-.064f)<.000001f});
            }
            foreach(var tr in go.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
            foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)) { if(f.sharedMesh==null)throw new InvalidOperationException("Missing mesh: "+f.name);triangles+=f.sharedMesh.triangles.Length/3; }
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)) {renderers++;if(r.sharedMaterial==null||r.sharedMaterial.shader==null||r.sharedMaterial.shader.name=="Hidden/InternalErrorShader")throw new InvalidOperationException("Invalid material: "+r.name);}
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        bool dimensionsMatch=Mathf.Abs(softFingerSize.x-.027f)<.000001f&&Mathf.Abs(softFingerSize.y-.0916f)<.000001f&&Mathf.Abs(softFingerSize.z-.03955f)<.000001f;
        bool passed=missing==0&&noClosedFingerOverlap&&dimensionsMatch;
        foreach(var s in samples)passed &= s.passed;
        var report=new {passed=passed,missingScripts=missing,renderers=renderers,triangles=triangles,maximumOpening=.095f,fingerLength=.0916f,fingerWidth=.03955f,fingerThickness=.027f,embeddedLength=.008f,measuredSoftFingerEnvelope=new[]{softFingerSize.x,softFingerSize.y,softFingerSize.z},dimensionsMatch=dimensionsMatch,noClosedFingerOverlap=noClosedFingerOverlap,samples=samples};
        Directory.CreateDirectory("Logs/GripperUpgrade/2026-10-26");
        File.WriteAllText("Logs/GripperUpgrade/2026-10-26/geometry-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        if(!passed)throw new InvalidOperationException("AG16095 geometry validation failed.");
        return report;
    }

    sealed class MeshDraft
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector3> normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            Vector3 n=Vector3.Cross(b-a,c-a).normalized;int i=vertices.Count;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);
            normals.Add(n);normals.Add(n);normals.Add(n);
            colors.Add(color);colors.Add(color);colors.Add(color);
            triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Triangle(a,b,c,color);Triangle(a,c,d,color);}
        void Outward(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 center,Color color)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c+d)*.25f-center)<0)Quad(d,c,b,a,color);else Quad(a,b,c,d,color);
        }
        public void Box(Vector3 at,Vector3 size,Color color,float bevel)
        {
            float x=size.x*.5f,y=size.y*.5f,b=Mathf.Min(bevel,Mathf.Min(x,y)*.8f);
            Vector2[] p={new Vector2(-x+b,-y),new Vector2(x-b,-y),new Vector2(x,-y+b),new Vector2(x,y-b),new Vector2(x-b,y),new Vector2(-x+b,y),new Vector2(-x,y-b),new Vector2(-x,-y+b)};
            Extrude(p,size.z,at,color,Mathf.Min(bevel,size.z*.3f));
        }
        public void Extrude(Vector2[] points,float depth,Vector3 at,Color color,float bevel)
        {
            var p=(Vector2[])points.Clone();float area=0;
            for(int i=0;i<p.Length;i++){var a=p[i];var b=p[(i+1)%p.Length];area+=a.x*b.y-b.x*a.y;}
            if(area<0)Array.Reverse(p);
            Vector2 center=Vector2.zero;foreach(var v in p)center+=v;center/=p.Length;
            var front=new Vector3[p.Length];var back=new Vector3[p.Length];
            var outerFront=new Vector3[p.Length];var outerBack=new Vector3[p.Length];
            for(int i=0;i<p.Length;i++)
            {
                Vector2 q=p[i]-(p[i]-center).normalized*bevel;
                front[i]=at+new Vector3(q.x,q.y,-depth*.5f);back[i]=at+new Vector3(q.x,q.y,depth*.5f);
                outerFront[i]=at+new Vector3(p[i].x,p[i].y,-depth*.5f+bevel);
                outerBack[i]=at+new Vector3(p[i].x,p[i].y,depth*.5f-bevel);
            }
            // The profiles are convex; a centre fan preserves the bevel silhouette.
            Vector3 cf=at+new Vector3(center.x,center.y,-depth*.5f),cb=at+new Vector3(center.x,center.y,depth*.5f);
            for(int i=0;i<p.Length;i++)
            {
                int j=(i+1)%p.Length;
                Triangle(cf,front[j],front[i],color);Triangle(cb,back[i],back[j],color);
                Quad(outerFront[i],outerFront[j],outerBack[j],outerBack[i],color);
                if(bevel>0){Quad(front[i],front[j],outerFront[j],outerFront[i],color);Quad(outerBack[i],outerBack[j],back[j],back[i],color);}
            }
        }
        public void CylinderZ(Vector3 at,float rx,float ry,float depth,Color color,int sides)
        {
            Vector3 front=at+Vector3.back*depth*.5f,back=at+Vector3.forward*depth*.5f;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=new Vector3(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry,0),q=new Vector3(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry,0);
                Triangle(front,front+q,front+p,color);Triangle(back,back+p,back+q,color);
                Quad(front+p,front+q,back+q,back+p,color);
            }
        }
        public void Wedge(float top,float bottom,float x0Top,float x1Top,float x0Bottom,float x1Bottom,float zTop,float zBottom,Color color)
        {
            Vector3[] v={new Vector3(x0Top,top,-zTop),new Vector3(x1Top,top,-zTop),new Vector3(x1Bottom,bottom,-zBottom),new Vector3(x0Bottom,bottom,-zBottom),new Vector3(x0Top,top,zTop),new Vector3(x1Top,top,zTop),new Vector3(x1Bottom,bottom,zBottom),new Vector3(x0Bottom,bottom,zBottom)};
            Vector3 c=Vector3.zero;foreach(var p in v)c+=p;c/=8;
            Outward(v[0],v[1],v[2],v[3],c,color);Outward(v[4],v[7],v[6],v[5],c,color);
            Outward(v[0],v[4],v[5],v[1],c,color);Outward(v[3],v[2],v[6],v[7],c,color);
            Outward(v[0],v[3],v[7],v[4],c,color);Outward(v[1],v[5],v[6],v[2],c,color);
        }
        public void Tube(Vector3[] points,float radius,Color color,int sides)
        {
            var rings=new Vector3[points.Length,sides];
            for(int i=0;i<points.Length;i++)
            {
                Vector3 tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                Quaternion rot=Quaternion.FromToRotation(Vector3.forward,tangent);
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;rings[i,j]=points[i]+rot*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);}
            }
            for(int i=0;i<points.Length-1;i++)for(int j=0;j<sides;j++){int k=(j+1)%sides;Quad(rings[i,j],rings[i,k],rings[i+1,k],rings[i+1,j],color);}
        }
        public void Append(MeshDraft source,Vector3 position,Quaternion rotation,Vector3 scale)
        {
            int start=vertices.Count;
            for(int i=0;i<source.vertices.Count;i++)
            {
                vertices.Add(position+rotation*Vector3.Scale(source.vertices[i],scale));
                normals.Add((rotation*source.normals[i]).normalized);colors.Add(source.colors[i]);
            }
            foreach(int t in source.triangles)triangles.Add(start+t);
        }
        public void MirrorX()
        {
            for(int i=0;i<vertices.Count;i++){var p=vertices[i];p.x=-p.x;vertices[i]=p;var n=normals[i];n.x=-n.x;normals[i]=n;}
            for(int i=0;i<triangles.Count;i+=3){int t=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=t;}
        }
        public Mesh ToMesh(string name)
        {
            var mesh=new Mesh{name=name};if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            return mesh;
        }
    }
}

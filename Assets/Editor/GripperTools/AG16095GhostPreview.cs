using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repeatable real Unity renders, with a visible background behind the gripper.</summary>
public static class AG16095GhostPreview
{
    [MenuItem("Tools/ARISE/Gripper/Render Transparency Comparison")]
    public static void RenderComparison()
    {
        const int width=600, height=800;
        var sheet=new Texture2D(width*3,height*2,TextureFormat.RGB24,false);
        try
        {
            for(int row=0;row<2;row++)
                for(int mode=0;mode<3;mode++)
                {
                    Texture2D panel=RenderPanel(mode,row==0,width,height);
                    sheet.SetPixels(mode*width,(1-row)*height,width,height,panel.GetPixels());
                    Object.DestroyImmediate(panel);
                }
            sheet.Apply();
            Directory.CreateDirectory("Docs/Images");
            File.WriteAllBytes("Docs/Images/AG16095-transparency-comparison.png",sheet.EncodeToPNG());
            Debug.Log("[ARISE Gripper] Transparency comparison: columns = fade .05 / fade .28 / future .28; rows = light / dark.");
        }
        finally { Object.DestroyImmediate(sheet); }
    }

    static Texture2D RenderPanel(int mode,bool light,int width,int height)
    {
        Scene scene=EditorSceneManager.NewPreviewScene();
        RenderTexture previous=RenderTexture.active, target=null;
        Camera camera=null;
        Material material=null,checkerA=null,checkerB=null;
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AG16095PrefabBuilder.PrefabPath);
            var gripper=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            gripper.GetComponent<AG16095GripperVisual>().SetJawWidth(.085f);
            material=new Material(AssetDatabase.LoadAssetAtPath<Material>(AG16095PrefabBuilder.AssetFolder+"/AG16095GhostSurface.mat"));
            material.color=new Color(1,1,1,mode==0?.05f:.28f);
            material.SetFloat("_GhostEffect",mode==2?1:0);
            foreach(Renderer r in gripper.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;

            checkerA=new Material(Shader.Find("Unlit/Color"));
            checkerB=new Material(Shader.Find("Unlit/Color"));
            checkerA.color=light?new Color(.78f,.81f,.83f):new Color(.12f,.16f,.20f);
            checkerB.color=light?new Color(.54f,.58f,.61f):new Color(.055f,.075f,.10f);
            for(int x=-7;x<=7;x++)for(int y=-7;y<=9;y++)
            {
                var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);
                SceneManager.MoveGameObjectToScene(tile,scene);
                tile.transform.position=new Vector3(x*.035f,y*.035f,.16f);
                tile.transform.localScale=new Vector3(.035f,.035f,.001f);
                tile.GetComponent<Renderer>().sharedMaterial=((x+y)&1)==0?checkerA:checkerB;
            }
            var cameraGo=new GameObject("Transparency comparison camera");
            SceneManager.MoveGameObjectToScene(cameraGo,scene);
            camera=cameraGo.AddComponent<Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=.174f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=checkerA.color;
            Vector3 focus=new Vector3(0,.08f,0);
            camera.transform.position=focus+new Vector3(.36f,.12f,-1f).normalized*.65f;
            camera.transform.LookAt(focus);camera.nearClipPlane=.01f;camera.farClipPlane=3;
            AddLight(scene,new Vector3(35,-30,0),1.4f);
            AddLight(scene,new Vector3(25,135,0),.8f);
            target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            target.antiAliasing=4;target.Create();camera.targetTexture=target;
            camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image;
        }
        finally
        {
            RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;
            if(target!=null){target.Release();Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
            if(material!=null)Object.DestroyImmediate(material);
            if(checkerA!=null)Object.DestroyImmediate(checkerA);
            if(checkerB!=null)Object.DestroyImmediate(checkerB);
        }
    }

    static void AddLight(Scene scene,Vector3 rotation,float intensity)
    {
        var go=new GameObject("Comparison directional light");SceneManager.MoveGameObjectToScene(go,scene);
        var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=intensity;
        go.transform.eulerAngles=rotation;
    }
}

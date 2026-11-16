using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Local, immutable APK releases: latest five versions plus explicitly pinned milestones.</summary>
public static class VersionedApkBuilds
{
    public const string ReleaseDirectory="Build/Android/Releases";
    public const string CatalogPath=ReleaseDirectory+"/catalog.json";
    [Serializable] public class Release
    {
        public string version,packageName,path,sha256,createdUtc,notes;
        public int buildCode;
        public bool key,legacy,pruned;
    }
    [Serializable] public class Catalog
    {
        public int keepLatest=5;
        public List<Release> releases=new List<Release>();
    }
    public static string Absolute(string path) { return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath),path)); }
    public static string Hash(string path)
    {
        using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }
    public static Catalog Load()
    {
        return File.Exists(Absolute(CatalogPath))?JsonUtility.FromJson<Catalog>(File.ReadAllText(Absolute(CatalogPath))):new Catalog();
    }
    public static bool IsRetainedPath(string path)
    {
        string absolute=Absolute(path);
        return File.Exists(absolute)&&Load().releases.Any(x=>!x.pruned&&Absolute(x.path)==absolute);
    }
    static void Save(Catalog catalog)
    {
        Directory.CreateDirectory(Absolute(ReleaseDirectory));string path=Absolute(CatalogPath),temp=path+".tmp";
        File.WriteAllText(temp,JsonUtility.ToJson(catalog,true));
        if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
    }
    public static void ImportPreviousApks()
    {
        var c=Load();
        foreach(string path in Directory.GetFiles(Absolute("Build/Android"),"*.apk"))
        {
            string relative="Build/Android/"+Path.GetFileName(path);
            if(c.releases.Any(x=>x.path==relative))continue;
            c.releases.Add(new Release{version="legacy/"+Path.GetFileNameWithoutExtension(path),path=relative,sha256=Hash(path),createdUtc=File.GetLastWriteTimeUtc(path).ToString("o"),key=true,legacy=true,notes="Pre-versioning milestone; preserve original file."});
        }
        Save(c);
    }
    public static void IdentifyBaseline(string path,string version,int code,string packageName)
    {
        var c=Load();var r=c.releases.Single(x=>x.path==path);
        if(Hash(Absolute(path))!=r.sha256)throw new InvalidOperationException("Baseline APK changed.");
        r.version=version;r.buildCode=code;r.packageName=packageName;r.legacy=false;r.key=true;r.notes="First installed ARISE release; key rollback version.";Save(c);
    }
    public static string PrepareNextPatch()
    {
        var current=new Version(PlayerSettings.bundleVersion);
        string next=new Version(current.Major,current.Minor,Math.Max(0,current.Build)+1).ToString();
        var c=Load();string packageName=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        int code=Math.Max(PlayerSettings.Android.bundleVersionCode,c.releases.Where(x=>x.packageName==packageName).Select(x=>x.buildCode).DefaultIfEmpty(0).Max())+1;
        string path=ReleaseDirectory+"/ARISE_AR_APP-"+next+"-b"+code+".apk";
        if(File.Exists(Absolute(path)))throw new InvalidOperationException("Release APK already exists; no overwrite allowed: "+path);
        Directory.CreateDirectory(Absolute(ReleaseDirectory));PlayerSettings.bundleVersion=next;PlayerSettings.Android.bundleVersionCode=code;
        return path;
    }
    public static Release RegisterSuccessfulBuild(string path,bool key,string notes)
    {
        string absolute=Absolute(path);
        if(!File.Exists(absolute)||new FileInfo(absolute).Length==0)throw new InvalidOperationException("Built APK is missing or empty.");
        if(!absolute.StartsWith(Absolute(ReleaseDirectory)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidOperationException("New releases must be inside the managed release folder.");
        var c=Load();string packageName=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android),hash=Hash(absolute);
        var existing=c.releases.FirstOrDefault(x=>x.packageName==packageName&&x.buildCode==PlayerSettings.Android.bundleVersionCode);
        if(existing!=null)
        {
            if(existing.sha256!=hash||existing.path!=path)throw new InvalidOperationException("A different APK already uses this build code.");
            return existing;
        }
        var release=new Release{path=path,packageName=packageName,version=PlayerSettings.bundleVersion,buildCode=PlayerSettings.Android.bundleVersionCode,sha256=hash,createdUtc=DateTime.UtcNow.ToString("o"),key=key,notes=notes};
        c.releases.Add(release);ApplyRetention(c,Absolute(ReleaseDirectory));Save(c);Debug.Log("[ARISE release] "+path);return release;
    }
    public static int ApplyRetention(Catalog catalog,string managedDirectory)
    {
        int count=0;string root=Path.GetFullPath(managedDirectory)+Path.DirectorySeparatorChar;
        foreach(var group in catalog.releases.Where(x=>!x.legacy&&!x.pruned).GroupBy(x=>x.packageName))
        {
            var keep=new HashSet<Release>(group.OrderByDescending(x=>x.buildCode).Take(Math.Max(1,catalog.keepLatest)));
            foreach(var r in group.Where(x=>!x.key&&!keep.Contains(x)))
            {
                string full=Path.GetFullPath(Path.IsPathRooted(r.path)?r.path:Absolute(r.path));
                if(!full.StartsWith(root,StringComparison.Ordinal)||Path.GetExtension(full)!=".apk")continue;
                if(File.Exists(full))
                {
                    // Never delete unknown or externally modified files, or a pinned milestone.
                    if(Hash(full)!=r.sha256){Debug.LogWarning("[ARISE release] Changed APK retained: "+r.path);continue;}
                    File.Delete(full);
                }
                r.pruned=true;count++;
            }
        }
        return count;
    }
    [MenuItem("Tools/ARISE/Releases/Build Next Patch APK")]
    public static void BuildNextPatch()
    {
        string path=PrepareNextPatch();
        var scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray();
        var report=BuildPipeline.BuildPlayer(scenes,path,BuildTarget.Android,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("APK build failed; release was not registered.");
        RegisterSuccessfulBuild(path,false,"Built with the versioned release menu.");
    }
    [MenuItem("Tools/ARISE/Releases/Mark Current Version as Key")]
    public static void MarkCurrentAsKey()
    {
        var c=Load();string packageName=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        var r=c.releases.Single(x=>!x.pruned&&x.packageName==packageName&&x.buildCode==PlayerSettings.Android.bundleVersionCode);
        r.key=true;Save(c);Debug.Log("[ARISE release] Key version retained: "+r.version);
    }
    [MenuItem("Tools/ARISE/Releases/Open APK Folder")]
    public static void OpenFolder() { Directory.CreateDirectory(Absolute(ReleaseDirectory));EditorUtility.RevealInFinder(Absolute(ReleaseDirectory)); }
}

// Registered APKs are immutable even if File > Build is used with an old filename.
public sealed class ProtectReleasedApks : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform!=BuildTarget.Android)return;
        if(VersionedApkBuilds.IsRetainedPath(report.summary.outputPath))
            throw new BuildFailedException("This APK is a retained release. Use Tools > ARISE > Releases > Build Next Patch APK to avoid overwriting it.");
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
//
namespace libx
{
    public class BuildRules : ScriptableObject
    {
        // ...
        // 覆盖 GetBuilds
        public AssetBundleBuild[] GetBuilds() {
            ClrSAFiles();
            var builds = new List<AssetBundleBuild>();
            foreach (var bundle in ruleBundles) {
                List<string> bigPngs = new List<string>();
                string hackPath1 = HackSA(bundle.assets, bigPngs);
                if (!string.IsNullOrEmpty(hackPath1)) {
                    string[] assetNames = null;
                    if (bigPngs.Count > 0) {
                        assetNames = new string[bigPngs.Count+1];
                        assetNames[bigPngs.Count] = hackPath1;
                        for (int i = 0; i < bigPngs.Count; i++) {
                            assetNames[i] = bigPngs[i];
                        }
                    } else {
                        assetNames = new string[] {hackPath1};
                    }
                    builds.Add(new AssetBundleBuild {
                        assetNames = assetNames,
                        assetBundleName = bundle.name
                    });
                } else
                    builds.Add(new AssetBundleBuild {
                        assetNames = bundle.assets,
                        assetBundleName = bundle.name
                    });
            }
            AssetDatabase.SaveAssets();
            return builds.ToArray();
        }

        // ---- hack to support sprite atlas 2021/06/27 by yxl ----
        // 支持 Assets/HotUpdateResources/UI/下的文件夹按文件夹合图
        // eg. Assets/HotUpdateResources/UI/fight 或者 Assets/HotUpdateResources/UI/fight/test
        const string UIBasePath = "Assets/HotUpdateResources/UI/";
        const string UISAPath = "Assets/HotUpdateResources/UI/Atlas/";
        const float bigPngWidth = 512;
        const float bigPngHeight = 512;
        char[] sp1 = new char[] {'/'};
        string HackSA(string[] ls, List<string> bigPngs) {
            if (ls.Length < 1)
                return null;
            string path1 = ls[0];
            if (path1.IndexOf(UIBasePath) < 0)
                return null;
            string tail = path1.Substring(UIBasePath.Length); // fight/test/a.png
            string[] sls = tail.Split(sp1);
            if (sls.Length > 1) {
                string[] sls2 = new string[sls.Length-1];
                Array.Copy(sls, sls2, sls.Length-1);
                string s1 = string.Join("/", sls2); // eg. fight/test
                string s2 = string.Join("_", sls2); // eg. fight_test
                string rFolder = UIBasePath + s1; // Assets/HotUpdateResources/UI/fight/test
                CreateSAFile(rFolder, s2, bigPngs);
                return rFolder;
            }
            return null;
        }
        void CreateSAFile(string folder, string folderName, List<string> bigPngs) {
            var atlas = new SpriteAtlas();
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings() {
                blockOffset = 1,
                padding = 2,
                enableRotation = true,
                enableTightPacking = false,
            }); //enableTightPacking为true会造成图片重叠
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings() {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings() {
                maxTextureSize = 2048,
                compressionQuality = 50,
                format = TextureImporterFormat.Automatic,
                crunchedCompression = false,
                textureCompression = TextureImporterCompression.Compressed,
            });
            var dir = new DirectoryInfo(folder);
            FileInfo[] files = dir.GetFiles("*.png");
            foreach (FileInfo file in files) {
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{file.Name}");
                var rect = sp.rect;
                if (rect.width > bigPngWidth && rect.height > bigPngHeight) {
                    bigPngs.Add(folder+"/"+file.Name);
                } else
                    atlas.Add(new[] {sp});
            }
            AssetDatabase.CreateAsset(atlas, UISAPath + folderName + ".spriteatlas");
            AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();
        }
        void ClrSAFiles() {
            string folder = UISAPath.Substring(0, UISAPath.Length-1);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
    }
}
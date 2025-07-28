using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureOptimizer : EditorWindow
{
    [MenuItem("Tools/Performance/Optimize Textures for VR")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizer>("Texture Optimizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("VR Texture Optimization Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("This tool will optimize all textures in the Resources/GalleryImages folder for VR performance.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Optimize Gallery Images", GUILayout.Height(30)))
        {
            OptimizeGalleryImages();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Optimize All Project Textures", GUILayout.Height(30)))
        {
            OptimizeAllTextures();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Texture Settings", GUILayout.Height(30)))
        {
            ResetTextureSettings();
        }
    }

    private void OptimizeGalleryImages()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/GalleryImages" });
        
        int optimizedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null && OptimizeTextureImporter(importer, true))
            {
                optimizedCount++;
            }
        }

        AssetDatabase.Refresh();
        
        Debug.Log($"Optimized {optimizedCount} gallery images for VR performance.");
        EditorUtility.DisplayDialog("Optimization Complete", 
            $"Successfully optimized {optimizedCount} gallery images.\n\nEstimated memory savings: ~60-70%", "OK");
    }

    private void OptimizeAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        
        int optimizedCount = 0;
        int totalCount = guids.Length;
        
        for (int i = 0; i < totalCount; i++)
        {
            string guid = guids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            EditorUtility.DisplayProgressBar("Optimizing Textures", $"Processing {Path.GetFileName(path)}", (float)i / totalCount);
            
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null && OptimizeTextureImporter(importer, false))
            {
                optimizedCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"Optimized {optimizedCount} of {totalCount} textures for VR performance.");
        EditorUtility.DisplayDialog("Optimization Complete", 
            $"Successfully optimized {optimizedCount} of {totalCount} textures.", "OK");
    }

    private bool OptimizeTextureImporter(TextureImporter importer, bool isGalleryImage)
    {
        bool wasChanged = false;

        // Skip if already optimized
        if (importer.textureCompression == TextureImporterCompression.Compressed &&
            importer.maxTextureSize <= 512)
        {
            return false;
        }

        // General settings
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            wasChanged = true;
        }

        if (!importer.mipmapEnabled)
        {
            importer.mipmapEnabled = true;
            wasChanged = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Compressed)
        {
            importer.textureCompression = TextureImporterCompression.Compressed;
            wasChanged = true;
        }

        // VR-specific optimizations
        int maxSize = isGalleryImage ? 512 : 1024;
        if (importer.maxTextureSize > maxSize)
        {
            importer.maxTextureSize = maxSize;
            wasChanged = true;
        }

        // Platform-specific settings for Android (Quest)
        var androidSettings = importer.GetPlatformTextureSettings("Android");
        if (androidSettings.maxTextureSize > maxSize || 
            androidSettings.format != TextureImporterFormat.ASTC_6x6)
        {
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = maxSize;
            androidSettings.format = TextureImporterFormat.ASTC_6x6;
            androidSettings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(androidSettings);
            wasChanged = true;
        }

        // Standalone settings (for development)
        var standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
        if (standaloneSettings.maxTextureSize > maxSize)
        {
            standaloneSettings.overridden = true;
            standaloneSettings.maxTextureSize = maxSize;
            standaloneSettings.format = TextureImporterFormat.DXT5;
            standaloneSettings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(standaloneSettings);
            wasChanged = true;
        }

        if (wasChanged)
        {
            importer.SaveAndReimport();
            Debug.Log($"Optimized texture: {importer.assetPath}");
        }

        return wasChanged;
    }

    private void ResetTextureSettings()
    {
        if (EditorUtility.DisplayDialog("Reset Texture Settings", 
            "This will reset all texture import settings to Unity defaults. Continue?", "Yes", "Cancel"))
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 2048;
                    
                    // Reset platform settings
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = false;
                    importer.SetPlatformTextureSettings(androidSettings);
                    
                    var standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
                    standaloneSettings.overridden = false;
                    importer.SetPlatformTextureSettings(standaloneSettings);
                    
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Reset all texture settings to defaults.");
        }
    }
}
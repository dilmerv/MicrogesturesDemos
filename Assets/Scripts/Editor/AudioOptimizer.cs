using UnityEngine;
using UnityEditor;
using System.IO;

public class AudioOptimizer : EditorWindow
{
    [MenuItem("Tools/Performance/Optimize Audio for VR")]
    public static void ShowWindow()
    {
        GetWindow<AudioOptimizer>("Audio Optimizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("VR Audio Optimization Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("This tool will optimize audio files for VR performance and smaller bundle size.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Optimize All Audio Files", GUILayout.Height(30)))
        {
            OptimizeAllAudio();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Optimize Audio Folder Only", GUILayout.Height(30)))
        {
            OptimizeAudioFolder();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Audio Settings", GUILayout.Height(30)))
        {
            ResetAudioSettings();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Optimization Details:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Compression: Vorbis (OGG) for smaller size");
        EditorGUILayout.LabelField("• Quality: 60-70% for good balance");
        EditorGUILayout.LabelField("• Load Type: Compressed in Memory");
        EditorGUILayout.LabelField("• Sample Rate: 22050 Hz for UI sounds");
    }

    private void OptimizeAllAudio()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        
        int optimizedCount = 0;
        int totalCount = guids.Length;
        
        for (int i = 0; i < totalCount; i++)
        {
            string guid = guids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            EditorUtility.DisplayProgressBar("Optimizing Audio", $"Processing {Path.GetFileName(path)}", (float)i / totalCount);
            
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            
            if (importer != null && OptimizeAudioImporter(importer))
            {
                optimizedCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"Optimized {optimizedCount} of {totalCount} audio files for VR performance.");
        EditorUtility.DisplayDialog("Optimization Complete", 
            $"Successfully optimized {optimizedCount} of {totalCount} audio files.\n\nEstimated size reduction: ~50-60%", "OK");
    }

    private void OptimizeAudioFolder()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        
        int optimizedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            
            if (importer != null && OptimizeAudioImporter(importer))
            {
                optimizedCount++;
            }
        }

        AssetDatabase.Refresh();
        
        Debug.Log($"Optimized {optimizedCount} audio files in Assets/Audio folder.");
        EditorUtility.DisplayDialog("Optimization Complete", 
            $"Successfully optimized {optimizedCount} audio files.\n\nEstimated size reduction: ~50-60%", "OK");
    }

    private bool OptimizeAudioImporter(AudioImporter importer)
    {
        bool wasChanged = false;

        // Get default sample settings
        var defaultSettings = importer.defaultSampleSettings;
        
        // Check if already optimized
        if (defaultSettings.compressionFormat == AudioCompressionFormat.Vorbis &&
            defaultSettings.quality == 0.6f)
        {
            return false;
        }

        // Optimize default settings
        if (defaultSettings.loadType != AudioClipLoadType.CompressedInMemory)
        {
            defaultSettings.loadType = AudioClipLoadType.CompressedInMemory;
            wasChanged = true;
        }

        if (defaultSettings.compressionFormat != AudioCompressionFormat.Vorbis)
        {
            defaultSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            wasChanged = true;
        }

        if (defaultSettings.quality != 0.6f)
        {
            defaultSettings.quality = 0.6f; // 60% quality for good balance
            wasChanged = true;
        }

        if (defaultSettings.sampleRateSetting != AudioSampleRateSetting.OptimizeSize)
        {
            defaultSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSize;
            wasChanged = true;
        }

        if (wasChanged)
        {
            importer.defaultSampleSettings = defaultSettings;
        }

        // Platform-specific settings for Android (Quest)
        var androidSettings = importer.GetOverrideSampleSettings("Android");
        if (!androidSettings.overrideForPlatform || 
            androidSettings.compressionFormat != AudioCompressionFormat.Vorbis ||
            androidSettings.quality != 0.6f)
        {
            androidSettings.overrideForPlatform = true;
            androidSettings.loadType = AudioClipLoadType.CompressedInMemory;
            androidSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            androidSettings.quality = 0.6f;
            androidSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSize;
            
            importer.SetOverrideSampleSettings("Android", androidSettings);
            wasChanged = true;
        }

        // Optimize for Standalone (development)
        var standaloneSettings = importer.GetOverrideSampleSettings("Standalone");
        if (!standaloneSettings.overrideForPlatform ||
            standaloneSettings.compressionFormat != AudioCompressionFormat.Vorbis)
        {
            standaloneSettings.overrideForPlatform = true;
            standaloneSettings.loadType = AudioClipLoadType.CompressedInMemory;
            standaloneSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            standaloneSettings.quality = 0.7f; // Slightly higher quality for development
            standaloneSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSize;
            
            importer.SetOverrideSampleSettings("Standalone", standaloneSettings);
            wasChanged = true;
        }

        // Special handling for UI sounds (lower sample rate)
        string fileName = Path.GetFileNameWithoutExtension(importer.assetPath).ToLower();
        if (fileName.Contains("ui") || fileName.Contains("success") || fileName.Contains("error") || 
            fileName.Contains("click") || fileName.Contains("button"))
        {
            if (defaultSettings.sampleRateSetting != AudioSampleRateSetting.OverrideSampleRate ||
                defaultSettings.sampleRateOverride != 22050)
            {
                defaultSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                defaultSettings.sampleRateOverride = 22050; // Lower sample rate for UI sounds
                importer.defaultSampleSettings = defaultSettings;
                wasChanged = true;
            }
        }

        if (wasChanged)
        {
            importer.SaveAndReimport();
            Debug.Log($"Optimized audio: {importer.assetPath}");
        }

        return wasChanged;
    }

    private void ResetAudioSettings()
    {
        if (EditorUtility.DisplayDialog("Reset Audio Settings", 
            "This will reset all audio import settings to Unity defaults. Continue?", "Yes", "Cancel"))
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                
                if (importer != null)
                {
                    // Reset to default settings
                    var defaultSettings = importer.defaultSampleSettings;
                    defaultSettings.loadType = AudioClipLoadType.CompressedInMemory;
                    defaultSettings.compressionFormat = AudioCompressionFormat.PCM;
                    defaultSettings.quality = 1.0f;
                    defaultSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    importer.defaultSampleSettings = defaultSettings;
                    
                    // Clear platform overrides
                    var androidSettings = importer.GetOverrideSampleSettings("Android");
                    androidSettings.overrideForPlatform = false;
                    importer.SetOverrideSampleSettings("Android", androidSettings);
                    
                    var standaloneSettings = importer.GetOverrideSampleSettings("Standalone");
                    standaloneSettings.overrideForPlatform = false;
                    importer.SetOverrideSampleSettings("Standalone", standaloneSettings);
                    
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Reset all audio settings to defaults.");
        }
    }
}
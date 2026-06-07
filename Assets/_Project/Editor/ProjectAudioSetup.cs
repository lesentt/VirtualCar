using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Assets/_Project/Audio 下的音效自动绑定到 ScriptableObject 配置。
/// </summary>
public static class ProjectAudioSetup
{
    const string AudioRoot = "Assets/_Project/Audio";
    const string ConfigRoot = "Assets/_Project/Resources/CollisionConfig";

    [MenuItem("Virtual Vehicle/Wire Project Audio Clips")]
    public static void WireProjectAudioClips()
    {
        CollisionAudioProfile collisionProfile =
            LoadOrCreate<CollisionAudioProfile>(ConfigRoot + "/DefaultCollisionAudioProfile.asset");
        ProjectAudioLibrary library =
            LoadOrCreate<ProjectAudioLibrary>(ConfigRoot + "/ProjectAudioLibrary.asset");

        AudioClip crashLight = LoadClip("crash/dragon-studio-car-crash-sound-effect-376874.mp3");
        AudioClip crashMedium = LoadClip("crash/dragon-studio-car-crash-sound-376882.mp3");
        AudioClip crashHeavy = LoadClip("crash/u_mgq59j5ayf-sound-effect-car-crash-394903.mp3");
        AudioClip grass = LoadClip("grass/dragon-studio-dry-grass-rustling-478361.mp3");
        AudioClip branchBreak = LoadClip("tree-branch-break/tanweraman-tree-branch-break-2-329003.mp3");
        AudioClip treeFall = LoadClip("tree-falling/mollyroselee-falling-tree-ai-generated-431321.mp3");
        AudioClip scrape = LoadClip("scrape/freesound_community-metal-scrape-103668.mp3");
        AudioClip brake = LoadClip("刹车/freesound_community-car-stop-breaks-screech-engine-rev-6171.mp3");
        AudioClip horn = LoadClip("喇叭/dragon-studio-car-honk-386166.mp3");

        AssignClipSet(collisionProfile.metalMetal, crashLight, crashMedium, crashHeavy);
        AssignClipSet(collisionProfile.metalConcrete, crashLight, crashMedium, crashHeavy);
        collisionProfile.metalPlant = CreateClipSet(grass, branchBreak, treeFall, 0.9f);
        collisionProfile.metalGuardrail = CreateClipSet(scrape, crashMedium, crashHeavy, 1f);
        collisionProfile.scrapeLoop = scrape;

        library.engineStartup = LoadClip("engine/i6_german_free/startup.wav");
        library.engineIdle = LoadClip("engine/i6_german_free/idle.wav");
        library.engineLow = LoadClip("engine/i6_german_free/low_on.wav");
        library.engineMed = LoadClip("engine/i6_german_free/med_on.wav");
        library.engineHigh = LoadClip("engine/i6_german_free/high_on.wav");
        library.engineMaxRpm = LoadClip("engine/i6_german_free/maxRPM.wav");
        library.brakeScreech = brake;
        library.horn = horn;
        library.propFall = treeFall;
        library.propBranchBreak = branchBreak;

        ConfigureLoopingClip(scrape);
        ConfigureLoopingClip(library.engineIdle);
        ConfigureLoopingClip(library.engineLow);
        ConfigureLoopingClip(library.engineMed);
        ConfigureLoopingClip(library.engineHigh);
        ConfigureLoopingClip(library.engineMaxRpm);
        ConfigureLoopingClip(brake);

        EditorUtility.SetDirty(collisionProfile);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Project audio clips wired to CollisionConfig assets.");
    }

    static void AssignClipSet(CollisionAudioProfile.ClipSet set, AudioClip light, AudioClip medium, AudioClip heavy)
    {
        if (set == null)
            return;

        set.light = light;
        set.medium = medium;
        set.heavy = heavy;
    }

    static CollisionAudioProfile.ClipSet CreateClipSet(AudioClip light, AudioClip medium, AudioClip heavy, float volumeScale)
    {
        return new CollisionAudioProfile.ClipSet
        {
            light = light,
            medium = medium,
            heavy = heavy,
            volumeScale = volumeScale,
            pitchMin = 0.9f,
            pitchMax = 1.1f
        };
    }

    static AudioClip LoadClip(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{relativePath}");
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void ConfigureLoopingClip(AudioClip clip)
    {
        if (clip == null)
            return;

        string path = AssetDatabase.GetAssetPath(clip);
        AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
            return;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;

        SerializedObject so = new SerializedObject(importer);
        SerializedProperty loop = so.FindProperty("m_Loop");
        if (loop != null)
            loop.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        importer.SaveAndReimport();
    }
}

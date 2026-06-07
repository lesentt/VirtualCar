using UnityEngine;

/// <summary>
/// 项目音效资源库：引擎、驾驶与环境一次性音效。
/// </summary>
[CreateAssetMenu(fileName = "ProjectAudioLibrary", menuName = "VirtualVehicle/Project Audio Library")]
public class ProjectAudioLibrary : ScriptableObject
{
    [Header("发动机（车外）")]
    public AudioClip engineStartup;
    public AudioClip engineIdle;
    public AudioClip engineLow;
    public AudioClip engineMed;
    public AudioClip engineHigh;
    public AudioClip engineMaxRpm;

    [Header("驾驶反馈")]
    public AudioClip brakeScreech;
    public AudioClip horn;

    [Header("环境道具")]
    public AudioClip propFall;
    public AudioClip propBranchBreak;
}

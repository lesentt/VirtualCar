using UnityEngine;

[CreateAssetMenu(fileName = "CollisionAudioProfile", menuName = "VirtualVehicle/Collision Audio Profile")]
public class CollisionAudioProfile : ScriptableObject
{
    [System.Serializable]
    public class ClipSet
    {
        public AudioClip light;
        public AudioClip medium;
        public AudioClip heavy;
        public float volumeScale = 1f;
        public float pitchMin = 0.9f;
        public float pitchMax = 1.1f;
    }

    public ClipSet metalMetal;
    public ClipSet metalConcrete;
    public ClipSet metalPlant;
    public ClipSet metalGuardrail;
    public AudioClip scrapeLoop;

    public float lightImpulseThreshold = 500f;
    public float heavyImpulseThreshold = 4000f;
}

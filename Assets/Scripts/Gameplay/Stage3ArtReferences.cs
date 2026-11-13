using UnityEngine;

/// <summary>Scene-assigned attack, drone, and objective references for Stage 3.</summary>
public class Stage3ArtReferences : MonoBehaviour
{
    [SerializeField] private Sprite[] attackFrames = new Sprite[4];
    [SerializeField] private Sprite attackBeam;
    [SerializeField] private Sprite droneBeam;
    [SerializeField] private Sprite[] droneFloatFrames = new Sprite[4];
    [SerializeField] private Sprite heart;
    [Header("Stage 3 Objectives")]
    [SerializeField, Min(1)] private int droneGoal = 5;
    [SerializeField, Min(1)] private int lifeLimit = 5;

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite DroneBeam => droneBeam;
    public Sprite[] DroneFloatFrames => droneFloatFrames;
    public Sprite Heart => heart;
    public int DroneGoal => droneGoal;
    public int LifeLimit => lifeLimit;
}

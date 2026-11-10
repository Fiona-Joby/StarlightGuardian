using UnityEngine;

/// <summary>Scene-assigned art references used by Stage 2 runtime systems.</summary>
public class Stage2ArtReferences : MonoBehaviour
{
    [SerializeField] private Sprite[] attackFrames = new Sprite[4];
    [SerializeField] private Sprite attackBeam;
    [SerializeField] private Sprite turretShort;
    [SerializeField] private Sprite turretTall;
    [SerializeField] private Sprite turretBeam;
    [SerializeField] private Sprite[] droneFloatFrames = new Sprite[4];
    [SerializeField] private Sprite heart;
    [Header("Stage 2 Objectives")]
    [SerializeField, Min(1)] private int turretGoal = 6;
    [SerializeField, Min(1)] private int droneGoal = 5;
    [SerializeField, Min(1)] private int lifeLimit = 5;

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite TurretShort => turretShort;
    public Sprite TurretTall => turretTall;
    public Sprite TurretBeam => turretBeam;
    public Sprite[] DroneFloatFrames => droneFloatFrames;
    public Sprite Heart => heart;
    public int TurretGoal => turretGoal;
    public int DroneGoal => droneGoal;
    public int LifeLimit => lifeLimit;
}

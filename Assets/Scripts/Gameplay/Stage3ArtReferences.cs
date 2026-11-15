using UnityEngine;

/// <summary>Scene-assigned attack, drone, and objective references for Stage 3.</summary>
public class Stage3ArtReferences : MonoBehaviour
{
    [SerializeField] private Sprite[] attackFrames = new Sprite[4];
    [SerializeField] private Sprite attackBeam;
    [SerializeField] private Sprite droneBeam;
    [SerializeField] private Sprite[] droneFloatFrames = new Sprite[4];
    [SerializeField] private Sprite heart;
    [SerializeField] private Sprite[] slimeWalkFrames = new Sprite[4];
    [SerializeField] private Sprite slimeAttack;
    [Header("Stage 3 Hard Blob Beast")]
    [SerializeField, Min(1)] private int hardBeastGoal = 2;
    [SerializeField] private Sprite[] hardBeastWalkFrames = new Sprite[5];
    [SerializeField] private Sprite hardBeastJump;
    [SerializeField] private Sprite hardBeastStomp;
    [SerializeField] private Stage2BlobBeast.AISettings hardBeastAI = new Stage2BlobBeast.AISettings
    {
        patrolSpeed = 1.8f,
        detectionRange = 9f,
        visionAngle = 120f,
        spottingTime = 0.05f,
        preparationTime = 0.25f,
        predictionTime = 1.1f,
        maximumPredictionDistance = 4.5f,
        jumpHeight = 3.2f,
        attackDuration = 1.1f,
        damage = 1f,
        recoveryTime = 0.8f
    };
    [SerializeField] private Sprite lootBoxWithShield;
    [SerializeField] private Sprite lootBox;
    [SerializeField] private Sprite[] starSpiritMoveFrames = new Sprite[2];
    [SerializeField] private Sprite starSpiritFinalPose;
    [SerializeField] private Sprite[] companionMoveFrames = new Sprite[2];
    [SerializeField] private Sprite companionReunionPose;
    [Header("Stage 3 Objectives")]
    [SerializeField, Min(1)] private int droneGoal = 5;
    [SerializeField, Min(1)] private int slimeGoal = 4;
    [SerializeField, Min(1)] private int lifeLimit = 5;

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite DroneBeam => droneBeam;
    public Sprite[] DroneFloatFrames => droneFloatFrames;
    public Sprite Heart => heart;
    public Sprite[] SlimeWalkFrames => slimeWalkFrames;
    public Sprite SlimeAttack => slimeAttack;
    public int HardBeastGoal => Mathf.Max(1, hardBeastGoal);
    public Sprite[] HardBeastWalkFrames => hardBeastWalkFrames;
    public Sprite HardBeastJump => hardBeastJump;
    public Sprite HardBeastStomp => hardBeastStomp;
    public Stage2BlobBeast.AISettings HardBeastAI => hardBeastAI;
    public Sprite LootBoxWithShield => lootBoxWithShield;
    public Sprite LootBox => lootBox;
    public Sprite[] StarSpiritMoveFrames => starSpiritMoveFrames;
    public Sprite StarSpiritFinalPose => starSpiritFinalPose;
    public Sprite[] CompanionMoveFrames => companionMoveFrames;
    public Sprite CompanionReunionPose => companionReunionPose;
    public int DroneGoal => droneGoal;
    public int SlimeGoal => slimeGoal;
    public int LifeLimit => lifeLimit;
}

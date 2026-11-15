using UnityEngine;

/// <summary>Scene-assigned art and encounter references used by Stage 2.</summary>
public class Stage2ArtReferences : MonoBehaviour
{
    [SerializeField] private Sprite[] attackFrames = new Sprite[4];
    [SerializeField] private Sprite attackBeam;
    [SerializeField] private Sprite turretShort;
    [SerializeField] private Sprite turretTall;
    [SerializeField] private Sprite turretBeam;
    [SerializeField] private Sprite heart;
    [SerializeField] private Sprite[] companionMoveFrames = new Sprite[2];
    [Header("Stage 2 Turrets")]
    [SerializeField, Min(1)] private int turretGoal = 6;
    [Header("Shared Encounter Settings")]
    [SerializeField, Min(1)] private int lifeLimit = 5;
    [Header("Stage 2 Blob Beast")]
    [SerializeField, Min(1)] private int blobBeastGoal = 2;
    [SerializeField] private Sprite[] blobWalkFrames = new Sprite[5];
    [SerializeField] private Sprite blobJump;
    [SerializeField] private Sprite blobStomp;
    [SerializeField] private Stage2BlobBeast.AISettings blobBeastAI = new Stage2BlobBeast.AISettings();

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite TurretShort => turretShort;
    public Sprite TurretTall => turretTall;
    public Sprite TurretBeam => turretBeam;
    public Sprite Heart => heart;
    public Sprite[] CompanionMoveFrames => companionMoveFrames;
    public int TurretGoal => turretGoal;
    public int LifeLimit => lifeLimit;
    public Sprite[] BlobWalkFrames => blobWalkFrames;
    public int BlobBeastGoal => Mathf.Max(1, blobBeastGoal);
    public Sprite BlobJump => blobJump;
    public Sprite BlobStomp => blobStomp;
    public Stage2BlobBeast.AISettings BlobBeastAI => blobBeastAI;
}

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
    [Header("Stage 2 Turrets")]
    [SerializeField, Min(1)] private int turretGoal = 6;
    [Header("Shared Encounter Settings")]
    [SerializeField, Min(1)] private int lifeLimit = 5;

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite TurretShort => turretShort;
    public Sprite TurretTall => turretTall;
    public Sprite TurretBeam => turretBeam;
    public Sprite Heart => heart;
    public int TurretGoal => turretGoal;
    public int LifeLimit => lifeLimit;
}

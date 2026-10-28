using UnityEngine;

/// <summary>Scene-assigned art references used by Stage 2 runtime systems.</summary>
public class Stage2ArtReferences : MonoBehaviour
{
    [SerializeField] private Sprite[] attackFrames = new Sprite[4];
    [SerializeField] private Sprite attackBeam;
    [SerializeField] private Sprite turretShort;
    [SerializeField] private Sprite turretTall;
    [SerializeField] private Sprite turretBeam;

    public Sprite[] AttackFrames => attackFrames;
    public Sprite AttackBeam => attackBeam;
    public Sprite TurretShort => turretShort;
    public Sprite TurretTall => turretTall;
    public Sprite TurretBeam => turretBeam;
}

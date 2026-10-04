/// <summary>
/// Decoupled event bus for star collection. Scoring, combo, energy, and
/// any other system can subscribe without coupling to StarMovement.
/// </summary>
public static class StarEvents
{
    public static event System.Action<StarMovement> StarCaught;
    internal static void RaiseCaught(StarMovement star) => StarCaught?.Invoke(star);
}

/// <summary>
/// Playtime information structure for UI display and debugging
/// </summary>
public struct PlayTimeInfo
{
    public float GameTime;
    public float SessionTime;
    public string FormattedGameTime;
    public string FormattedSessionTime;
    public bool IsTracking;
        
    public override string ToString()
    {
        return $"PlayTime[Game: {FormattedGameTime}, Session: {FormattedSessionTime}, Tracking: {IsTracking}]";
    }
}
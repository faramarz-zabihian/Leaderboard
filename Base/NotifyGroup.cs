class NotifyGroup
{
    public DateTime Created;
    public ScoreGroup group = null;
    public DateTime first_changed;
    public int Score;
    private String? LowerRankName = null;
    private int? LowerRankScore = null;
    private String? HighRankName = null;
    private int? HighRankScore = null;
    
    public NotifyGroup(ScoreGroup scoreGroup, Player? lowerRankPlayer, Player? higherRankPlayer, int score, DateTime changeTime)
    {
        group = scoreGroup;
        LowerRankName  = lowerRankPlayer?.Name;
        LowerRankScore = lowerRankPlayer?.Score;
        HighRankName =higherRankPlayer?.Name;
        HighRankScore = higherRankPlayer?.Score;
        Score = score;
        first_changed = changeTime;
    }
}

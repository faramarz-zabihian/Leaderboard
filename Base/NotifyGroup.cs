class NotifyGroup
{
    public List<Player> Players = null;
    public Player? NextPlayer;
    public Player? PrevPlayer;
    public int Score;
    private Player? LowerRank = null;
    private Player? HigherRank = null;

    public NotifyGroup(List<Player> players, Player? lowerRankPlayer, Player? higherRankPlayer, int score)
    {
        Players = players;
        LowerRank = lowerRankPlayer;
        HigherRank = higherRankPlayer;
        Score = score;
    }
}

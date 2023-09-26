public class ScoreGroup
{
    public ScoreGroup(int score)
    {
        this.Score = score;
    }
    public int Score { get; }
    public List<Player> Players = new();
    public int GroupCount;
    public Player GetDelegate()
    {
        return Players[0];
    }
}
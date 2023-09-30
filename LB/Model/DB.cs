namespace LeaderBoard.Base
{
    internal class DB
    {
        static Random rnd = new(4000);
        internal static void getSamplePlayer(int score, Action<Player> action)
        {
            // call db
            Task.Run(() =>
            {
                Thread.Sleep(3000);
                action(new Player { Score = score, Id = 6000001 + rnd.Next(1000000) });
            });
        }

    }
}

using LeaderBoard.Base;

internal class Program
{
    const int NUM_ONLINE_PLAYERS = 1000000;
    const int NUM_SCORES = 10000;
    const int MAX_GAINED_SCORE_PER_PLAY = 12;

    private static void Main(string[] args)
    {
        Random rndScore = new(1000);
        var rndPlayer = new Random(4000);

        DateTime start = DateTime.Now;
        List<Player> players = new(NUM_ONLINE_PLAYERS);
        for (int i = 1; i < NUM_ONLINE_PLAYERS; i++) // one million playes
        {
            var player = new Player { Id = i, Score = rndScore.Next(NUM_SCORES) };
            players.Add(player);
        }

        NotificationController noti = new(1);
        LB lb = new(players, noti);
        var endSetup = DateTime.Now;
        Console.WriteLine($"{endSetup - start}");
        start = endSetup;
        Console.WriteLine($"spa:{lb.GetArray().Length}");

        players.Clear();   // clear memory

        for (int i = 0; i < NUM_ONLINE_PLAYERS; i++)
        {
            // extract a random player and push
            var sg = lb.GetGroup(rndScore.Next(NUM_SCORES));
            if (sg == null)
                continue;
            var player = sg.Players[rndPlayer.Next(sg.Players.Count)];
            lb.Push(player, player.Score + rndScore.Next(MAX_GAINED_SCORE_PER_PLAY) + 1); // re rank the player

        }
        endSetup = DateTime.Now;

        Console.WriteLine();
        Console.WriteLine($"process         : {endSetup - start}");
        Console.WriteLine();
        Console.WriteLine($"new Items       : {lb.Stats.newItems}");
        Console.WriteLine($"removed         : {lb.Stats.removedCount}");
        Console.WriteLine($"updates         : {lb.Stats.updateCount}");
        Console.WriteLine($"removed players : {lb.Stats.removePlayers}");

        Console.WriteLine();
        Console.WriteLine($"single_notifications   :{noti.SinglesItemsCount}");
        Console.WriteLine($"by_group_notifications :{noti.GroupedCount}");
        Console.WriteLine($"group noti_invokations :{noti.Invokations}");
        Console.WriteLine($"End SparseArray with  :{lb.GetArray().Length}");
        Task.WaitAll();

        Console.ReadKey();
    }
}

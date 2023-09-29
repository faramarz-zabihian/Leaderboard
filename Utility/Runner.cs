using LeaderBoard.Base;
using MemoryPack;


namespace LeaderBoard.Utility
{
    public class Runner : ILogger
    {

        private readonly NotificationController noti;
        private readonly LB lb;
        private readonly int TopScorePerPlay;
        private readonly ILogger logger;

        public Runner(int scoreRange, int topScorePerPlay, int onLinePlayersCount, int reportInterval, ILogger logger)
        {
            Random rndScore = new(1000);
            TopScorePerPlay = topScorePerPlay;

            this.logger = logger ?? this;

            List<Player> players = new(onLinePlayersCount);
            noti = new(reportInterval);
            for (int i = 1; i < onLinePlayersCount; i++) // Generating onLine players
            {
                var player = new Player { Id = i, Score = rndScore.Next(scoreRange) };
                players.Add(player);
            }
            lb = new(players, noti);


            var s1 = DateTime.Now;
            MemoryPackSerializer.Serialize<List<Player>>(players);
            var e1 = DateTime.Now;
            var sd = new SortedDictionary<int, Player>();
            for (int i = 0; i < players.Count; i++)
            {
                sd.Add(players[i].Id, players[i]);
            }
        }
        DateTime start;
        public void Perform(int numSeconds)
        {
            Random rndScore = new(1000);
            var rndPlayer = new Random(4000);
            start = DateTime.Now.AddSeconds(numSeconds);
            LeaderBoardIterator it = lb.GetArray().GetIteraror();
            it.Last();
            var toppestScore = it.CurrentItem()!.Score;
            lb.Stats.startSize = lb.GetArray().Length;
            int j = 0;
            //while (DateTime.Now < start)
            DateTime s1 = DateTime.Now;
            while (j++ < 6000000)
            {
                // extract a random player and push
                var sg = lb.GetGroup(rndScore.Next(toppestScore));
                if (sg == null)
                {
                    --j; // guarantee the number of pushes
                    continue;
                }
                var player = sg.Players.GetValueAtIndex(rndPlayer.Next(sg.Players.Count));
                lb.Push(player, player.Score + rndScore.Next(TopScorePerPlay) + 1); // re rank the player                
            }
            DateTime e1 = DateTime.Now;
            Task.WaitAll();
            lb.Stats.seconds = (e1 - s1).Seconds;
            lb.Stats.endSize = lb.GetArray().Length;
            lb.Stats.TPS = lb.Stats.pushes / numSeconds;
            logger.Log(lb, noti);
        }
        public void Log(LB lb, NotificationController nc)
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Seconds                  : {lb.Stats.seconds,7}");
            Console.WriteLine($"Pushes                   : {lb.Stats.pushes,7}");
            Console.WriteLine($"Groups created           : {lb.Stats.createdGroups,7}");
            Console.WriteLine($"Groups removed           : {lb.Stats.removedGroups,7}");
            Console.WriteLine($"TPS                      : {lb.Stats.TPS,7}");
            Console.WriteLine($"LB start size            : {lb.Stats.startSize,7}");
            Console.WriteLine($"LB end size              : {lb.Stats.endSize,7}");
            Console.WriteLine(".........................................");
            Console.WriteLine($"Single item notes        : {noti.singlesCounter,7}");
            Console.WriteLine($"Grouped notes            : {noti.groupedCounter,7}");
            Console.WriteLine($"Total Notes              : {noti.totalSent,7}");
            Console.WriteLine($"Task invokations         : {noti.task_invokations,7}");
            if (noti.task_invokations > 0)
                Console.WriteLine($"Average per task invoks  : {noti.totalSent / noti.task_invokations,7}");
            Console.WriteLine("*****************************************");
        }
    }
}

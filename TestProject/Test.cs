using LeaderBoard;
using LeaderBoard.Base;
using LeaderBoard.Utility;
using Xunit.Abstractions;

namespace TestProject
{
    class TestPlayer : Player
    {
        public DateTime? lastSubmittedTime = null;
    }
    public class UnitTest
    {
        readonly LB lb;
        readonly NotificationController noti;
        readonly List<TestPlayer> players;
        public UnitTest()
        {
            noti = new(1000);
            players = Enumerable.Range(0, 10).Select(i => new TestPlayer { Id = i + 1, Score = i * 5 }).ToList(); // 0, 5, ..., 40, 45 - 10
            players.AddRange(Enumerable.Range(5, 5).Select(i => new TestPlayer { Id = (i + 1) * 3, Score = i * 5 })); // 25,.., 45  - 5

            lb = new(players.CastList<TestPlayer, Player>(), noti);
        }
        Player GS(int score)
        {
            return players.Where(p => p.Score == score).First();
        }

        [Fact]
        // Move a player to a higher existing score
        public void TestNonExistentScoreUpdate()
        {
            var player = GS(5);
            player.Score = 6;
            lb.Push(player, 10);
        }
        [Fact]
        // Move a player to a higher existing score
        public void TestRemovalAndUpdate()
        {
            lb.Push(GS(5), 10);
            Assert.Equal(9, lb.GetArray().Length);
            Assert.Null(lb.GetGroup(5));
            Assert.Equal(2, lb.GetGroup(10)?.GroupCount ?? 0);
            Assert.Equal(1, lb.Stats.removedGroups);
            Assert.Equal(0, lb.Stats.createdGroups);
        }

        [Fact]
        // Move two players to a higher none existing score
        public void TestRemovalAndInsert()
        {
            lb.Push(GS(5), 51);
            lb.Push(GS(10), 52);

            Assert.Equal(10, lb.GetArray().Length); // two added - two removed = 0

            Assert.Null(lb.GetGroup(5));
            Assert.Null(lb.GetGroup(10));
            Assert.Equal(1, lb.GetGroup(51)?.GroupCount);
            Assert.Equal(1, lb.GetGroup(52)?.GroupCount);

            Assert.Equal(2, lb.Stats.removedGroups);
            Assert.Equal(2, lb.Stats.createdGroups);
        }
        [Fact]
        // Move two players to a lower or equal score
        public void TestInvalidScore()
        {
            lb.Push(GS(5), 5);
            lb.Push(GS(10), 4);
            Assert.NotNull(lb.GetGroup(5));
            Assert.NotNull(lb.GetGroup(10));
            Assert.Null(lb.GetGroup(4));
            Assert.Equal(0, lb.Stats.removedGroups);
            Assert.Equal(0, lb.Stats.createdGroups);
        }

        // notification Checks
        [Fact]
        public void TestRemoveDelegate()
        {
            Player? player = null;
            var iter = lb.GetIteraror();
            while (iter.CurrentItem != null) // generally CurrentItem can be null, and this must be rectified
            {
                if (iter.CurrentItem()?.Players.Count > 1)
                {
                    player = iter.CurrentItem()!.GetDelegate();
                    break;
                }
                iter.MoveForward();
            }
            Assert.NotNull(player);

            int score = player.Score;
            lb.Push(player, score + 15); // this should cause adjacent groups to get notified
            var noti_list = noti.GetList();

            Assert.Single(noti_list.Where(it => it.Score == player.Score)); // single item in the new group
            Assert.Single(noti_list.Where(it => it.Score == score - 5));   // old score group neighbors must be notified
            Assert.Single(noti_list.Where(it => it.Score == score + 5));
        }

        [Fact]
        public void TestPutMultiplePlayers_In_NotificationList()
        {
            int TargetScore = 25;
            lb.Push(GS(5), TargetScore);
            lb.Push(GS(10), TargetScore);
            lb.Push(GS(15), TargetScore);
            var noti_list = noti.GetList();
            Assert.Single(noti_list.Where(it => it.Score == TargetScore));
            var players = noti_list.Where(it => it.Score == TargetScore).First().Players;
            Assert.Equal(3, players.Count);
            Assert.Superset<int>(new HashSet<int>(new List<int> { 1 + 1, 1 + 2, 1 + 3 }), new HashSet<int>(players.Values.Select(p => p.Id)));
        }

        [Fact]
        void TestNotificationCheck()
        {
            // if users change score faster than notification period, it will prevent them from being notified, this must be handled
            const int MAX_GAINED_SCORE_PER_PLAY = 5;
            int NUM_OF_EXPERMENTS = 200;
            Random rndScore = new(300);
            Random rndPlayer = new(200);
            int n = 0;
            int k = 0;
            while (n < NUM_OF_EXPERMENTS)
            {
                k++;
                if (k % 100 == 0)
                {
                    k = 0;
                    Thread.Sleep(1000);
                }

                var player = players[rndPlayer.Next(players.Count)];
                if (player.lastSubmittedTime != null && player.lastSubmittedTime.Value! > DateTime.Now)
                    continue;
                n++;
                player.lastSubmittedTime = DateTime.Now.AddSeconds(2);
                lb.Push(player, player.Score + rndScore.Next(MAX_GAINED_SCORE_PER_PLAY) + 1); // re rank the player
            }
            Task.WaitAll();
        }
    }
    public class TestMain : ILogger
    {
        private readonly ITestOutputHelper output;
        private readonly Runner rlb;

        public TestMain(ITestOutputHelper output)
        {
            const int SCORE_RANGE = 10000;
            const int MAX_GAINED_SCORE_PER_PLAY = 12;
            const int NUM_ONLINE_PLAYERS = 1000000;
            const int NOTIFY_INTERVAL = 500;
            this.output = output;
            rlb = new(SCORE_RANGE, MAX_GAINED_SCORE_PER_PLAY, NUM_ONLINE_PLAYERS, NOTIFY_INTERVAL, this);
        }
        void ILogger.Log(LB lb, NotificationController noti)
        {
            output.WriteLine("-----------------------------------------");
            output.WriteLine($"Seconds                  : {lb.Stats.duration,7:###.##}");
            output.WriteLine($"Pushes                   : {lb.Stats.pushes,7}");
            output.WriteLine($"Groups created           : {lb.Stats.createdGroups,7}");
            output.WriteLine($"Groups removed           : {lb.Stats.removedGroups,7}");
            output.WriteLine($"TPS                      : {lb.Stats.TPS,7}");
            output.WriteLine($"LB start size            : {lb.Stats.startSize,7}");
            output.WriteLine($"LB end size              : {lb.Stats.endSize,7}");
            output.WriteLine(".........................................");
            output.WriteLine($"Single item notes        : {noti.singlesCounter,7}");
            output.WriteLine($"Grouped notes            : {noti.groupedCounter,7}");
            output.WriteLine($"Total Notes              : {noti.totalSent,7}");
            output.WriteLine($"Task invokations         : {noti.task_invokations,7}");
            if (noti.task_invokations > 0)
                output.WriteLine($"Average per task invoks  : {noti.totalSent / noti.task_invokations,7}");
            output.WriteLine("*****************************************");
        }

        [Fact]
        public void TestPerformance()
        {
            rlb.Perform(2);
        }
    }
}
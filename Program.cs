using LeaderBoard.Base;
using MemoryPack;
using System;

internal class Program
{
    static int newItems = 0;
    static int removedCount = 0;
    static int updateCount = 0;
    static int removePlayers = 0;
    static int single_notifications = 0;
    static int by_group_notifications = 0;
    static int noti_invokations = 0;
    static readonly Random randomId = new();
    static readonly SortedDictionary<int, NotifyGroup> notified_groups = new();

    private static void Main(string[] args)
    {
        const int NUM_ONLINE_PLAYERS = 1000000;
        const int NUM_SCORES = 10000;
        const int MAX_GAINED_SCORE_PER_PLAY = 12;

        Random rnd = new(1000);
        SparseArray spa = new();

        DateTime start = DateTime.Now;
        for (int i = 1; i < NUM_ONLINE_PLAYERS; i++) // one million playes
        {
            var score = rnd.Next(NUM_SCORES);
            var sg = spa[score].CurrentItem();
            if (sg == null)
            {
                sg = new ScoreGroup(score) { GroupCount = 1 };
                spa.Add(score, sg);
                sg.Players.Add(GetPlayer(i));
            }
            else
            {
                sg.Players.Add(GetPlayer(i));
                sg.GroupCount++;
            }
        }

        var end = DateTime.Now;
        var ts = end - start;
        Console.WriteLine(ts.ToString());

        start = end;
        // create a new random sequence
        rnd = new Random(400);
        for (int i = 0; i < 1000000; i++)
        {
            // choose an arbitrary ScoreGroup
            var sg = spa[rnd.Next(spa.Length)].CurrentItem();
            if (sg == null)
                continue;
            var player = sg.Players[rnd.Next(sg.Players.Count)];
            // re rank the player
            NewPlayEvent(spa, player.Id, sg.Score, sg.Score + rnd.Next(MAX_GAINED_SCORE_PER_PLAY) + 1);
        }
        Task.WaitAll();
        end = DateTime.Now;
        ts = end - start;
        Console.WriteLine(ts.ToString());
        Console.WriteLine();
        Console.WriteLine($"new Items: {newItems}");
        Console.WriteLine($"removed {removedCount}");
        Console.WriteLine($"updates : {updateCount}");
        Console.WriteLine($"remove players : {removePlayers}");
        Console.WriteLine($"spa:{spa.Length}");
        Console.WriteLine();
        Console.WriteLine($"single_notifications:{single_notifications}");
        Console.WriteLine($"by_group_notifications:{by_group_notifications}");
        Console.WriteLine($"group noti_invokations:{noti_invokations}");
        Console.WriteLine($"start spa with:{spa.Length}");
        Console.ReadKey();
    }
    static void Add_to_notification_list(Iterator<ScoreGroup> iter)
    {        
        if (iter.CurrentItem() == null)
            return;

        DateTime dt;
        var cur = new { iter.CurrentItem()!.Players, iter.CurrentItem()!.Score };
        if (notified_groups.TryGetValue(cur.Score, out NotifyGroup? ng)) // get the insertion time of previous instance if any
        {
            dt = ng.FirstCreated;
            notified_groups.Remove(cur.Score);
        }
        else
            dt = DateTime.Now;

        iter.MoveForward(); var _next = new { iter.CurrentItem()?.getDelegate().Name, iter.CurrentItem()?.Score }; iter.MoveBackward();
        iter.MoveBackward(); var _prev = new { iter.CurrentItem()?.getDelegate().Name, iter.CurrentItem()?.Score }; iter.MoveForward();
        notified_groups.Add(cur.Score, new NotifyGroup(cur.Players, _prev.Name, _prev.Score, _next.Name, _next.Score, cur.Score, dt));


    }
    static int GetRandomId()
    {
        return randomId.Next(999999999);
    }
    static Player GetPlayerFromDB(int score)
    {
        //Thread.Sleep( 1000 );
        //todo: implement database access    
        int id = 200000 + GetRandomId();
        return new Player { Id = id, Name = "Name:" + id.ToString() };
    }
    static Player GetPlayer(int id)
    {
        return new Player { Id = id };
    }
    static DateTime notification_dead_line = DateTime.Now.AddSeconds(1);
    public static void NewPlayEvent(SparseArray spa, int id, int old_score, int new_score)
    {


        var it_old_score = spa[old_score];
        var it_new_score = spa[new_score];
        var g_new_score = it_new_score.CurrentItem(); if (g_new_score == null) it_new_score = null; // unnecessary

        ScoreGroup g_old = it_old_score.CurrentItem()!;
        var dp_old_score = g_old.getDelegate();

        Player? player = null;
        for (int ndx = 0; ndx < g_old.Players.Count; ndx++) // note: SortedList or SortedDictionary are faster in this respect, but they are very slow on ToList()
            if (g_old.Players[ndx].Id == id)
            {
                player = g_old.Players![ndx];
                g_old.Players.RemoveRange(ndx, 1);
                g_old.GroupCount--;
                removePlayers++;
                break;
            }
        if (player == null)
            throw new ApplicationException("player not found"); // this never should happn

        Iterator<ScoreGroup>? removed_group_top_neighbor = null;
        bool oldRemoved = false;

        if (g_old.GroupCount == 0) // the group must be removed
        {
            if (notified_groups.ContainsKey(old_score))
                notified_groups.Remove(old_score);

            removed_group_top_neighbor = spa.Remove(old_score); // removes score, and gets nearest higher rank
            it_old_score = null;                                // make sure it's not accidentally used
            oldRemoved = true;
            removedCount++;
        }
        else if (g_old.Players.Count == 0) // group count is bigger than zero
        {
            Player dummyPlayer = new() { Id = -GetRandomId(), Fake = true };
            g_old.Players.Add(dummyPlayer);
            /*  Task.Run(async () => {
                var p = getPlayerName(score);
                dummyPlayer.Id = p.Id;
                dummyPlayer.Name = p.Name;
                dummyPlayer.Fake = false;
                // Notifier can wait until Fake has been reset
                }
                );
            */
        }
        if (g_new_score == null) // the group does not exist, and must be created        
        {
            player = GetPlayer(id);
            ScoreGroup sg = new(new_score) { GroupCount = 1 };
            sg.Players.Add(player);
            it_new_score = spa.Add(new_score, sg);
            newItems++;
        }
        else
        {
            g_new_score.GroupCount++;
            g_new_score.Players.Add(player);
            updateCount++;
        }
        // list of score groups that must be notified 
        // p2, p1, c, n1, n2

        // if group c is going to be notified then p1 and n1 must be traversed for their delegates
        // (1)- if c removed then n1 becomes current, p1 and n1 must be notified
        // (2)- if c is inserted p2 p1 c n1 n2, p2 then n1 and p1 are going to be notified
        // (3)- if a player from c1 moves to n? and the player is the delegate of c, then p1, n1 must be notified
        // (4)- if a player relocates from c  to n?, the player must be notified
        // (5)- if the plyaer has simply changed its group, he/she is the one to be notified

        bool must_notify_player = true; // case 4 : the player must be notofied
        if (g_new_score == null) // case 2: The entering group is new, its new neighbors are definitly affected
        {
            // the palyer resides in a notified group
            must_notify_player = false;

            it_new_score!.MoveForward();
            Add_to_notification_list(it_new_score); // n1
            it_new_score.MoveBackward();
            it_new_score.MoveBackward();
            Add_to_notification_list(it_new_score); // p1
            it_new_score.MoveForward();
            Add_to_notification_list(it_new_score); // c            
        }
        if (must_notify_player)
        {
            // must create a single notification for the player
            // if the player is among an already being notified group, they can be ignored
            NotifyGroup g;
            if (notified_groups.TryGetValue(new_score, out g))
            {
                if (g!.Players.IndexOf(player) < 0)
                    g.Players.Add(player);
            }
            else
            {
                it_new_score.MoveBackward();
                var p1 = new { it_new_score.CurrentItem()?.getDelegate().Name, it_new_score.CurrentItem()?.Score };
                it_new_score.MoveForward();

                it_new_score.MoveForward();
                var n1 = new { it_new_score.CurrentItem()?.getDelegate().Name, it_new_score.CurrentItem()?.Score };
                it_new_score.MoveBackward();
                notified_groups.Add(new_score, new NotifyGroup(new List<Player> { player }, p1.Name, p1?.Score, n1.Name, n1.Score, new_score));
            }

            // 
            //notified_groups.Add(new NotifyGroup(new List<Player> { player}));
            single_notifications++;
        }


        if (oldRemoved) // case 1: n1, p1 should be notified
        {
            Add_to_notification_list(removed_group_top_neighbor!); // n1
            removed_group_top_neighbor!.MoveBackward();
            Add_to_notification_list(removed_group_top_neighbor);  // p1
        }
        else
        {
            // wondering if the left user have been the representative of its owen group, then p1, n1 must be notified
            if (dp_old_score.Id == id) // should check if the moving player has represented his old group or not
            {
                // p2 p1 c n1 n2   : nothing is removed, an element from c has been relocated        
                it_old_score!.MoveBackward(); Add_to_notification_list(it_old_score); it_old_score.MoveForward();
                it_old_score.MoveForward(); Add_to_notification_list(it_old_score);
            }
        }



        // now must sum up notificatios and pass them to another task every 5 seconds
        if (DateTime.Now > notification_dead_line)
        {
            var notify_array = notified_groups.Values.ToArray();
            var events = new List<NotifyGroup>();
            foreach (var it in notify_array)
                if (it.FirstCreated < notification_dead_line)
                {
                    notified_groups.Remove(it.Score);
                    events.Add(it);
                    by_group_notifications += it.Players.Count;
                }

            if (events.Count > 0)
            {
                noti_invokations++;
                var bytes = MemoryPackSerializer.Serialize<List<NotifyGroup>>(events);
                var t = new Task(() => MemoryPackSerializer.Deserialize<List<NotifyGroup>>(bytes));
                t.Start();
                events.Clear();
            }
            notification_dead_line = DateTime.Now.AddSeconds(1);
        }
    }

}
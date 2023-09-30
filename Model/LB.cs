using LeaderBoard.Base;

namespace LeaderBoard
{
    public class LB
    {
        private readonly List<Player> offLinePlayers = new();
        private readonly Dictionary<int, int> onDemandScores = new();
        private readonly SparseArray spa;
        private readonly Statistics stats;
        private readonly NotificationController notiController;

        public SparseArray GetArray() => spa;
        public Statistics Stats { get => stats; }

        public LB(IList<Player> list, NotificationController notif)
        {
            spa = new();
            stats = new();
            notiController = notif;
            ScoreGroup? sg;

            for (int i = 0; i < list.Count; i++)
            {
                var player = list[i];
                sg = spa[player.Score]?.CurrentItem();
                if (sg == null)
                {
                    sg = new ScoreGroup(player.Score);
                    spa.Add(sg.Score, sg);
                }

                sg.Players.Add(player.Id, player);
                sg.GroupCount++;
            }
        }
        void ask_for_actual_delegate(int score)
        {         
            stats.DBCalls++;
            Task.Run(async () =>
            {
                DB.getSamplePlayer(score, (p) =>
                {
                    lock (offLinePlayers)
                    {
                        offLinePlayers.Add(p);
                        stats.DBResponses++;
                    }
                });
            });
        }

        void CheckForVirtualGroups()
        {

            if (offLinePlayers.Count() > 0)
            {
                // remove Virtual players from the scoreGroups
                lock (offLinePlayers)
                {
                    while (offLinePlayers.Count() > 0)
                    {
                        Player p = offLinePlayers[0];
                        int score = p.Score;
                        var sg = spa[score].CurrentItem();
                        if (sg != null && sg.HasVitualUser) // 
                        {
                            sg.Players.Remove(-1);
                            sg.Players.Add(p.Id, p);
                            sg.HasVitualUser = false;
                        }
                        offLinePlayers.RemoveAll(p => p.Score == score);
                    }
                }
            }
        }
        public void Push(Player player, int new_score)
        {
            //CheckForVirtualGroups();
            stats.pushes++;
            int old_score = player.Score;
            player.Score = new_score; // for the next time

            if (new_score <= old_score)
                return;

            var it_old_score = spa[old_score];
            if (it_old_score.CurrentItem() == null) // non existent score
                return;
            
            ScoreGroup? g_new_score = null;
            /*
            // a binary search on 10000 element array needs 2^15 checks, while new_score resides in 2 or 3 indexed higher

            ITerator<ScoreGroup>? it_new_score = (it_old_score as LeaderBoardIterator).clone();
            while ((g_new_score = it_new_score.CurrentItem()) != null && g_new_score.Score < new_score)
                it_new_score.MoveForward();            

            if ((g_new_score?.Score?? -1) != new_score) // not found
            {
                g_new_score = null;
                it_new_score = null;
            }
            */

            var it_new_score = spa[new_score];
            g_new_score = it_new_score.CurrentItem();
            if (g_new_score == null)
                it_new_score = null; // unnecessary

            ScoreGroup g_old = it_old_score.CurrentItem()!;
            var dp_old_score = g_old.GetDelegate();

            int ndx = g_old.Players.IndexOfKey(player.Id);
            if (ndx >= 0) // else player is a newcomer
            {
                g_old.Players.Remove(player.Id);
                //g_old.Players.RemoveAt(ndx);
                g_old.GroupCount--;
            }

            ITerator<ScoreGroup>? removed_group_top_neighbor = null;
            bool oldRemoved = false;

            if (g_old.GroupCount == 0) // the group must be removed
            {
                notiController.Remove(old_score);
                removed_group_top_neighbor = spa.Remove(old_score); // removes score, and gets nearest higher rank
                it_old_score = null;                                // make sure it's not accidentally used
                oldRemoved = true;
                stats.removedGroups++;
            }
            else if (g_old.Players.Count == 0) // group count is bigger than zero
            {
                // approach 1: simple and at some situations even efficient
                // Player p = DB.getSamplePlayer(g_old.Score).Result;
                // g_old.Players.Add(g_old.Score, p);

                if (!g_old.HasVitualUser) //approach 2:
                {
                    // this virtual user must be replace as soon as possible, and the groups containing this user should wait to be notified
                    g_old.Players.Add(-1, new() { Id = -1, Score = g_old.Score, Name = $"User({g_old.Score}) Place Holder" });
                    g_old.HasVitualUser = true;
                    ask_for_actual_delegate(g_old.Score);
                }
            }

            if (g_new_score == null) // the group does not exist, and must be created        
            {
                ScoreGroup sg = new(new_score) { GroupCount = 1 };
                sg.Players.Add(player.Id, player);
                it_new_score = spa.Add(new_score, sg);
                stats.createdGroups++;
            }
            else
            {
                g_new_score.GroupCount++;
                g_new_score.Players.Add(player.Id, player);
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
                // if the player is among an already being notified group, they can be ignored
                if (notiController.TryGetValue(new_score, out NotifyGroup? g))
                {
                    // for instance if scoregroup 40 has been perviously registered to be notified and this is a newcomer
                    // maybe group 40 is not a complete group and only some particular members are notified, let's check if player is among them
                    if (g!.Players.IndexOfKey(player.Id) < 0)
                        g.Players.Add(player.Id, player);
                }
                else // must create a single notification for the player
                {
                    it_new_score!.MoveBackward();
                    var p1 = new { it_new_score.CurrentItem()?.GetDelegate().Name, it_new_score.CurrentItem()?.Score };
                    it_new_score.MoveForward();

                    it_new_score.MoveForward();
                    var n1 = new { it_new_score.CurrentItem()?.GetDelegate().Name, it_new_score.CurrentItem()?.Score };
                    it_new_score.MoveBackward();
                    notiController.Add(new_score, new NotifyGroup(new SortedList<int, Player> { { player.Id, player } }, p1.Name, p1?.Score, n1.Name, n1.Score, new_score));
                    notiController.singlesCounter++;
                }
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
                if (dp_old_score.Id == player.Id) // should check if the moving player has represented his old group or not
                {
                    // p2 p1 c n1 n2   : nothing is removed, an element from c has been relocated        
                    it_old_score!.MoveBackward(); Add_to_notification_list(it_old_score); it_old_score.MoveForward();
                    it_old_score.MoveForward(); Add_to_notification_list(it_old_score);
                }
            }
            // now must sum up notificatios and pass them to another task every 5 duration
            notiController.Check();
        }

        private Player GetOffLinePlayer(int score)
        {
            ask_for_actual_delegate(score);
            // wherever this player is involded, its operations must be delayed
            Player player = new() { Id = -1, Score = score };
            return player;

        }

        // used only to add groups of players
        void Add_to_notification_list(ITerator<ScoreGroup> iter)
        {
            if (iter.CurrentItem() == null)
                return;

            DateTime dt;
            var cur = new { iter.CurrentItem()!.Players, iter.CurrentItem()!.Score };
            if (notiController.TryGetValue(cur.Score, out NotifyGroup? ng)) // get the insertion time of previous instance if any
            {
                dt = ng!.FirstCreated;
                notiController.Remove(cur.Score);
            }
            else
                dt = DateTime.Now;

            iter.MoveForward(); var _next = new { iter.CurrentItem()?.GetDelegate().Name, iter.CurrentItem()?.Score }; iter.MoveBackward();
            iter.MoveBackward(); var _prev = new { iter.CurrentItem()?.GetDelegate().Name, iter.CurrentItem()?.Score }; iter.MoveForward();
            notiController.Add(cur.Score, new NotifyGroup(cur.Players, _prev.Name, _prev.Score, _next.Name, _next.Score, cur.Score, dt));
            notiController.groupedCounter++;
        }
        public ScoreGroup? GetGroup(int score)
        {
            return spa[score].CurrentItem();
        }
        public LeaderBoardIterator GetIteraror()
        {
            return spa.GetIteraror();
        }
    }
}
using LeaderBoard.Base;
using System;
using System.Collections.Generic;

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
                // replace virtual players with real users
                lock (offLinePlayers)
                {
                    while (offLinePlayers.Count() > 0)
                    {
                        Player p = offLinePlayers[0];
                        int score = p.Score;
                        var sg = spa[score].CurrentItem();
                        // the group may have been abandoned when an offline player gets inline while their data is being feteched from DB
                        if (sg != null && sg.HasVirtualUser)
                        {
                            // remove Virtual players from the scoreGroups
                            sg.Players.Remove(-1);
                            sg.Players.Add(p.Id, p);
                            sg.HasVirtualUser = false;
                        }
                        offLinePlayers.RemoveAll(p => p.Score == score);

                    }
                }
            }
        }
        public void Push(Player player, int new_score)
        {
            CheckForVirtualGroups();
            stats.pushes++;
            int old_score = player.Score;
            player.Score = new_score; // for the next time

            if (new_score <= old_score || player.Id < 0) // check against invalid data
                return;

            var it_old_score = spa[old_score];
            if (it_old_score.CurrentItem() == null) // non existent score
                return;

            ScoreGroup? g_new_score = null;

            var it_new_score = spa[new_score];
            g_new_score = it_new_score.CurrentItem();
            if (g_new_score == null) // unnecessary check
                it_new_score = null;

            ScoreGroup g_old = it_old_score.CurrentItem()!;
            var dp_old_score = g_old.GetDelegate();

            if (g_old.Players.Remove(player.Id))
                g_old.GroupCount--;

            ITerator<ScoreGroup>? removed_group_top_neighbor = null;
            bool groupAbandoned = false;

            if (g_old.GroupCount == 0) // the group must be removed
            {
                notiController.Remove(old_score);
                removed_group_top_neighbor = spa.Remove(old_score); // removes score, and gets nearest higher rank
                it_old_score = null;                                // make sure it's not accidentally used
                groupAbandoned = true;
                stats.removedGroups++;
            }
            else if (g_old.Players.Count == 0) // group count is bigger than zero
            {
                // approach 1: simple and at some situations even efficient
                // Player p = DB.getSamplePlayer(g_old.Score).Result;
                // g_old.Players.Add(g_old.Score, p);

                if (!g_old.HasVirtualUser) //approach 2:
                {
                    // this virtual user must be replace as soon as possible, and the groups containing this user should wait to be notified
                    g_old.Players.Add(-1, new() { Id = -1, Score = g_old.Score, Name = $"User({g_old.Score}) Place Holder" });
                    g_old.HasVirtualUser = true;
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
                if (g_new_score.HasVirtualUser)
                {
                    g_new_score.Players.Remove(-1);
                    g_new_score.HasVirtualUser = false;
                }
            }

            // list of score groups that must be notified 
            // p2, p1, c(left player), n1, n2, n?(arrived player)
            // if c is going to be notified then p1 and n1 must be traversed for their delegates
            // cases:
            // (1)- if c removed p1 and n1 (now c) must be notified
            // (2)- if c is inserted p2 p1 c n1 n2, p2 then n1 and p1 are going to be notified
            // (3)- if a player from c1 moves to n? and the player is the delegate of c, then p1, n1 must be notified
            // (4)- if a player relocates from c  to n?, the player must be notified
            // (5)- if a new group has not been created and the old group has been removed, all groups below old group should be notified.

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

            if (must_notify_player) // case 4
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

            if (groupAbandoned) // case 1: n1, p1 should be notified
            {
                Add_to_notification_list(removed_group_top_neighbor!); // old n1, now c
                removed_group_top_neighbor!.MoveBackward();
                Add_to_notification_list(removed_group_top_neighbor);  // p1
            }
            else // case 3
            {
                // wondering if the left user have been the representative of its owen group, then p1, n1 must be notified
                if (dp_old_score.Id == player.Id) // should check if the moving player has represented his old group or not
                {
                    // p2 p1 c n1 n2   : nothing is removed, an element from c has been relocated        
                    it_old_score!.MoveBackward(); Add_to_notification_list(it_old_score); it_old_score.MoveForward();
                    it_old_score.MoveForward(); Add_to_notification_list(it_old_score);
                }
            }
            // send notifications
            notiController.Check();
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

            var pickItem = (ITerator<ScoreGroup> sg) =>
            {
                var cur = iter.CurrentItem();
                return new { cur?.GetDelegate().Name, cur?.Score };
            };

            iter.MoveForward(); var _next = pickItem(iter); iter.MoveBackward();
            iter.MoveBackward(); var _prev = pickItem(iter); iter.MoveForward();

            //todo: groups that have HasVirtualUser must be marked
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
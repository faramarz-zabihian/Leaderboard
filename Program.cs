using LeaderBoard.Base;

const int NUM_ONLINE_PLAYERS = 1000000;
const int NUM_SCORES = 10000;
const int MAX_GAINED_SCORE_PER_PLAY = 12;

SortedDictionary<int, NotifyGroup> notified_groups = new SortedDictionary<int, NotifyGroup>();

Random randomId = new Random();
Random rnd = new Random(1000);
SparseArray spa = new SparseArray();

DateTime start = DateTime.Now;
for (int i = 1; i < NUM_ONLINE_PLAYERS; i++) // one million playes
{
    var score = rnd.Next(NUM_SCORES);
    var sg = spa[score].CurrentItem();
    if (sg == null)
    {
        var player = getPlayer(i);
        player.Score = score;
        sg = new ScoreGroup(score) { GroupCount = 1 };
        spa.Add(score, sg);
        sg.Players.Add(player);
    }
    else
    {
        sg.Players.Add(getPlayer(i));
        sg.GroupCount++;
    }
}

var end = DateTime.Now;
var ts = end - start;
Console.WriteLine(ts.ToString());

start = end;
// create a new random sequence
rnd = new Random(400);

int newItems = 0;
int removedCount = 0;
int updateCount = 0;
int removePlayers = 0;

Console.WriteLine($"start spa with:{spa.Length}");
var exit_task = false;
Task t = Task.Run(() => { NotifyRunner(); });


for (int i = 0; i < 1000000; i++)
{
    // choose an arbitrary ScoreGroup
    var sg = spa[rnd.Next(spa.Length)].CurrentItem();
    if (sg== null)
        continue;
    var player = sg.Players[0];
    // re rank the player
    newScore(player.Id, sg.Score, sg.Score + rnd.Next(MAX_GAINED_SCORE_PER_PLAY) + 1);
}
end = DateTime.Now;
ts = end - start;
Console.WriteLine(ts.ToString());
Console.WriteLine();
Console.WriteLine($"new Items: {newItems}");
Console.WriteLine($"removed {removedCount}");
Console.WriteLine($"updates : {updateCount}");
Console.WriteLine($"remove players : {removePlayers}");
Console.WriteLine($"spa:{spa.Length}");
Console.ReadKey();

void newScore(int id, int old_score, int new_score)
{
    Iterator<ScoreGroup>? newScoreIterator = null;

    var tf_old_score = spa[old_score];    
    var new_score_ScoreGroup = spa[new_score].CurrentItem();
    
    ScoreGroup currGroup = tf_old_score.CurrentItem()!;
    var old_score_delegate_player = currGroup.getDelegate();

    Player? player = null;
    // note: SortedList or SortedDictionary are faster in this respect, but they are very slow on ToList()
    for (int ndx = 0; ndx < currGroup.Players.Count; ndx++)
        if (currGroup.Players[ndx].Id == id)
        {
            player = currGroup.Players![ndx];
            currGroup.Players.RemoveRange(ndx, 1);
            currGroup.GroupCount--;
            removePlayers++;
            break;
        }
    if (player == null)
        return;

    Iterator<ScoreGroup>? removed_group_top_neighbor = null;
    bool oldRemoved = false;

    if (currGroup.GroupCount == 0) // the group must be removed
    {
        removed_group_top_neighbor = spa.Remove(old_score); // removes score, and gets nearest higher rank
        tf_old_score = null;                                // make sure it's not accidentally used
        oldRemoved = true;
        removedCount++;
    }
    else if (currGroup.Players.Count == 0) // group count is bigger than zero
    {
        Player dummyPlayer = new Player { Id = -getRandomId(), Fake = true };
        currGroup.Players.Add(dummyPlayer);
        /*        Task.Run(async () => {
                    var p = getPlayerName(score);
                    dummyPlayer.Id = p.Id;
                    dummyPlayer.Name = p.Name;
                    dummyPlayer.Fake = false;
                    // Notifier can wait until Fake has been reset
                    }
                );*/
    }
    if (new_score_ScoreGroup == null) // the group does not exist, and must be created        
    {
        player = getPlayer(id);

        ScoreGroup sg = new ScoreGroup(new_score) { GroupCount = 1 };
        sg.Players.Add(player);
        newScoreIterator = spa.Add(new_score, sg);
        newItems++;
    }
    else
    {
        new_score_ScoreGroup.GroupCount++;
        new_score_ScoreGroup.Players.Add(player);
        updateCount++;
    }
    // list of score groups that must be notified 
    // p2, p1, c, n1, n2
    // if group c is going to be notified then p1 and n1 must be checked
    // if c removed then n1 becomes current, p1 and n1 must be notified
    // if c is inserted p2 p1 c n1 n2, p2 then n1 and p1 are going to be notified

    notified_groups.Clear(); 

    if (newScoreIterator != null) // The group is new, its new neighbors are definitly affected
    {     
        newScoreIterator.MoveForward();                
        add_to_notification_list(newScoreIterator); // n1
        newScoreIterator.MoveBackward();       
        newScoreIterator.MoveBackward();
        add_to_notification_list(newScoreIterator); // p1
        newScoreIterator.MoveForward();
        add_to_notification_list(newScoreIterator); // c
    }
    
    if (oldRemoved) // n1, p1 should be notified
    {
        add_to_notification_list(removed_group_top_neighbor!); // n1
        removed_group_top_neighbor!.MoveBackward(); 
        add_to_notification_list(removed_group_top_neighbor);  // p1
    }
    else
    {
        // wondering if the left user have been the representative of its owen group, then p1, n1 must be notified
        if (old_score_delegate_player.Id == id) // should check if the moving player has represented his old group or not
        {
            // p2 p1 c n1 n2   : nothing is removed, an element from c has been relocated        
            tf_old_score!.MoveBackward();  add_to_notification_list(tf_old_score);tf_old_score.MoveForward();
            tf_old_score.MoveForward(); add_to_notification_list(tf_old_score);
        }
    }
    // now must sum up notificatios and pass them to its handler
    /*foreach(var it in notified_groups) {
            var p = it.Value.Players.ToArray();
            
            
    }*/
}
void NotifyRunner()
{
    while(!exit_task)
    {

    }
}

void add_to_notification_list(Iterator<ScoreGroup>  iter) {
    ScoreGroup? sg = iter.CurrentItem();
    if (sg != null && !notified_groups.ContainsKey(sg.Score))
    {
        iter.MoveForward(); 
        var _next = iter.CurrentItem(); 
        iter.MoveBackward();

        iter.MoveBackward(); 
        var _prev = iter.CurrentItem(); 
        iter.MoveForward();
        notified_groups.Add(
            sg.Score, 
            new NotifyGroup(sg.Players, _prev?.getDelegate(), _next?.getDelegate(), sg.Score)
        );
    }
};
int getRandomId()
{
    return randomId.Next(999999999);
}
Player getPlayerFromDB(int score)
{
    //Thread.Sleep( 1000 );
    //todo: implement database access    
    int id = 200000 + getRandomId();
    return new Player { Id = id, Name = "Name:" + id.ToString(), Score = score };
}
Player getPlayer(int id)
{
    return new Player { Id = id };
}

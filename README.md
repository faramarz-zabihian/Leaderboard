# **Some thoughts over a leaderboard problem**

##

## **Assumptions:**

- For simplicity, it is assumed that there is a one-to-one relationship between scores and ranks. Therefore, many players may share the same rank. The solution could be justified according to the new presumptions.
- The scores and ranks are unbounded.
- The number of users is over 500 million. (%4 of world population!)
- The number of online users is over 50 million.
- All users play at least once a day (on average for 20 min.), therefore 5000 players per second enter and leave the game.
- The number of transactions per seconds is over one million.
- There is no negative score.

## Points to consider:

- Player's total score is gradually incrementing; therefore, the new rank of the user is in its vicinity.
- Total score is always incremental.
- Rank of a player, is a calculated entity based on its score.
- Only active players change the scores.

## Overview of the solution

The general idea is to emulate a minimal and yet reliable representation of the actual database in memory in such a way that:

- Calculations are all done in memory.
- Only changed records are replicated to persistence layer.
- Only active users are in the memory.

## The proposed solution's advantages:

- Calls to the persistence layer are limited to the very minimum necessary (~ number of events/sec) and its updates are accomplished in a "Fire and Forget" manner (non-blocking).
- The solution is not dependent on the choice of data persistence or notification pushing mechanism.
- In-memory lookups are optimal (due to the primitive key type of sparse array and the meager number of players per score)
- Due to the optimized structure of In-Memory database, operations per event are minimized.
- Notification spamming is actively prevented.
- Memory and persistence layer can be segmented; hence, the operations can split across multiple servers, overcoming resource limitations (memory, CPU clock).

## Implementation:

An internally linked and sorted sparse-array which can be accessed by its index is the core of the solution. Every index represents a single score and contains a structure which contains a list of online users with the same score. This score-group also includes the total number of users on that group (online and offline), an online or offline user representing that group. Any score event causes a player to either move to an existing group or new one and leaving its own group either perishing or still populated.

classScoreGroup

{

publicScoreGroup(int score)

{

this.Score = score;

}up

publicint Score { get; }

public Player? Representative = null;

public SortedDictionary\<string, Player\> Players;

publicint GroupCount;

}

This implementation requires that the game event include the player's total score prior to the game. If not applicable, a sorted set can be introduced to lookup player's last total score as soon as an event is received.

**Event Processor:**

Consumes the events and steers the process:

1. Lookup the old score, remove player from that group
2. Lookup the new score, if not exists, create a new one, and add the player
3. Look for the groups that must be notified (those adjacent to old score group and new score group)

Notes:

if a group has no online player but its counter is above zero, a sample user of that score from the database should be retrieved to represent the group.

If a group's counter reaches zero, it's eliminated from the array

If removal of a player perishes the group and does not create a new score group, all ranks below the leaving group should be notified.

Every score update will be reported to persistence layer in a non-blocking manner.

Whenever needed, a new dummy sample user will be created and its update will be delegated to itself, in a way that current execution does not get blocked.

The underlying premise for efficiency is that:

however wide the score range may be, very soon each group will contain at least 2 online members, and the whole operations circles around moving one player from one group to another one.

Ranks are automatically created on the memory and need not to be calculated or updated.

Score increases are so meager that finding new score place by linear search is much more efficient than a binary one.

At system load, a complete list of distinct scores and ranks will be loaded into memory with at least one arbitrary player for each score. By the assumptions of the problem, this may amount to 50000 thousand distinct scores. The table for such a data can be upkept in persistence layer. So, the load would be instantaneous. In the worst case all 500M users play daily, 5000 new online join and leave the game per second which is not comparable to 1 million score change per second. So the expected hit on the persistent layer would be around 1 million fire & forget updates.

With regard to the notification mechanism, the service is asynchronously informed of rank update events.

Potential Issues :

- Database commands require a sequential order of execution. The persistence layer should take charge as to ensure no data is retrieved before the prior updates are sequentially executed or the updates do not have interference.
- To prevent the potential notification spamming issue, the respective service could use fixed intervals or other arrangements to forward only the most recent event for each online player.

Parallel tasks to implement

1. GameEvent Ingestion service to collect game events & feed them to the "GameEvent Processor"
2. GameEvent Processor service as explained above
3. Persistence layer service
4. Notification Service

The persistent layer can benefit from execution of most database operations on an emulated database. For example, a particular score update request, may also include the new rank of the player, and the range of affected players.

Furthermore, certain calculations on the database may benefit from pre-calculated queries, such as upkeeping a table for each score, rank, count of players on that score, and an arbitrary player ID of the score.

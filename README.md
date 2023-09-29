## Welcome to my Leaderboard project.

Leaderboard assumptions:

- Al players with the same score share the same rank.
- There is no limit of total scores and lowest rank.
- There is no negative score.

### Description of the solution:

The general idea is to emulate a minimal representation of the actual database, with these characteristics:
r
- Calculations are done in memory.
- Only changed records are replicated to DB.
- Only active users are in the memory.

These points needed more consideration:

- Player's total score is gradually incrementing; therefore, the new rank of the user is in its vicinity.
- Total score is always incremental (No zero, No negativity).
- Rank of a player, is a calculated entirely based on position of its score among others.
- Only active players change the scores.
- Application could not wait for the physical DB to respond. And the choice of the DB is left to the service provider.
- Since the number of users and the number of transactions per seconds would be overwhelming, solution had to be splittable over multiple servers or processors.
- Players must be notified about their rank change, and spamming should be avoided.

### Implementation:

An internally linked and sorted sparse-array that can be accessed by its index is the core of the solution. Players are grouped by their scores and kept within this array.

Every index in the sparse-array represents a single score and rank and contains a structure which contains a list of online users with the same score. This score-group also includes the total number of users on that group (online and offline), an online or offline user representing that group.

Any score event causes a player to either move to a pre-existing group or new one and leaving its own group still populated or abandoned.

A login requires player's numeric id, string id, last score, and name be part of the event. At initial Load, the sparse array should be filled with all existing score groups, their user base count and a sample player for that score

Here is the proofread version:

An internally linked and sorted sparse-array that can be accessed by its index is the core of the solution. Players are grouped by their scores and kept within this array. Every element in the sparse-array represents a single score and rank group which contains a list of online players with the same score. Each group also includes the total number of users in that group (online and offline) and a user who represents that group.

Any score event causes a player to move to either a pre-existing group or a new one while leaving its own group still populated or abandoned. A user login requires the player's numeric id, string id, last score, and player's name to be part of the event. At the initial load, the sparse array should be filled with all existing score groups, their user base count, and a sample player for that score.

**Steps:**

1. Lookup the last score, remove player from that group
2. Lookup the new score, or create a new one, and add the player
3. Look for the score groups that must be notified

**Notes:**

- if a group has no online player but its group counter is above zero, a sample user of that score should be retrieved to represent the group.
- If a group's counter reaches zero, it's eliminated from the array
- If the removal of a player eliminates the group and does not create a new one, all ranks below the leaving group can be notified selectively.
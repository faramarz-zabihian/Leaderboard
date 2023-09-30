// See https://aka.ms/new-console-template for more information
const int SCORE_RANGE = 10000;
const int MAX_GAINED_SCORE_PER_PLAY = 12;
const int NUM_ONLINE_PLAYERS = 1000000;
const int NOTIFY_INTERVAL = 3000;

LeaderBoard.Utility.Runner rlb = new(SCORE_RANGE, MAX_GAINED_SCORE_PER_PLAY, NUM_ONLINE_PLAYERS, NOTIFY_INTERVAL * 2, null);
rlb.Perform(10);

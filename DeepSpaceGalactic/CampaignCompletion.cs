using System;

namespace DeepSpaceGalactic
{
    public sealed class CampaignCompletion
    {
        public int TimeTaken { get; }
        public int Score { get; }
        public int CompletedLevel { get; }
        public DateTimeOffset Date { get; }

        public CampaignCompletion(int timeTaken, int score, int completedLevel, DateTimeOffset date)
        {
            TimeTaken = timeTaken;
            Score = score;
            CompletedLevel = completedLevel;
            Date = date;
        }
    }
}

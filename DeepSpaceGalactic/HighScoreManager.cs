using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace DeepSpaceGalactic
{
    public class HighScoreManager
    {
        //stores the campaign file name and leaderboard limit
        private const string CampaignHighScoresFileName = "campaign_highscores.txt";
        private const int MaximumEntries = 10;
        private readonly List<HighScoreEntry> campaignEntries = new List<HighScoreEntry>();

        //provides access to the current leaderboard and last error
        public IList<HighScoreEntry> CampaignEntries => campaignEntries;
        public string LastErrorMessage { get; private set; }

        public async Task LoadCampaignScoresAsync()
        {
            campaignEntries.Clear();
            LastErrorMessage = null;

            try
            {
                //opens or creates the file in the apps local storage
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync(CampaignHighScoresFileName, CreationCollisionOption.OpenIfExists);
                IList<string> lines = await FileIO.ReadLinesAsync(file);

                //converts valid lines into entries and skips invalid records
                foreach (string line in lines)
                {
                    if (HighScoreEntry.TryParse(line, out HighScoreEntry entry))
                        campaignEntries.Add(entry);
                }

                SortAndLimitEntries();
            }
            catch (Exception)
            {
                LastErrorMessage = "Campaign high scores could not be loaded.";
            }
        }

        public bool QualifiesForCampaignLeaderboard(int timeTaken)
        {
            if (timeTaken < 0)
                return false;

            //a result qualifies if there is room or it beats the slowest saved time
            return campaignEntries.Count < MaximumEntries || timeTaken < campaignEntries[MaximumEntries - 1].TimeTaken;
        }

        public async Task<int> AddCampaignEntryAsync(HighScoreEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            //checks the entry before adding it to the leaderboard
            if (!HighScoreEntry.IsValidName(entry.Name)
                || entry.TimeTaken < 0
                || entry.Score < 0
                || entry.CompletedLevel < 1)
            {
                throw new ArgumentException("The campaign high score entry is invalid.", nameof(entry));
            }

            campaignEntries.Add(entry);
            SortAndLimitEntries();
            int rank = campaignEntries.IndexOf(entry);
            LastErrorMessage = null;

            try
            {
                await SaveCampaignScoresAsync();
            }
            catch (Exception)
            {
                LastErrorMessage = "Your campaign result was added, but the high score file could not be saved.";
            }

            return rank;
        }

        private async Task SaveCampaignScoresAsync()
        {
            //rewrites the file with the complete current leaderboard
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync(CampaignHighScoresFileName, CreationCollisionOption.OpenIfExists);
            string completeText = string.Join(Environment.NewLine, campaignEntries.Select(entry => entry.GetDelimitedLine()));
            await FileIO.WriteTextAsync(file, completeText);
        }

        private void SortAndLimitEntries()
        {
            //sorts the fastest times first and removes entries beyond the top ten
            campaignEntries.Sort(CompareEntries);

            if (campaignEntries.Count > MaximumEntries)
                campaignEntries.RemoveRange(MaximumEntries, campaignEntries.Count - MaximumEntries);
        }

        private static int CompareEntries(HighScoreEntry first, HighScoreEntry second)
        {
            //uses score and date to break ties when completion times match
            int timeComparison = first.TimeTaken.CompareTo(second.TimeTaken);
            if (timeComparison != 0)
                return timeComparison;

            int scoreComparison = second.Score.CompareTo(first.Score);
            if (scoreComparison != 0)
                return scoreComparison;

            return first.Date.CompareTo(second.Date);
        }
    }
}
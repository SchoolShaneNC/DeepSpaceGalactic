using System;
using System.Globalization;

namespace DeepSpaceGalactic
{
    public sealed class HighScoreEntry
    {
        private string name;
        private int timeTaken;
        private int score;
        private int completedLevel;
        private DateTimeOffset date;

        public string Name
        {
            get { return name; }
            set
            {
                if (IsValidName(value))
                    name = value.Trim();
                else
                    name = "Unknown";
            }
        }

        public int TimeTaken
        {
            get { return timeTaken; }
            set
            {
                if (value < 0)
                    timeTaken = int.MaxValue; //defaulting to 0 would be a glitch to rig the leaderboard so it goes to int.max
                else
                    timeTaken = value;
            }
        }

        public int Score
        {
            get { return score; }
            set
            {
                if (value < 0)
                    score = 0;
                else
                    score = value;
            }
        }

        public int CompletedLevel
        {
            get { return completedLevel; }
            set
            {
                if (value < 1)
                    completedLevel = 1;
                else
                    completedLevel = value;
            }
        }

        public DateTimeOffset Date
        {
            get { return date; }
            set
            {
                if (value == default(DateTimeOffset))
                    date = DateTimeOffset.Now;
                else
                    date = value;
            }
        }

        public HighScoreEntry(string name, int timeTaken, int score, int completedLevel, DateTimeOffset date)
        {
            Name = name;
            TimeTaken = timeTaken;
            Score = score;
            CompletedLevel = completedLevel;
            Date = date;
        }

        public string GetDelimitedLine()
        {
            //returns the set line for the fileIO to keep it consistant
            //cultrueInfo keeps datetime formatted all the same no matter users computer
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4:yyyy-MM-dd}", Name, TimeTaken, Score, CompletedLevel, Date);
        }

        //tries to convert a saved text line into a high score entry
        public static bool TryParse(string line, out HighScoreEntry entry)
        {
            //sets the output to null until a valid entry is created
            entry = null;

            //rejects empty or whitespace lines
            if (string.IsNullOrWhiteSpace(line))
                return false;

            //splits the line into its five saved fields
            string[] fields = line.Split(':');
            if (fields.Length != 5)
                return false;

            //removes extra whitespace from the player name
            string name = fields[0].Trim();

            //validates each field and checks that numbers and dates can be parsed
            //InvariantCulture keeps numeric formatting consistent across user computer settings
            //the date must match the yyyy-MM-dd format used when saving the entry
            if (!IsValidName(name)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeTaken)
                || !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int score)
                || !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int completedLevel)
                || !DateTimeOffset.TryParseExact(fields[4], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset date)
                || timeTaken < 0
                || score < 0
                || completedLevel < 1)
            {
                return false;
            }

            //creates the entry after all fields pass validation
            entry = new HighScoreEntry(name, timeTaken, score, completedLevel, date);
            return true;
        }
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && name.Trim().Length <= 24
                && name.IndexOf(':') < 0
                && name.IndexOfAny(new[] { '\r', '\n' }) < 0;
        }
    }
}

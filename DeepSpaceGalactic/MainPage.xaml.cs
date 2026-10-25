using System;
using System.Text;
using System.Threading.Tasks;
using DSG_Library;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DeepSpaceGalactic
{
    public sealed partial class MainPage : Page
    {
        private readonly GameSession gameSession;
        private readonly HighScoreManager highScoreManager;
        private Task loadCampaignScoresTask;

        public MainPage()
        {
            InitializeComponent();

            gameSession = new GameSession();
            highScoreManager = new HighScoreManager();
            gameSession.ElementAdded += GameSession_ElementAdded;
            gameSession.ElementRemoved += GameSession_ElementRemoved;
            gameSession.BoardCleared += GameSession_BoardCleared;
            gameSession.PlayerCreated += GameSession_PlayerCreated;
            gameSession.LevelChanged += GameSession_LevelChanged;
            gameSession.Ended += GameSession_Ended;
            gameSession.CampaignCompleted += GameSession_CampaignCompleted;

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            MainGrid.SizeChanged += MainGrid_SizeChanged;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            gameSession.SetBoardSize(MainGrid.ActualWidth, MainGrid.ActualHeight);
            await EnsureCampaignScoresLoadedAsync();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
            gameSession.Dispose();
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            gameSession.SetBoardSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            gameSession.HandleKeyDown(e.VirtualKey);
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            gameSession.HandleKeyUp(e.VirtualKey);
        }

        private void GameSession_ElementAdded(UIElement element)
        {
            MainGrid.Children.Add(element);
        }

        private void GameSession_ElementRemoved(UIElement element)
        {
            MainGrid.Children.Remove(element);
        }

        private void GameSession_BoardCleared()
        {
            MainGrid.Children.Clear();
            HudGrid.DataContext = null;
        }

        private void GameSession_PlayerCreated(Player player)
        {
            HudGrid.DataContext = player;
        }

        private void GameSession_LevelChanged(int level)
        {
            LevelTextBlock.Text = $"Level: {level}";
        }

        private void GameSession_Ended(string title)
        {
            EndGameTitleTextBlock.Text = title;
            GameOverOverlayGrid.Visibility = Visibility.Visible;
        }

        private async void GameSession_CampaignCompleted(CampaignCompletion completion)
        {
            await EnsureCampaignScoresLoadedAsync();

            if (!highScoreManager.QualifiesForCampaignLeaderboard(completion.TimeTaken))
            {
                EndGameFeedbackTextBlock.Text = "Campaign complete. Your time did not reach the top 10.";
                return;
            }

            TextBox nameTextBox = new TextBox
            {
                PlaceholderText = "Pilot name",
                MaxLength = 24
            };
            ContentDialog nameDialog = new ContentDialog
            {
                Title = "Top 10 campaign time",
                Content = nameTextBox,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel"
            };

            ContentDialogResult result = await nameDialog.ShowAsync();
            string name = nameTextBox.Text == null ? string.Empty : nameTextBox.Text.Trim();

            if (result != ContentDialogResult.Primary)
            {
                EndGameFeedbackTextBlock.Text = "Campaign complete. Leaderboard entry canceled.";
                return;
            }

            if (!HighScoreEntry.IsValidName(name))
            {
                EndGameFeedbackTextBlock.Text = "Enter a name from 1 to 24 characters without colons to save a leaderboard result.";
                return;
            }

            int rank = await highScoreManager.AddCampaignEntryAsync(new HighScoreEntry(
                name,
                completion.TimeTaken,
                completion.Score,
                completion.CompletedLevel,
                completion.Date));
            UpdateLeaderboardDisplays();

            if (rank == 0)
                EndGameFeedbackTextBlock.Text = "Fastest campaign time! Your result is #1.";
            else
                EndGameFeedbackTextBlock.Text = "Your campaign result was added to the leaderboard.";

            if (!string.IsNullOrEmpty(highScoreManager.LastErrorMessage))
                EndGameFeedbackTextBlock.Text = highScoreManager.LastErrorMessage;
        }

        private async void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            await EnsureCampaignScoresLoadedAsync();
            ShowPlayingView();
            gameSession.Start();
        }

        private async void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await EnsureCampaignScoresLoadedAsync();
            ShowPlayingView();
            gameSession.Start();
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            gameSession.ReturnToMenu();
            GameOverOverlayGrid.Visibility = Visibility.Collapsed;
            PlayingGrid.Visibility = Visibility.Collapsed;
            MainMenuGrid.Visibility = Visibility.Visible;
        }

        private void ShowPlayingView()
        {
            MainMenuGrid.Visibility = Visibility.Collapsed;
            GameOverOverlayGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
            PlayingGrid.Visibility = Visibility.Visible;
            EndGameFeedbackTextBlock.Text = string.Empty;
        }

        private async Task EnsureCampaignScoresLoadedAsync()
        {
            if (loadCampaignScoresTask == null)
                loadCampaignScoresTask = highScoreManager.LoadCampaignScoresAsync();

            await loadCampaignScoresTask;
            UpdateLeaderboardDisplays();
        }

        private void UpdateLeaderboardDisplays()
        {
            StringBuilder leaderboardText = new StringBuilder();

            if (highScoreManager.CampaignEntries.Count == 0)
            {
                leaderboardText.Append("No completed campaigns yet.");
            }
            else
            {
                for (int index = 0; index < highScoreManager.CampaignEntries.Count; index++)
                {
                    HighScoreEntry entry = highScoreManager.CampaignEntries[index];
                    leaderboardText.AppendFormat("{0}. {1}  {2}s  Score {3}  Level {4}  {5:yyyy-MM-dd}",
                        index + 1,
                        entry.Name,
                        entry.TimeTaken,
                        entry.Score,
                        entry.CompletedLevel,
                        entry.Date);

                    if (index < highScoreManager.CampaignEntries.Count - 1)
                        leaderboardText.AppendLine();
                }
            }

            CampaignLeaderboardTextBlock.Text = leaderboardText.ToString();
            EndGameLeaderboardTextBlock.Text = leaderboardText.ToString();
            LeaderboardStatusTextBlock.Text = highScoreManager.LastErrorMessage ?? string.Empty;
        }
    }
}

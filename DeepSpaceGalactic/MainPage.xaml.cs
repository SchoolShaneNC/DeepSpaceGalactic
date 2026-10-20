using System;
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

        public MainPage()
        {
            InitializeComponent();

            gameSession = new GameSession();
            gameSession.ElementAdded += GameSession_ElementAdded;
            gameSession.ElementRemoved += GameSession_ElementRemoved;
            gameSession.BoardCleared += GameSession_BoardCleared;
            gameSession.PlayerCreated += GameSession_PlayerCreated;
            gameSession.LevelChanged += GameSession_LevelChanged;
            gameSession.Ended += GameSession_Ended;

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            MainGrid.SizeChanged += MainGrid_SizeChanged;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            gameSession.SetBoardSize(MainGrid.ActualWidth, MainGrid.ActualHeight);
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

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPlayingView();
            gameSession.Start();
        }

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
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
        }
    }
}

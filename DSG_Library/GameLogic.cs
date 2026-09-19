using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace DSG_Library
{
    public static class GameLogic
    {
        public static GamePiece CreatePiece(string imgSrc, int size, int left, int top)
        {
            string imageName = "Img" + char.ToUpper(imgSrc[0]) + imgSrc.Remove(imgSrc.IndexOf('.')).Substring(1);

            Image img = new Image
            {
                Source = new BitmapImage(new Uri($"ms-appx:///Assets/SpaceShips/{imgSrc}")),
                Width = size,
                Height = size,
                Name = imageName,
                Margin = new Thickness(left, top, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            return new GamePiece(img);
        }
        public static bool IsCollision(GamePiece piece1, GamePiece piece2)
        {
            double piece1Left = piece1.Position.Left;
            double piece1Top = piece1.Position.Top;
            double piece2Left = piece2.Position.Left;
            double piece2Top = piece2.Position.Top;

            return piece1Left < piece2Left + piece2.Img.Width
                && piece1Left + piece1.Img.Width > piece2Left
                && piece1Top < piece2Top + piece2.Img.Height
                && piece1Top + piece1.Img.Height > piece2Top;
        }

    }
    

}

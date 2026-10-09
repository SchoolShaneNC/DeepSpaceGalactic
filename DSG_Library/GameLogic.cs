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
        //another contructor for creating a game piece with a specific size
        public static GamePiece CreatePiece(string imgSrc, int size, int left, int top)
        {
            GamePiece piece = CreatePiece(imgSrc, left, top);
            piece.Img.Width = size;
            piece.Img.Height = size;
            return piece;
        }

        public static GamePiece CreatePiece(string imgSrc, int left, int top)
        {
            string fileName = imgSrc.Substring(imgSrc.LastIndexOf('/') + 1);
            string imageName = "Img" + char.ToUpper(fileName[0])
                + fileName.Remove(fileName.IndexOf('.')).Substring(1);

            Image img = new Image
            {
                Source = new BitmapImage(new Uri($"ms-appx:///Assets/{imgSrc}")),
                Name = imageName,
                Margin = new Thickness(left, top, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            return new GamePiece(img);
        }

        //where base collision is kept
        public static bool IsCollision(GamePiece piece1, GamePiece piece2)
        {
            //null validation
            if (piece1 == null || piece2 == null || piece1.Img == null || piece2.Img == null)
            {
                return false;
            }
            //gets hight and width for both game pieces being checked
            double piece1Width = GetImageDimension(piece1.Img.Width, piece1.Img.ActualWidth);

            double piece1Height = GetImageDimension(piece1.Img.Height, piece1.Img.ActualHeight);

            double piece2Width = GetImageDimension(piece2.Img.Width, piece2.Img.ActualWidth);

            double piece2Height = GetImageDimension(piece2.Img.Height, piece2.Img.ActualHeight);

            //validation that the pieces have proper sizes
            if (piece1Width <= 0 || piece1Height <= 0 || piece2Width <= 0 || piece2Height <= 0)
            {
                return false;
            }

            //gets the top bottom and side positions of each game piece to do the check
            double piece1Left = piece1.Position.Left;
            double piece1Right = piece1Left + piece1Width;
            double piece1Top = piece1.Position.Top;
            double piece1Bottom = piece1Top + piece1Height;

            double piece2Left = piece2.Position.Left;
            double piece2Right = piece2Left + piece2Width;
            double piece2Top = piece2.Position.Top;
            double piece2Bottom = piece2Top + piece2Height;

            //if they lap horizontally and vertically then collision is detected and it returns true 
            return piece1Left < piece2Right && piece1Right > piece2Left && piece1Top < piece2Bottom && piece1Bottom > piece2Top;
        }

        //simply for nan checks if a piece isnt fully loaded in the xaml or not
        private static double GetImageDimension(double specifiedDimension, double actualDimension)
        {
            return !double.IsNaN(specifiedDimension) && specifiedDimension > 0 ? specifiedDimension : actualDimension;
        }

    }
    

}

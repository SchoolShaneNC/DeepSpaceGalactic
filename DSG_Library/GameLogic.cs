using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace DSG_Library
{
    public static class GameLogic
    {
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
        public static bool IsCollision(GamePiece piece1, GamePiece piece2)
        {
            if (piece1 == null || piece2 == null || piece1.Img == null || piece2.Img == null)
            {
                return false;
            }

            double piece1Width = GetImageDimension(piece1.Img.Width, piece1.Img.ActualWidth);
            double piece1Height = GetImageDimension(piece1.Img.Height, piece1.Img.ActualHeight);
            double piece2Width = GetImageDimension(piece2.Img.Width, piece2.Img.ActualWidth);
            double piece2Height = GetImageDimension(piece2.Img.Height, piece2.Img.ActualHeight);

            if (piece1Width <= 0 || piece1Height <= 0 || piece2Width <= 0 || piece2Height <= 0)
            {
                return false;
            }

            Rect piece1Bounds = new Rect(piece1.Position.Left, piece1.Position.Top, piece1Width, piece1Height);
            Rect piece2Bounds = new Rect(piece2.Position.Left, piece2.Position.Top, piece2Width, piece2Height);

          //  return piece1Bounds.IntersectsWith(piece2Bounds);
            return piece1Bounds.Left < piece2Bounds.Right &&
                piece1Bounds.Right > piece2Bounds.Left &&
                piece1Bounds.Top < piece2Bounds.Bottom &&
                piece1Bounds.Bottom > piece2Bounds.Top;
        }

        private static double GetImageDimension(double specifiedDimension, double actualDimension)
        {
            return !double.IsNaN(specifiedDimension) && specifiedDimension > 0
                ? specifiedDimension
                : actualDimension;
        }

    }
    

}

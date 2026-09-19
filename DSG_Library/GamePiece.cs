using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class GamePiece
    {
        private Thickness objectMargins;                    //represents the location of the piece on the game board
        private Image onScreen;                             //the image that is displayed on screen

        public Image Img => onScreen;                       //access to the gamepiece's visual representation on screen (the Image)           
        public Thickness Position => onScreen.Margin;       //get access to the Image's top left position - can not directly modify the location of the piece

        public GamePiece(Image img)                 //constructor creates a piece and a reference to its associated image
        {                                           //use this to set up other GamePiece properties
            onScreen = img;
            objectMargins = img.Margin;
        }

        public bool Move(double horizontalDistance, double verticalDistance)
        {
            if (horizontalDistance == 0 && verticalDistance == 0)
            {
                return false;
            }

            objectMargins.Left += horizontalDistance;
            objectMargins.Top += verticalDistance;
            onScreen.Margin = objectMargins;
            return true;
        }

        public bool Move(Windows.System.VirtualKey direction)   //calculate a new location for the piece, based on a key press
        {
            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    return Move(0, -10);
                case Windows.System.VirtualKey.Down:
                    return Move(0, 10);
                case Windows.System.VirtualKey.Left:
                    return Move(-10, 0);
                case Windows.System.VirtualKey.Right:
                    return Move(10, 0);
                default:
                    return false;
        }
    }

    }
    

}

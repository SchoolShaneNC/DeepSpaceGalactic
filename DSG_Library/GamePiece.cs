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

        public bool Move(Windows.System.VirtualKey direction)   //calculate a new location for the piece, based on a key press
        {
            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    objectMargins.Top -= 10;
                    break;
                case Windows.System.VirtualKey.Down:
                    objectMargins.Top += 10;
                    break;
                case Windows.System.VirtualKey.Left:
                    objectMargins.Left -= 10;
                    break;
                case Windows.System.VirtualKey.Right:
                    objectMargins.Left += 10;
                    break;
                default:
                    return false;
            }
            onScreen.Margin = objectMargins;            //assign the new position to the on-screen image
            return true;
        }
    }
    

}

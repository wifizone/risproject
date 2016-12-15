using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeLabDispetcher
{
    class Game
    {
        string name = "", player1 = "", player2 = "";

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Player1
        {
            get { return player1; }
            set { player1 = value; }
        }
        public string Player2
        {
            get { return player2; }
            set { player2 = value; }
        }
    }
}

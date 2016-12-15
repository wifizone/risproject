using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeLabServer
{
    class GameLogic
    {
        string field = "";
        string gameNum = "";

        public string Field
        {
            get { return field; }
            set { field = value; }
        }
        public string GameNum
        {
            get { return gameNum; }
            set { gameNum = value; }
        }
        public string generateField(string player1, string player2)
        {
            string field = "";
            field += player1 + ": 0/";
            field += player2 + ": 0/";
            int m = 0;
            Random rn = new Random();
            for (int i = 1; i < 6; i++)
                for (int j = 1; j < 6; j++)
                {
                    m = rn.Next(0, 2);
                    if (m.Equals(0))
                    {
                        field += i.ToString() + j.ToString() + 0 + 0 + "/";
                    }
                    else
                    {
                        field += i.ToString() + j.ToString() + 1 + 0 + "/";
                    }
                }
            return field;
        }

        public void WriteInDoc(string field, string gameNum)
        {
            StreamWriter sw = new StreamWriter(File.OpenWrite(gameNum + ".txt"));
            sw.WriteLine(field);
            sw.Close();
        }

        public string ReadDoc(string gameNum)
        {
            StreamReader sr = new StreamReader(File.OpenRead("game" + gameNum + ".txt"));
            string field = sr.ReadToEnd();
            sr.Close();
            return field;
        }
    }
}

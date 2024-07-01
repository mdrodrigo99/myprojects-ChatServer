using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows_Forms_CORE_CHAT_UGH;


//https://www.youtube.com/watch?v=xgLRe7QV6QI&ab_channel=HazardEditHazardEdit
namespace Windows_Forms_Chat
{
    public partial class Form1 : Form
    {
        TicTacToe ticTacToe = new TicTacToe();
        TCPChatServer server = null;
        TCPChatClient client = null;

        public Form1()
        {
            InitializeComponent();
        }
        
        public bool CanHostOrJoin()
        {
            if (server == null && client == null)
                return true;
            else
                return false;
        }

        private void HostButton_Click(object sender, EventArgs e)
        {
            if (CanHostOrJoin())
            {
                try
                {
                    int port = int.Parse(MyPortTextBox.Text);
                    server = TCPChatServer.createInstance(port, ChatTextBox, ticTacToe);
                    //oh no, errors
                    if (server == null)
                        throw new Exception("Incorrect port value!");//thrown exceptions should exit the try and land in next catch

                    server.SetupServer();

                }
                catch (Exception ex)
                {
                    ChatTextBox.Text += "Error: " + ex ;
                    ChatTextBox.AppendText(Environment.NewLine);
                }
            }

        }

        private void JoinButton_Click(object sender, EventArgs e)
        {
            if (CanHostOrJoin())
            {
                try
                {
                    int port = int.Parse(MyPortTextBox.Text);
                    int serverPort = int.Parse(serverPortTextBox.Text);
                    client = TCPChatClient.CreateInstance(port, serverPort, ServerIPTextBox.Text, ChatTextBox, ticTacToe);
                    if (client == null)
                        throw new Exception("Incorrect port value!");//thrown exceptions should exit the try and land in next catch

                    client.ConnectToServer();
                    //ChatTextBox.AppendText("Please send your proposed username (!username [new_username])\n");


                    // begins Assignment 3 - Step 3
                    Form2 form2 = new Form2();
                    form2.ShowDialog();
                    ChatTextBox.Text +=  form2.data + Environment.NewLine;
                    client.SendString("!username " + form2.username);

                }
                catch (Exception ex)
                {
                    client = null;
                    ChatTextBox.Text += "Error: " + ex;
                    ChatTextBox.AppendText(Environment.NewLine);
                }
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (client != null)
                client.SendString(TypeTextBox.Text);
            else if (server != null)
                server.SendToAll(TypeTextBox.Text, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //On form loaded
            ticTacToe.buttons.Add(button1);
            ticTacToe.buttons.Add(button2);
            ticTacToe.buttons.Add(button3);
            ticTacToe.buttons.Add(button4);
            ticTacToe.buttons.Add(button5);
            ticTacToe.buttons.Add(button6);
            ticTacToe.buttons.Add(button7);
            ticTacToe.buttons.Add(button8);
            ticTacToe.buttons.Add(button9);
        }

        private void AttemptMove(int i)
        {
            if (ticTacToe.myTurn)
            {
                bool validMove = ticTacToe.SetTile(i, ticTacToe.playerTileType);
                if (validMove)
                {
                    //tell server about it
                    if (client != null)
                        client.SendString("mv@p1p2%" + ticTacToe.GridToString());

                    ticTacToe.myTurn = false;//call this too when ready with server
                }
                //example, do something similar from server
                GameState gs = ticTacToe.GetGameState();
                if (gs == GameState.crossWins)
                {
                    client.SendString("p1@p1p2%");
                }
                if (gs == GameState.naughtWins)
                {
                    client.SendString("p2@p1p2%");
                }
                if (gs == GameState.draw)
                {
                    client.SendString("dd@p1p2%");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AttemptMove(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AttemptMove(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AttemptMove(2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AttemptMove(3);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AttemptMove(4);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            AttemptMove(5);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            AttemptMove(6);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            AttemptMove(7);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            AttemptMove(8);
        }

        private void TypeTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

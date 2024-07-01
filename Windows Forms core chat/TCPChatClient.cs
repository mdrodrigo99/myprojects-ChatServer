using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

//reference: https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiClient/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatClient : TCPChatBase
    {
        //public static TCPChatClient tcpChatClient;
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public ClientSocket clientSocket = new ClientSocket();


        public int serverPort;
        public string serverIP;


        public static TCPChatClient CreateInstance(int port, int serverPort, string serverIP, TextBox chatTextBox, TicTacToe game)
        {
            TCPChatClient tcp = null;
            //if port values are valid and ip worth attempting to join
            if (port > 0 && port < 65535 && 
                serverPort > 0 && serverPort < 65535 && 
                serverIP.Length > 0 &&
                chatTextBox != null)
            {
                tcp = new TCPChatClient();
                tcp.port = port;
                tcp.serverPort = serverPort;
                tcp.serverIP = serverIP;
                tcp.chatTextBox = chatTextBox;
                tcp.clientSocket.socket = tcp.socket;
                tcp.game = game; 
            }

            return tcp;
        }

        public void ConnectToServer()
        {
            int attempts = 0;

            while (!socket.Connected)
            {
                try
                {
                    attempts++;
                    SetChat("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    socket.Connect(serverIP, serverPort);
                }
                catch (SocketException)
                {
                    chatTextBox.Text = "";
                }
            }

            //Console.Clear();
            AddToChat("Connected");

            //keep open thread for receiving data
            clientSocket.socket.BeginReceive(clientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, clientSocket);
        }

        public void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;

            int received;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch (SocketException)
            {
                AddToChat("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentClientSocket.socket.Close();
                return;
            }
            //read bytes from packet
            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            //convert to string so we can work with it
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Text: " + text);

            if (text.ToLower().Contains("mv@p1p2%") && !text.Remove(0, 8).ToLower().Contains("mv@p1p2%"))
            {
                // update the client's board uisng move information
                UpdateGame(text.Remove(0, 8));
            }
            else if (text.ToLower().Contains("player 1"))
            {
                this.game.myTurn = true;
                this.game.playerTileType = TileType.cross;
                AddToChat(text);
                AddToChat("Now it is your turn to make a move.");
            }
            else if (text.ToLower().Contains("player 2"))
            {
                this.game.myTurn = false;
                this.game.playerTileType = TileType.naught;
                AddToChat(text);
            }
            else if (text.ToLower() == "ct@p1p2%")
            {
                // inform the player that it it their turn to make a move
                this.game.myTurn = true;
                AddToChat("Now it is your turn to make a move.");
            }
            else if (text.ToLower() == "cm@p1p2%")
            {
                this.game.myTurn = true;
                this.game.ResetBoard();
                AddToChat("Other player unavailable!");
            }
            else if (text.ToLower() == "rb@p1p2%") 
            {
                this.game.myTurn = false;
                this.game.playerTileType = TileType.blank;
                ResetBoard();

                currentClientSocket.state = ClientState.chatting;
                currentClientSocket.player = PlayerNumber.nonplayer;
                AddToChat("Your state changed to <chatting>. You can now chat but cannot play TicTacToe games.");
            }
            else
            {
                //text is from server but could have been broadcast from the other clients
                AddToChat(text);
            }
            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }
        public void Close()
        {
            socket.Close();
        }
    }

}

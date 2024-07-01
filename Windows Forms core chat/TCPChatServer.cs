using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.Intrinsics.X86;
using static System.Net.Mime.MediaTypeNames;
using Windows_Forms_CORE_CHAT_UGH;
using System.Linq;

//https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatServer : TCPChatBase
    {
        
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

        // create a tictactoeteam, so players can join to existing team or create a new game
        public TicTacToeTeam board = new TicTacToeTeam(null, null);

        public static TCPChatServer createInstance(int port, TextBox chatTextBox, TicTacToe game)
        {
            TCPChatServer tcp = null;
            //setup if port within range and valid chat box given
            if (port > 0 && port < 65535 && chatTextBox != null)
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
                tcp.game = game;
            }

            //return empty if user not enter useful details
            return tcp;
        }

        public void SetupServer()
        {
            chatTextBox.Text += "Setting up server..." + Environment.NewLine;
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
            //kick off thread to read connecting clients, when one connects, it'll call out AcceptCallback function
            serverSocket.BeginAccept(AcceptCallback, this);
            chatTextBox.Text += "Server setup complete" + Environment.NewLine;

            // Assignment 3 - Step 2 - Create the User table after server setup process complete
            chatTextBox.Text += DatabaseServer.CreateUser() + Environment.NewLine;
        }



        public void CloseAllSockets()
        {
            foreach (ClientSocket clientSocket in clientSockets)
            {
                clientSocket.socket.Shutdown(SocketShutdown.Both);
                clientSocket.socket.Close();
            }
            clientSockets.Clear();
            serverSocket.Close();
        }

        public void AcceptCallback(IAsyncResult AR)
        {
            Socket joiningSocket;

            try
            {
                joiningSocket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            ClientSocket newClientSocket = new ClientSocket();
            newClientSocket.socket = joiningSocket;

            clientSockets.Add(newClientSocket);
            //start a thread to listen out for this new joining socket. Therefore there is a thread open for each client
            joiningSocket.BeginReceive(newClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, newClientSocket);
            AddToChat("Client connected, waiting for request...");

            //we finished this accept thread, better kick off another so more people can join
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        public void SendToClient(string str, ClientSocket client) 
        {
            byte[] data = Encoding.ASCII.GetBytes(str);
            client.socket.Send(data);
        }

        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;
            
            int received = 0;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch (Exception e)
            {
                if (e is SocketException)
                {
                    AddToChat("Client forcefully disconnected");
                    // Don't shutdown because the socket may be disposed and its disconnected anyway.
                    currentClientSocket.socket.Close();
                    clientSockets.Remove(currentClientSocket);
                    return;
                }
                else if (e is ObjectDisposedException) 
                {
                    return ;
                }
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            AddToChat( text );
            if (text.ToLower() == "!commands") // Client requested time
            {
                SendToClient("\nCommands are !commands !about !who !whisper !exit", currentClientSocket);
                AddToChat("Commands sent to client");
            }
            // extra feature - players can leave the game at anytime
            else if (text.ToLower() == "!quit") 
            {
                if (board.IsPlayerEqual(currentClientSocket))
                {
                    // reset server game board
                    ResetBoard();
                    SendToClient("Game Over: You left the game.", currentClientSocket);
                    // reset the client board
                    SendToClient("rb@p1p2%", currentClientSocket);
                    
                    // check is second player available
                    if (board.IsPlayerNotNull(board.GetOtherPlayer(currentClientSocket)))
                    {
                        SendToClient("Game Over: {" + currentClientSocket.username + "} left the game.", board.GetOtherPlayer(currentClientSocket));
                        // reset the client board
                        SendToClient("rb@p1p2%", board.GetOtherPlayer(currentClientSocket));
                    }

                    // end the current game and start a new game with zero players
                    board.NewGame(); 
                }
            }
            else if (text.ToLower() == "p1@p1p2%" || text.ToLower() == "p2@p1p2%" || text.ToLower() == "dd@p1p2%")
            {
                if (text.ToLower() == "p1@p1p2%")
                {
                    SendToAll("Player: {" + board.GetPlayer1().username + "} wins!", null);
                    // add each players scores into the database - updates wins,losses column
                    DatabaseServer.AddScoreToDB(board.GetPlayer1().username, "wins");
                    DatabaseServer.AddScoreToDB(board.GetPlayer2().username, "losses");
                }
                else if (text.ToLower() == "p2@p1p2%")
                {
                    SendToAll("Player: {" + board.GetPlayer2().username + "} wins!", null);
                    // add each players scores into the database - updates wins,losses column
                    DatabaseServer.AddScoreToDB(board.GetPlayer2().username, "wins");
                    DatabaseServer.AddScoreToDB(board.GetPlayer1().username, "losses");
                }
                else
                {
                    SendToAll("Match between {" + board.GetPlayer1().username + "} and {" + board.GetPlayer2().username + "} draws!", null);
                    // add each players scores into the database - updates draws column
                    DatabaseServer.AddScoreToDB(board.GetPlayer1().username, "draws");
                    DatabaseServer.AddScoreToDB(board.GetPlayer2().username, "draws");
                }

                // reset server game board
                ResetBoard();
                SendToClient("rb@p1p2%", board.GetPlayer1());
                SendToClient("rb@p1p2%", board.GetPlayer2());

                // end the current game and start a new game with zero players
                board.NewGame();
            }
            else if (text.ToLower().Contains("mv@p1p2%"))
            {
                // used mv@p1p2% code to send movement information

                // temporary variable
                int count = 0;

                // check game as already two players, if not can't move
                if (board.IsPlayerEqual(currentClientSocket) && board.IsTwoPlayersAvailable())
                {
                    // update the server's board uisng move information
                    UpdateGame(text.Remove(0, 8));

                    // send move information to current client
                    SendToClient(text, currentClientSocket);

                    // send move information to other player
                    SendToClient(text, board.GetOtherPlayer(currentClientSocket));
                    SendToClient("ct@p1p2%", board.GetOtherPlayer(currentClientSocket));
                    count++;
                }

                if (count == 0)
                {
                    // only one player in the game
                    SendToClient("cm@p1p2%", currentClientSocket);
                }
            }
            else if (text.ToLower() == "!join")
            {
                // if the current games have a space then the player will be added to it
                if (board.CheckSpaceAvailable(currentClientSocket))
                {
                    currentClientSocket.state = ClientState.playing;
                    SendToClient("Your state changed to <playing>. Now you can play the TicTacToe game.", currentClientSocket);

                    if (board.GetPlayer1() == currentClientSocket)
                    {
                        currentClientSocket.player = PlayerNumber.player1;
                        SendToClient("You are the Player 1", currentClientSocket);
                    }
                    else
                    {
                        currentClientSocket.player = PlayerNumber.player2;
                        SendToClient("You are the Player 2 ", currentClientSocket);
                    }
                }
                else
                {
                    SendToClient("No space available on the TicTacToe game. Please wait!", currentClientSocket);
                }
            }
            else if (text.ToLower() == "!exit") // Client wants to exit gracefully
            {
                // Always Shutdown before closing
                currentClientSocket.socket.Shutdown(SocketShutdown.Both);
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                AddToChat("Client disconnected");
                return;
            }
            else if (text.ToLower() == "!scores")
            {
                List<string> data = DatabaseServer.GetScores();
                SendToClient(Environment.NewLine + "Scoreboard ----------------------------------", currentClientSocket);
                SendToClient("Wins \tLosses \tDraws \tUsername " + Environment.NewLine, currentClientSocket);
                // data[0] means username, data[1] means wins, data[2] means losses,
                // data[3] means draws, data[4] again username, like wise
                for (int i = 0; i < data.Count(); i += 4)
                {
                    string rowData = data[i + 1] + "\t" + data[i + 2] + "\t" + data[i + 3] + "\t" + data[i] + Environment.NewLine;
                    SendToClient(rowData, currentClientSocket);
                }
            }
            else if (text.ToLower().Contains("!username"))
            {
                // temporary variable to use as a count
                int tmpcnt = 0;

                try
                {
                    // remove '!username' from client message to get the username of the client
                    text = text.Remove(0, 10);

                    // check any other client already have the username
                    foreach (ClientSocket c in clientSockets)
                    {
                        if (c.username.Equals(text))
                        {
                            tmpcnt++;
                            break;
                        }
                    }

                    if (tmpcnt == 0)
                    {
                        currentClientSocket.username = text;
                        // change client state to chatting
                        currentClientSocket.state = ClientState.chatting;
                        byte[] data = Encoding.ASCII.GetBytes("Your state changed to <chatting>. You can now chat but cannot play TicTacToe games.");
                        currentClientSocket.socket.Send(data);
                    }
                    // if username already someone taken
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("\n" + text + " unsuccessfull!");
                        currentClientSocket.socket.Send(data);

                        // remove the client from the server
                        currentClientSocket.socket.Shutdown(SocketShutdown.Both);
                        currentClientSocket.socket.Close();
                        clientSockets.Remove(currentClientSocket);
                        AddToChat("New client named {" + text + "} disconnected!");
                        return;
                    }
                }
                catch (Exception)
                {
                    byte[] error = Encoding.ASCII.GetBytes("Please use the correct command: {!username <username>}");
                    currentClientSocket.socket.Send(error);
                }
            }
            else if (text.ToLower().Contains("!user"))
            {
                // gain 0.5 experience when client use this command
                currentClientSocket.level += 0.5;

                // temporary variable to use as a count
                int tmpcnt = 0;

                try
                {
                    // remove '!user' from client message to get the new username of the client
                    text = text.Remove(0, 6);

                    //tmporary variable to store previous username
                    String tmpusername = currentClientSocket.username;

                    // check any other client already have the new username
                    foreach (ClientSocket c in clientSockets)
                    {
                        if (c.username.Equals(text))
                            tmpcnt++;
                    }

                    if (tmpcnt == 0)
                    {
                        currentClientSocket.username = text;
                        byte[] data = Encoding.ASCII.GetBytes("\n" + text + " successfully renamed!");
                        currentClientSocket.socket.Send(data);
                        SendToAll("Client {" + tmpusername + "} changed their username to {" + text + "}.", currentClientSocket);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("\n" + text + " username already taken!");
                        currentClientSocket.socket.Send(data);
                    }
                }
                catch (Exception)
                {
                    byte[] error = Encoding.ASCII.GetBytes("Please use the correct command: {!user <new username>}");
                    currentClientSocket.socket.Send(error);
                }
            }
            else if (text.ToLower() == "!who")
            {
                byte[] data = Encoding.ASCII.GetBytes("Connected client list");
                currentClientSocket.socket.Send(data);
                foreach (ClientSocket c in clientSockets)
                {
                    data = Encoding.ASCII.GetBytes(c.username + Environment.NewLine);
                    currentClientSocket.socket.Send(data);
                }
            }
            else if (text.ToLower() == "!about")
            {
                // gain 0.75 experience when client use this command
                currentClientSocket.level += 0.75;

                byte[] data = Encoding.ASCII.GetBytes("Creator: Torrens University Australia" +
                    Environment.NewLine + "Purpose: Produce a secured server-clients connection" +
                    Environment.NewLine + "Year of Development: 2022");
                currentClientSocket.socket.Send(data);
            }
            else if (text.ToLower().Contains("!whisper"))
            {
                // gain 0.5 experience when client use this command
                currentClientSocket.level += 0.5;

                // temporary variable to use as a count
                int tmpcnt = 0;

                // check is user correctly entered the command
                try
                {
                    // remove '!whisper' from client message to get the username and the message of the specified client
                    text = text.Remove(0, 9);

                    // extract the client username from the command
                    String tmpusername = text.Substring(0, text.IndexOf(' '));

                    // extract the sending message from the command
                    text = text.Remove(0, tmpusername.Length + 1);

                    // check specifed client is online or not
                    foreach (ClientSocket c in clientSockets)
                    {
                        if (c.username.Equals(tmpusername))
                        {
                            byte[] tmpdata = Encoding.ASCII.GetBytes("[" + currentClientSocket.username + "] {whisper} " + text);
                            c.socket.Send(tmpdata);
                            tmpcnt++;
                            break;
                        }
                    }

                    // identify the non existing user and display the failed message
                    if (tmpcnt == 0)
                    {
                        byte[] data = Encoding.ASCII.GetBytes("Cannot find anyone named {" + tmpusername + "}.");
                        currentClientSocket.socket.Send(data);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("Message successfully send!");
                        currentClientSocket.socket.Send(data);
                    }
                }
                catch (Exception)
                {
                    byte[] error = Encoding.ASCII.GetBytes("Please use the correct command: {!whisper <username> <message>}");
                    currentClientSocket.socket.Send(error);
                }
            }
            else if (text.ToLower().Contains("!kick"))
            {
                // temporary variable to store a specified client, if found
                ClientSocket tempClientSocket = null;

                try
                {
                    // remove '!kick' from the command
                    text = text.Remove(0, 6);

                    if (currentClientSocket.role == "moderator")
                    {
                        // check specifed client is online or not
                        foreach (ClientSocket c in clientSockets)
                        {
                            if (c.username.Equals(text))
                            {
                                tempClientSocket = c;
                                break;
                            }
                        }

                        if (tempClientSocket != null)
                        {
                            clientSockets.Remove(tempClientSocket);
                            tempClientSocket.socket.Close();
                            AddToChat("{" + text + "} removed from the server.");
                            return;
                        }
                        else
                        {
                            byte[] data = Encoding.ASCII.GetBytes("Cannot find the client named {" + text + "}!");
                            currentClientSocket.socket.Send(data);
                        }
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("You have to be a moderator to continue the process!");
                        currentClientSocket.socket.Send(data);
                    }
                }
                catch (Exception)
                {
                    byte[] error = Encoding.ASCII.GetBytes("Please use the correct command: {!kick <username>}");
                    currentClientSocket.socket.Send(error);
                }
            }
            // clients can check their own level or any other client's level using level command
            else if (text.ToLower().Contains("!level"))
            {
                // temporary variable to use as a count
                int tmpcnt = 0;

                if (text.Length == 6)
                {
                    byte[] data = Encoding.ASCII.GetBytes("Your Level: " + currentClientSocket.level);
                    currentClientSocket.socket.Send(data);
                }
                else if (text.Length > 6)
                {
                    try
                    {
                        // remove the !level from the command to get the client name
                        text = text.Remove(0, 7);

                        // check is the client already exists
                        foreach (ClientSocket c in clientSockets)
                        {
                            if (c.username.Equals(text))
                            {
                                // display the found client's level
                                byte[] data = Encoding.ASCII.GetBytes("{" + text + "}'s Level: " + c.level);
                                currentClientSocket.socket.Send(data);
                                tmpcnt++;
                                break;
                            }
                        }

                        // if could not find the client, display error message
                        if (tmpcnt == 0)
                        {
                            byte[] data = Encoding.ASCII.GetBytes("Cannot find the client named {" + text + "}!");
                            currentClientSocket.socket.Send(data);
                        }
                    }
                    catch (Exception)
                    {
                        byte[] data = Encoding.ASCII.GetBytes("Please use the correct command: {!level} or {!level <username>}");
                        currentClientSocket.socket.Send(data);
                    }
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes("Please use the correct command: {!level} or {!level <username>}");
                    currentClientSocket.socket.Send(data);
                }
            }
            else
            {
                //normal message broadcast out to all clients
                SendToAll(text, currentClientSocket);
            }

            {
                //we just received a message from this socket, better keep an ear out with another thread for the next one
                currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
            }
        }

        public void SendToAll(string str, ClientSocket from)
        {
            // temporary variable to store a value to check is it a message from server or is it a command from server
            Boolean isCommandFromServer = true;

            if (from == null) 
            {
                MakeMod(str, ref isCommandFromServer);
            }

            foreach(ClientSocket c in clientSockets)
            {
                // if server send message to other client
                if (from == null && !isCommandFromServer) 
                {
                    byte[] data = Encoding.ASCII.GetBytes("[Server] " + str);
                    c.socket.Send(data);
                }
                // or else if one of client send a message
                else if(from != null && !from.socket.Equals(c))
                {
                    if (str.ToLower().Contains("move"))
                    {
                        byte[] data = Encoding.ASCII.GetBytes(str);
                        c.socket.Send(data);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("[" + from.username + "] " + str);
                        c.socket.Send(data);
                    }
                }
            }
        }

        public void MakeMod(String str, ref Boolean isCommand) 
        {
            // display moderators as a list
            if (str.ToLower().Contains("!mods"))
            {
                // temporary variable to use as a count
                int tmpcnt = 0;

                AddToChat("List of moderators in the server.");
                foreach (ClientSocket c in clientSockets)
                {
                    if (c.role == "moderator")
                    {
                        AddToChat(c.username);
                        tmpcnt++;
                    }
                }

                if (tmpcnt == 0)
                    AddToChat("none");
            }

            // server can able to make a new moderator
            else if (str.ToLower().Contains("!mod"))
            {
                // temporary variable to use as a count
                int tmpcnt = 0;

                try
                {
                    // remove '!mod' from the server command
                    str = str.Remove(0, 5);

                    foreach (ClientSocket c in clientSockets)
                    {
                        if (c.username.Equals(str))
                        {
                            // if the client is not a moderator yet
                            if (c.role != "moderator")
                            {
                                c.role = "moderator";
                                tmpcnt = 1;
                                break;
                            }

                            // if client is a moderator, then they are demoted
                            else
                            {
                                c.role = "client";
                                tmpcnt = 2;
                                break;
                            }
                        }
                    }

                    // display error if cloud not find the client
                    if (tmpcnt == 0)
                    {
                        AddToChat("Cannot find anyone named {" + str + "}.");
                    }
                    else if (tmpcnt == 1)
                    {
                        AddToChat("Now {" + str + "} is a moderator.");
                    }
                    else if (tmpcnt == 2)
                    {
                        AddToChat("Demoted and Now {" + str + "} is a client.");
                    }
                }
                catch (Exception)
                {
                    AddToChat("Please use the correct command: {!mod <username>}");
                }
            }
            else 
            {
                isCommand = false;
            }
        }
    }
}

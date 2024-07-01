using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Windows_Forms_Chat
{
    // created an enum to store the states of the client
    public enum ClientState 
    {
        login, chatting, playing
    }

    // created an enum to store the player numbers
    public enum PlayerNumber 
    {
        nonplayer, player1, player2
    }

    public class ClientSocket
    {
        //add other attributes to this, e.g username, what state the client is in etc
        public Socket socket;
        public const int BUFFER_SIZE = 2048;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public String username = "";
        // changed 'state' to 'role' (to assign client or moderator)
        public string role = "client";

        // create a new variable as 'state' to track each client state as for Assignment 3 - Step 3
        public ClientState state = ClientState.login;

        public Double level = 1; // custom variable, used to track the client current level

        // store what player they are
        public PlayerNumber player = PlayerNumber.nonplayer;
    }
}

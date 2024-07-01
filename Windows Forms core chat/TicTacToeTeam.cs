using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows_Forms_Chat;

namespace Windows_Forms_CORE_CHAT_UGH
{
    public class TicTacToeTeam
    {
        private ClientSocket player1;
        private ClientSocket player2;
    
        public TicTacToeTeam(ClientSocket p1, ClientSocket p2) 
        {
            player1 = p1;
            player2 = p2;
        }

        // check current/new game has both two players, and if not add the player to the game
        public bool CheckSpaceAvailable(ClientSocket player) 
        {
            if (player1 == null)
                player1 = player;
            else if (player2 == null)
                player2 = player;
            else
                return false; // if players cannot add to current game, then return false

            return true; // if process success, return true
        }

        // return player 1, can access by the outside
        public ClientSocket GetPlayer1() 
        {
            return player1;
        }

        // return player  2, can access by the outside
        public ClientSocket GetPlayer2() 
        {
            return player2;
        }

        // uses to get second player/ other player from the game
        public ClientSocket GetOtherPlayer(ClientSocket player) 
        {
            if(player1 == player)
                return player2;
            else 
                return player1;
        }

        // uses to check is game already has two players according to the current client
        public bool IsTwoPlayersAvailable() 
        {
            return IsPlayerNotNull(player1) && IsPlayerNotNull(player2);
        }

        // uses to check is the player null or not
        public bool IsPlayerNotNull(ClientSocket player) 
        {
            return player != null;
        }

        // uses to check is the player equal or not

        public bool IsPlayerEqual(ClientSocket player) 
        {
            if (player == player1)
                return true;
            else if (player == player2)
                return true;
            return false;
        }

        // remove two players from the current game - new game
        public void NewGame() 
        {
            player1 = null;
            player2 = null;
        }
    }
}

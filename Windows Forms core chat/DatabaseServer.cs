using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Windows_Forms_CORE_CHAT_UGH
{
    public static class DatabaseServer
    {
        private static SQLiteConnection connection;

        // connect to the database file
        private static SQLiteConnection GetDB()
        {
            try
            {
                connection = new SQLiteConnection("Data Source=dbfile.db");
                return connection;
            }
            catch (Exception)
            {

            }

            return null;
        }

        // create the user table
        public static string CreateUser()
        {
            SQLiteConnection con = GetDB();
            if (con != null)
            {
                con.Open();
                // sql query as a string
                string sql = "CREATE TABLE IF NOT EXISTS user (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "username text, password text, wins INTEGER, losses INTEGER, draws INTEGER);";
                SQLiteCommand query = new(sql, con);
                // execute the sql query
                query.ExecuteNonQuery();
                con.Close();

                // display message if user table created
                return "User table succefully created.";
            }
            else
                return "Cannot connect to the database!";
        }

        public static string LoginUser(string username, string password)
        {
            SQLiteConnection con = GetDB();

            // temporary variable as a count
            int count = 0;

            if (con != null)
            {
                con.Open();
                // sql query as a string
                string sql = "SELECT username, password FROM user;";
                SQLiteCommand query = new(sql, con);

                // execute the sql query check if user already exists
                SQLiteDataReader reader = query.ExecuteReader();

                while (reader.Read())
                {
                    if (username == reader["username"].ToString())
                    {
                        if (password == reader["password"].ToString())
                            count++;
                        else
                            return null;
                    }
                }

                if (count == 0)
                {
                    sql = "INSERT INTO user (username, password, wins, losses, draws) VALUES (@p1, @p2, 5, 0, 0);";
                    query = new SQLiteCommand(sql, con);
                    query.Parameters.Add(new SQLiteParameter("@p1", username));
                    query.Parameters.Add(new SQLiteParameter("@p2", password));
                    query.ExecuteNonQuery();

                    con.Close();

                    return "You are successfully logged In!";
                }
                else
                {
                    con.Close();
                    return "Welcome back!";
                }
            }
            else
                return "User login unsuccessfull";
        }

        public static int GetWinLossDrawTotal(string username, string type)
        {
            // temporary variable to store total
            int total = 0;

            SQLiteConnection con = GetDB();

            con.Open();

            // sql query as a string, get the previous count of wins/losses/draws
            string sql = "SELECT wins, losses, draws FROM user WHERE username = @p2;";
            SQLiteCommand query = new(sql, con);
            query.Parameters.Add(new SQLiteParameter("@p1", type));
            query.Parameters.Add(new SQLiteParameter("@p2", username));

            // execute the sql
            SQLiteDataReader reader = query.ExecuteReader();
            while (reader.Read())
            {
                total = Convert.ToInt32(reader[type].ToString());
            }

            con.Close();

            return total;
        }

        // update user's details according to the data
        public static void AddScoreToDB(string username, string type)
        {
            SQLiteConnection con = GetDB();

            con.Open();

            // sql query as a string
            string sql = "UPDATE user SET ";
            sql += type;
            sql += " = @p3 WHERE username = @p2";
            SQLiteCommand query = new(sql, con);
            //query.Parameters.Add(new SQLiteParameter("@p1", type));
            query.Parameters.Add(new SQLiteParameter("@p2", username));
            query.Parameters.Add(new SQLiteParameter("@p3", GetWinLossDrawTotal(username, type) + 1));
            query.ExecuteNonQuery();

            con.Close();
        }

        //  outputs all scores in the database
        public static List<string> GetScores() 
        {
            // temporary string list to store table data
            List<string> data = new();

            SQLiteConnection con = GetDB();

            con.Open();

            // sql query as a string
            string sql = "SELECT username, wins, losses, draws FROM user ORDER BY wins desc";
            SQLiteCommand query = new(sql, con);

            // execute the sql query check if user already exists
            SQLiteDataReader reader = query.ExecuteReader();

            while (reader.Read())
            {
                data.Add(reader["username"].ToString());
                data.Add(reader["wins"].ToString());
                data.Add(reader["losses"].ToString());
                data.Add(reader["draws"].ToString());
            }

            con.Close();

            return data;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using System.Security.Cryptography;
using System.IO;

using RomGeo.QuizObjects;
using RomGeo.Utils;

namespace RomGeo.DatabaseAbstractionLayer
{
    static class DAL
    {
        private static MySqlConnection connection;
        private static string server;
        private static string database;
        private static string uid;
        private static string password;

        // Constructor (static)
        static DAL()
        {
            server = "86.120.252.100";
            database = "erg_db";
            uid = "romgeo";
            password = "romgeo";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }

        // Open Connection
        private static bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        Debug.ExitWithErrorMessage("Cannot connect to server.", ex.Number);
                        break;

                    case 1045:
                        Debug.ExitWithErrorMessage("Failed to authenticate client.", ex.Number);
                        break;
                }
                return false;
            }
        }

        // Close connection
        private static bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static Question GetQuestion()
        {
            int id = 0;
            int difficultyPercent = 0;
            bool isGraphic = false;
            string text = string.Empty;
            Domain domain = Domain.None;
            Answers answers = new Answers();

            // Open connection
            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("GetQuestion", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        MySqlDataReader myReader = command.ExecuteReader();
                        if (myReader.Read())
                        {
                            id = myReader.GetInt32(0);
                            text = myReader.GetString(1);
                            domain = myReader.GetDomain(2);
                            difficultyPercent = myReader.GetInt32(3);
                            isGraphic = myReader.GetBoolean(4);
                            answers.CorrectAnswer = myReader.GetString(5);

                            int i = 1;
                            while (i <= PersistentData.MAX_ANSWERS)
                            {
                                answers[i] = myReader.GetString(5 + i);
                                i++;
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }

                // Close connection
                CloseConnection();
            }
            else Debug.ExitWithErrorMessage("Connection failed to open using DAL method.");
            Debug.Log("GET Q: " + id + text + domain + difficultyPercent + isGraphic + answers);
            return new Question(id, text, domain, difficultyPercent, isGraphic, answers);
        }

        public static bool ValidateUser(string user, string password)
        {
            bool result = false;

            password = MD5.Create().GetHash(password);

            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("ValidateUser", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@hash", password);

                        
                        MySqlDataReader myReader = command.ExecuteReader();
                        if (myReader.Read())
                        {
                            result = myReader.GetBoolean(0);
                        }
                        
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }
                // Close connection
                CloseConnection();
            }
            Debug.Log(user + " - " + result);
            return result;
        }

        public static bool SearchUser(string user)
        {
            bool result = false; //nu exista numele de utilizator user

            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("SearchUser", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.AddWithValue("@user", user);

                        MySqlDataReader myReader = command.ExecuteReader();
                        if (myReader.Read())
                        {
                            result = myReader.GetBoolean(0);
                        }

                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }
                // Close connection
                CloseConnection();
            }
            return result;
        }

        public static int GetID(string user)
        {
            int result = 32;

            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    String readCommand = "SELECT idUser FROM user WHERE username = @user;";
                    MySqlCommand command = new MySqlCommand(readCommand, connection);
                    command.Parameters.AddWithValue("@user", user);
                    result = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }
                // Close connection
                CloseConnection();
            }
            Debug.Log("GETID" + user + " return: " + result);
            return result;
        }

        public static void CreateUser(User user, string password)
        {

            password = MD5.Create().GetHash(password);

            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("CreateUser", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@hash", password);
                        if (command.ExecuteNonQuery() > 0)
                        {
                            Debug.Log("User " + user + " created");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }

                // Close connection
                CloseConnection();
            }
        }

        public static void MarkQueried(User user, Question question)
        {
            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("MarkQueried", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@idQ", question.Id);
                        if (command.ExecuteNonQuery() > 0)
                        {
                            Debug.Log("Question " + question.Id + " marked as queried");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }

                // Close connection
                CloseConnection();
            }
        }

        public static void MarkCorrect(User user, Question question)
        {
            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("MarkCorrect", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@idQ", question.Id);
                        if (command.ExecuteNonQuery() > 0) Debug.Log("Question " + question.Id + " marked as correct");
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }

                // Close connection
                CloseConnection();
            }
        }


        // Added for question uploader (admin)
        public static void UploadQuestion(Question question, String fileName)
        {
            if (OpenConnection() == true)
            {
                // Create command and assign the query and connection from the constructor
                try
                {
                    using (var command = new MySqlCommand("UploadQuestion", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        BinaryReader reader = new BinaryReader(fs);
                        byte[] imgData = reader.ReadBytes((int)fs.Length);

                        MySqlParameter mParm = new MySqlParameter("@data", MySqlDbType.LongBlob);
                        mParm.Size = imgData.Length;
                        mParm.Value = imgData;

                        command.Parameters.Add(mParm);

                        command.Parameters.AddWithValue("@text", question.Text);
                        command.Parameters.AddWithValue("@answer1", question.Answers[1]);
                        command.Parameters.AddWithValue("@answer2", question.Answers[2]);
                        command.Parameters.AddWithValue("@answer3", question.Answers[3]);
                        command.Parameters.AddWithValue("@answer4", question.Answers[4]);
                        command.Parameters.AddWithValue("@correctAnswer", question.CorrectAnswer);
                        command.Parameters.AddWithValue("@domain", Utils.Coverters.DomainToString(question.Domain));
                        command.Parameters.AddWithValue("@graphic", 1); 

                        if (command.ExecuteNonQuery() > 0) Debug.Log("Question uploaded!");
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.ExitWithErrorMessage(ex.Message, ex.Number);
                }

                // Close connection
                CloseConnection();
            }
        }
    }
}

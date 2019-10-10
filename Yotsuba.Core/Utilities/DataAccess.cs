using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yotsuba.Core.Models;

namespace Yotsuba.Core.Utilities
{
    public static class DataAccess
    {
        //https://stackoverflow.com/a/11285360/9017481
        /*
              AuthorTable
            AuthorID | AuthorName
            ----------------------
            0        | Big Boss
           
           
              BoardTable
             BoardID | BoardName        
             --------------------------
                  1  | August - 2019    
                  2  | September - 2019
             
             TaskTable
            id | BoardID  | TaskName | TaskDescription | Category          | TagName
            ---------------------------------------------------------------------------------------
            1  | 1        | Task 1   | N/A             | 8/18/19 - 8/24/19 | Airbus A320
            2  | 1        | Task 2   | N/A             | 8/25/19 - 8/31/19 | S-76D Helicopter
            3  | 2        | Task 3   | N/A             | 9/1/19 - 9/7/19   | S-76D Helicopter
            4  | 2        | Task 4   | N/A             | 9/8/19 - 8/14/19  | CSF General

             HourTable
            id | BoardID  | Category          | TagName          | Hour
            ---------------------------------------------------------------------------------------
            1  | 1        | 8/18/19 - 8/24/19 | Airbus A320      | 2
            2  | 1        | 8/25/19 - 8/31/19 | S-76D Helicopter | 10
            3  | 2        | 9/1/19 - 9/7/19   | S-76D Helicopter | 5
            4  | 2        | 9/8/19 - 8/14/19  | CSF General      | 6

         */

        public static void InitializeDatabase()
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            String boardTableCommand = "CREATE TABLE IF NOT " +
                "EXISTS BoardTable (BoardID INTEGER PRIMARY KEY, " +
                "BoardName NVARCHAR(2048) NULL)";

            using SqliteCommand createBoardTable = new SqliteCommand(boardTableCommand, db);

            createBoardTable.ExecuteReader();

            String TaskTableCommand = "CREATE TABLE IF NOT " +
                "EXISTS TaskTable (TaskID INTEGER PRIMARY KEY, " +
                "BoardID INTEGER NULL, " +
                "TaskName NVARCHAR(2048) NULL, " +
                "TaskDescription NVARCHAR(2048) NULL, " +
                "Category NVARCHAR(2048) NULL, " +
                "TagName NVARCHAR(2048) NULL)";

            using SqliteCommand createTaskTable = new SqliteCommand(TaskTableCommand, db);

            createTaskTable.ExecuteReader();

            String HourTableCommand = "CREATE TABLE IF NOT " +
                "EXISTS HourTable (HourID INTEGER PRIMARY KEY, " +
                "BoardID INTEGER NULL, " +
                "Category NVARCHAR(2048) NULL, " +
                "TagName NVARCHAR(2048) NULL, " +
                "Hour REAL NULL)";

            using SqliteCommand createHourTable = new SqliteCommand(HourTableCommand, db);

            createHourTable.ExecuteReader();

            String authorTableCommand = "CREATE TABLE IF NOT " +
                "EXISTS AuthorTable (AuthorID INTEGER PRIMARY KEY, " +
                "AuthorName NVARCHAR(2048) NULL)";

            using SqliteCommand createAuthorTable = new SqliteCommand(authorTableCommand, db);

            createAuthorTable.ExecuteReader();

            using SqliteCommand insertCommand = new SqliteCommand();
            insertCommand.Connection = db;

            // Initialize AuthorTable, which should only have one row
            insertCommand.CommandText = "INSERT OR IGNORE INTO AuthorTable VALUES (@AuthorID, @AuthorName);";
            insertCommand.Parameters.AddWithValue("@AuthorID", 0);
            insertCommand.Parameters.AddWithValue("@AuthorName", "Big Boss");

            insertCommand.ExecuteReader();

            db.Close();
        }

        public static void AddBoard(int boardid, string boardname)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            using SqliteCommand insertCommand = new SqliteCommand();
            insertCommand.Connection = db;

            // Use parameterized query to prevent SQL injection attacks
            insertCommand.CommandText = "INSERT INTO BoardTable VALUES (@BoardID, @BoardName);";
            insertCommand.Parameters.AddWithValue("@BoardID", boardid);
            insertCommand.Parameters.AddWithValue("@BoardName", boardname);

            insertCommand.ExecuteReader();

            db.Close();
        }

        public static ObservableCollection<BoardModel> GetAllBoards()
        {
            ObservableCollection<BoardModel> entries = new ObservableCollection<BoardModel>();

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            using SqliteCommand selectCommand = new SqliteCommand("SELECT * from BoardTable", db);

            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                entries.Add(new BoardModel(query.GetInt32(0), query.GetString(1)));
            }

            db.Close();

            foreach (var board in entries)
            {
                var tasklist = GetAllTasksInBoard(board.ID);

                foreach (var task in tasklist)
                {
                    board.TaskList.Add(task);
                }

                board.Hours = GetAllHourInBoard(board.ID);
            }

            return entries;
        }

        public static List<Tuple<string, HourModel>> GetAllHourInBoard(int boardid)
        {
            List<Tuple<string, HourModel>> entries = new List<Tuple<string, HourModel>>();

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand selectCommand = new SqliteCommand("SELECT * from HourTable WHERE BoardID = @BoardID", db);

            selectCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                HourModel hourfortag = new HourModel
                {
                    Tag = query.GetString(3),
                    Hours = query.GetFloat(4),
                };
                entries.Add(new Tuple<string, HourModel>(query.GetString(2), hourfortag));
            }

            db.Close();

            return entries;
        }

        public static void AddTaskToBoard(int taskid, int boardid, string taskname, string taskdescription, string tag, string category)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand insertCommand = new SqliteCommand();
            insertCommand.Connection = db;

            // Use parameterized query to prevent SQL injection attacks
            insertCommand.CommandText = "INSERT INTO TaskTable VALUES (@TaskID, @BoardID, @TaskName, " +
                                                "@TaskDescription, @Category, @Tag);";
            insertCommand.Parameters.AddWithValue("@TaskID", taskid);
            insertCommand.Parameters.AddWithValue("@BoardID", boardid);
            insertCommand.Parameters.AddWithValue("@TaskName", taskname);
            insertCommand.Parameters.AddWithValue("@TaskDescription", taskdescription);
            insertCommand.Parameters.AddWithValue("@Category", category);
            insertCommand.Parameters.AddWithValue("@Tag", tag);

            insertCommand.ExecuteReader();

            // Check whether the tag for particular category is in the table
            SqliteCommand selectCommand = new SqliteCommand();
            selectCommand.Connection = db;

            selectCommand.CommandText = "SELECT * from HourTable WHERE BoardID = @BoardID " +
                "AND TagName = @TagName AND Category = @Category";

            selectCommand.Parameters.AddWithValue("@BoardID", boardid);
            selectCommand.Parameters.AddWithValue("@TagName", tag);
            selectCommand.Parameters.AddWithValue("@Category", category);

            SqliteDataReader query = selectCommand.ExecuteReader();

            // There should be only one row
            if (!query.Read())
            {
                AddToHourTable(boardid, tag, category);
            }

            db.Close();
        }

        private static void AddToHourTable(int boardid, string tag, string category)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            /*Update Hour Table for use later*/
            SqliteCommand insertHourCommand = new SqliteCommand();
            insertHourCommand.Connection = db;

            // Use parameterized query to prevent SQL injection attacks
            insertHourCommand.CommandText = "INSERT OR REPLACE INTO HourTable VALUES (@HourID, @BoardID, @Category, @Tag, @Hour);";
            insertHourCommand.Parameters.AddWithValue("@HourID", Guid.NewGuid().GetHashCode());
            insertHourCommand.Parameters.AddWithValue("@BoardID", boardid);
            insertHourCommand.Parameters.AddWithValue("@Category", category);
            insertHourCommand.Parameters.AddWithValue("@Tag", tag);
            insertHourCommand.Parameters.AddWithValue("@Hour", 0);

            insertHourCommand.ExecuteReader();

            db.Close();
        }

        public static List<string> GetAllCategoryInBoard(int boardid)
        {
            List<string> entries = new List<string>();

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand selectCommand = new SqliteCommand("SELECT DISTINCT Category from HourTable WHERE BoardID = @BoardID", db);

            selectCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                entries.Add(query.GetString(0));
            }

            db.Close();

            return entries;
        }

        public static List<string> GetAllTagInCategory(int boardid, string category)
        {
            List<string> entries = new List<string>();

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            /*Update Hour Table for use later*/
            SqliteCommand selectCommand = new SqliteCommand();
            selectCommand.Connection = db;

            // Use parameterized query to prevent SQL injection attacks
            selectCommand.CommandText = "SELECT DISTINCT TagName from HourTable WHERE BoardID = @BoardID " +
                "AND Category = @Category";

            selectCommand.Parameters.AddWithValue("@BoardID", boardid);
            selectCommand.Parameters.AddWithValue("@Category", category);

            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                entries.Add(query.GetString(0));
            }

            db.Close();

            return entries;
        }

        public static void UpdateHourForTag(int boardid, string category, string tag, float hour)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand updateCommand = new SqliteCommand();
            updateCommand.Connection = db;

            updateCommand.CommandText = "UPDATE HourTable SET Hour = @Hour " +
                "WHERE BoardID = @BoardID AND " +
                "Category = @Category AND " +
                "TagName = @Tag";
            updateCommand.Parameters.AddWithValue("@Hour", hour);
            updateCommand.Parameters.AddWithValue("@BoardID", boardid);
            updateCommand.Parameters.AddWithValue("@Category", category);
            updateCommand.Parameters.AddWithValue("@Tag", tag);

            SqliteDataReader query = updateCommand.ExecuteReader();

            db.Close();

        }

        public static void UpdateTask(int taskid, int boardid, string taskname, string taskdescription, string tag, string category)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand updateCommand = new SqliteCommand();
            updateCommand.Connection = db;

            updateCommand.CommandText = "UPDATE TaskTable SET TaskName = @NewTaskName, " +
                "BoardID = @NewBoardID, " +
                "TaskDescription = @NewTaskDescription, " +
                "TagName = @NewTagName, " +
                "Category = @NewCategory " +
                "WHERE TaskID = @TaskID";
            updateCommand.Parameters.AddWithValue("@NewBoardID", boardid);
            updateCommand.Parameters.AddWithValue("@NewTaskName", taskname);
            updateCommand.Parameters.AddWithValue("@NewTaskDescription", taskdescription);
            updateCommand.Parameters.AddWithValue("@NewTagName", tag);
            updateCommand.Parameters.AddWithValue("@NewCategory", category);
            updateCommand.Parameters.AddWithValue("@TaskID", taskid);

            SqliteDataReader query = updateCommand.ExecuteReader();

            db.Close();
        }

        public static List<TaskModel> GetAllTasksInBoard(int boardid)
        {
            List<TaskModel> entries = new List<TaskModel>();

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand selectCommand = new SqliteCommand("SELECT * from TaskTable WHERE BoardID = @BoardID", db);

            selectCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                entries.Add(new TaskModel
                {
                    ID = query.GetInt32(0),
                    BoardID = query.GetInt32(1),
                    Title = query.GetString(2),
                    Description = query.GetString(3),
                    Category = query.GetString(4),
                    Tag = query.GetString(5),
                });
            }

            db.Close();

            return entries;
        }

        public static void DeleteTaskFromBoard(int boardid, int taskid)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand deleteCommand = new SqliteCommand();
            deleteCommand.Connection = db;

            deleteCommand.CommandText = "DELETE FROM TaskTable " +
                "WHERE TaskID = @TaskID AND " +
                "BoardID = @BoardID";
            deleteCommand.Parameters.AddWithValue("@BoardID", boardid);
            deleteCommand.Parameters.AddWithValue("@TaskID", taskid);

            SqliteDataReader query = deleteCommand.ExecuteReader();

            db.Close();
        }

        public static void UpdateBoard(int boardid, string boardname)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand updateCommand = new SqliteCommand();
            updateCommand.Connection = db;

            updateCommand.CommandText = "UPDATE BoardTable SET BoardName = @BoardName " +
                "WHERE BoardID = @BoardID";
            updateCommand.Parameters.AddWithValue("@BoardName", boardname);
            updateCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader query = updateCommand.ExecuteReader();

            db.Close();
        }

        public static void DeleteBoard(int boardid)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand deleteBoardTableCommand = new SqliteCommand();
            deleteBoardTableCommand.Connection = db;

            deleteBoardTableCommand.CommandText = "DELETE FROM BoardTable " +
                "WHERE BoardID = @BoardID";
            deleteBoardTableCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader board_table_query = deleteBoardTableCommand.ExecuteReader();

            /*----------------------------------------------------*/
            SqliteCommand deleteTaskTableCommand = new SqliteCommand();
            deleteTaskTableCommand.Connection = db;

            deleteTaskTableCommand.CommandText = "DELETE FROM TaskTable " +
                "WHERE BoardID = @BoardID";
            deleteTaskTableCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader task_table_query = deleteTaskTableCommand.ExecuteReader();

            /*----------------------------------------------------*/
            SqliteCommand deleteHourTableCommand = new SqliteCommand();
            deleteHourTableCommand.Connection = db;

            deleteHourTableCommand.CommandText = "DELETE FROM HourTable " +
                "WHERE BoardID = @BoardID";
            deleteHourTableCommand.Parameters.AddWithValue("@BoardID", boardid);

            SqliteDataReader hour_table_query = deleteHourTableCommand.ExecuteReader();

            db.Close();
        }

        public static void UpdateAuthorName(string authorname)
        {
            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand updateCommand = new SqliteCommand();
            updateCommand.Connection = db;

            updateCommand.CommandText = "UPDATE AuthorTable SET AuthorName = @AuthorName " +
                "WHERE AuthorID = 0";
            updateCommand.Parameters.AddWithValue("@AuthorName", authorname);

            SqliteDataReader query = updateCommand.ExecuteReader();

            db.Close();
        }

        public static string GetAuthorName()
        {
            string entries = string.Empty;

            using SqliteConnection db = new SqliteConnection("Filename=ImaginaryNumberSpace.db");

            db.Open();

            SqliteCommand selectCommand = new SqliteCommand("SELECT AuthorName from AuthorTable WHERE AuthorID = 0", db);

            SqliteDataReader query = selectCommand.ExecuteReader();

            if (query.Read())
            {
                entries = query.GetString(0);
            }

            db.Close();

            return entries;
        }

    }
}

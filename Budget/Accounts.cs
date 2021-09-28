using System;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Globalization;

namespace budget
{
    class Accounts
    {
        public static void createNewFile(string aFilePath)
        {
            System.Diagnostics.Debug.Assert(!System.IO.File.Exists(aFilePath));

            var connectionString = new SqliteConnectionStringBuilder(string.Format("Data Source={0}", aFilePath))
            {
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
            var original = new SqliteConnection(connectionString);
            original.Open();

            var sql = @"CREATE TABLE 'transactions'(
                            'id'    INTEGER,
                            'Date'  TEXT,
                            'Description'   TEXT,
                            'Value' INTEGER,
                            'Balance'   INTEGER,
                            'AccountName'   TEXT,
                            'AccountNumber' TEXT,
                            'Comment'   TEXT,
                            'OriginID'  INTEGER,
                            'DestinationID' INTEGER,
                            PRIMARY KEY('id')
                        );
                        CREATE TABLE IF NOT EXISTS 'pots'(
                            'id'    INTEGER,
                            'title' TEXT,
                            'sequence'  INT,
                            'visible'   INT,
                            'pickable'  INT,
                            PRIMARY KEY('id')
                        );
                        CREATE TABLE IF NOT EXISTS 'budgets'(
                            'id'    INTEGER,
                            'potid' INTEGER,
                            'title' TEXT,
                            'amount'    INTEGER,
                            PRIMARY KEY('id')
                        );";

            var command = new SqliteCommand(sql, original);
            command.ExecuteNonQuery();

            original.Close();
        }

        public static void save(string aFilePath, IEnumerable<Transaction> aTransactions, IEnumerable<Pot> aPots, IEnumerable<Budget> aBudgets)
        {
            var backupName = aFilePath + ".bak";
            if (System.IO.File.Exists(backupName))
            {
                System.IO.File.Delete(backupName);
            }
            System.IO.File.Move(aFilePath, backupName);

            var connectionString = new SqliteConnectionStringBuilder(string.Format("Data Source={0}", aFilePath))
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                
            }.ToString();
            var original = new SqliteConnection(connectionString);
            original.Open();

            var createBudgets = "CREATE TABLE budgets ( id INTEGER PRIMARY KEY, potid INTEGER, title TEXT, amount INTEGER )";
            var createPots = "CREATE TABLE pots(id INTEGER PRIMARY KEY, title TEXT, sequence INT, visible INT, pickable INT)";
            var createTransactions = "CREATE TABLE transactions ( id INTEGER PRIMARY KEY, Date TEXT, Description TEXT, Value INTEGER, Balance INTEGER, AccountName TEXT, AccountNumber TEXT, Comment TEXT, OriginID INTEGER, DestinationID INTEGER )";

            new SqliteCommand(createBudgets, original).ExecuteNonQuery();
            new SqliteCommand(createPots, original).ExecuteNonQuery();
            new SqliteCommand(createTransactions, original).ExecuteNonQuery();

            using (var sqliteTransaction = original.BeginTransaction())
            {
                foreach (var transaction in aTransactions)
                {
                    var sql = "INSERT INTO transactions (id, Date, Description, Value, Balance, AccountName, AccountNumber, Comment, OriginID, DestinationID) VALUES " +
                                      $"({transaction.Id}, \"{transaction.Date}\", \"{transaction.Description}\", {transaction.ValueInPennies}, {transaction.BalanceInPennies}, \"{transaction.AccountName}\", \"{transaction.AccountNumber}\", \"{transaction.Comment}\", {toDB(transaction.OriginId)}, {toDB(transaction.DestinationId)})";
                    using (var command = new SqliteCommand(sql,
                                      original))
                    {
                        command.Transaction = sqliteTransaction;
                        command.ExecuteNonQuery();
                    }
                }

                foreach (var pot in aPots)
                {
                    using (var command = new SqliteCommand($"INSERT INTO pots (id, title, sequence, visible, pickable) VALUES ({pot.Id}, \"{pot.Title}\", {pot.Sequence}, {toDB(pot.IsVisible)}, {toDB(pot.IsPickable)})", original))
                    {
                        command.Transaction = sqliteTransaction;
                        command.ExecuteNonQuery();
                    }
                }

                foreach (var budget in aBudgets)
                {
                    using (var command = new SqliteCommand($"INSERT INTO budgets (id, potid, title, amount) VALUES ({budget.Id}, {toDB(budget.PotId)}, \"{budget.Title}\", {budget.AmountInPennies})", original))
                    {
                        command.Transaction = sqliteTransaction;
                        command.ExecuteNonQuery();
                    }
                }

                sqliteTransaction.Commit();
            }

            original.Close();
        }

        #region Utility methods
        public static Tuple<List<Transaction>,List<Pot>,List<Budget>> loadData(string aFilePath)
        {
            var mDatabase = loadDatabase(aFilePath);
            List<Transaction> transactions = new List<Transaction>();
            List<Budget> budgets = new List<Budget>();
            List<Pot> pots = new List<Pot>();

            if (mDatabase != null && mDatabase.State == System.Data.ConnectionState.Open)
            {
                {
                    var cmd = new SqliteCommand("SELECT id, Date, Description, Value, OriginID, DestinationID, Balance, Comment, AccountName, AccountNumber FROM transactions", mDatabase);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Transaction transaction = extractTransaction(reader);
                        transactions.Add(transaction);
                    }
                }

                {
                    var cmd = new SqliteCommand("SELECT id, potid, title, amount FROM budgets", mDatabase);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Budget budget = extractBudget(reader);
                        budgets.Add(budget);
                    }
                }

                {
                    var cmd = new SqliteCommand("SELECT id, title, sequence, visible, pickable FROM pots", mDatabase);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Pot pot = extractPot(reader);
                        pots.Add(pot);
                    }
                }
            }

            return new Tuple<List<Transaction>, List<Pot>, List<Budget>>((List<Transaction>)transactions, (List<Pot>)pots, (List<Budget>)budgets);
        }

        private static SqliteConnection loadDatabase(string aFilePath)
        {
            Debug.Assert(System.IO.File.Exists(aFilePath)
                && System.IO.Path.GetExtension(aFilePath) == ".sqlite3");

            var connectionString = new SqliteConnectionStringBuilder(string.Format("Data Source={0}", aFilePath))
            {
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();
            var original = new SqliteConnection(connectionString);
            original.Open();

            connectionString = new SqliteConnectionStringBuilder("Data Source=:memory:")
            {
                Mode = SqliteOpenMode.ReadWrite
            }.ToString();
            var database = new SqliteConnection(connectionString);
            database.Open();

            original.BackupDatabase(database);
            original.Close();

            migrateDatabase(database);

            return database;
        }

        private static string toDB(int? aValue)
        {
            return aValue.HasValue ? aValue.Value.ToString() : "null";
        }

        private static int toDB(bool aValue)
        {
            return aValue ? 1 : 0;
        }

        private static Transaction extractTransaction(SqliteDataReader aReader)
        {
            return new Transaction
            {
                Id = aReader.GetInt32(0),
                Date = aReader.GetString(1),
                Description = aReader.GetString(2),
                ValueInPennies = aReader.GetInt32(3),
                OriginId = aReader.IsDBNull(4) ? (int?)null : aReader.GetInt32(4),
                DestinationId = aReader.IsDBNull(5) ? (int?)null : aReader.GetInt32(5),
                BalanceInPennies = aReader.GetInt32(6),
                Comment = aReader.GetString(7),
                AccountName = aReader.GetString(8),
                AccountNumber = aReader.GetString(9)
            };
        }

        private static Pot extractPot(SqliteDataReader aReader)
        {
            return new Pot
            {
                Id = aReader.GetInt32(0),
                Title = aReader.GetString(1),
                Sequence = aReader.GetInt32(2),
                IsVisible = aReader.GetInt32(3) == 1,
                IsPickable = aReader.GetInt32(4) == 1,
            };
        }

        private static Budget extractBudget(SqliteDataReader aReader)
        {
            return new Budget
            {
                Id = aReader.GetInt32(0),
                PotId = aReader.IsDBNull(1) ? (int?)null : aReader.GetInt32(1),
                Title = aReader.GetString(2),
                AmountInPennies = aReader.GetInt32(3)
            };
        }

        private static bool columnExists(SqliteConnection aConnection, string aTable, string aColumn)
        {
            var cmd = new SqliteCommand($"PRAGMA table_info(\"{aTable}\")", aConnection);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1) == aColumn)
                {
                    return true;
                }
            }
            return false;
        }

        private static void migrateDatabase(SqliteConnection aConnection)
        {
            if (!columnExists(aConnection, "pots", "pickable"))
            {
                new SqliteCommand("ALTER TABLE pots ADD COLUMN pickable INT", aConnection).ExecuteNonQuery();
                new SqliteCommand("UPDATE pots SET pickable=1", aConnection).ExecuteNonQuery();
            }
        }
        #endregion
    }
}

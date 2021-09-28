using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace budget
{
    static class Util
    {
        public static void AddRange<T>(this ObservableCollection<T> aDestination, IEnumerable<T> aOrigin)
        {
            foreach (var value in aOrigin)
            {
                aDestination.Add(value);
            }
        }

        public static void register<T>(IEnumerable<T> aObjects, PropertyChangedEventHandler aHandler) where T : INotifyPropertyChanged
        {
            foreach (var obj in aObjects)
            {
                obj.PropertyChanged += aHandler;
            }
        }
        public static void unregister<T>(IEnumerable<T> aObjects, PropertyChangedEventHandler aHandler) where T : INotifyPropertyChanged
        {
            foreach (var obj in aObjects)
            {
                obj.PropertyChanged -= aHandler;
            }
        }

        public static IEnumerable<Transaction> importTransactions(string aFilePath)
        {
            bool exists = System.IO.File.Exists(aFilePath);
            Debug.Assert(exists);

            if (exists)
            {
                var file = System.IO.File.OpenRead(aFilePath);
                List<List<string>> rows = loadCSV(file);
                var converter = new PenniesTypeConverter();
                var transactions = new List<Transaction>();

                rows.RemoveAt(0); // Drop the column headers

                // Date, Type, Description, Value, Balance, Account Name, Account Number
                foreach (var row in rows)
                {
                    Debug.Assert(row.Count >= 7);

                    var transaction = new Transaction
                    {
                        Id = -1,
                        Date = row[0],
                        Description = row[2],
                        ValueInPennies = (int)converter.ConvertBack(row[3], typeof(int), null, null),
                        BalanceInPennies = (int)converter.ConvertBack(row[4], typeof(int), null, null),
                        AccountName = row[5],
                        AccountNumber = row[6]
                    };
                    transactions.Add(transaction);
                }

                return transactions;

            }

            return new List<Transaction>();
        }

        private static List<List<string>> loadCSV(System.IO.FileStream aFileStream)
        {
            Debug.Assert(aFileStream.CanRead);

            List<List<string>> result = new List<List<string>>();

            if (aFileStream.CanRead)
            {
                string token = string.Empty;
                List<string> row = new List<string>();
                bool escaped = false;
                char escapedWith = ' ';
                while (aFileStream.Position < aFileStream.Length)
                {
                    var c = (char)aFileStream.ReadByte();
                    if ((c == ',' && !escaped) || c == '\n')
                    {
                        row.Add(token.Trim(" \t\n\r\'\"".ToArray()));
                        token = string.Empty;
                    }
                    if (c == '\n' && row.Count > 0)
                    {
                        if (string.Join("", row) != string.Empty)
                        {
                            result.Add(row);
                        }
                        row = new List<string>();
                    }
                    if ((escaped && escapedWith == c) || (!escaped && "\"\'".Contains(c)))
                    {
                        escaped = !escaped;
                        escapedWith = c;
                    }
                    if (!"\n,".Contains(c))
                    {
                        token += c;
                    }
                }
            }

            return result;
        }
    }
}

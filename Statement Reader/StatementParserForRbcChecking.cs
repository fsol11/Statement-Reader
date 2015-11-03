using System;
using System.IO;
using System.Linq;

namespace Statement_Reader
{
    internal class StatementParserForRbcChecking : IStatementParser
    {
        public Statement ExtractStatement(string filename, bool generateInterimFiles)
        {
            var blocklist = Routines.ExtractBlockList(filename);

            foreach (var blocks in blocklist)
            {
                // Removing barcode and code on the left side
                var item1 = Routines.FindTextBlock(blocks, "Details of your account activity", Routines.TextSearchOptions.StartsWith);
                if (item1 != null)
                    blocks.Where(block => block.Left < item1.Left).ToList().ForEach(block => blocks.Remove(block));

                // Removing the numbers on the right side of the form
                var item2 = Routines.FindTextBlock(blocks, "Balance ($)", Routines.TextSearchOptions.StartsWith);
                if (item2 != null)
                    blocks.Where(block => block.Left > item2.Right).ToList().ForEach(block => blocks.Remove(block));

                // Adding minus to Withdrawals
                var withdrawals = Routines.FindTextBlock(blocks, "Withdrawals ($)", Routines.TextSearchOptions.ExactMatch);
                if (withdrawals != null)
                    blocks.Where(block => block.Top < withdrawals.Top && Math.Abs(block.Right - withdrawals.Right) < 2).ToList().ForEach(block => block.Text = $"-{block.Text}");

                // Removing Balance amounts
                var balance = Routines.FindTextBlock(blocks, "Balance ($)", Routines.TextSearchOptions.ExactMatch);
                if (balance != null)
                    blocks.Where(block => block.Top < balance.Top && Math.Abs(block.Right - balance.Right) < 2).ToList().ForEach(block => blocks.Remove(block));

            }

            var textStream = Routines.GetStream(Routines.BlockListToString(blocklist));

            if (generateInterimFiles)
                Routines.WriteStreamToFile(textStream, filename + ".txt");

            using (var textReader = new StreamReader(textStream))
            {
                var statement = new Statement();
                var previousTransaction = new Transaction();
                var statementDateNoted = false;
                var readTransactions = false;

                while (!textReader.EndOfStream)
                {
                    var line = textReader.ReadLine();
                    if (line == null || line.Length == 1) // Ignoring 1 character lines
                        continue;

                    var items = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!statementDateNoted && items[0].StartsWith("From ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var s = items[0].Substring(items[0].IndexOf("to ", StringComparison.Ordinal) + 3);
                        statement.StatementDate = DateTime.Parse(s);
                        statementDateNoted = true;
                    }
                    else if (!readTransactions && items.Last() == "Balance ($)")
                    {
                        readTransactions = true;
                    }
                    else if (readTransactions && Routines.IsNumeric(items.Last()) && (items.Length == 2 || (items.Length == 3 && Routines.IsDayAndShortMonth(items.First()))))
                    {
                        var t = new Transaction();

                        if (items.Length == 3) // Process date
                        {
                            var tokens = items[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var month = Routines.ShortMonthToInt(tokens[1]);
                            var day = Routines.StrToInt(tokens[0]);
                            var year = statement.StatementDate.Year -
                                       ((statement.StatementDate.Month == 1 && month == 12) ? 1 : 0);

                            t.DateTime = new DateTime(year, month, day);
                            t.Description = items[1];
                            t.Value = Routines.StrToNumber(items[2]);
                        }
                        else
                        {
                            t.DateTime = previousTransaction.DateTime;
                            t.Description = items[0];
                            t.Value = Routines.StrToNumber(items[1]);

                        }

                        // Ignoring payments
                        statement.Transactions.Add(t);

                        previousTransaction = t;
                    }
                }

                return statement;
            }
        }
    }
}

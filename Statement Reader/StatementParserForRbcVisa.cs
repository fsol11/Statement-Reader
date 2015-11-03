using System;
using System.IO;
using System.Linq;
using Telerik.Windows.Controls.Animation;

namespace Statement_Reader
{
    internal class StatementParserForRbcVisa : IStatementParser
    {
        public Statement ExtractStatement(string filename, bool generateInterimFiles)
        {
            var textStream = Routines.ExtractPdfText(filename, new TextExtractStrategy());

            if (generateInterimFiles)
                Routines.WriteStreamToFile(textStream, filename + ".txt");

            using (var textReader = new StreamReader(textStream))
            {
                var statement = new Statement();
                var statementDateNoted = false;

                while (!textReader.EndOfStream)
                {
                    var line = textReader.ReadLine();
                    var items = line?.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!statementDateNoted && items[0].StartsWith("Statement From", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var dates = items[0].Substring("Statement From ".Length).Split(new[] { " TO " }, StringSplitOptions.RemoveEmptyEntries);
                        if (dates.Length == 2)
                        {
                            statement.StatementDate = DateTime.Parse(dates[1]);
                            statementDateNoted = true;
                        }
                    }
                    else if (items.Length == 4 && Routines.IsNumeric(items[3]) && Routines.IsShortMonthAndDay(items[0]) && Routines.IsShortMonthAndDay(items[1])) // Process date
                        {
                            var t = new Transaction();

                            var tokens = items[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            var month = Routines.ShortMonthToInt(tokens[0]);
                            var day = Routines.StrToInt(tokens[1]);
                            var year = statement.StatementDate.Year - ((statement.StatementDate.Month == 1 && month == 12) ? 1 : 0);
                            t.DateTime = new DateTime(year, month, day);

                            t.Description = items[2];
                            t.Value = -Routines.StrToNumber(items[3]);

                            statement.Transactions.Add(t);
                        }
                }

                return statement;
            }
        }
    }
}

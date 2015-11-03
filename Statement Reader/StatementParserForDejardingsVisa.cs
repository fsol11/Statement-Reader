using System;
using System.IO;

namespace Statement_Reader
{
    class StatementParserForDejardingsVisa : IStatementParser
    {

        public Statement ExtractStatement(string filename, bool generateInterimFiles)
        {
            var blocklist = Routines.ExtractBlockList(filename);
            var textStream = Routines.GetStream(Routines.BlockListToString(blocklist));

            //var textStream = Routines.ExtractPdfText(filename, new TextExtractStrategy());

            if (generateInterimFiles)
                Routines.WriteStreamToFile(textStream, filename + ".txt");

            using (var textReader = new StreamReader(textStream))
            {
                var statement = new Statement();
                var statementDateNoted = false;

                while (!textReader.EndOfStream)
                {
                    var line = textReader.ReadLine();
                    var items = line?.Split('\t');

                    if (!statementDateNoted && line == "transactions")
                    {
                        line = textReader.ReadLine();
                        items = line?.Split('\t');


                        statement.StatementDate = new DateTime(Routines.StrToInt(items[2]), Routines.StrToInt(items[1]), Routines.StrToInt(items[0]));

                        statementDateNoted = true;
                    }

                    else if ((items?.Length == 7) &&
                             (Routines.IsNumeric(items[0]) &&
                              Routines.IsNumeric(items[1]) &&
                              Routines.IsNumeric(items[2]) &&
                              Routines.IsNumeric(items[3]) &&
                              Routines.IsNumeric(items[4]) &&
                              Routines.IsNumeric(items[6])))
                    {

                        var month = Routines.StrToInt(items[1]);
                        var day = Routines.StrToInt(items[0]);
                        var year = (statement.StatementDate.Month == 1 && month == 12) ? statement.StatementDate.Year - 1 : statement.StatementDate.Year;

                        var t = new Transaction
                        {
                            DateTime = new DateTime(year, month, day),
                            Description = items[5],
                            Value = -Routines.StrToNumber(items[6])
                        };

                        // Ignoring payments
                        if (!string.Equals(t.Description, "PAYMENT THANK YOU", StringComparison.InvariantCultureIgnoreCase))
                            statement.Transactions.Add(t);
                    }
                }

                return statement;
            }
        }
    }
}

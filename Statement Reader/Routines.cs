using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace Statement_Reader
{
    public static class Routines
    {
        public enum TextSearchOptions
        {
            StartsWith,
            Contains,
            ExactMatch
        }

        private static readonly string[] ShortMonthNames =
        {
            "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep",
            "oct", "nov", "dec"
        };

        public static bool IsDayAndShortMonth(string s)
        {
            var tokens = s.Split(' ');
            return tokens.Length == 2 && IsNumeric(tokens[0]) && IsShortMonthName(tokens[1]);
        }

        public static bool IsShortMonthAndDay(string s)
        {
            var tokens = s.Split(' ');
            return tokens.Length == 2 && IsNumeric(tokens[1]) && IsShortMonthName(tokens[0]);
        }

        public static bool IsShortMonthName(string s)
        {
            return ShortMonthNames.Contains(s.ToLower());
        }

        public static int ShortMonthToInt(string s)
        {
            return Array.FindIndex(ShortMonthNames, a => a.Equals(s, StringComparison.InvariantCultureIgnoreCase)) + 1;
        }

        public static bool IsNumeric(object s)
        {
            try
            {
                StrToNumber(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static double StrToNumber(object str)
        {
            var s = str as string;

            if (string.IsNullOrEmpty(s))
                throw new ArgumentOutOfRangeException(nameof(str));

            s = s.Replace(",", string.Empty);

            var credit = s.EndsWith("CR");
            if (credit)
                s = "-" + s.Substring(0, s.Length - 2).Trim();

            return double.Parse(s, NumberStyles.Currency);
        }

        public static int StrToInt(object s)
        {
            return (int)StrToNumber(s);
        }


        public static void WriteStreamToFile(Stream stream, string filename)
        {
            if(File.Exists(filename))
                File.Delete(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using (var outputStream = new FileStream(filename, FileMode.CreateNew))
                stream.CopyTo(outputStream);
            stream.Seek(0, SeekOrigin.Begin);
        }

        public static void WriteStatementToCsv(this Statement statement, StreamWriter csvStream)
        {
            foreach (var transaction in statement.Transactions)
            {
                csvStream.WriteLine($"{transaction.DateTime:yyyy-MM-dd},{transaction.Description.Replace(",", "_")},{transaction.Value:##.00},{transaction.Category}");
            }
        }

        public static MemoryStream GetStream(string s)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(s));

        }

        public static MemoryStream BlockListToStream(List<List<TextBlock>> blockList, bool expandSpacesToMultipleTabs)
        {
            return GetStream(BlockListToString(blockList, expandSpacesToMultipleTabs));
        }

        public static string BlockListToString(List<List<TextBlock>> blockList, bool expandSpacesToMultipleTabs = false)
        {
            var result = new StringBuilder();
            TextBlock lastBlock = null;

            foreach (var blocks in blockList)
            {
                if (blocks.Count == 0) continue;

                // Sorint the text blocks
                var maxY = blocks.Max(t => t.TopLeft[1]);
                blocks.Sort(
                    (a, b) =>
                        string.Compare(
                            string.Format(
                                $"{maxY - a.TopLeft[1]:0000}{a.TopLeft[0]:0000}"),
                            string.Format(
                                $"{maxY - b.TopLeft[1]:0000}{b.TopLeft[0]:0000}"),
                            StringComparison.Ordinal));


                foreach (var block in blocks)
                {

                    var newText = block.Text;

                    var isBlank = result.Length == 0;


                    float spaceLength = -1;
                    float tabLength = -1;

                    if (spaceLength.Equals(-1))
                    {
                        spaceLength = (block.BottomRight[0] - block.TopLeft[0]) / newText.Length;
                        tabLength = spaceLength * 8;
                    }

                    if (lastBlock != null)
                    {
                        if (!isBlank && block.TopLeft[1] < lastBlock.BottomRight[1])
                            result.Append("\r\n");
                        else
                        {
                            var distance = lastBlock.TopRight.Subtract(lastBlock.TopLeft).Length;

                            if (distance > spaceLength)
                            {
                                result.Append(expandSpacesToMultipleTabs
                                    ? new string('\t', ((int)(distance / tabLength)) + 1)
                                    : "\t");
                            }
                            else if (distance > 0.6)
                                result.Append(" ");
                        }
                    }

                    result.Append(newText);

                    lastBlock = block;
                }
            }

            return result.ToString();
        }
        public static MemoryStream ExtractPdfText(string filename, ITextExtractionStrategy textExtractionStrategy)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File: [" + filename + "] does not exist.");
            var textStream = new MemoryStream();

            using (var output = new StreamWriter(textStream, Encoding.UTF8, 1024, true))
            {
                using (var pdfReader = new PdfReader(filename))
                {
                    for (var page = 1; page <= pdfReader.NumberOfPages; page++)
                    {
                        var text =
                            Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8,
                                Encoding.Default.GetBytes(PdfTextExtractor.GetTextFromPage(pdfReader, page, textExtractionStrategy))));

                        output.WriteLine(text);
                        output.WriteLine("*********************************************************************************************");
                    }
                }
            }

            textStream.Seek(0, SeekOrigin.Begin);
            return textStream;
        }

        public static List<List<TextBlock>> ExtractBlockList(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File: [" + filename + "] does not exist.");
            var strategy = new TextExtractionStrategyForBlockList();

            using (var pdfReader = new PdfReader(filename))
            {
                for (var page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);
                }
            }

            return strategy.BlockList;
        }

        public static TextBlock FindTextBlock(List<List<TextBlock>> blockList, string toSearch, TextSearchOptions option)
        {
            return (from block in blockList
                    select FindTextBlock(block, toSearch, option)).FirstOrDefault();
        }

        public static TextBlock FindTextBlock(List<TextBlock> block, string toSearch, TextSearchOptions option)
        {
            var culture = new CultureInfo(string.Empty); // <- Invariant culture

            return (
                    from item in block
                    where
                        (option == TextSearchOptions.StartsWith && item.Text.StartsWith(toSearch, StringComparison.InvariantCultureIgnoreCase)) ||
                        (option == TextSearchOptions.Contains && culture.CompareInfo.IndexOf(item.Text, toSearch, CompareOptions.IgnoreCase) > -1) ||
                        (option == TextSearchOptions.ExactMatch && string.Equals(item.Text, toSearch, StringComparison.CurrentCultureIgnoreCase))
                    select item).FirstOrDefault();
        }

    }
}

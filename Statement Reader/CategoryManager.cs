using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace Statement_Reader
{
    class CategoryManager
    {
        private static List<CategoryCondition> _categoryConditions = new List<CategoryCondition>();

        static CategoryManager()
        {
            ReadCategoryonditions();
        }

        private static void ReadCategoryonditions()
        {
            var filename = Path.Combine(Directory.GetCurrentDirectory(), "CategoryConditions.csv");

            if (!File.Exists(filename))
            {
                MessageBox.Show($"Cannot find file: {filename}", "Error");
                return;
            }

            _categoryConditions = new List<CategoryCondition>();
            var lines = File.ReadAllLines(filename);

            // Adding categories to the list
            foreach (var tokens in lines.Select(line => line.Split(',')).Where(tokens => tokens.Length == 3))
                _categoryConditions.Add(
                    new CategoryCondition(
                        tokens[0].Trim(),
                        (Routines.TextSearchOptions) Enum.Parse(typeof (Routines.TextSearchOptions), tokens[1].Trim()),
                        (tokens[2].StartsWith("\"") && tokens[2].EndsWith("\""))
                            ? tokens[2].Substring(1, tokens[2].Length - 2)
                            : tokens[2].Trim()));
        }

        public static string Categorize(string description)
        {
            var culture = new CultureInfo(string.Empty); // <- Invariant culture
            
            var c = _categoryConditions.FirstOrDefault(condition => !string.IsNullOrEmpty(condition.Expression) &&
                                                                             (
                                                                                 (condition.Option == Routines.TextSearchOptions.StartsWith &&
                                                                                  description.StartsWith(condition.Expression, StringComparison.InvariantCultureIgnoreCase))
                                                                                 ||
                                                                                 (condition.Option == Routines.TextSearchOptions.Contains &&
                                                                                  culture.CompareInfo.IndexOf(description, condition.Expression, CompareOptions.IgnoreCase) > -1)
                                                                             ));

            return c != null ? c.Category : string.Empty;
        }
    }
}

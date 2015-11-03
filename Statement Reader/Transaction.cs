using System;

namespace Statement_Reader
{
    public class Transaction
    {
        public double Value { get; set; }
        public DateTime DateTime { get; set; }
        public string Category { get; private set; }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                Category = CategoryManager.Categorize(Description);
            }
        }

        private string _description;
    }
}

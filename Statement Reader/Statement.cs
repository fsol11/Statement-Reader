using System;
using System.Collections.Generic;
using System.ComponentModel;
namespace Statement_Reader
{
    public class Statement
    {
        public string Filename { get; set; }
        public List<Transaction> Transactions = new List<Transaction>();
        public DateTime StatementDate;
        public DateTime FromDate;
    }
}

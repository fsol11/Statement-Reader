namespace Statement_Reader
{
    public interface IStatementParser
    {
        Statement ExtractStatement(string filename, bool generateInterimFiles);
    }
}

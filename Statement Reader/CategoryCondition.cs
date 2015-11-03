using System.Security.RightsManagement;

namespace Statement_Reader
{
    internal class CategoryCondition
    {


        public CategoryCondition(string category, Routines.TextSearchOptions option, string expression)
        {
            Expression = expression;
            Category = category;
            Option = option;
        }

        public readonly string Expression;
        public readonly Routines.TextSearchOptions Option;
        public readonly string Category;
    }
}
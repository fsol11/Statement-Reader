using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf.parser;

namespace Statement_Reader
{

    class TextExtractStrategy : ITextExtractionStrategy
    {
        private List<TextRenderInfo> _blocks = new List<TextRenderInfo>();

        public TextExtractStrategy(bool expandSpacesToMultipleTabs = false)
        {
            _expandSpacesToMultipleTabs = expandSpacesToMultipleTabs;
        }

        /// used to store the resulting String.
        private readonly bool _expandSpacesToMultipleTabs;


        /// @since 5.0.1
        public virtual void BeginTextBlock()
        {
        }

        /// @since 5.0.1
        public virtual void EndTextBlock()
        {
        }

        /// Returns the result so far.
        ///             @return  a String with the resulting text.
        public virtual string GetResultantText()
        {
            return GetResult();
        }


        /// <summary>
        /// Renders text from list of blocks
        /// </summary>
        /// <returns></returns>
        private string GetResult()
        {
            var result = new StringBuilder();
            var lastStart = new Vector(-1, 0, 0);
            var lastEnd = new Vector(-1, 0, 0);

            // Sorint the text blocks
            var maxY = _blocks.Max(t => t.GetBaseline().GetStartPoint()[1]);
            _blocks.Sort((a, b) => string.Compare(string.Format($"{maxY - a.GetBaseline().GetStartPoint()[1]:0000}{a.GetBaseline().GetStartPoint()[0]:0000}"), string.Format($"{maxY - b.GetBaseline().GetStartPoint()[1]:0000}{b.GetBaseline().GetStartPoint()[0]:0000}"), StringComparison.Ordinal));


            foreach (var renderInfo in _blocks)
            {

                var newText = renderInfo.GetText();

                var isBlank = result.Length == 0;
                var isEolRequired = false;


                var baseline = renderInfo.GetBaseline();
                var start = baseline.GetStartPoint();
                var end = baseline.GetEndPoint();

                float spaceLength = -1;
                float tabLength = -1;

                if (spaceLength.Equals(-1))
                {
                    // Calculating length of space and tab 
                    // This calculation only happens one time
                    spaceLength = (end[0] - start[0]) / newText.Length;
                    tabLength = spaceLength * 8;
                }

                if (!isBlank)
                {
                    //if (lastEnd.Subtract(lastStart).Cross(lastStart.Subtract(start)).LengthSquared / lastEnd.Subtract(lastStart).LengthSquared > 1.0)
                    //isEolRequired = true;

                    if (Math.Round(start[1], 0) < Math.Round(lastEnd[1], 0))
                        isEolRequired = true;
                }

                if (!lastStart[0].Equals(-1))
                {
                    if (isEolRequired)
                        result.Append("\r\n");
                    else
                    {
                        var distance = new Vector(lastEnd[0], lastStart[1], lastStart[2]).Subtract(start).Length;

                        if (distance > spaceLength)
                        {
                            result.Append(_expandSpacesToMultipleTabs ? new string('\t', ((int)(distance / tabLength)) + 1) : "\t");
                        }
                        else if (distance > 0.6)
                            result.Append(" ");
                    }
                }

                result.Append(newText);

                lastStart = start;
                lastEnd = end;
            }

            // Clear the list of blocks
            _blocks  = new List<TextRenderInfo>();

            return result.ToString();
        }


        /// Captures text using a simplified algorithm for inserting hard returns and spaces
        ///             @param   renderInfo  render info
        public virtual void RenderText(TextRenderInfo renderInfo)
        {
            _blocks.Add(renderInfo);
        }

        /// no-op method - this renderer isn't interested in image events
        ///             @see com.itextpdf.text.pdf.parser.RenderListener#renderImage(com.itextpdf.text.pdf.parser.ImageRenderInfo)
        ///             @since 5.0.1
        public virtual void RenderImage(ImageRenderInfo renderInfo)
        {
        }
    }
}

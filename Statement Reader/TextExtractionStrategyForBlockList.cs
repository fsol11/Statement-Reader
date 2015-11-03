using System;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text.pdf.parser;

namespace Statement_Reader
{
    class TextExtractionStrategyForBlockList : ITextExtractionStrategy
    {
        public List<List<TextBlock>> BlockList { get; } = new List<List<TextBlock>>();

 
        private readonly List<TextBlock> _blocks = new List<TextBlock>();

        /// used to store the resulting String.

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

        private string GetResult()
        {
            var maxY = _blocks.Max(t => t.TopLeft[1]);
            _blocks.Sort((a, b) => string.Compare(string.Format($"{maxY - a.TopLeft[1]:0000}{a.TopLeft[0]:0000}"), string.Format($"{maxY - b.TopLeft[1]:0000}{b.TopLeft[0]:0000}"), StringComparison.Ordinal));

            // Combining blocks



            TextBlock lastBlock = null;
            var result = new List<TextBlock>();
            var spaceLength = (_blocks[0].BottomRight[0] - _blocks[0].TopLeft[0]) / _blocks[0].Text.Length * 1.05;
            float distance = -1;

            foreach (var block in _blocks)
            {
                if (lastBlock != null)
                    distance = lastBlock.TopRight.Subtract(block.TopLeft).Length;

                if (distance > -1 && distance <= spaceLength)
                {
                    // Combine current block with previous block
                    var prev = result.Last();
                    prev.Text += ((distance > 0.6) ? " " : string.Empty) + block.Text;
                    prev.Right = block.Right;
                    prev.Bottom = block.Bottom;
                }
                else
                    result.Add(new TextBlock
                    {
                        Text = block.Text,
                        TopLeft = block.TopLeft,
                        BottomRight = block.BottomRight
                    });

                lastBlock = block;
            }



            BlockList.Add(result);
            _blocks.Clear();

            return string.Empty;
        }


        /// Captures text using a simplified algorithm for inserting hard returns and spaces
        ///             @param   renderInfo  render info
        public virtual void RenderText(TextRenderInfo renderInfo)
        {
            _blocks.Add(new TextBlock
            {
                Text = renderInfo.GetText(),
                TopLeft = renderInfo.GetBaseline().GetStartPoint(),
                BottomRight = renderInfo.GetBaseline().GetEndPoint()
            });
        }

        /// no-op method - this renderer isn't interested in image events
        ///             @see com.itextpdf.text.pdf.parser.RenderListener#renderImage(com.itextpdf.text.pdf.parser.ImageRenderInfo)
        ///             @since 5.0.1
        public virtual void RenderImage(ImageRenderInfo renderInfo)
        {
        }
    }
}

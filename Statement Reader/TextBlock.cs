using System;
using iTextSharp.text.pdf.parser;

namespace Statement_Reader
{
    public class TextBlock
    {
        public string Text { get; set; }

        public Vector TopLeft
        {
            get { return new Vector(Left, Top, 1); }
            set
            {
                Top = value[1];
                Left = value[0];
            }
        }

        public Vector BottomRight
        {
            get
            {
                return new Vector(Right, Bottom, 1);
            }
            set
            {
                Bottom = value[1];
                Right = value[0];
            }
        } 
        public Vector BottomLeft
        {
            get
            {
                return new Vector(Left, Bottom, 1);
            }
            set
            {
                Bottom = value[1];
                Left = value[0];
            }
        }

        public Vector TopRight
        {
            get
            {
                return new Vector(Right, Top, 1);
            }
            set
            {
                Top = value[1];
                Right = value[0];
            }
        }

        public float Left { get; set; }

        public float Right { get; set; }

        public float Top { get; set; }

        public float Bottom { get; set; }

        public float Width => Math.Abs(Right - Left);

        public float Height => Math.Abs(Top - Bottom);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace TheDotFactory
{
    class FontFile
    {
        protected OutputConfiguration m_outputConfig;
        protected MainForm.FontInfo   m_fontInfo;
        protected string              m_output;
        protected string m_nl = "\n";
        protected string m_indent = "    ";
        Stack<string> m_indentStack;

        public FontFile(OutputConfiguration OutputConfig, MainForm.FontInfo FontInfo)
        {
            m_outputConfig = OutputConfig;
            m_fontInfo = FontInfo;
            m_indentStack = new Stack<string>();
        }

        public void PushIndent(string indent)
        {
            m_indentStack.Push(m_indent);
            m_indent = indent; 
        }

        public void PopIndent()
        {
            m_indent = m_indentStack.Pop();
        }

        public void GenerateLine(string line)
        {
            m_output += m_indent + line + m_nl;
        }

        public virtual string EndOfLineComment(string comment)
        {
            return "# " + comment;
        }


        public void GenerateBitmapData()
        {
            // iterate through letters
            for (int charIdx = 0; charIdx < m_fontInfo.characters.Length; ++charIdx)
            {
                // skip empty bitmaps
                if (m_fontInfo.characters[charIdx].bitmapToGenerate == null) continue;


                GenerateLine(
                    EndOfLineComment(
                        String.Format("@{0} '{1}' ({2} pixels wide)",
                                    m_fontInfo.characters[charIdx].offsetInBytes,
                                    m_fontInfo.characters[charIdx].character,
                                    m_fontInfo.characters[charIdx].width)));

                // now add letter array
                var charInfo = m_fontInfo.characters[charIdx];
                var bitmap = m_fontInfo.characters[charIdx].bitmapToGenerate;
                GenerateStringFromPageArray(bitmap.Width, bitmap.Height, charInfo.pages);

                // space out
                if (charIdx != m_fontInfo.characters.Length - 1)
                {
                    m_output += m_nl;
                }
              
            }
        }

        // generate string from character info
        private void GenerateStringFromPageArray(int width, int height, ArrayList pages)
        {
            // generate the data rows
            string[] data;
            generateData(width, height, pages, m_outputConfig.bitLayout, out data);

            // generate the visualizer
            string[] visualizer;
            generateVisualizer(width, height, pages, m_outputConfig.bitLayout, out visualizer);

            // build the result string
            StringBuilder resultString = new StringBuilder();

            // output row major data
            if (m_outputConfig.bitLayout == OutputConfiguration.BitLayout.RowMajor)
            {
                // the visualizer is drawn after the data on the same rows, so they must have the same length
                System.Diagnostics.Debug.Assert(data.Length == visualizer.Length);

                // output the data and visualizer together
                if (m_outputConfig.lineWrap == OutputConfiguration.LineWrap.AtColumn)
                {
                    // one line per row
                    for (int row = 0; row != data.Length; ++row)
                    {
                        resultString.Append(m_indent).Append(data[row]).Append(EndOfLineComment(visualizer[row])).Append(m_nl);
                    }
                }
                else if (m_outputConfig.lineWrap == OutputConfiguration.LineWrap.AtBitmap)
                {
                    // one line per bitmap
                    resultString.Append(m_indent);
                    for (int row = 0; row != data.Length; ++row)
                    {
                        resultString.Append(data[row]);
                    }
                    resultString.Append(m_nl);
                }
            }

            // output column major data
            else if (m_outputConfig.bitLayout == OutputConfiguration.BitLayout.ColumnMajor)
            {
                // output the visualizer
                for (int row = 0; row != visualizer.Length; ++row)
                {
                    resultString.Append(m_indent).Append(visualizer[row]).Append(m_nl);
                }

                // output the data
                if (m_outputConfig.lineWrap == OutputConfiguration.LineWrap.AtColumn)
                {
                    // one line per row
                    for (int row = 0; row != data.Length; ++row)
                    {
                        resultString.Append(m_indent).Append(data[row]).Append(m_nl);
                    }
                }
                else if (m_outputConfig.lineWrap == OutputConfiguration.LineWrap.AtBitmap)
                {
                    // one line per bitmap
                    resultString.Append(m_indent);
                    for (int row = 0; row != data.Length; ++row)
                    {
                        resultString.Append(data[row]);
                    }
                    resultString.Append(m_nl);
                }
            }

            // return the result
            m_output += resultString.ToString();
        }

        // builds a string array of the data in 'pages'
        private void generateData(int width, int height, ArrayList pages, OutputConfiguration.BitLayout layout, out string[] data)
        {
            int colCount = (layout == OutputConfiguration.BitLayout.RowMajor) ? (width + 7) / 8 : width;
            int rowCount = (layout == OutputConfiguration.BitLayout.RowMajor) ? height : (height + 7) / 8;

            data = new string[rowCount];

            // iterator over rows
            for (int row = 0; row != rowCount; ++row)
            {
                data[row] = "";

                // iterator over columns
                for (int col = 0; col != colCount; ++col)
                {
                    // get the byte to output
                    int page = (byte)pages[row * colCount + col];

                    // add leading character
                    data[row] += m_outputConfig.byteLeadingString;

                    // check format
                    if (m_outputConfig.byteFormat == OutputConfiguration.ByteFormat.Hex)
                    {
                        // convert byte to hex
                        data[row] += page.ToString("X").PadLeft(2, '0');
                    }
                    else
                    {
                        // convert byte to binary
                        data[row] += Convert.ToString(page, 2).PadLeft(8, '0');
                    }

                    // add comma
                    data[row] += ", ";
                }
            }
        }

        // builds a string array visualization of 'pages'
        private void generateVisualizer(int width, int height, ArrayList pages, OutputConfiguration.BitLayout layout, out string[] visualizer)
        {
            visualizer = new string[height];

            // the number of pages per row in 'pages'
            int colCount = (layout == OutputConfiguration.BitLayout.RowMajor) ? (width + 7) / 8 : width;
            int rowCount = (layout == OutputConfiguration.BitLayout.RowMajor) ? height : (height + 7) / 8;

            // iterator over rows
            for (int row = 0; row != height; ++row)
            {
                // iterator over columns
                for (int col = 0; col != width; ++col)
                {
                    // get the byte containing the bit we want
                    int page = (layout == OutputConfiguration.BitLayout.RowMajor)
                        ? (byte)pages[row * colCount + (col / 8)]
                        : (byte)pages[(row / 8) * colCount + col];

                    // make a mask to extract the bit we want
                    int bitMask = (layout == OutputConfiguration.BitLayout.RowMajor)
                        ? getBitMask(7 - (col % 8))
                        : getBitMask(row % 8);

                    // check if bit is set
                    visualizer[row] += (bitMask & page) != 0 ? m_outputConfig.bmpVisualizerChar : " ";
                }
            }

            // for debugging
            //foreach (var s in visualizer)
            //  System.Diagnostics.Debug.WriteLine(s);
        }


        // return a bitMask to pick out the 'bitIndex'th bit allowing for byteOrder
        // MsbFirst: bitIndex = 0 = 0x01, bitIndex = 7 = 0x80
        // LsbFirst: bitIndex = 0 = 0x80, bitIndex = 7 = 0x01
        private int getBitMask(int bitIndex)
        {
            return m_outputConfig.byteOrder == OutputConfiguration.ByteOrder.MsbFirst
                ? 0x01 << bitIndex
                : 0x80 >> bitIndex;
        }

        protected void GenerateDescriptorData()
        {
            for (int charIdx = 0; charIdx < m_fontInfo.characters.Length; ++charIdx)
            {
                GenerateLine(String.Format("({0},{1}),{2}",
                    m_fontInfo.characters[charIdx].width,
                    m_fontInfo.characters[charIdx].offsetInBytes,
                    EndOfLineComment(m_fontInfo.characters[charIdx].character.ToString())
                ));
            }
        }
    }
}

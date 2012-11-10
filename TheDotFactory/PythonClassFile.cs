using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TheDotFactory
{
    class PythonFontFile : FontFile
    {
        public PythonFontFile(OutputConfiguration OutputConfig, MainForm.FontInfo FontInfo)
            : base(OutputConfig, FontInfo)
        {
            m_indent = "";
        }

        public string Generate()
        {
            GenerateClass();
            return m_output;
        }

        void GenerateClass()
        {
            GenerateClassHeader();
            PushIndent(m_indent + "    ");
            GenerateClassMetrics();
            GenerateBitmaps();
            GenerateDescriptors();
            PopIndent();
            GenerateClassFooter();
        }

        string ClassName()
        {
            return m_fontInfo.font.Name.Replace(" ", "");
        }

        void GenerateClassHeader()
        {
            GenerateLine(String.Format("class {0}{1}:", ClassName(), m_fontInfo.charHeight));
        }

        void GenerateClassFooter()
        {
            // no footer
        }

        void GenerateClassMetrics()
        {
            GenerateLine(String.Format("start_char  = '{0}'", m_fontInfo.startChar));
            GenerateLine(String.Format("end_char    = '{0}'", m_fontInfo.endChar));
            GenerateLine(String.Format("char_height = {0}", m_fontInfo.charHeight));
            GenerateLine(String.Format("space_width = {0}", m_fontInfo.charHeight/2));  // REVISIT - get this from font metrics?
            GenerateLine(String.Format("gap_width   = {0}", m_fontInfo.charHeight/8));  // REVISIT - get this from font metrics?
        }

        void GenerateBitmaps()
        {

            GenerateLine("bitmaps = (");
            PushIndent(m_indent + "    ");
            GenerateBitmapData();
            PopIndent();
            GenerateLine(")");
        }

        void GenerateDescriptors()
        {
            GenerateLine("descriptors = (");
            PushIndent(m_indent + "    ");
            GenerateDescriptorData();
            PopIndent();
            GenerateLine(")");
        }
    }
}

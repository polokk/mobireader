using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace MobiReader
{
    class ConsoleReader
    {
        protected MobiFile m_File;
        protected Int32 m_pos;
        protected List<String> m_BookLines;
        protected static Int32 LinesSize = 24;
        protected static Int32 LineMax = 75;

        public ConsoleReader(MobiFile mf)
        {
            m_File = mf;
            m_BookLines = new List<String>();
            String filetext = m_File.BookText;
            filetext = filetext.Substring(filetext.IndexOf("<body>"));
            filetext = filetext.Replace("</html>","");
            filetext = filetext.Replace("<mbp:", "<");
            filetext = filetext.Replace("svg:svg","svg");
            filetext = filetext.Replace("svg:image", "image");
            filetext = filetext.Replace("xlink:href", "href");

            Int32 pos = 0;
            while (pos != -1)
            {
                pos = filetext.IndexOf("filepos=", pos+1);
                if (pos != -1)
                {
                    Int32 pos2 = filetext.IndexOf(" ", pos+1);
                    if (pos2 != -1)
                    {
                        filetext = filetext.Insert(pos2, "\"");
                    }
                }

            }
            filetext = filetext.Replace("filepos=", "filepos=\"");

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(filetext);
            sw.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.None;
            settings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xtr = XmlTextReader.Create(ms, settings);
            StringBuilder sb = new StringBuilder();
            Boolean foundBody = false;
            Boolean r = xtr.Read();
            while(r)
            {
                if (xtr.Name.ToLower() == "head" ||
                    xtr.Name.ToLower() == "svg" ||
                    xtr.Name.ToLower() == "image")
                {
                    xtr.Skip();
                }

                if (xtr.Name.ToLower() == "body")
                {
                    foundBody = true;
                }
                if (xtr.Name.ToLower() == "p")
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }
                if (foundBody)
                {
                    try
                    {
                        String tempstring = xtr.ReadContentAsString();
                        if (tempstring.Trim().Length > 0)
                        {
                            sb.Append(tempstring);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                try
                {
                    r = xtr.Read();
                }
                catch (Exception)
                {
                }

            }
            xtr.Close();
            sw.Close();
            sw.Dispose();
            ms.Close();
            ms.Dispose();


            String[] delim = new String[1];
            delim[0] = "\r\n";
            String[] lines = sb.ToString().Split(delim, StringSplitOptions.None);

            foreach (string str in lines)
            {
                if (str.Length <= LineMax)
                {
                    m_BookLines.Add(str.Trim());
                }
                else
                {
                    String temp = str;
                    Int32 ppos = 0;
                    while (temp.Length > LineMax)
                    {
                        ppos = temp.LastIndexOf(' ', LineMax);
                        if (ppos != -1)
                        {
                            m_BookLines.Add(temp.Substring(0, ppos).Trim());
                            temp = temp.Substring(ppos + 1);
                        }
                        else
                        {
                            m_BookLines.Add(temp.Substring(0, LineMax)+"-".Trim());
                            temp = temp.Substring(LineMax + 1);
                        }
                    }
                    m_BookLines.Add(temp.Trim());
                }
            }
            LoadPosition();
        }

        public String GetLines()
        {
            StringBuilder sb = new StringBuilder();
            Int32 percent = (Int32)(((double)m_pos / (double)m_BookLines.Count) * 100);
            String strpercent = "(" + percent.ToString() + "%)";
            String titleline = new String(' ',LineMax-strpercent.Length) +strpercent;
            sb.AppendLine(titleline);

            for (int i = 0; i < LinesSize-1; i++)
            {
                if (m_pos + i < m_BookLines.Count)
                {
                    sb.AppendLine(m_BookLines[m_pos + i]);
                }
                else
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public void PageForward()
        {
            if (m_pos + (LinesSize - 1) < m_BookLines.Count)
            {
                m_pos += (LinesSize - 1);
                while (m_BookLines[m_pos].Length == 0 && m_pos + 1 < m_BookLines.Count)
                {
                   m_pos++;
                }
            }
        }

        public void PageBack()
        {
            if (m_pos - (LinesSize - 1) > 0)
            {
                m_pos -= (LinesSize - 1);
                while (m_BookLines[m_pos].Length == 0 && m_pos - 1 > 0)
                {
                    m_pos--;
                }
            }
        }

        public Boolean Find(String searchstring)
        {
            Boolean Retval = false;
            Int32 i = m_pos;
            while (i < m_BookLines.Count && m_BookLines[i].IndexOf(searchstring) == -1)
            {
                i++;
            }
            if (i == m_BookLines.Count)
            {
                Retval = true;
            }
            else
            {
                m_pos = i;
            }
            return Retval;
        }

        public void SavePosition()
        {
            String mykey = m_File.Name.Replace('_', ' ').Replace('\0', ' ').Trim();
            StreamReader sr = null;
            fileposition[] positions;
            XmlSerializer xs = new XmlSerializer(typeof(fileposition[]));
            try
            {
                sr = new StreamReader("filepositions.xml");
                positions = (fileposition[])xs.Deserialize(sr);
            }
            catch (FileNotFoundException)
            {
                positions = new fileposition[1];
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                    sr.Dispose();
                }
            }

            List<fileposition> mypos = new List<fileposition>(positions);
            Int32 index = 0;
            Int32 found = -1;
            foreach (fileposition fp in positions)
            {
                if (fp.Key == mykey)
                {
                    found = index;
                }
                index++;
            }
            StreamWriter sw = new StreamWriter("filepositions.xml");
            if (found != -1)
            {
                fileposition fpp;
                fpp.Key = mypos[found].Key;
                fpp.Value = m_pos;
                mypos[found] = fpp;
            }
            else
            {
                fileposition fpp;
                fpp.Key = mykey;
                fpp.Value = m_pos;
                mypos.Add(fpp);
            }
            xs.Serialize(sw, mypos.ToArray());
        }

        public void LoadPosition()
        {
            String mykey = m_File.Name.Replace('_', ' ').Replace('\0',' ').Trim();
            StreamReader sr = null;
            try
            {
                sr = new StreamReader("filepositions.xml");

                XmlSerializer xs = new XmlSerializer(typeof(fileposition[]));
                fileposition[] positions = (fileposition[])xs.Deserialize(sr);
                List<fileposition> mypos = new List<fileposition>(positions);
                Int32 index = 0;
                Int32 found = -1;
                foreach (fileposition fp in positions)
                {
                    if (fp.Key == mykey)
                    {
                        found = index;
                    }
                    index++;
                }
                if (found == -1)
                {
                    m_pos = 0;
                }
                else
                {
                    m_pos = positions[found].Value;
                }
            }
            catch (Exception)
            {
                m_pos = 0;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                    sr.Dispose();
                }
            }
        }
    }

    public struct fileposition
    {
        public String Key;
        public Int32 Value;
    }

}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CheckPreformance
{
    public class IniDataReader
    {
        Dictionary<string, Dictionary<string, string>> skv;

        public IniDataReader(string path)
        {
            skv = new Dictionary<string, Dictionary<string, string>>();

            string section = null;

            using (var fs = new StreamReader(path))
            {
                while (!fs.EndOfStream)
                {
                    var line = fs.ReadLine();

                    if (0 == line.Length) continue;

                    if ('[' == line[0])
                    {
                        int si = line.LastIndexOf(']');
                        section = line.Substring(1, si - 1);
                        skv.Add(section, new Dictionary<string, string>());
                    }

                    else if (';' != line[0])
                    {
                        var parse = line.Split('=');
                        skv[section].Add(parse[0].Trim(), parse[1].Trim());
                    }
                }
            }
        }

        public string GetString(string Section, string Key)
        {
            try
            {
                return skv[Section][Key];
            }
            catch { return string.Empty; }
        }
        public int GetInteger(string Section, string Key)
        {
            try
            {
                return int.Parse(skv[Section][Key]);
            }
            catch { return 0; }
        }
        public byte GetByte(string Section, string Key)
        {
            try
            {
                return byte.Parse(skv[Section][Key]);
            }
            catch { return 0; }
        }
        public double GetDouble(string Section, string Key)
        {
            try
            {
                return double.Parse(skv[Section][Key]);
            }
            catch { return 0; }
        }
        public float GetFloat(string Section, string Key)
        {
            try
            {
                return float.Parse(skv[Section][Key]);
            }
            catch { return 0; }
        }
        public bool GetBoolean(string Section, string Key)
        {
            try
            {
                return int.Parse(skv[Section][Key]) == 1;
            }
            catch { return false; }
        }

        public void GetString(string Section, string Key, ref string Value)
        {
            try
            {
                Value = skv[Section][Key];
            }
            catch { Value = string.Empty; }
        }
        public void GetInteger(string Section, string Key, ref int Value)
        {
            try
            {
                Value = int.Parse(skv[Section][Key]);
            }
            catch { Value = 0; }
        }
        public void GetByte(string Section, string Key, ref byte Value)
        {
            try
            {
                Value = byte.Parse(skv[Section][Key]);
            }
            catch { Value = 0; }
        }
        public void GetDouble(string Section, string Key, ref double Value)
        {
            try
            {
                Value = double.Parse(skv[Section][Key]);
            }
            catch { Value = 0; }
        }
        public void GetFloat(string Section, string Key, ref float Value)
        {
            try
            {
                Value = float.Parse(skv[Section][Key]);
            }
            catch { Value = 0; }
        }
        public void GetBoolean(string Section, string Key, ref bool Value)
        {
            try
            {
                Value = int.Parse(skv[Section][Key]) == 1;
            }
            catch { Value = false; }
        }

        public string[] GetAllKeys(string section)
        {
            try
            {
                return skv[section].Keys.ToArray();
            }
            catch (Exception)
            {

                return new string[] { };
            }
        }
    }
}

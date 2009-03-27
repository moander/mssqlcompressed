/*
	Copyright 2008 Clay Lenhart <clay@lenharts.net>


	This file is part of MSSQL Compressed Backup.

    MSSQL Compressed Backup is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Foobar is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace MSBackupPipe.StdPlugins
{
    internal static class ConfigUtil
    {
        public static Dictionary<string, string> ParseConfig(string s, params string[] excludeNames)
        {

            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            Dictionary<string, List<string>> arrayConfig = ParseArrayConfig(s);

            foreach (string exName in excludeNames)
            {
                if (arrayConfig.ContainsKey(exName))
                {
                    arrayConfig.Remove(exName);
                }
            }

            foreach (string name in arrayConfig.Keys)
            {
                List<string> valList = arrayConfig[name];
                if (valList.Count > 1)
                {
                    throw new ArgumentException(string.Format("The paramenter, {0}, can only exist once.", name));
                }

                result.Add(name, valList[0]);
            }

            return result;
        }


        public static Dictionary<string, List<string>> ParseArrayConfig(string s)
        {
            string[] pairs = s.Split(';');

            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string pair in pairs)
            {
                if (!string.IsNullOrEmpty(pair))
                {

                    string[] nameValue = pair.Split(new char[] { '=' }, 2);
                    string name = nameValue[0].Trim();
                    string val = nameValue.Length > 1 ? nameValue[1].Trim() : null;

                    name = name.Replace("\\s", ";");
                    name = name.Replace("\\p", "|");
                    name = name.Replace("\\\\", "\\");

                    if (val != null)
                    {
                        val = val.Replace("\\s", ";");
                        val = val.Replace("\\p", "|");
                        val = val.Replace("\\\\", "\\");
                    }

                    if (result.ContainsKey(name))
                    {
                        result[name].Add(val);
                    }
                    else
                    {
                        result.Add(name, new List<string>(new string[] { val }));
                    }
                }
            }

            return result;
        }
    }
}
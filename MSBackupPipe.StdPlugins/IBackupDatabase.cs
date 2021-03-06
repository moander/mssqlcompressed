/*
	Copyright 2009 Clay Lenhart <clay@lenharts.net>


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
using System.Data.SqlClient;

namespace MSBackupPipe.StdPlugins
{
    public interface IBackupDatabase : IBackupPlugin
    {
        void ConfigureBackupCommand(Dictionary<string, List<string>> config, IEnumerable<string> deviceNames, SqlCommand cmd);
        void ConfigureRestoreCommand(Dictionary<string, List<string>> config, IEnumerable<string> deviceNames, SqlCommand cmd);
        string GetInstanceName(Dictionary<string, List<string>> config);
        string GetClusterNetworkName(Dictionary<string, List<string>> config);
    }
}

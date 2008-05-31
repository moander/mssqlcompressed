﻿/*
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
using System.IO;

using ICSharpCode.SharpZipLib.BZip2;

using VirtualBackupDevice;

namespace MSSQLBackupPipe.Plugins
{
    public class BzipTransform : IBackupTransformer
    {

        #region IBackupTransformer Members

        Stream IBackupTransformer.GetBackupWriter(string config, Stream writeToStream)
        {
            return new BZip2OutputStream(writeToStream, 9);
        }

        string IBackupTransformer.GetName()
        {
            return "bzip2";
        }

        Stream IBackupTransformer.GetRestoreReader(string config, Stream readFromStream)
        {
            return new BZip2InputStream(readFromStream);
        }

        #endregion
    }
}
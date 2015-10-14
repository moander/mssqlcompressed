# mssqlcompressed
Fork of http://mssqlcompressed.sourceforge.net

## What is SQL Server Compressed Backup?

SQL Server Compressed Backup is a command line utility for backing up and restoring SQL Server 2000, 2005 and 2008 databases in various compression formats including gzip, zip64, and bzip2.

Project Features

* **Open Source.** SQL Server Compressed Backup is released under GPL v3.
* **Compress on the Fly.** SQL Server Compressed Backup compresses the data as it saves the data. No temporary files are used.
* **Standard Formats.** Your data is important, so it stores the data in a standard SQL Server *.bak file, compressed in standard gzip, zip64, or bzip2 formats so that it is easy to restore the database using the method you are most comfortable with. Though SQL Server Compressed Backup can also decompress and restore on the fly.
* **Reliable.** SQL Server Compressed Backup faithfully stores the bytes that SQL Server writes and compresses with well known and reliable compression formats. The uncompressed data is in the same format as the standard BACKUP DATABASE command.
* **Pipeline Architecture.** SQL Server Compressed Backup uses a pipeline architecture to backup the data. This allows you to pass the data through a compression plugin, and then, one day, through an encryption plugin (when an encryption plugin exists). Any number of plugins, including plugins you write can be used in the pipeline.
* **Multithreaded Compression.** SQL Server Compressed Backup can compress your backups using multiple threads to take advantage of multiple cores.





## Getting Started Examples

Below are some examples to get you started using msbp.exe on the command line. msbp.exe must run on the same machine as SQL Server, due to how SQL Server dumps the data to msbp.exe.

### Basic Backup

To backup to a standard SQL Server *.bak file, run the following command:

```
msbp.exe backup "db(database=model)" "local(path=c:\model.bak)"
```


### Basic Restore

To restore from a standard SQL Server *.bak file, run the following command:

```
msbp.exe restore "local(path=c:\model.bak)" "db(database=model)"
```

### Compressed Backup

Using the basic command above, you can add any number of plugins between the source and destination. For example, you may want to compress the data with the gzip plugin:

```
msbp.exe backup "db(database=model)" "gzip()" "local(path=c:\model.bak.gz)"
```

### Compressed Restore

And to restore compressed the file, insert "gzip()" in the middle again. Here the gzip plugin knows it is restoring the database, so it will uncompress the data:

```
msbp.exe restore "local(path=c:\model.bak.gz)" "gzip()" "db(database=model)"
```

### Multithreaded Compressed Backup

Using the compression command above, you can add any number of files. Since each file runs on its own thread, you can acheive multithreaded compression. For example:

```
msbp.exe backup "db(database=model)" "gzip()" "local(path=c:\model1.bak.gz;path=c:\model2.bak.gz;)"
```



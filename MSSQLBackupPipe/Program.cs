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
using System.IO;
using System.Reflection;

//using VirtualBackupDevice;
using MSSQLBackupPipe.StdPlugins;

namespace MSSQLBackupPipe
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {


                Dictionary<string, Type> pipelineComponents = LoadComponents("IBackupTransformer");
                Dictionary<string, Type> databaseComponents = LoadComponents("IBackupDatabase");
                Dictionary<string, Type> destinationComponents = LoadComponents("IBackupDestination");


                if (args.Length == 0)
                {
                    Console.WriteLine("For help, type 'msbp.exe help'");
                }
                else
                {
                    switch (args[0].ToLowerInvariant())
                    {
                        case "help":

                            if (args.Length == 1)
                            {
                                PrintUsage();
                            }
                            else
                            {
                                switch (args[1].ToLowerInvariant())
                                {
                                    case "backup":
                                        PrintBackupUsage();
                                        break;
                                    case "restore":
                                        PrintRestoreUsage();
                                        break;
                                    case "listplugins":
                                        Console.WriteLine("Lists the plugins available.  Go on, try it.");
                                        break;
                                    case "helpplugin":
                                        Console.WriteLine("Displays a plugin's help text. For example:");
                                        Console.WriteLine("\tmsbp.exe helpplugin gzip");
                                        break;
                                    case "version":
                                        Console.WriteLine("Displays the version number.");
                                        break;
                                    default:
                                        Console.WriteLine(string.Format("Command doesn't exist: {0}", args[1]));
                                        PrintUsage();
                                        break;
                                }

                            }
                            break;

                        case "backup":
                            {
                                ConfigPair destinationConfig;
                                ConfigPair databaseConfig;
                                bool isBackup = true;

                                List<ConfigPair> pipelineConfig = ParseBackupOrRestoreArgs(CopySubArgs(args), isBackup, pipelineComponents, databaseComponents, destinationComponents, out databaseConfig, out destinationConfig);


                                BackupOrRestore(isBackup, destinationConfig, databaseConfig, pipelineConfig);
                            }
                            break;

                        case "restore":
                            {
                                ConfigPair destinationConfig;
                                ConfigPair databaseConfig;

                                bool isBackup = false;

                                List<ConfigPair> pipelineConfig = ParseBackupOrRestoreArgs(CopySubArgs(args), isBackup, pipelineComponents, databaseComponents, destinationComponents, out databaseConfig, out destinationConfig);

                                BackupOrRestore(isBackup, destinationConfig, databaseConfig, pipelineConfig);
                            }
                            break;
                        case "listplugins":
                            PrintPlugins(pipelineComponents, databaseComponents, destinationComponents);
                            break;
                        case "helpplugin":
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Please give a plugin name, like msbp.exe helpplugin <plugin>");
                            }
                            else
                            {
                                PrintPluginHelp(args[1], pipelineComponents, databaseComponents, destinationComponents);
                            }
                            break;
                        case "version":
                            Version version = Assembly.GetEntryAssembly().GetName().Version;
                            Console.WriteLine(string.Format("v{0} ({1:yyyy MMM dd})", version, (new DateTime(2000, 1, 1)).AddDays(version.Build)));
                            break;
                        default:
                            Console.WriteLine(string.Format("Unknown command: {0}", args[0]));
                            PrintUsage();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //Console.WriteLine(e.StackTrace);

                Exception ie = e;
                while (ie.InnerException != null)
                {
                    ie = ie.InnerException;
                }
                if (!ie.Equals(e))
                {
                    Console.WriteLine(ie.Message);
                }


                PrintUsage();
                //             Console.WriteLine(e.StackTrace);


            }

        }

        private static List<string> CopySubArgs(string[] args)
        {
            List<string> result = new List<string>(args.Length - 2);
            for (int i = 1; i < args.Length; i++)
            {
                result.Add(args[i]);
            }
            return result;
        }

        private static void BackupOrRestore(bool isBackup, ConfigPair destConfig, ConfigPair databaseConfig, List<ConfigPair> pipelineConfig)
        {

            string deviceName = Guid.NewGuid().ToString();

            IBackupDestination dest = destConfig.TransformationType.GetConstructor(new Type[0]).Invoke(new object[0]) as IBackupDestination;
            IBackupDatabase databaseComp = databaseConfig.TransformationType.GetConstructor(new Type[0]).Invoke(new object[0]) as IBackupDatabase;
            try
            {

                //NotifyWhenReady notifyWhenReady = new NotifyWhenReady(deviceName, isBackup);
                using (DeviceThread device = new DeviceThread())
                {
                    using (SqlThread sql = new SqlThread())
                    {
                        string sqlStmt = isBackup ? databaseComp.GetBackupSqlStatement(databaseConfig.ConfigString, deviceName) :
                            databaseComp.GetRestoreSqlStatement(databaseConfig.ConfigString, deviceName);
                        sqlStmt = string.Format(sqlStmt, deviceName);
                        sql.PreConnect(sqlStmt);
                        device.PreConnect(isBackup, deviceName, dest, destConfig.ConfigString, pipelineConfig);
                        device.ConnectInAnoterThread();
                        sql.ConnectInAnoterThread();
                        Exception e = sql.WaitForCompletion();
                        if (e != null)
                        {
                            Console.WriteLine(e.Message);
                            //Console.WriteLine(e.StackTrace);
                        }

                        e = device.WaitForCompletion();
                        if (e != null)
                        {
                            Console.WriteLine(e.Message);
                            //Console.WriteLine(e.StackTrace);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                dest.CleanupOnAbort();
            }
        }




        private static Dictionary<string, Type> LoadComponents(string interfaceName)
        {
            Dictionary<string, Type> result = new Dictionary<string, Type>();

            DirectoryInfo binDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;

            foreach (FileInfo file in binDir.GetFiles("*.dll"))
            {

                Assembly dll = null;
                try
                {
                    dll = Assembly.LoadFrom(file.FullName);
                }
                catch
                { }
                if (dll != null)
                {
                    FindPlugins(dll, result, interfaceName);
                }
            }

            return result;
        }



        private static void FindPlugins(Assembly dll, Dictionary<string, Type> result, string interfaceName)
        {
            foreach (Type t in dll.GetTypes())
            {
                try
                {
                    if (t.IsPublic)
                    {
                        if (!((t.Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract))
                        {
                            if (t.GetInterface(interfaceName) != null)
                            {
                                object o = t.GetConstructor(new Type[0]).Invoke(new object[0]);


                                IBackupPlugin test = o as IBackupPlugin;

                                if (test != null)
                                {
                                    string name = test.GetName().ToLowerInvariant();

                                    if (name.Contains("|") || name.Contains("("))
                                    {
                                        throw new ArgumentException(string.Format("The name of the plugin, {0}, cannot contain these characters: |, (", name));
                                    }

                                    if (result.ContainsKey(name))
                                    {
                                        throw new ArgumentException(string.Format("plugin found twice: {0}", name));
                                    }
                                    result.Add(name, t);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Warning: Plugin not loaded due to error: {0}", e.Message));
                }
            }
        }



        private static List<ConfigPair> ParseBackupOrRestoreArgs(List<string> args, bool isBackup, Dictionary<string, Type> pipelineComponents, Dictionary<string, Type> databaseComponents, Dictionary<string, Type> destinationComponents, out ConfigPair databaseConfig, out ConfigPair destinationConfig)
        {
            if (args.Count < 2)
            {
                throw new ArgumentException("Please provide both the database and destination after the backup subcommand.");
            }


            string databaseArg;
            string destinationArg;

            if (isBackup)
            {
                databaseArg = args[0];
                destinationArg = args[args.Count - 1];
            }
            else
            {
                databaseArg = args[args.Count - 1];
                destinationArg = args[0];
            }


            if (databaseArg.Contains("://"))
            {
                throw new ArgumentException("The first sub argument must be the name of the database.");
            }

            if (databaseArg[0] == '[' && databaseArg[databaseArg.Length - 1] == ']')
            {
                databaseArg = string.Format("db(database={0})", databaseArg.Substring(1, databaseArg.Length - 2));
            }

            databaseConfig = FindConfigPair(databaseComponents, databaseArg);



            if (destinationArg[0] == '[' && destinationArg[databaseArg.Length - 1] == ']')
            {
                throw new ArgumentException("The last sub argument must be the destination.");
            }

            if (destinationArg.StartsWith("file://"))
            {
                Uri uri = new Uri(destinationArg);
                destinationArg = string.Format("local(path={0})", uri.LocalPath);
            }


            destinationConfig = FindConfigPair(destinationComponents, destinationArg);



            List<string> pipelineArgs = new List<string>();
            for (int i = 1; i < args.Count - 1; i++)
            {
                pipelineArgs.Add(args[i]);
            }


            List<ConfigPair> pipeline = BuildPipelineFromString(pipelineArgs, pipelineComponents);


            return pipeline;
        }





        private static List<ConfigPair> BuildPipelineFromString(List<string> pipelineArgs, Dictionary<string, Type> pipelineComponents)
        {

            for (int i = 0; i < pipelineArgs.Count; i++)
            {
                pipelineArgs[i] = pipelineArgs[i].Trim();
            }

            List<ConfigPair> results = new List<ConfigPair>(pipelineArgs.Count);

            foreach (string componentString in pipelineArgs)
            {
                ConfigPair config = FindConfigPair(pipelineComponents, componentString);

                results.Add(config);
            }


            return results;
        }

        private static ConfigPair FindConfigPair(Dictionary<string, Type> pipelineComponents, string componentString)
        {

            ConfigPair config = new ConfigPair();

            string componentName;
            string configString;

            int pPos = componentString.IndexOf('(');
            if (pPos < 0)
            {
                componentName = componentString;
                configString = "";
            }
            else
            {
                componentName = componentString.Substring(0, pPos).Trim();

                if (componentString.Substring(componentString.Length - 1, 1) != ")")
                {
                    throw new ArgumentException(string.Format("Invalid pipeline.  The closing parenthesis not found: {0}", componentString));
                }

                configString = componentString.Substring(pPos + 1, componentString.Length - pPos - 2);
            }

            Type foundType;
            if (pipelineComponents.TryGetValue(componentName.ToLowerInvariant(), out foundType))
            {
                config.ConfigString = configString;
                config.TransformationType = foundType;
            }
            else
            {
                throw new ArgumentException(string.Format("Plugin not found: {0}", componentName));
            }
            return config;
        }


        private static void PrintUsage()
        {
            Console.WriteLine("Below are the commands for msbp.exe:");
            Console.WriteLine("\tmsbp.exe help");
            Console.WriteLine("\tmsbp.exe backup");
            Console.WriteLine("\tmsbp.exe restore");
            Console.WriteLine("\tmsbp.exe listplugins");
            Console.WriteLine("\tmsbp.exe helpplugin");
            Console.WriteLine("\tmsbp.exe version");
            Console.WriteLine("");
            Console.WriteLine("For more information, type msbp.exe help <command>");
        }

        private static void PrintBackupUsage()
        {
            Console.WriteLine("To backup a database, the first parameter must be the database in brackets, and the last parameter must be the file.  The middle parameters can modify the data, for example compressing it.");
            Console.WriteLine("To backup to a standard *.bak file:");
            Console.WriteLine("\tmsbp.exe backup [model] file:///c:\\model.bak");
            Console.WriteLine("To compress the backup file using gzip:");
            Console.WriteLine("\tmsbp.exe backup [model] gzip file:///c:\\model.bak.gz");
            Console.WriteLine("");
            Console.WriteLine("For more information on the different pipline options, type msbp.exe listplugins");
        }

        private static void PrintRestoreUsage()
        {
            Console.WriteLine("To restore a database, the first parameter must be the file, and the last parameter must be the database in brackets.  The middle parameters can modify the data, for example uncompressing it.");
            Console.WriteLine("To restore to a standard *.bak file:");
            Console.WriteLine("\tmsbp.exe restore file:///c:\\model.bak [model]");
            Console.WriteLine("To compress the backup file using gzip:");
            Console.WriteLine("\tmsbp.exe restore file:///c:\\model.bak.gz gzip [model]");
            Console.WriteLine("");
            Console.WriteLine("For more information on the different pipline options, type msbp.exe listplugins");
        }


        private static void PrintPlugins(Dictionary<string, Type> pipelineComponents, Dictionary<string, Type> databaseComponents, Dictionary<string, Type> destinationComponents)
        {
            Console.WriteLine("Database plugins:");
            PrintComponents(databaseComponents);
            Console.WriteLine("Pipeline plugins:");
            PrintComponents(pipelineComponents);
            Console.WriteLine("Destination plugins:");
            PrintComponents(destinationComponents);

            Console.WriteLine("");
            Console.WriteLine("To find more information about a plugin, type msbp.exe helpplugin <plugin>");
        }

        private static void PrintComponents(Dictionary<string, Type> components)
        {
            foreach (string key in components.Keys)
            {
                IBackupPlugin db = components[key].GetConstructor(new Type[0]).Invoke(new object[0]) as IBackupPlugin;
                Console.WriteLine("\t" + db.GetName());
            }
        }

        private static void PrintPluginHelp(string pluginName, Dictionary<string, Type> pipelineComponents, Dictionary<string, Type> databaseComponents, Dictionary<string, Type> destinationComponents)
        {
            PrintPluginHelp(pluginName, databaseComponents);
            PrintPluginHelp(pluginName, pipelineComponents);
            PrintPluginHelp(pluginName, destinationComponents);
        }

        private static void PrintPluginHelp(string pluginName, Dictionary<string, Type> components)
        {
            if (components.ContainsKey(pluginName))
            {
                IBackupPlugin db = components[pluginName].GetConstructor(new Type[0]).Invoke(new object[0]) as IBackupPlugin;
                Console.WriteLine(db.GetConfigHelp());
            }
        }

    }
}

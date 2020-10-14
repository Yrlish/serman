﻿namespace serman
{
    using CommandLine;
    using RunProcessAsTask;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    [Verb("install", HelpText = "Install a service")]
    public class InstallOpts
    {
        [Value(0, MetaName = "config", HelpText = "The service configuration file to install")]
        public string Config { get; set; }

        [Value(1, MetaName = "KeyValues", Default = "", HelpText = "The key value pairs (key=value) to be used to fill in the service configuration file template")]
        public string KeyValues { get; set; }

        [Option(Default = false, HelpText = "Overwrite the existing service directory")]
        public bool Overwrite { get; set; }
    }

    [Verb("uninstall", HelpText = "Uninstall a service")]
    class UninstallOpts
    {
        [Value(0, HelpText = "The service ID to uninstall")]
        public string Id { get; set; }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--pause-on-start"))
            {
                Console.Read();
                args = args.Where(a => a != "--pause-on-start").ToArray();
            }

            try
            {
                Parser.Default.ParseArguments<InstallOpts, UninstallOpts>(args)
                    .MapResult(
                        (InstallOpts opts) =>
                        {
                            new Context
                            {
                                Config = new Configuration(),
                                SourceServiceConfigPath = opts.Config,
                                Values = ParseCommandLineKeyValues(string.IsNullOrEmpty(opts.KeyValues) ? null : opts.KeyValues.Split(',')),
                                ServiceId = GetServiceId(opts.Config),
                                Overwrite = opts.Overwrite,
                            }
                            .PopulateValues()
                            .DeployWrapper()
                            .DeployServiceConfig()
                            .RunWrapper("install")
                            .RunWrapper("start");

                            return 0;
                        },
                        (UninstallOpts opts) =>
                        {
                            var ctx = new Context
                            {
                                Config = new Configuration(),
                                ServiceId = opts.Id,
                            }
                            .RunWrapper("uninstall");

                            Console.WriteLine("Done. You should manually remove directory " +
                                              ctx.GetServiceBinDirectory() + " and " + ctx.GetServiceDataDirectory());

                            return 0;
                        },
                        errs => 1);
            }
            catch (HandlableException e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        internal static IDictionary<string, string> ParseCommandLineKeyValues(string[] kvs) =>
            (kvs ?? new string[0])
            .Select(kv => kv.Split(new[] { '=' }, 2))
            .ToDictionary(kv => kv[0], kv => kv[1]);

        internal static string GetServiceId(string configPath) => 
            Path.GetFileNameWithoutExtension(configPath);

        internal static Context PopulateValues(this Context ctx)
        {
            ctx.Values = ctx.Values
                .Concat(new[] { new KeyValuePair<string, string>("dir", ctx.GetSourceServiceConfigDirectory()) })
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            return ctx;
        }

        static Context DeployServiceConfig(this Context ctx)
        {
            string xml = File.ReadAllText(ctx.SourceServiceConfigPath);

            // render xml
            xml = Nustache.Core.Render.StringToString(xml, ctx.Values);
            File.WriteAllText(ctx.GetTargetServiceConfigPath(), xml);

            // Persist env vars
            var vars = GetPersistentVars(xml);
            PersistEnv(vars);

            return ctx;
        }

        static Context DeployWrapper(this Context ctx)
        {
            EnsureDirectory(ctx.GetServiceBinDirectory(), ctx.Overwrite);
            EnsureDirectory(ctx.GetServiceDataDirectory(), ctx.Overwrite);
            File.Copy(ctx.Config.WrapperPath, ctx.GetTargetWrapperPath(), ctx.Overwrite);
            return ctx;
        }

        static Context RunWrapper(this Context ctx, string command)
        {
            Console.WriteLine($"Executing {ctx.GetTargetWrapperPath()} {command}...");
            using (var res = ProcessEx.RunAsync(ctx.GetTargetWrapperPath(), command).Result.Display()) { }
            return ctx;
        }

        private static void EnsureDirectory(string serviceDirectory, bool overwrite)
        {
            if (!overwrite && Directory.Exists(serviceDirectory))
            {
                throw new HandlableException($"Service directory already exists: {serviceDirectory}. Consider use --overwrite to force install.");
            }

            Directory.CreateDirectory(serviceDirectory);
        }

        static ProcessResults Display(this ProcessResults res)
        {
            try
            {
                foreach (var l in res.StandardOutput)
                {
                    Console.WriteLine(l);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var l in res.StandardError)
                {
                    Console.WriteLine(l);
                }
            }
            finally
            {
                Console.ResetColor();
            }

            return res;
        }

        internal static List<KeyValuePair<string, string>> GetPersistentVars(string xml)
        {
            return XDocument.Parse(xml).Root
                .Descendants("persistent_env")
                .Select(e => new KeyValuePair<string, string>(
                    e.Attribute("name").Value,
                    e.Attribute("value").Value))
                .ToList();
        }

        static void PersistEnv(IEnumerable<KeyValuePair<string,string>> kvs)
        {
            // Persist env:
            kvs.ToList().ForEach(kv =>
            {
                Console.WriteLine($"Exporting environment variable {kv.Key}={kv.Value}...");
                using (ProcessEx.RunAsync("cmd.exe", $"/c SETX {kv.Key} {kv.Value} /M").Result.Display()) { }
            });
        }
    }

    class HandlableException : Exception
    {
        public HandlableException(string msg) : base(msg)
        {
        }
    }
}

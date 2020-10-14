namespace serman
{
    using System.Collections.Generic;
    using System.IO;

    public class Context
    {
        public IConfiguration Config { get; set; }

        /// <summary>
        /// The ID of the service to be worked on
        /// </summary>
        public string ServiceId { get; internal set; }

        /// <summary>
        /// The path of the source service config file to be installed
        /// </summary>
        public string SourceServiceConfigPath { get; internal set; }

        /// <summary>
        /// The key-value pairs to be set in target service config file as env vars
        /// </summary>
        public IDictionary<string,string> Values { get; internal set; }

        /// <summary>
        /// Overwrite existing service directory
        /// </summary>
        public bool Overwrite { get; internal set; }
    }

    public static class ContextUtils
    {
        public static string GetServiceBinDirectory(this Context ctx)
        {
            return Path.Combine(ctx.Config.ServiceBin, ctx.ServiceId);
        }

        public static string GetServiceDataDirectory(this Context ctx)
        {
            return Path.Combine(ctx.Config.ServiceData, ctx.ServiceId);
        }

        public static string GetTargetServiceConfigPath(this Context ctx)
        {
            return Path.Combine(ctx.GetServiceBinDirectory(), $"{ctx.ServiceId}.xml");
        }

        public static string GetTargetWrapperPath(this Context ctx)
        {
            return Path.Combine(ctx.GetServiceBinDirectory(), $"{ctx.ServiceId}.exe");
        }

        public static string GetSourceServiceConfigDirectory(this Context ctx) =>
            Path.GetDirectoryName(Path.GetFullPath(ctx.SourceServiceConfigPath));
    }
}
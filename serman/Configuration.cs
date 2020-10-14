namespace serman
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IConfiguration
    {
        /// <summary>
        /// Directory containing the services
        /// </summary>
        string ServiceBin { get; }

        /// <summary>
        /// Directory containing the services log files
        /// </summary>
        string ServiceData { get; }

        /// <summary>
        /// Path to the wrapper.exe
        /// </summary>
        string WrapperPath { get; }
    }

    public class Configuration : IConfiguration
    {
        public string ServiceBin => @"C:\Program Files\serman";
        public string ServiceData => @"C:\ProgramData\serman";
        public string WrapperPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "winsw.exe");
    }
}

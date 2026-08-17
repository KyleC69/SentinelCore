using System.ServiceProcess;

namespace WindowsService1
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SentinelCoreService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

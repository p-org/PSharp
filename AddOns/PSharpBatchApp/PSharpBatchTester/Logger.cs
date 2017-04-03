using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTester
{
    class Logger
    {
        private static string InstrumentationKey = "c1248782-ff24-4fd4-8953-a1b8acf649f4";
        static TelemetryClient telemetryClient;

        static Logger()
        {
            telemetryClient = new TelemetryClient();
            telemetryClient.InstrumentationKey = InstrumentationKey;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
        }

        public static void LogEvents(string eventName, Dictionary<string, string> properties = null, Dictionary<string,double> metrics = null)
        {
            try
            {
                telemetryClient.TrackEvent(eventName, properties, metrics);
            }
            catch(Exception e)
            {

            }
        }

        public static void FlushLogs()
        {
            try
            {
                telemetryClient.Flush();
            }
            catch(Exception e)
            {

            }
        }


    }
}

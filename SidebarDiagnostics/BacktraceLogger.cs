using Backtrace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SidebarDiagnostics.Framework;
using Backtrace.Model;
using System.Diagnostics;
using System.IO;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Create new instance of Backtrace logger
    /// New instance is necessary if you want to access to logger from any place in application
    /// </summary>
    public class BacktraceLogger
    {
        /// <summary>
        /// New instance of client
        /// </summary>
        private static BacktraceClient _client;

        /// <summary>
        /// Log report to Backtrace API
        /// </summary>
        /// <param name="report">Current exception or client message</param>
        public static void Log(BacktraceReport report)
        {
            if (_client == null)
            {
                return;
            }
            _client.Send(report);
        }

        /// <summary>
        /// Update current instanc settings
        /// </summary>
        public static void UpdateSettings()
        {
            if (_client == null)
            {
                return;
            }
            var currentSettings = Framework.Settings.Instance;
            var logger = new BacktraceLogger(currentSettings);
        }

        /// <summary>
        /// Current logger attributes
        /// </summary>
        private Dictionary<string, object> _attributes = new Dictionary<string, object>();


        /// <summary>
        /// Create new instance of Logger
        /// </summary>
        /// <param name="settings">Application settings</param>
        public BacktraceLogger(Framework.Settings settings)
        {
            if (string.IsNullOrEmpty(settings.BacktraceHost)
                || string.IsNullOrEmpty(settings.BacktraceToken))
            {
                //there is no logger settings
                return;
            }
            LoadAttributes(settings);
            SetupClient(settings);
        }

        public static bool ValidateBacktraceHostUri(string uri)
        {
            bool result = Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute);
            if(result == false)
            {
                throw new FormatException("Invalid URI");
            }

            return result;
        }

        public static bool ValidateDatabase(string path)
        {
            bool result = Directory.Exists(path);
            if (result == false)
            {
                throw new DirectoryNotFoundException("Invalid path to directory");
            }
            if(System.IO.Directory.EnumerateFileSystemEntries(path).Any())
            {
                throw new ArgumentException("Directory is not empty");
            }

            return result;
        }

        private void SetupClient(Framework.Settings settings)
        {
            // setup new Backtrace client instance
            var credentials = new BacktraceCredentials(settings.BacktraceHost, settings.BacktraceToken);
            _client = new BacktraceClient(credentials, _attributes, settings.BacktraceDatabasePath, settings.BacktraceClientSiteLimiting);
            _client.HandleApplicationException();
            _client.AsyncRequest = true;
            _client.HandleApplicationException();
            _client.OnServerAnswer = (BacktraceServerResponse response) =>
            {
                Trace.WriteLine(response);
            };

            _client.WhenServerUnvailable = (Exception e) =>
            {
                Trace.WriteLine(e.Message);
            };
        }

        private void LoadAttributes(Framework.Settings settings)
        {
            foreach (var settingProperty in settings.GetType().GetProperties())
            {
                // we dont need to send information about current client settings
                if (settingProperty.Name.StartsWith("Backtrace"))
                {
                    continue;
                }
                _attributes.Add(settingProperty.Name, settingProperty.GetValue(settings, null));
            }
        }


    }
}

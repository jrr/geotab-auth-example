using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Geotab.Checkmate.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace geotab_authentication
{
    class Program
    {
        const string InputFileName = "GeotabAccountCreds.json";
        static void Main(string[] args)
        {
            if (!File.Exists(InputFileName))
            {
                var template = new GeotabFleetInformation
                {
                    GeotabDatabase = "",
                    Username = "",
                    Password = ""
                };

                var serialized = JsonConvert.SerializeObject(template, Formatting.Indented);
                File.WriteAllText(InputFileName, serialized);
                Console.WriteLine("\nNo configuration file found, a new one has been created.");
                Console.WriteLine($"Please update \"{InputFileName}\" with login creds.\n");
                return;
            }
            var content = File.ReadAllText(InputFileName);
            var fleetInfo = JObject.Parse(content).ToObject<GeotabFleetInformation>();
            if (string.IsNullOrEmpty(fleetInfo.Username))
            {
                Console.WriteLine("Failed to read creds.");
                return;
            }

            string sessionId = null;

            sessionId = Example("Connecting with only password",
                username: fleetInfo.Username,
                password: fleetInfo.Password,
                database: fleetInfo.GeotabDatabase,
                sessionId: null);

            sessionId = Example("Reusing sessionId with new API object (no password, no Authenticate())",
                username: fleetInfo.Username,
                password: null,
                database: fleetInfo.GeotabDatabase,
                sessionId: sessionId,
                authenticate: false);

            sessionId = Example("Connecting with both password and sessionId",
                username: fleetInfo.Username,
                password: fleetInfo.Password,
                database: fleetInfo.GeotabDatabase,
                sessionId: sessionId);

            sessionId = Example("Reusing sessionId with new API object (no password, calling Authenticate())",
                username: fleetInfo.Username,
                password: null,
                database: fleetInfo.GeotabDatabase,
                sessionId: sessionId,
                authenticate: true);
        }

        private static string Example(string description, string username, string password, string database, string sessionId, bool authenticate = true)
        {
            Console.WriteLine($"\n\n#\n#\t{description}\n#\n");
            var api = new Geotab.Checkmate.API(
                userName: username,
                password: password,
                database: database,
                sessionId: sessionId,
                handler: new LoggingHttpHandler()
            );

            try
            {
                if (authenticate) api.Authenticate();

                var devices = api.Call<List<Device>>("Get", typeof(Device));

                Console.WriteLine($"\n\tGot {devices.Count} devices.");
            }
            catch (Geotab.Checkmate.ObjectModel.InvalidUserException e)
            {
                Console.WriteLine($"\n\tGeotab Exception: {e.Message}");
            }

            return api.SessionId;
        }

        public struct GeotabFleetInformation
        {
            public string Username;
            public string Password;
            public string GeotabDatabase;
        }
    }

    internal class LoggingHttpHandler : System.Net.Http.MessageProcessingHandler
    {
        public LoggingHttpHandler()
        {
            this.InnerHandler = new System.Net.Http.HttpClientHandler();
        }
        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"\tRequest {request.Method} {request.RequestUri} {content}");
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            var extra = "";
            if (content.Contains("error")) extra = content;
            Console.WriteLine($"\tReponse {response.StatusCode} {extra}");
            return response;
        }
    }
}

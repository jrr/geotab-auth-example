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
            var httpHandler = new CountingHttpHandler();

            var api = new Geotab.Checkmate.API(fleetInfo.Username,fleetInfo.Password, sessionId,fleetInfo.GeotabDatabase, handler: httpHandler);
            api.Authenticate();

            var devices = api.Call<List<Device>>("Get", typeof(Device));
            Console.WriteLine($"got {devices.Count} devices.");

        }

        public struct GeotabFleetInformation
        {
            public string Username;
            public string Password;
            public string GeotabDatabase;
        }
    }

    internal class CountingHttpHandler : System.Net.Http.MessageProcessingHandler
    {
        public CountingHttpHandler(){
            this.InnerHandler = new System.Net.Http.HttpClientHandler();
        }
        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Request {request.Method} {request.RequestUri} {content}");
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Reponse {response.StatusCode}");
            return response;
        }
    }
}

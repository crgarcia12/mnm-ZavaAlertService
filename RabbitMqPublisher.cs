using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace ZavaAlertService
{
    internal sealed class RabbitMqPublisher
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public bool PublishAlert(AlertEvent alert)
        {
            var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
            var managementPort = Environment.GetEnvironmentVariable("RABBITMQ_MANAGEMENT_PORT") ?? "15672";
            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "zava_app";
            var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "zava_pass";
            var vhost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/zavabank";

            var baseUri = string.Format("http://{0}:{1}/api/exchanges/{2}/zava.notifications/publish", host, managementPort, Uri.EscapeDataString(vhost));
            var payloadJson = _serializer.Serialize(alert);
            var bodyObject = new
            {
                properties = new { },
                routing_key = string.Empty,
                payload = payloadJson,
                payload_encoding = "string"
            };

            var body = Encoding.UTF8.GetBytes(_serializer.Serialize(bodyObject));

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(baseUri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = body.Length;
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password));
                request.Headers["Authorization"] = "Basic " + credentials;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(body, 0, body.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    var responseContent = reader.ReadToEnd();
                    return response.StatusCode == HttpStatusCode.OK && responseContent.IndexOf("\"routed\":true", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

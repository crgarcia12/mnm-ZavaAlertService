using System.Web;

namespace ZavaAlertService
{
    public class HealthHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Write("OK");
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CConfig;
using ClassModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApi.Controllers
{
    [RoutePrefix("Echo")]
    public class EchoController : ApiController
    {
        XMLConf Config = new XMLConf();
        ApiConfig ApiConfig = new ApiConfig();
        ServiceConfig ServiceConfig = new ServiceConfig();
        Functions FC = new Functions();

        public EchoController()
        {
            try
            {
                string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                Config.CarpetaLogAplicativo = "Log_Api";
                Config.CarpetaAplicativo = BaseDirectory;
                Config.CarpetaFecha = true;

                string pathConfig = BaseDirectory + "\\Config\\Configuration.json";

                if (File.Exists(pathConfig))
                {
                    using (StreamReader file = File.OpenText(pathConfig))
                    {
                        using (JsonTextReader reader = new JsonTextReader(file))
                        {
                            JObject JOConfig = (JObject)JToken.ReadFrom(reader);

                            ApiConfig = JsonConvert.DeserializeObject<ApiConfig>(
                                JOConfig.ToString()
                            );
                        }
                    }

                    if (File.Exists(ApiConfig?.ConfigPath?.Trim()))
                    {
                        using (StreamReader file = File.OpenText(ApiConfig.ConfigPath.Trim()))
                        {
                            using (JsonTextReader reader = new JsonTextReader(file))
                            {
                                JObject JOConfig = (JObject)JToken.ReadFrom(reader);

                                ServiceConfig = JsonConvert.DeserializeObject<ServiceConfig>(
                                    JOConfig.ToString()
                                );

                                FC.Connection =
                                    $"DATA SOURCE={ServiceConfig.Oracle.Server}:{ServiceConfig.Oracle.Port}/{ServiceConfig.Oracle.Database};PASSWORD={ServiceConfig.Oracle.Password};PERSIST SECURITY INFO=True;USER ID={ServiceConfig.Oracle.Username};Connection Timeout={ServiceConfig.Oracle.TimeOut}";
                                FC.Schema = ServiceConfig.Oracle.Username;
                                FC.ApiConfig = ServiceConfig;
                                FC.Config = Config;

                                Config.MyLog(
                                    "EchoController",
                                    "Log",
                                    "Configuration loaded successfully.",
                                    ApiConfig.EnableLog
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Config.MyLog("EchoController", "Error", ex.Message, ApiConfig.EnableLog);
            }
        }

        [HttpGet]
        [Route("Database")]
        public IHttpActionResult Echo()
        {
            string CorrelationId = Request.GetCorrelationId().ToString();

            Response Response = FC.Connect(CorrelationId);

            if (!Response.Status)
            {
                return Content(HttpStatusCode.InternalServerError, Response);
            }

            return Content(HttpStatusCode.OK, Response);
        }
    }
}

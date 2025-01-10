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
    [RoutePrefix("Interface")]
    public class ReleaseOrderController : ApiController
    {
        XMLConf Config = new XMLConf();
        Functions FC = new Functions();
        ApiConfig ApiConfig = new ApiConfig();
        ServiceConfig ServiceConfig = new ServiceConfig();

        public ReleaseOrderController()
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
                        using (StreamReader ConfigFile = File.OpenText(ApiConfig.ConfigPath.Trim()))
                        {
                            using (JsonTextReader ConfigReader = new JsonTextReader(ConfigFile))
                            {
                                JObject JConfig = (JObject)JToken.ReadFrom(ConfigReader);

                                ServiceConfig = JsonConvert.DeserializeObject<ServiceConfig>(
                                    JConfig.ToString()
                                );

                                FC.Connection =
                                    $"DATA SOURCE={ServiceConfig.Oracle.Server}:{ServiceConfig.Oracle.Port}/{ServiceConfig.Oracle.Database};PASSWORD={ServiceConfig.Oracle.Password};PERSIST SECURITY INFO=True;USER ID={ServiceConfig.Oracle.Username};Connection Timeout={ServiceConfig.Oracle.TimeOut}";
                                FC.Schema = ServiceConfig.Oracle.Username;
                                FC.ApiConfig = ServiceConfig;
                                FC.Config = Config;

                                Config.MyLog(
                                    "ReleaseOrderController",
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
                Config.MyLog("ReleaseOrderController", "Error", ex.Message, ApiConfig.EnableLog);
            }
        }

        [HttpGet]
        [Route("SalesOrder")]
        public IHttpActionResult Echo()
        {
            var Response = new Response { Status = true, Message = "Sales Order API active" };

            return Content(HttpStatusCode.OK, Response);
        }

        [HttpPost]
        [Route("SalesOrder")]
        public IHttpActionResult SaveSalesOrder([FromBody] SalesOrder Body)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "SaveSalesOrder",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );
            Config.MyLog(
                "SaveSalesOrder",
                "Request",
                JsonConvert.SerializeObject(Body, Formatting.None),
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "SaveSalesOrder",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            if (Body == null)
            {
                Response Response = FC.HandleError(
                    "SaveSalesOrder",
                    "The body is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.BadRequest, Response);
            }

            FResponse FResponse = FC.ProcessSalesOrder(
                Body,
                Request.Headers.Authorization?.Parameter,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

        [HttpPut]
        [Route("SalesOrderCancel")]
        public IHttpActionResult CancelSalesOrder([FromBody] CancelOrder Body)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "CancelSalesOrder",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );
            Config.MyLog(
                "CancelSalesOrder",
                "Request",
                JsonConvert.SerializeObject(Body, Formatting.None),
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "CancelSalesOrder",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            if (Body == null)
            {
                Response Response = FC.HandleError(
                    "CancelSalesOrder",
                    "The body is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.BadRequest, Response);
            }

            FResponse FResponse = FC.CancelSalesOrder(
                Body,
                Request.Headers.Authorization?.Parameter,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

        [HttpPut]
        [Route("SalesOrderExchange")]
        public IHttpActionResult ExchangeSalesOrder([FromBody] ExchangeOrder Body)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "ExchangeSalesOrder",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );
            Config.MyLog(
                "ExchangeSalesOrder",
                "Request",
                JsonConvert.SerializeObject(Body, Formatting.None),
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "ExchangeSalesOrder",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            if (Body == null)
            {
                Response Response = FC.HandleError(
                    "ExchangeSalesOrder",
                    "The body is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.BadRequest, Response);
            }

            FResponse FResponse = FC.ExchangeSalesOrder(
                Body,
                Request.Headers.Authorization?.Parameter,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

        [HttpPut]
        [Route("SalesOrderReturn")]
        public IHttpActionResult ReturnSalesOrder([FromBody] CancelOrder Body)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "ReturnSalesOrder",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );
            Config.MyLog(
                "ReturnSalesOrder",
                "Request",
                JsonConvert.SerializeObject(Body, Formatting.None),
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "ReturnSalesOrder",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            if (Body == null)
            {
                Response Response = FC.HandleError(
                    "ReturnSalesOrder",
                    "The body is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.BadRequest, Response);
            }

            FResponse FResponse = FC.CancelSalesOrder(
                Body,
                Request.Headers.Authorization?.Parameter,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }
    }
}

using ClassModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using CConfig;
using System.Net;
using System.Net.Http;

namespace WebApi.Controllers
{
    [RoutePrefix("Users")]
    public class UsersController : ApiController
    {
        XMLConf Config = new XMLConf();
        Functions FC = new Functions();
        ApiConfig ApiConfig = new ApiConfig();
        ServiceConfig ServiceConfig = new ServiceConfig();
        public  UsersController()
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
                                    "UsersController",
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
                Config.MyLog("ControUsersControllerller", "Error", ex.Message, ApiConfig.EnableLog);
            }
        }

        [HttpGet]
        [Route("Echo")]
        public IHttpActionResult Echo()
        {
            var Response = new Response { Status = true, Message = "Users API active" };

            return Content(HttpStatusCode.OK, Response);
        }

        [HttpGet]
        [Route("GetStoresAndEmployees")]
        public IHttpActionResult GetStoresAndEmployees()
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "GetStoresAndEmployees",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );


            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "GetStoresAndEmployees",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

        

            FResponse FResponse = FC.GetStoresAndEmployees(
                Request.Headers.Authorization?.Parameter,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }
    }
}
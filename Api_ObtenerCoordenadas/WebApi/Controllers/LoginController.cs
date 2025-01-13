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
    [RoutePrefix("Api/v1/login")]
    public class LoginController : ApiController
    {
        XMLConf Config = new XMLConf();
        Functions FC = new Functions();
        ApiConfig ApiConfig = new ApiConfig();
        ServiceConfig ServiceConfig = new ServiceConfig();

        public LoginController()
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
                    Config.MyLog("LoginController", "Info", "Configuration.json found.", ApiConfig.EnableLog);

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
                        Config.MyLog("LoginController", "Info", "Service configuration file found.", ApiConfig.EnableLog);

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
                                    "LoginController",
                                    "Log",
                                    "Configuration loaded successfully.",
                                    ApiConfig.EnableLog
                                );
                            }
                        }
                    }
                    else
                    {
                        Config.MyLog("LoginController", "Error", "Service configuration file not found.", ApiConfig.EnableLog);
                    }
                }
                else
                {
                    Config.MyLog("LoginController", "Error", "Configuration.json not found.", ApiConfig.EnableLog);
                }
            }
            catch (Exception ex)
            {
                Config.MyLog("Controller", "Error", ex.Message, ApiConfig.EnableLog);
            }
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Echo()
        {
            var Response = new Response { Status = true, Message = "Authentication API active" };

            return Content(HttpStatusCode.OK, Response);
        }

        [HttpPost]
        [Route("Token")]
        public IHttpActionResult Token(LoginBody Login)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();

            Config.MyLog(
                "Token",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            Config.MyLog(
                "Token",
                "Request",
                JsonConvert.SerializeObject(Login, Formatting.None),
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Login == null)
            {
                Response Response = new Response
                {
                    Status = false,
                    Message = "The body is invalid or missing"
                };
                Config.MyLog(
                    "Token",
                    "Error",
                    Response.Message,
                    ApiConfig.EnableLog,
                    $"{CorrelationId}_"
                );
                return Content(HttpStatusCode.BadRequest, Response);
            }

            FResponse FResponse = FC.Login(Login?.User, Login?.Password, CorrelationId);

            return Content(FResponse.StatusCode, FResponse.Response);
        }

    }
}

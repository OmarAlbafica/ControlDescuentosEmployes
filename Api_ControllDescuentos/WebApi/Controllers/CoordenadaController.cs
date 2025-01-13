using CConfig;
using ClassModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;


namespace WebApi.Controllers
{

    [RoutePrefix("Coordenada")]
    public class CoordenadaController : ApiController
    {
        XMLConf Config = new XMLConf();
        Functions FC = new Functions();
        ApiConfig ApiConfig = new ApiConfig();
        ServiceConfig ServiceConfig = new ServiceConfig();
        public CoordenadaController()
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

        [HttpPost]
        [Route("CreateCoordenada")]
        public IHttpActionResult CreateCoordenada([FromBody] CoordenadaRequest request)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "CreateCoordenada",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "CreateCoordenada",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            FResponse FResponse = FC.CrearCoordenada(
                Request.Headers.Authorization?.Parameter,
                new Coordenada
                {
                    A1 = request.A1,
                    B1 = request.B1,
                    C1 = request.C1,
                    D1 = request.D1,
                    E1 = request.E1,
                    F1 = request.F1,
                    G1 = request.G1,
                    H1 = request.H1,
                    I1 = request.I1,
                    J1 = request.J1,
                    A2 = request.A2,
                    B2 = request.B2,
                    C2 = request.C2,
                    D2 = request.D2,
                    E2 = request.E2,
                    F2 = request.F2,
                    G2 = request.G2,
                    H2 = request.H2,
                    I2 = request.I2,
                    J2 = request.J2,
                    A3 = request.A3,
                    B3 = request.B3,
                    C3 = request.C3,
                    D3 = request.D3,
                    E3 = request.E3,
                    F3 = request.F3,
                    G3 = request.G3,
                    H3 = request.H3,
                    I3 = request.I3,
                    J3 = request.J3,
                    A4 = request.A4,
                    B4 = request.B4,
                    C4 = request.C4,
                    D4 = request.D4,
                    E4 = request.E4,
                    F4 = request.F4,
                    G4 = request.G4,
                    H4 = request.H4,
                    I4 = request.I4,
                    J4 = request.J4,
                    A5 = request.A5,
                    B5 = request.B5,
                    C5 = request.C5,
                    D5 = request.D5,
                    E5 = request.E5,
                    F5 = request.F5,
                    G5 = request.G5,
                    H5 = request.H5,
                    I5 = request.I5,
                    J5 = request.J5,
                    UsuarioCoordenadaId = request.UsuarioCoordenadaId
                },
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

        [HttpPost]
        [Route("UpdateCoordenada")]
        public IHttpActionResult UpdateCoordenada([FromBody] CoordenadaRequest request)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "UpdateCoordenada",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "UpdateCoordenada",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            FResponse FResponse = FC.ActualizarCoordenada(
                Request.Headers.Authorization?.Parameter,
                new Coordenada
                {
                    A1 = request.A1,
                    B1 = request.B1,
                    C1 = request.C1,
                    D1 = request.D1,
                    E1 = request.E1,
                    F1 = request.F1,
                    G1 = request.G1,
                    H1 = request.H1,
                    I1 = request.I1,
                    J1 = request.J1,
                    A2 = request.A2,
                    B2 = request.B2,
                    C2 = request.C2,
                    D2 = request.D2,
                    E2 = request.E2,
                    F2 = request.F2,
                    G2 = request.G2,
                    H2 = request.H2,
                    I2 = request.I2,
                    J2 = request.J2,
                    A3 = request.A3,
                    B3 = request.B3,
                    C3 = request.C3,
                    D3 = request.D3,
                    E3 = request.E3,
                    F3 = request.F3,
                    G3 = request.G3,
                    H3 = request.H3,
                    I3 = request.I3,
                    J3 = request.J3,
                    A4 = request.A4,
                    B4 = request.B4,
                    C4 = request.C4,
                    D4 = request.D4,
                    E4 = request.E4,
                    F4 = request.F4,
                    G4 = request.G4,
                    H4 = request.H4,
                    I4 = request.I4,
                    J4 = request.J4,
                    A5 = request.A5,
                    B5 = request.B5,
                    C5 = request.C5,
                    D5 = request.D5,
                    E5 = request.E5,
                    F5 = request.F5,
                    G5 = request.G5,
                    H5 = request.H5,
                    I5 = request.I5,
                    J5 = request.J5,
                    UsuarioCoordenadaId = request.UsuarioCoordenadaId,
                    Id = request.Id
                },
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

        [HttpGet]
        [Route("GetUsuarioCoordenada")]
        public IHttpActionResult GetUsuarioCoordenada(int id)
        {
            string CorrelationId = Request.GetCorrelationId().ToString();
            Config.MyLog(
                "GetUsuarioCoordenada",
                "Url",
                Request.RequestUri.AbsoluteUri,
                ApiConfig.EnableLog,
                $"{CorrelationId}_"
            );

            if (Request.Headers.Authorization == null)
            {
                Response Response = FC.HandleError(
                    "GetUsuarioCoordenada",
                    "The authentication token is invalid or missing",
                    null,
                    CorrelationId
                );
                return Content(HttpStatusCode.Unauthorized, Response);
            }

            FResponse FResponse = FC.GetUsuarioCoordenada(
                Request.Headers.Authorization?.Parameter,
                id,
                CorrelationId
            );

            return Content(FResponse.StatusCode, FResponse.Response);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using CConfig;
using ClassModel.Model;
using CSID;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace ClassModel
{
    public class Functions
    {
        public string Connection { get; set; }
        public string Schema { get; set; }
        public XMLConf Config { get; set; } = new XMLConf();
        public ServiceConfig ApiConfig { get; set; } = new ServiceConfig();
        public ApiConfig AConfig { get; set; } = new ApiConfig();
        CSID.CSID CSid = new CSID.CSID();

        public Functions()
        {
            Config.CarpetaLogAplicativo = "Log_Api";
            Config.CarpetaAplicativo = AppDomain.CurrentDomain.BaseDirectory;
            Config.CarpetaFecha = true;
        }

        public Response Connect(string CorrelationId)
        {
            var Response = new Response();
            try
            {
                using (ModelOracle Context = new ModelOracle(Connection, Schema))
                {
                    Context.Database.Connection.Open();

                    Response.Status = true;
                    Response.Message = "Connection to the database established successfully.";

                    Config.MyLog(
                        "Connect",
                        "Log",
                        Response.Message,
                        AConfig.EnableLog,
                        $"{CorrelationId}_"
                    );

                    Context.Database.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                Response.Status = false;
                Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "Connect",
                    "Error",
                    Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }

            return Response;
        }

        public FResponse GetUser(
            string Username,
            string Password,
            List<AuthorizedUser> AuthorizedUsers,
            string CorrelationId
        )
        {
            FResponse FResponse = new FResponse();
            AuthorizedUser User = new AuthorizedUser();

            try
            {
                User = AuthorizedUsers
                    .Where(u => u.User == Username && u.Password == Password)
                    .SingleOrDefault();

                if (User != null)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "User obtained";
                    FResponse.Response.Result = User;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "User not found";
                    FResponse.Response.Result = User;
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "GetUser",
                    "Log",
                    FResponse.Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                FResponse.Response.Status = false;
                FResponse.Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;

                Config.MyLog(
                    "GetUser",
                    "Error",
                    FResponse.Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }

            return FResponse;
        }

        public FResponse ValidateLogin(string Username, string Password, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<string> Errors = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(Username?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Invalid credentials";
                    FResponse.StatusCode = HttpStatusCode.BadRequest;

                    Errors.Add("The User field is required");

                    Config.MyLog(
                        "ValidateLogin",
                        "Error",
                        "The User field is required",
                        AConfig.EnableLog,
                        $"{CorrelationId}_"
                    );
                }
                if (string.IsNullOrEmpty(Password?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Invalid credentials";
                    FResponse.StatusCode = HttpStatusCode.BadRequest;

                    Errors.Add("The Password field is required");

                    Config.MyLog(
                        "ValidateLogin",
                        "Error",
                        "The Password field is required",
                        AConfig.EnableLog,
                        $"{CorrelationId}_"
                    );
                }

                FResponse.Response.Result = Errors.ToArray();

                if (Errors.Count == 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Valid data";
                }

                Config.MyLog(
                    "ValidateLogin",
                    FResponse.Response.Status ? "Log" : "Error",
                    FResponse.Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                FResponse.Response.Status = false;
                FResponse.Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Errors.Add(ex.Message);

                FResponse.Response.Result = Errors.ToArray();
                FResponse.StatusCode = HttpStatusCode.InternalServerError;

                Config.MyLog(
                    "ValidateLogin",
                    "Error",
                    FResponse.Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }

            return FResponse;
        }

        public FResponse Login(string Username, string Password, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<string> Errors = new List<string>();

            try
            {
                FResponse RespValidData = ValidateLogin(Username, Password, CorrelationId);

                if (!RespValidData.Response.Status)
                    return RespValidData;

                FResponse RespUser = GetUser(
                    Username,
                    Password,
                    ApiConfig.AuthorizedUsers,
                    CorrelationId
                );

                if (!RespUser.Response.Status)
                    return RespUser;

                AuthorizedUser FoundUser = RespUser.Response.Result;
                Session NewSession = new Session
                {
                    InitialDate = DateTime.Now.ToString("s"),
                    FinalDate = DateTime.Now.AddMinutes(ApiConfig.TimeOutSession).ToString("s"),
                    User = FoundUser.User
                };

                string Juser = JsonConvert.SerializeObject(NewSession);
                string JencryptedUser = Config.Encriptar_conKey(Juser, "MD5", "ControlDescuentosEJJE");

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Token generated";
                FResponse.Response.Result = JencryptedUser;

                Config.MyLog(
                    "Login",
                    "Token",
                    FResponse.Response.Result,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                FResponse.Response.Status = false;
                FResponse.Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;
                Errors.Add(FResponse.Response.Message);
                FResponse.Response.Result = Errors.ToArray();

                FResponse.StatusCode = HttpStatusCode.InternalServerError;

                Config.MyLog(
                    "Login",
                    "Error",
                    FResponse.Response.Message,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
            }

            return FResponse;
        }


        public FResponse ValidBody(SalesOrder Body, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<string> Errors = new List<string>();
            try
            {
                if (string.IsNullOrEmpty(Body.StoreNo?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'storeno' is missing");
                }
                if (string.IsNullOrEmpty(Body.OrigStoreNo?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'origstoreno' is missing");
                }
                if (Body?.OrderType < 0 || Body?.OrderType > 6)
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordertype' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.OrderStatus?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'orderstatus' is invalid or missing");
                }
                if (!DateTime.TryParse(Body?.OrderedDate, out _))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordereddate' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.OrderTrackingNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordertrackingno' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.FoNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'fono' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.RoNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'rono' is invalid or missing");
                }
                if (Body.Customer == null || !(Body.Customer is Customer))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.Customer?.FirstName?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.firstname' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.Customer?.LastName?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.lastname' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.Customer?.StoreNo?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.storeno' is invalid or missing");
                }
                if (
                    Body.Customer?.CustAddresses == null
                    || !(Body.Customer?.CustAddresses is List<CustAddress>)
                    || Body.Customer?.CustAddresses?.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.custaddress' is invalid or missing");
                }
                if (
                    Body.Customer?.CustEmails == null
                    || !(Body.Customer?.CustEmails is List<CustEmail>)
                    || Body.Customer?.CustEmails?.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.custemail' is invalid or missing");
                }
                if (
                    Body.Customer?.CustPhones == null
                    || !(Body.Customer?.CustPhones is List<CustPhone>)
                    || Body.Customer?.CustPhones?.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'customer.custphone' is invalid or missing");
                }
                if ((bool)!(Body.Customer?.CustAddresses?.All(c => c is CustAddress)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "One or more items in the list 'customer.custaddress' are invalid or missing."
                    );
                }
                if ((bool)!(Body.Customer?.CustEmails?.All(c => c is CustEmail)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "One or more items in the list 'customer.custemail' are invalid or missing."
                    );
                }
                if ((bool)!(Body.Customer?.CustPhones?.All(c => c is CustPhone)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "One or more items in the list 'customer.custphone' are invalid or missing."
                    );
                }
                if ((bool)Body.Customer?.CustAddresses?.Any(a => string.IsNullOrEmpty(a?.Address1)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custaddress' have the property 'address1' invalid or missing."
                    );
                }
                if (
                    (bool)
                        Body.Customer?.CustAddresses?.Any(a => string.IsNullOrEmpty(a?.AddressType))
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custaddress' have the property 'addresstype' invalid or missing."
                    );
                }
                if (
                    (bool)
                        Body.Customer?.CustAddresses?.Any(a => string.IsNullOrEmpty(a?.PostalCode))
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custaddress' have the property 'postalcode' invalid or missing."
                    );
                }
                if (
                    (bool)Body.Customer?.CustEmails?.Any(e => string.IsNullOrEmpty(e?.EmailAddress))
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custemail' have the property 'emailaddress' invalid or missing."
                    );
                }
                if ((bool)Body.Customer?.CustEmails?.Any(e => string.IsNullOrEmpty(e?.EmailType)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custemail' have the property 'emailtype' invalid or missing."
                    );
                }
                if ((bool)Body.Customer?.CustPhones?.Any(p => string.IsNullOrEmpty(p?.PhoneNo)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custphone' have the property 'phoneno' invalid or missing."
                    );
                }
                if ((bool)Body.Customer?.CustPhones?.Any(p => string.IsNullOrEmpty(p?.PhoneType)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'customer.custphone' have the property 'phonetype' invalid or missing."
                    );
                }
                if (
                    Body.DocItems == null
                    || !(Body.DocItems is List<DocItem>)
                    || Body.DocItems.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'docitem' is invalid or missing");
                }
                if ((bool)!(Body.DocItems?.All(di => di is DocItem)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("One or more items in the list 'docitem' are invalid or missing.");
                }
                if ((bool)Body.DocItems?.Any(di => di?.ItemPos <= 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'itempos' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.Qty <= 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'qty' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => string.IsNullOrEmpty(di?.ScanUpc)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'scanupc' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.ItemType != 3))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'itemtype' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => string.IsNullOrEmpty(di?.StoreNo)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'storeno' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => string.IsNullOrEmpty(di?.FulfillStoreNo)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'fulfillstoreno' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.Price <= 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'price' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.TaxAmt < 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'taxamt' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.TaxPerc < 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'taxperc' invalid or missing."
                    );
                }
                if (Body.FulfilmentInfo == null || !(Body.FulfilmentInfo is FulfilmentInfo))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'fulfilmentinfo' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.FulfilmentInfo?.DeliveryMethod?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "The property 'fulfilmentinfo.deliverymethod' is invalid or missing"
                    );
                }
                if (string.IsNullOrEmpty(Body.FulfilmentInfo?.DeliverySpeed?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'fulfilmentinfo.deliveryspeed' is invalid or missing");
                }
                if (!DateTime.TryParse(Body.FulfilmentInfo?.EstimatedShippingDate, out _))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "The property 'fulfilmentinfo.estimatedshippingdate' is invalid or missing"
                    );
                }
                if (
                    Body?.DocTenders == null
                    || !(Body?.DocTenders is List<DocTender>)
                    || Body?.DocTenders?.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'doctender' is invalid or missing");
                }
                if ((bool)!(Body?.DocTenders?.All(di => di is DocTender)))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("One or more items in the list 'doctender' are invalid or missing.");
                }
                if ((bool)Body?.DocTenders?.Any(di => di?.TenderPos <= 0))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'doctender' have the property 'tenderpos' invalid or missing."
                    );
                }
                if ((bool)Body?.DocTenders?.Any(di => di?.TenderType < 0 || di?.TenderType > 16))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'doctender' have the property 'tendertype' invalid or missing."
                    );
                }

                FResponse.Response.Result = Errors.ToArray();

                if (Errors.Count == 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Valid data";
                }

                FResponse.StatusCode =
                    Errors.Count > 0 ? HttpStatusCode.BadRequest : HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                FResponse.Response = HandleError(
                    "ValidBody",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }


        public FResponse DecryptToken(string AuthToken, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                string JSession = Config.Desencriptar_conKey(AuthToken, "MD5", "ControlDescuentosEJJE");
                Config.MyLog(
                    "DecryptToken",
                    "Log",
                    JSession,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Decrypted token";
                FResponse.Response.Result = JSession;
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                FResponse.Response = HandleError(
                    "DecryptToken",
                    $"The token sent is not valid: {AuthToken}",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.Unauthorized;
            }

            return FResponse;
        }

        public Response HandleError(
            string Name,
            string errorMessage,
            Response response,
            string correlationId,
            Exception ex = null
        )
        {
            Config.MyLog(Name, "Error", errorMessage, AConfig.EnableLog, $"{correlationId}_");
            Response myResponse = response ?? new Response();

            if (ex != null)
            {
                string detailedErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;
                Config.MyLog(
                    Name,
                    "Error Detallado",
                    detailedErrorMessage,
                    AConfig.EnableLog,
                    $"{correlationId}_"
                );
            }

            myResponse.Status = false;
            myResponse.Message = errorMessage;
            myResponse.Result = null;

            return myResponse;
        }

        public FResponse GetValidToken(string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<Store> stores = new List<Store>();

            try
            {
                Config.MyLog("GetValidToken", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("GetValidToken", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("GetValidToken", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("GetValidToken", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Information obtained successfully";
                FResponse.Response.Result = 1;
                FResponse.StatusCode = HttpStatusCode.OK;
                Config.MyLog("GetValidToken", "Success", "Information obtained successfully", AConfig.EnableLog, $"{CorrelationId}_");

            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("GetValidToken", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }


        public FResponse LoginUsuarioControlDescuento(string Email, string Password, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<string> Errors = new List<string>();

            try
            {
                // Validar los datos de entrada
                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Email and Password are required";
                    FResponse.StatusCode = HttpStatusCode.BadRequest;
                    return FResponse;
                }

                Config.MyLog("LoginUsuarioControlDescuento", "Request", $"Email: {Email}", AConfig.EnableLog, $"{CorrelationId}_");

                // Conexión a la base de datos
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Llamar a la consulta para obtener el query con interpolación
                    string query = LoginUsuarioControlDescuentoQuery(Email, CorrelationId);

                    // Ejecutar la consulta y obtener el primer usuario
                    var user = context.Database.SqlQuery<UsuarioControlDescuento>(query).FirstOrDefault();


                    if (user == null)
                    {
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "User not found";
                        FResponse.StatusCode = HttpStatusCode.NotFound;
                        Config.MyLog("LoginUsuarioControlDescuento", "Error", "User not found", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Comparar la contraseña encriptada
                    bool isPasswordValid = CompararContraseña(Password, user.Password); // Método para comparar contraseñas encriptadas
                    if (!isPasswordValid)
                    {
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "Invalid password";
                        FResponse.StatusCode = HttpStatusCode.Unauthorized;
                        Config.MyLog("LoginUsuarioControlDescuento", "Error", "Invalid password", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Si las credenciales son correctas, generar el token
                    Session NewSession = new Session
                    {
                        InitialDate = DateTime.Now.ToString("s"),
                        FinalDate = DateTime.Now.AddMinutes(ApiConfig.TimeOutSession).ToString("s"),
                        User = user.Email
                    };

                    string Juser = JsonConvert.SerializeObject(NewSession);
                    string token = Config.Encriptar_conKey(Juser, "MD5", "ControlDescuentosEJJE");

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Token generated";
                    FResponse.Response.Result = new { token, user.isadmin };

                    FResponse.StatusCode = HttpStatusCode.OK;
                    Config.MyLog("LoginUsuarioControlDescuento", "Token", token, AConfig.EnableLog, $"{CorrelationId}_");
                }
            }
            catch (Exception ex)
            {
                FResponse.Response.Status = false;
                FResponse.Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;
                Errors.Add(FResponse.Response.Message);
                FResponse.Response.Result = Errors.ToArray();

                FResponse.StatusCode = HttpStatusCode.InternalServerError;

                Config.MyLog("LoginUsuarioControlDescuento", "Error", FResponse.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
            }

            return FResponse;
        }

        public string LoginUsuarioControlDescuentoQuery(string Email, string CorrelationId)
        {
            // Consulta SQL con interpolación directa de parámetros
            string Sql = $@"
        SELECT id, nombre, email, password, isadmin 
        FROM Usuario_Control_Descuento
        WHERE email = '{Email}'";

            // Log de la consulta SQL
            Config.MyLog("LoginUsuarioControlDescuentoQuery", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public bool CompararContraseña(string plainPassword, string encryptedPassword)
        {
            // Asumir que la contraseña está encriptada usando MD5 o el algoritmo correspondiente.
            string hashedPassword = Config.Encriptar_conKey(plainPassword, "MD5", "ControlDescuentosEJJE");

            return hashedPassword == encryptedPassword;
        }

        public FResponse getKeyEncript()
        {
            FResponse FResponse = new FResponse();
            List<string> Errors = new List<string>();

            try
            {
             
                string JencryptedUser = Config.Encriptar_conKey("Ejje2024", "MD5", "ControlDescuentosEJJE");

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Token generated";
                FResponse.Response.Result = JencryptedUser;

             

                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                FResponse.Response.Status = false;
                FResponse.Response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;
                Errors.Add(FResponse.Response.Message);
                FResponse.Response.Result = Errors.ToArray();

                FResponse.StatusCode = HttpStatusCode.InternalServerError;

                
            }

            return FResponse;
        }

        public FResponse GetUsuarioCoordenada(string Token, string subsidiaria, string tienda, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("GetUsuarioCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("GetUsuarioCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    u.ID AS USUARIO_COORDENADA_ID,
                    u.EMAIL,
                    TO_CHAR(u.SID) AS EMPLOYEE_SID,
                    u.TIENDA,
                    u.MAX_DESCUENTO,
                    u.CICLO_COORDENADA,
                    u.SUBSIDIARIA,
                    c.ID AS COORDENADA_ID,
                    c.""A1"", c.""B1"", c.""C1"", c.""D1"", c.""E1"", c.""F1"", c.""G1"", c.""H1"", c.""I1"", c.""J1"",
                    c.""A2"", c.""B2"", c.""C2"", c.""D2"", c.""E2"", c.""F2"", c.""G2"", c.""H2"", c.""I2"", c.""J2"",
                    c.""A3"", c.""B3"", c.""C3"", c.""D3"", c.""E3"", c.""F3"", c.""G3"", c.""H3"", c.""I3"", c.""J3"",
                    c.""A4"", c.""B4"", c.""C4"", c.""D4"", c.""E4"", c.""F4"", c.""G4"", c.""H4"", c.""I4"", c.""J4"",
                    c.""A5"", c.""B5"", c.""C5"", c.""D5"", c.""E5"", c.""F5"", c.""G5"", c.""H5"", c.""I5"", c.""J5""
                FROM 
                    USUARIO_COORDENADA u
                JOIN 
                    COORDENADAS c
                ON 
                    u.ID = c.USUARIO_COORDENADA_ID
                WHERE 
                    u.SUBSIDIARIA = :subsidiaria
                AND 
                    u.TIENDA = :tienda";

                    Config.MyLog("GetUsuarioCoordenada", "Query", query, true, $"{CorrelationId}_");

                    using (var command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("subsidiaria", subsidiaria));
                        command.Parameters.Add(new OracleParameter("tienda", tienda));

                        using (var reader = command.ExecuteReader())
                        {
                            var coordenadas = new List<Coordenada>();

                            while (reader.Read())
                            {
                                coordenadas.Add(new Coordenada
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("COORDENADA_ID")),
                                    A1 = reader.IsDBNull(reader.GetOrdinal("A1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("A1")),
                                    B1 = reader.IsDBNull(reader.GetOrdinal("B1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("B1")),
                                    C1 = reader.IsDBNull(reader.GetOrdinal("C1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("C1")),
                                    D1 = reader.IsDBNull(reader.GetOrdinal("D1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("D1")),
                                    E1 = reader.IsDBNull(reader.GetOrdinal("E1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("E1")),
                                    F1 = reader.IsDBNull(reader.GetOrdinal("F1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("F1")),
                                    G1 = reader.IsDBNull(reader.GetOrdinal("G1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("G1")),
                                    H1 = reader.IsDBNull(reader.GetOrdinal("H1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("H1")),
                                    I1 = reader.IsDBNull(reader.GetOrdinal("I1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("I1")),
                                    J1 = reader.IsDBNull(reader.GetOrdinal("J1")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("J1")),
                                    A2 = reader.IsDBNull(reader.GetOrdinal("A2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("A2")),
                                    B2 = reader.IsDBNull(reader.GetOrdinal("B2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("B2")),
                                    C2 = reader.IsDBNull(reader.GetOrdinal("C2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("C2")),
                                    D2 = reader.IsDBNull(reader.GetOrdinal("D2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("D2")),
                                    E2 = reader.IsDBNull(reader.GetOrdinal("E2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("E2")),
                                    F2 = reader.IsDBNull(reader.GetOrdinal("F2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("F2")),
                                    G2 = reader.IsDBNull(reader.GetOrdinal("G2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("G2")),
                                    H2 = reader.IsDBNull(reader.GetOrdinal("H2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("H2")),
                                    I2 = reader.IsDBNull(reader.GetOrdinal("I2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("I2")),
                                    J2 = reader.IsDBNull(reader.GetOrdinal("J2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("J2")),
                                    A3 = reader.IsDBNull(reader.GetOrdinal("A3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("A3")),
                                    B3 = reader.IsDBNull(reader.GetOrdinal("B3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("B3")),
                                    C3 = reader.IsDBNull(reader.GetOrdinal("C3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("C3")),
                                    D3 = reader.IsDBNull(reader.GetOrdinal("D3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("D3")),
                                    E3 = reader.IsDBNull(reader.GetOrdinal("E3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("E3")),
                                    F3 = reader.IsDBNull(reader.GetOrdinal("F3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("F3")),
                                    G3 = reader.IsDBNull(reader.GetOrdinal("G3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("G3")),
                                    H3 = reader.IsDBNull(reader.GetOrdinal("H3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("H3")),
                                    I3 = reader.IsDBNull(reader.GetOrdinal("I3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("I3")),
                                    J3 = reader.IsDBNull(reader.GetOrdinal("J3")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("J3")),
                                    A4 = reader.IsDBNull(reader.GetOrdinal("A4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("A4")),
                                    B4 = reader.IsDBNull(reader.GetOrdinal("B4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("B4")),
                                    C4 = reader.IsDBNull(reader.GetOrdinal("C4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("C4")),
                                    D4 = reader.IsDBNull(reader.GetOrdinal("D4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("D4")),
                                    E4 = reader.IsDBNull(reader.GetOrdinal("E4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("E4")),
                                    F4 = reader.IsDBNull(reader.GetOrdinal("F4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("F4")),
                                    G4 = reader.IsDBNull(reader.GetOrdinal("G4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("G4")),
                                    H4 = reader.IsDBNull(reader.GetOrdinal("H4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("H4")),
                                    I4 = reader.IsDBNull(reader.GetOrdinal("I4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("I4")),
                                    J4 = reader.IsDBNull(reader.GetOrdinal("J4")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("J4")),
                                    A5 = reader.IsDBNull(reader.GetOrdinal("A5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("A5")),
                                    B5 = reader.IsDBNull(reader.GetOrdinal("B5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("B5")),
                                    C5 = reader.IsDBNull(reader.GetOrdinal("C5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("C5")),
                                    D5 = reader.IsDBNull(reader.GetOrdinal("D5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("D5")),
                                    E5 = reader.IsDBNull(reader.GetOrdinal("E5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("E5")),
                                    F5 = reader.IsDBNull(reader.GetOrdinal("F5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("F5")),
                                    G5 = reader.IsDBNull(reader.GetOrdinal("G5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("G5")),
                                    H5 = reader.IsDBNull(reader.GetOrdinal("H5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("H5")),
                                    I5 = reader.IsDBNull(reader.GetOrdinal("I5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("I5")),
                                    J5 = reader.IsDBNull(reader.GetOrdinal("J5")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("J5")),
                                    UsuarioCoordenadaId = reader.GetInt32(reader.GetOrdinal("USUARIO_COORDENADA_ID")),
                                    Email = reader.GetString(reader.GetOrdinal("EMAIL")),
                                    EmployeeSid = reader.GetString(reader.GetOrdinal("EMPLOYEE_SID")),
                                    Tienda = reader.GetString(reader.GetOrdinal("TIENDA")),
                                    MaxDescuento = reader.GetInt32(reader.GetOrdinal("MAX_DESCUENTO")),
                                    CicloCoordenada = reader.GetInt32(reader.GetOrdinal("CICLO_COORDENADA")),
                                    Subsidiaria = reader.GetString(reader.GetOrdinal("SUBSIDIARIA"))
                                });
                            }

                            if (coordenadas == null || !coordenadas.Any())
                            {
                                FResponse.Response.Status = false;
                                FResponse.Response.Message = "Coordenadas no encontradas";
                                FResponse.StatusCode = HttpStatusCode.NotFound;
                                Config.MyLog("GetUsuarioCoordenada", "Not Found", "Coordenadas no encontradas", AConfig.EnableLog, $"{CorrelationId}_");
                            }
                            else
                            {
                                FResponse.Response.Status = true;
                                FResponse.Response.Message = "Coordenadas encontradas";
                                FResponse.Response.Result = coordenadas;
                                FResponse.StatusCode = HttpStatusCode.OK;
                                Config.MyLog("GetUsuarioCoordenada", "Success", "Coordenadas encontradas", AConfig.EnableLog, $"{CorrelationId}_");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("GetUsuarioCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

    }
}

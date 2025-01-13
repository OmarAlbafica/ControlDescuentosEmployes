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

        public FResponse ProcessSalesOrder(SalesOrder Body, string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                Config.MyLog(
                    "ProcessSalesOrder",
                    "Token",
                    Token,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(
                    RespDecrypt.Response.Result
                );

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError(
                        "ProcessSalesOrder",
                        "The session has expired. Generate a new token",
                        null,
                        CorrelationId
                    );
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                FResponse RespBody = ValidBody(Body, CorrelationId);
                if (!RespBody.Response.Status)
                    return RespBody;

                FResponse RespInsert = InsertPayload(Body, CorrelationId);
                if (!RespInsert.Response.Status)
                    return RespInsert;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data processed successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Config.MyLog(
                    "ProcessSalesOrder",
                    "Error",
                    $"{ex.Message}",
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
                FResponse.Response = HandleError(
                    "ProcessSalesOrder",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
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

        public FResponse ValidCancelBody(CancelOrder Body, string CorrelationId)
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
                if (string.IsNullOrEmpty(Body.OrigStoreNo?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'origstoreno' is missing");
                }
                if (!DateTime.TryParse(Body.OrderedDate.ToString(), out _))
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
                if (
                    Body.DocItems == null
                    || !(Body.DocItems is List<CancelItem>)
                    || Body.DocItems.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'docitem' is invalid or missing");
                }
                if ((bool)!(Body.DocItems?.All(di => di is CancelItem)))
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
                if ((bool)Body.DocItems?.Any(di => di?.ItemType != 2))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'itemtype' invalid or missing."
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
                    "ValidCancelBody",
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

        public Response InsertCustomer(INP_CUSTOMER Customer, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Customer SID";
                    Config.MyLog(
                        "InsertCustomer",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long CustSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_CUSTOMER (CUST_SID, CUST_ID, SBS_SID, SBS_NO,  "
                        + "STORE_SID, STORE_NO, CUST_TYPE, FIRST_NAME, LAST_NAME, STATUS_SID) "
                        + $"VALUES ({CustSid}, '{Customer.CUST_ID}', {Customer.SBS_SID}, {Customer.SBS_NO}, "
                        + $"{Customer.STORE_SID}, {Customer.STORE_NO}, '{Customer.CUST_TYPE}', '{Customer.FIRST_NAME}', "
                        + $"'{Customer.LAST_NAME}', {Customer.STATUS_SID} )";

                    Config.MyLog("InsertCustomer", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {CustSid}";
                    response.Result = CustSid;

                    Config.MyLog(
                        "InsertCustomer",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "InsertCustomer",
                    "Error",
                    response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return response;
        }

        public FResponse SearchSalesOrder(
            string OrderTrackingNo,
            string FoNo,
            string RoNo,
            string CorrelationId,
            bool Flag = true,
            bool SelectOr = true
        )
        {
            FResponse FResponse = new FResponse();
            INP_ORDER_HEADER Document = new INP_ORDER_HEADER();

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Document = context
                        .Database.SqlQuery<INP_ORDER_HEADER>(
                            SelectSalesOrder(OrderTrackingNo, FoNo, RoNo, CorrelationId, Flag, SelectOr)
                        )
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Document == null || Document.DOC_SID <= 0)
                {
                    FResponse.Response.Status = Flag;
                    FResponse.Response.Message =
                        $"No sales order found: ordertrackingno -> {OrderTrackingNo}, fono -> {FoNo}, rono -> {RoNo}";
                    FResponse.Response.Result = Document;
                    FResponse.StatusCode = Flag ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                }
                else
                {
                    FResponse.Response.Status = !Flag;
                    FResponse.Response.Message =
                        $"Some of this data already exists in a sales order: ordertrackingno -> {OrderTrackingNo}, fono -> {FoNo}, rono -> {RoNo}";
                    FResponse.Response.Result = Document;
                    FResponse.StatusCode = HttpStatusCode.Conflict;
                }

                Config.MyLog(
                    "SearchSalesOrder",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchSalesOrder", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = 0;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectSalesOrder(
            string OrderTrackingNo,
            string FoNo,
            string RoNo,
            string CorrelationId,
            bool Flag = true,
            bool SelectOr = true
        )
        {
            string Sql =
                "SELECT * FROM INP_ORDER_HEADER "
                + $"WHERE (ORDER_TRACKING_NO = '{OrderTrackingNo.Trim()}' "
                + $"{(Flag && SelectOr ? "OR" : "AND")} FO_NO = '{FoNo.Trim()}' {(Flag && SelectOr ? "OR" : "AND")} RO_NO = '{RoNo.Trim()}') "
                + $"AND ACTIVE = 1 ";

            Config.MyLog("SelectSalesOrder", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchStore(string StoreCode, short SbsNo, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            STORE Store = new STORE();

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Store = context
                        .Database.SqlQuery<STORE>(SelectStore(StoreCode, SbsNo, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Store != null)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully.";
                    FResponse.Response.Result = Store;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No store found with the store_code = {StoreCode} in subsidiary No.{SbsNo}";
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchStore",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchStore", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectStore(string StoreCode, short SbsNo, string CorrelationId)
        {
            string Sql =
                "SELECT S.SID, S.STORE_NO, S.STORE_CODE, SB.SBS_NO, S.SBS_SID  "
                + "FROM RPS.STORE S, RPS.SUBSIDIARY SB "
                + "WHERE S.SBS_SID = SB.SID "
                + $"AND S.STORE_CODE = {StoreCode} AND SB.SBS_NO = {SbsNo} ";

            Config.MyLog("SelectStore", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse InsertPayload(SalesOrder Body, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                FResponse RespStore = SearchStore(
                    Body.StoreNo,
                    ApiConfig.Subsidiary.No,
                    CorrelationId
                );
                if (!RespStore.Response.Status)
                    return RespStore;

                STORE Store = RespStore.Response.Result;

                FResponse RespOrigStore = SearchStore(
                    Body.OrigStoreNo,
                    ApiConfig.Subsidiary.No,
                    CorrelationId
                );
                if (!RespOrigStore.Response.Status)
                    return RespOrigStore;

                FResponse RespSalesOrder = SearchSalesOrder(
                    Body.OrderTrackingNumber,
                    Body.FoNumber,
                    Body.RoNumber,
                    CorrelationId,
                    true,
                    false
                );

                if (!RespSalesOrder.Response.Status)
                {
                    RespSalesOrder.Response.Result = null;
                    return RespSalesOrder;
                }

                FResponse RespCustomer = SearchCustomer(Body?.Customer?.CustId, CorrelationId);

                long CustSid = 0;
                long AddressSid = 0;

                if (!RespCustomer.Response.Status)
                {
                    FResponse RespInsertCust = InsertNewCustomer(Body.Customer, CorrelationId);
                    if (!RespInsertCust.Response.Status)
                        return RespInsertCust;

                    CustSid = RespInsertCust?.Response?.Result?[0] ?? 0;
                    AddressSid = RespInsertCust?.Response?.Result?[1] ?? 0;
                }
                else
                {
                    CustSid = RespCustomer.Response?.Result ?? 0;

                    FResponse RespAddress = SearchAddress(CustSid, CorrelationId);
                    if (!RespAddress.Response.Status)
                        return RespAddress;

                    AddressSid = RespAddress.Response?.Result ?? 0;
                }

                FResponse RespDocNo = SearchDocNo(ApiConfig.Subsidiary.No, CorrelationId);
                if (!RespDocNo.Response.Status)
                    return RespDocNo;

                FResponse RespInsertOrder = InsertOrder(
                    Body,
                    Store,
                    RespDocNo.Response.Result,
                    CustSid,
                    AddressSid,
                    CorrelationId
                );
                if (!RespInsertOrder.Response.Status)
                    return RespInsertOrder;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data saved successfully"
                };
                FResponse.StatusCode = HttpStatusCode.Created;

                Config.MyLog(
                    "InsertPayload",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertPayload", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse InsertNewCustomer(Customer Customer, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<long> Sids = new List<long>();

            try
            {
                FResponse RespCustStore = SearchStore(Customer.StoreNo, 1, CorrelationId);
                if (!RespCustStore.Response.Status)
                    return RespCustStore;

                STORE CustStore = RespCustStore.Response.Result;

                INP_CUSTOMER NewCustomer = new INP_CUSTOMER
                {
                    CUST_ID = Customer.CustId,
                    SBS_SID = CustStore.SBS_SID,
                    SBS_NO = CustStore.SBS_NO,
                    STORE_NO = CustStore.SBS_NO,
                    STORE_SID = CustStore.SID,
                    CUST_TYPE = "CUSTOMER ONLY",
                    FIRST_NAME = Customer.FirstName.Trim(),
                    LAST_NAME = Customer.LastName.Trim(),
                    STATUS_SID = 1 // PENDING
                };

                Response RespInsertCust = InsertCustomer(NewCustomer, CorrelationId);
                if (!RespInsertCust.Status)
                {
                    FResponse.Response = RespInsertCust;
                    FResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return FResponse;
                }

                long CustSid = RespInsertCust.Result;
                Sids.Add(CustSid);

                foreach (CustPhone Phone in Customer.CustPhones)
                {
                    INP_CUST_PHONE NewPhone = new INP_CUST_PHONE
                    {
                        PHONE_TYPE = Phone.PhoneType.Trim(),
                        PHONE_NO = (short)Phone.SeqNo,
                        PHONE_PRIMARY = (short)(Phone.PrimaryFlag ? 1 : 0),
                        PHONE_NUMBER = Phone.PhoneNo.Trim(),
                        CUST_SID = CustSid
                    };

                    Response RespInsertPhone = InsertPhone(NewPhone, CorrelationId);

                    if (!RespInsertPhone.Status)
                    {
                        FResponse.Response = RespInsertPhone;
                        FResponse.StatusCode = HttpStatusCode.InternalServerError;
                        return FResponse;
                    }
                }

                foreach (CustEmail Email in Customer.CustEmails)
                {
                    INP_CUST_EMAIL NewEmail = new INP_CUST_EMAIL
                    {
                        EMAIL_TYPE = Email.EmailType.Trim(),
                        EMAIL_NO = (short)Email.SeqNo,
                        EMAIL_PRIMARY = (short)(Email.PrimaryFlag ? 1 : 0),
                        EMAIL_ADDRESS = Email.EmailAddress.Trim(),
                        CUST_SID = CustSid
                    };

                    Response RespInsertEmail = InsertEmail(NewEmail, CorrelationId);

                    if (!RespInsertEmail.Status)
                    {
                        FResponse.Response = RespInsertEmail;
                        FResponse.StatusCode = HttpStatusCode.InternalServerError;
                        return FResponse;
                    }
                }

                foreach (CustAddress Address in Customer.CustAddresses)
                {
                    INP_CUST_ADDRESS NewAddress = new INP_CUST_ADDRESS
                    {
                        ADDRESS_TYPE = Address.AddressType.Trim(),
                        ADDRESS_NO = (short)Address.SeqNo,
                        ADDRESS_NAME =
                            $"{Customer.FirstName.Trim()}-{Customer.LastName.Trim()}-{Address.AddressType.Trim()}",
                        ADDRESS_PRIMARY = (short)(Address.PrimaryFlag ? 1 : 0),
                        ADDRESS1 = Address.Address1,
                        ADDRESS2 = Address.Address2,
                        ADDRESS3 = Address.Address3,
                        POSTAL_CODE = Address.PostalCode,
                        ACTIVE = (short)(Address.Active ? 1 : 0),
                        CUST_ID = CustSid
                    };

                    Response RespInsertAddress = InsertAddress(NewAddress, CorrelationId);

                    if (!RespInsertAddress.Status)
                    {
                        FResponse.Response = RespInsertAddress;
                        FResponse.StatusCode = HttpStatusCode.InternalServerError;
                        return FResponse;
                    }

                    long AddressSid = RespInsertAddress.Result;
                    Sids.Add(AddressSid);
                }

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Customer saved successfully.",
                    Result = Sids
                };
                FResponse.StatusCode = HttpStatusCode.OK;

                Config.MyLog(
                    "InsertNewCustomer",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertNewCustomer", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public Response InsertPhone(INP_CUST_PHONE CustPhone, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Phone SID";
                    Config.MyLog(
                        "InsertPhone",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long PhoneSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_CUST_PHONE (SID, PHONE_TYPE, PHONE_NO, "
                        + "PHONE_PRIMARY, PHONE_NUMBER, CUST_SID) "
                        + $"VALUES ({PhoneSid}, '{CustPhone.PHONE_TYPE}', {CustPhone.PHONE_NO}, "
                        + $"{CustPhone.PHONE_PRIMARY}, '{CustPhone.PHONE_NUMBER}', {CustPhone.CUST_SID})";

                    Config.MyLog("InsertPhone", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {PhoneSid}";
                    response.Result = PhoneSid;

                    Config.MyLog("InsertPhone", "Log", response.Message, true, $"{CorrelationId}_");
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertPhone", "Error", response.Message, true, $"{CorrelationId}_");
            }

            return response;
        }

        public Response InsertEmail(INP_CUST_EMAIL CustEmail, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Email SID";
                    Config.MyLog(
                        "InsertEmail",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long EmailSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_CUST_EMAIL (SID, EMAIL_TYPE, EMAIL_NO, "
                        + "EMAIL_PRIMARY, EMAIL_ADDRESS, CUST_SID) "
                        + $"VALUES ({EmailSid}, '{CustEmail.EMAIL_TYPE}', {CustEmail.EMAIL_NO}, "
                        + $"{CustEmail.EMAIL_PRIMARY}, '{CustEmail.EMAIL_ADDRESS}', {CustEmail.CUST_SID})";

                    Config.MyLog("InsertEmail", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {EmailSid}";
                    response.Result = EmailSid;

                    Config.MyLog("InsertEmail", "Log", response.Message, true, $"{CorrelationId}_");
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertEmail", "Error", response.Message, true, $"{CorrelationId}_");
            }

            return response;
        }

        public Response InsertAddress(INP_CUST_ADDRESS CustAddress, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Address SID";
                    Config.MyLog(
                        "InsertAddress",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long AddressSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_CUST_ADDRESS (SID, ADDRESS_TYPE, ADDRESS_NO, "
                        + "ADDRESS_NAME, ADDRESS_PRIMARY, ADDRESS1, ADDRESS2, ADDRESS3, "
                        + $"POSTAL_CODE, ACTIVE, CUST_SID) "
                        + $"VALUES ({AddressSid}, '{CustAddress.ADDRESS_TYPE}', {CustAddress.ADDRESS_NO}, "
                        + $"'{CustAddress.ADDRESS_NAME}', {CustAddress.ADDRESS_PRIMARY}, '{CustAddress.ADDRESS1}', "
                        + $"'{CustAddress.ADDRESS2}', '{CustAddress.ADDRESS3}', '{CustAddress.POSTAL_CODE}', "
                        + $"{CustAddress.ACTIVE}, {CustAddress.CUST_ID})";

                    Config.MyLog("InsertAddress", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {AddressSid}";
                    response.Result = AddressSid;

                    Config.MyLog(
                        "InsertAddress",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertAddress", "Error", response.Message, true, $"{CorrelationId}_");
            }

            return response;
        }

        public Response InsertOrderHeader(INP_ORDER_HEADER Order, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Order SID";
                    Config.MyLog(
                        "InsertOrderHeader",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long DocSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_ORDER_HEADER (DOC_SID, SBS_SID, SBS_NO, "
                        + "STORE_SID, STORE_NO, WKS_SID, WKS_NO, WKS_NAME, DOC_NO, "
                        + "ORDER_TYPE, USE_VAT, ORDER_QTY, TOTAL_TAX_AMT, TOTAL_AMT, "
                        + "ORDERED_DATETIME, COMMENT2, ORDER_TRACKING_NO, ARCHIVED, "
                        + "IS_HELD, STATUS, STATUS_NEBULA, STATUS_MESSAGE, CUST_BILLING_SID, "
                        + "CUST_SHIPPING_SID, CUST_SID, FO_NO, RO_NO, SHIP_METHOD) "
                        + $"VALUES ({DocSid}, {Order.SBS_SID}, {Order.SBS_NO}, {Order.STORE_SID}, {Order.STORE_NO}, "
                        + $"{Order.WKS_SID}, {Order.WKS_NO}, '{Order.WKS_NAME}', {Order.DOC_NO}, "
                        + $"'{Order.ORDER_TYPE}', {Order.USE_VAT}, {Order.ORDER_QTY}, {Order.TOTAL_TAX_AMT}, {Order.TOTAL_AMT}, "
                        + $"TO_DATE('{Order.ORDERED_DATETIME:dd/MM/yyyy HH:mm:ss}', 'DD/MM/YYYY HH24:MI:SS'), '{Order.COMMENT2}', '{Order.ORDER_TRACKING_NO}', {Order.ARCHIVED}, "
                        + $"{Order.IS_HELD}, {Order.STATUS}, {Order.STATUS_NEBULA}, '{Order.STATUS_MESSAGE}', {Order.CUST_BILLING_SID}, "
                        + $"{Order.CUST_BILLING_SID}, {Order.CUST_SID}, '{Order.FO_NO}', '{Order.RO_NO}', '{Order.SHIP_METHOD}')";

                    Config.MyLog("InsertOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {DocSid}";
                    response.Result = DocSid;

                    Config.MyLog(
                        "InsertOrderHeader",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "InsertOrderHeader",
                    "Error",
                    response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return response;
        }

        public Response InsertOrderDetail(
            INP_ORDER_DETAIL Item,
            string OrderType,
            string CorrelationId
        )
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating DocItem SID";
                    Config.MyLog(
                        "InsertOrderDetail",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long DocItemSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
               "INSERT INTO INP_ORDER_DETAIL (ORDER_DOC_SID, SID, ITEM_POS, " +
               "ITEM_SID, ITEM_UID, ITEM_TYPE, UPC, ALU, TAX_CODE, " +
               "TAX_CODE_SID, TAX_PERC, PRICE, TAX_AMT, COST, " +
               "QTY, CREATED_DATETIME, DETAX_FLAG, KIT_FLAG, " +
               "FULFILL_STORE_NO, STORE_NO, SBS_NO, FULFILL_SBS_NO, " +
               "ITEM_STATUS, STATUS, ACTIVE, ORDER_TYPE, USE_QTY_DECIMALS) " +
               $"VALUES ({Item.ORDER_DOC_SID}, {DocItemSid}, {Item.ITEM_POS}, " +
               $"{Item.ITEM_SID}, {Item.ITEM_UID}, {Item.ITEM_TYPE}, " +
               $"{Item.UPC}, '{Item.ALU}', '{Item.TAX_CODE}', " +
               $"{Item.TAX_CODE_SID}, {Item.TAX_PERC}, {Item.PRICE}, " +
               $"{Item.TAX_AMT}, {Item.COST}, {Item.QTY}, " +
               "SYSDATE, " + // Asignar la fecha actual
               $"{Item.DETAX_FLAG}, {Item.KIT_FLAG}, " +
               $"{Item.FULFILL_STORE_NO}, {Item.STORE_NO}, " +
               $"{Item.SBS_NO}, {Item.FULFILL_SBS_NO}, " +
               $"{Item.ITEM_STATUS}, {Item.STATUS}, 1, " + // Establecer ACTIVE como 1
               $"'{OrderType}', {Item.USE_QTY_DECIMALS})";

                    Config.MyLog("InsertOrderDetail", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {DocItemSid}";
                    response.Result = DocItemSid;

                    Config.MyLog(
                        "InsertOrderDetail",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "InsertOrderDetail",
                    "Error",
                    response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return response;
        }

        public Response InsertTender(INP_TENDER Tender, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating DocTender SID";
                    Config.MyLog(
                        "InsertTender",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long TenderSid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_TENDER (SID, DOC_SID, TENDER_POS, "
                        + "TENDER_TYPE, AMOUNT, DEPOSIT_REF_DOC_SID, BT_CUID, "
                        + "CURRENCY_SID, CURRENCY_NAME, CURRENCY_ALPHA_CODE, "
                        + "SBS_NO, SBS_SID, STORE_NO, STORE_SID, WKS_NO, "
                        + "WKS_SID, WKS_NAME, COMMENT2, USE_VAT, IS_HELD, ARCHIVED) "
                        + $"VALUES ({TenderSid}, {Tender.DOC_SID}, {Tender.TENDER_POS}, "
                        + $"{Tender.TENDER_TYPE}, {Tender.AMOUNT}, {Tender.DEPOSIT_REF_DOC_SID}, "
                        + $"{Tender.BT_CUID}, {Tender.CURRENCY_SID}, '{Tender.CURRENCY_NAME}', "
                        + $"'{Tender.CURRENCY_ALPHA_CODE}', {Tender.SBS_NO}, {Tender.SBS_SID}, "
                        + $"{Tender.STORE_NO}, {Tender.STORE_SID}, {Tender.WKS_NO}, {Tender.WKS_SID}, "
                        + $"'{Tender.WKS_NAME}', '{Tender.COMMENT2}', {Tender.USE_VAT}, {Tender.IS_HELD}, "
                        + $"{Tender.ARCHIVED})";

                    Config.MyLog("InsertTender", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {TenderSid}";
                    response.Result = TenderSid;

                    Config.MyLog(
                        "InsertTender",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertTender", "Error", response.Message, true, $"{CorrelationId}_");
            }

            return response;
        }

        public Response InsertFulfillmentInfo(INP_FULFILLMENT_INFO Info, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");
                if (!RespSid.estado)
                {
                    response.Status = false;
                    response.Message = "An error occurred while generating Fulfillment SID";
                    Config.MyLog(
                        "InsertFulfillmentInfo",
                        "Error",
                        $"{response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );
                }
                long Sid = long.Parse(RespSid.descripcion);
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "INSERT INTO INP_FULFILLMENT_INFO (SID, DELIVERY_METHOD, "
                        + "DELIVERY_SPEED, CARRIER_NAME, ESTIMATED_SHIPPING_DATE, ORDER_DOC_SID) "
                        + $"VALUES ({Sid}, '{Info.DELIVERY_METHOD}', '{Info.DELIVERY_SPEED}', "
                        + $"'{Info.CARRIER_NAME}', TO_DATE('{Info.ESTIMATED_SHIPPING_DATE.ToString("dd/MM/yyyy HH:mm:ss")}', 'DD/MM/YYYY HH24:MI:SS'), "
                        + $"{Info.ORDER_DOC_SID})";

                    Config.MyLog(
                        "InsertFulfillmentInfo",
                        "Query",
                        Query,
                        true,
                        $"{CorrelationId}_"
                    );

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record inserted successfully: {Sid}";
                    response.Result = Sid;

                    Config.MyLog(
                        "InsertFulfillmentInfo",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "InsertFulfillmentInfo",
                    "Error",
                    response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return response;
        }

        public FResponse InsertOrder(
            SalesOrder Body,
            STORE Store,
            long DocNo,
            long CustSid,
            long AddressSid,
            string CorrelationId
        )
        {
            FResponse FResponse = new FResponse();
            long DocSid = -1;
            try
            {
                INP_ORDER_HEADER NewOrder = new INP_ORDER_HEADER
                {
                    SBS_SID = Store.SBS_SID,
                    SBS_NO = Store.SBS_NO,
                    STORE_SID = Store.SID,
                    STORE_NO = Store.STORE_NO,
                    DOC_NO = DocNo,
                    WKS_SID = ApiConfig.Workstation.Sid,
                    WKS_NAME = ApiConfig.Workstation.Name,
                    WKS_NO = ApiConfig.Workstation.No ?? 0,
                    ORDER_TYPE = Body.OrderType,
                    USE_VAT = (short)(ApiConfig.Order.UseVat ? 1 : 0),
                    ORDER_QTY = Body.DocItems.Sum(item => item.Qty),
                    TOTAL_TAX_AMT = 0,
                    TOTAL_AMT = 0,
                    ORDERED_DATETIME = DateTime.Parse(Body.OrderedDate),
                    COMMENT2 = ApiConfig.Order.Comment2,
                    ORDER_TRACKING_NO = Body.OrderTrackingNumber.Trim(),
                    ARCHIVED = (short)(ApiConfig.Order.Archived ? 1 : 0),
                    IS_HELD = (short)(ApiConfig.Order.IsHeld ? 1 : 0),
                    STATUS = 3, // NORMAL
                    STATUS_NEBULA = 1, // PENDING
                    STATUS_MESSAGE = Body.OrderStatus.Trim(),
                    CUST_BILLING_SID = AddressSid,
                    CUST_SHIPPING_SID = AddressSid,
                    CUST_SID = CustSid,
                    FO_NO = Body.FoNumber.Trim(),
                    RO_NO = Body.RoNumber.Trim(),
                    SHIP_METHOD = Body.FulfilmentInfo.DeliveryMethod.Trim()
                };
                Response RespInsertOrder = InsertOrderHeader(NewOrder, CorrelationId);
                if (!RespInsertOrder.Status)
                {
                    FResponse.Response = RespInsertOrder;
                    FResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return FResponse;
                }

                foreach (DocItem Item in Body.DocItems)
                {
                    FResponse RespSearchProduct = SearchProduct(
                        Item.ScanUpc,
                        Store.SBS_NO,
                        CorrelationId
                    );

                    if (!RespSearchProduct.Response.Status)
                        return RespSearchProduct;

                    PRODUCT Product = RespSearchProduct.Response.Result;

                    INP_ORDER_DETAIL NewItem = new INP_ORDER_DETAIL
                    {
                        ORDER_DOC_SID = RespInsertOrder.Result,
                        ITEM_POS = (short)Item.ItemPos,
                        ITEM_SID = Product.SID,
                        ITEM_UID = Product.INVN_ITEM_UID,
                        ITEM_TYPE = (short)Item.ItemType,
                        UPC = long.Parse(Item.ScanUpc),
                        ALU = Product.ALU,
                        TAX_CODE = Product.TAX_CODE,
                        TAX_CODE_SID = Product.TAX_CODE_SID,
                        TAX_PERC = Item.TaxPerc,
                        PRICE = Item.Price,
                        TAX_AMT = Item.TaxAmt,
                        COST = Product.COST,
                        QTY = Item.Qty,
                        DETAX_FLAG = (short)(ApiConfig.Order.Detax ? 1 : 0),
                        KIT_FLAG = (short)(ApiConfig.Order.Kit ? 1 : 0),
                        FULFILL_STORE_NO = Store.STORE_NO,
                        STORE_NO = Store.STORE_NO,
                        SBS_NO = Store.SBS_NO,
                        FULFILL_SBS_NO = Store.SBS_NO,
                        ITEM_STATUS = 0,
                        STATUS = 1, // PENDING
                        USE_QTY_DECIMALS = Product.USE_QTY_DECIMALS
                    };

                    Response RespInsertItem = InsertOrderDetail(
                        NewItem,
                        NewOrder.STATUS_MESSAGE,
                        CorrelationId
                    );

                    DocSid = RespInsertOrder.Result;

                    if (!RespInsertItem.Status)
                    {
                        DeleteOrderHeader(RespInsertOrder.Result, CorrelationId);
                        FResponse.Response = RespInsertItem;
                        FResponse.StatusCode = HttpStatusCode.InternalServerError;

                        return FResponse;
                    }
                }

                FResponse RespCurrency = SearchCurrency(ApiConfig.Order.Currency, CorrelationId);

                if (!RespCurrency.Response.Status)
                {
                    DeleteOrderHeader(RespInsertOrder.Result, CorrelationId);
                    return RespCurrency;
                }

                CURRENCY Currency = RespCurrency.Response.Result;

                foreach (DocTender Tender in Body.DocTenders)
                {
                    INP_TENDER NewTender = new INP_TENDER
                    {
                        DOC_SID = Currency.DOC_SID,
                        TENDER_POS = (short)Tender.TenderPos,
                        TENDER_TYPE = (short)Tender.TenderType,
                        AMOUNT = Tender.Amount,
                        DEPOSIT_REF_DOC_SID = RespInsertOrder.Result,
                        BT_CUID = CustSid,
                        CURRENCY_SID = Currency.SID,
                        CURRENCY_NAME = Currency.CURRENCY_NAME,
                        CURRENCY_ALPHA_CODE = Currency.ALPHABETIC_CODE,
                        SBS_SID = Store.SBS_SID,
                        SBS_NO = Store.SBS_NO,
                        STORE_SID = Store.SID,
                        STORE_NO = Store.STORE_NO,
                        WKS_SID = NewOrder.WKS_SID,
                        WKS_NAME = NewOrder.WKS_NAME,
                        WKS_NO = NewOrder.WKS_NO,
                        USE_VAT = NewOrder.USE_VAT,
                        IS_HELD = 0,
                        ARCHIVED = NewOrder.ARCHIVED,
                    };

                    Response RespInsertTender = InsertTender(NewTender, CorrelationId);

                    if (!RespInsertTender.Status)
                    {
                        FResponse.Response = RespInsertTender;
                        FResponse.StatusCode = HttpStatusCode.InternalServerError;

                        return FResponse;
                    }
                }

                INP_FULFILLMENT_INFO NewFulFillment = new INP_FULFILLMENT_INFO
                {
                    DELIVERY_METHOD = Body.FulfilmentInfo.DeliveryMethod.Trim().ToUpper(),
                    DELIVERY_SPEED = Body.FulfilmentInfo.DeliverySpeed.Trim().ToUpper(),
                    CARRIER_NAME =
                        Body.FulfilmentInfo.Transportactions?[0]?.CarrierName?.Trim() ?? "None",
                    ESTIMATED_SHIPPING_DATE = DateTime.Parse(
                        Body.FulfilmentInfo.EstimatedShippingDate
                    ),
                    ORDER_DOC_SID = RespInsertOrder.Result
                };

                Response RespInsertFulfillment = InsertFulfillmentInfo(
                    NewFulFillment,
                    CorrelationId
                );
                if (!RespInsertFulfillment.Status)
                {
                    FResponse.Response = RespInsertFulfillment;
                    FResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return FResponse;
                }

                FResponse RespUpdateAmount = UpdateOrderHeader(
                    RespInsertOrder.Result,
                    CorrelationId
                );
                if (!RespUpdateAmount.Response.Status)
                    return RespUpdateAmount;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Order saved successfully"
                };

                FResponse.StatusCode = HttpStatusCode.OK;

                Config.MyLog(
                    "InsertOrder",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("InsertOrder", "Error", ErrorMessage, true, $"{CorrelationId}_");

                DeleteOrderHeader(DocSid, CorrelationId);

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
            return FResponse;
        }

        public Response DeleteOrderHeader(long DocSid, string CorrelationId)
        {
            Response response = new Response();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query = $"DELETE FROM INP_ORDER_DETAIL WHERE ORDER_DOC_SID = {DocSid}";

                    Config.MyLog("DeleteOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    Query = $"DELETE FROM INP_ORDER_HEADER WHERE DOC_SID = {DocSid}";

                    Config.MyLog("DeleteOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    Query = $"DELETE FROM INP_FULFILLMENT_INFO WHERE ORDER_DOC_SID = {DocSid}";

                    Config.MyLog("DeleteOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    Query = $"DELETE FROM INP_TENDER WHERE DEPOSIT_REF_DOC_SID = {DocSid}";

                    Config.MyLog("DeleteOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    response.Status = true;
                    response.Message = $"Record deleted successfully: {DocSid}";
                    response.Result = DocSid;

                    Config.MyLog(
                        "DeleteOrderHeader",
                        "Log",
                        response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "DeleteOrderHeader",
                    "Error",
                    response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return response;
        }

        public FResponse SearchCurrency(string AlphabeticCode, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            CURRENCY Currency = new CURRENCY();

            try
            {
                respuesta RespSid = CSid.Generar_SID("RANDOM");

                if (!RespSid.estado)
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "An error occurred while generating DocTender SID";

                    Config.MyLog(
                        "SearchCurrency",
                        "Error",
                        $"{FResponse.Response.Message} - {RespSid.mensaje_error}",
                        true,
                        $"{CorrelationId}_"
                    );

                    return FResponse;
                }

                long DocTenderSid = long.Parse(RespSid.descripcion);

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Currency = context
                        .Database.SqlQuery<CURRENCY>(SelectCurrency(AlphabeticCode, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Currency != null)
                {
                    Currency.DOC_SID = DocTenderSid;

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully.";
                    FResponse.Response.Result = Currency;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No currency found with Alphabetic Code: {AlphabeticCode}";
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchCurrency",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchCurrency", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectCurrency(string AlphabeticCode, string CorrelationId)
        {
            string Sql =
                $"SELECT SID, CURRENCY_NAME, ALPHABETIC_CODE "
                + $"FROM RPS.CURRENCY "
                + $"WHERE ALPHABETIC_CODE = '{AlphabeticCode.ToUpper()}' ";
            // + $"AND ACTIVE = 1";

            Config.MyLog("SelectCurrency", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchCustomer(string CustId, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            long? CustSid = null;

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    CustSid = context
                        .Database.SqlQuery<long>(SelectCustomer(CustId, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (CustSid != null && CustSid > 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully.";
                    FResponse.Response.Result = CustSid;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No customer found with the identifier No.{CustId}";
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchCustomer",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchCustomer", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectCustomer(string CustId, string CorrelationId)
        {
            string Sql =
                "SELECT SID "
                + "FROM RPS.CUSTOMER "
                + $"WHERE UDF5_STRING = '{CustId}' "
                + "UNION "
                + "SELECT CUST_SID as SID "
                + "FROM INP_CUSTOMER "
                + $"WHERE CUST_ID = '{CustId}'";

            Config.MyLog("SelectCustomer", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchAddress(long CustSid, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            long? AddressSid = null;

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    AddressSid = context
                        .Database.SqlQuery<long>(SelectAddress(CustSid, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (AddressSid != null && AddressSid > 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully.";
                    FResponse.Response.Result = AddressSid;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No address found with the customer identifier No.{CustSid}";
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchAddress",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchAddress", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectAddress(long CustSid, string CorrelationId)
        {
            string Sql =
                "SELECT SID "
                + "FROM RPS.CUSTOMER_ADDRESS "
                + $"WHERE CUST_SID = {CustSid} "
                + "UNION "
                + "SELECT SID "
                + "FROM INP_CUST_ADDRESS "
                + $"WHERE CUST_SID = {CustSid}";

            Config.MyLog("SelectAddress", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchProduct(string Upc, short SbsNo, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            PRODUCT Product = new PRODUCT();

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Product = context
                        .Database.SqlQuery<PRODUCT>(SelectProduct(Upc, SbsNo, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Product != null)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully.";
                    FResponse.Response.Result = Product;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No product found with the UPC {Upc} in subsidiary No.{SbsNo}";
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchProduct",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchProduct", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectProduct(string Upc, short SbsNo, string CorrelationId)
        {
            string Sql =
                "SELECT I.SID, I.UPC, I.ALU, I.INVN_ITEM_UID, "
                + "I.TAX_CODE_SID, T.TAX_CODE, I.COST, I.USE_QTY_DECIMALS "
                + "FROM RPS.INVN_SBS_ITEM I, RPS.TAX_CODE T, RPS.SUBSIDIARY SB "
                + $"WHERE I.UPC = {Upc} AND SB.SID = I.SBS_SID AND SB.SBS_NO = {SbsNo} "
                + "AND I.TAX_CODE_SID = T.SID AND I.ACTIVE = 1";

            Config.MyLog("SelectProduct", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchDocNo(short SbsNo, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            long? DocNo;

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    DocNo = context
                        .Database.SqlQuery<long>(SelectDocNo(SbsNo, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Information obtained successfully.";
                FResponse.Response.Result = DocNo != null && DocNo > 0 ? (DocNo + 1) : (1);
                FResponse.StatusCode = HttpStatusCode.OK;

                Config.MyLog(
                    "SearchDocNo",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchDocNo", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectDocNo(short SbsNo, string CorrelationId)
        {
            string Sql =
                "SELECT * FROM ( "
                + "SELECT TO_NUMBER(D.ORDER_DOC_NO) AS ORDER_DOC_NO "
                + "FROM RPS.DOCUMENT D "
                + $"WHERE D.ORDER_DOC_NO IS NOT NULL AND D.SBS_NO = {SbsNo} "
                + "UNION "
                + "SELECT IH.DOC_NO AS ORDER_DOC_NO "
                + "FROM INP_ORDER_HEADER IH "
                + $"WHERE IH.DOC_NO > 0 AND IH.SBS_NO = {SbsNo})A "
                + "ORDER BY A.ORDER_DOC_NO DESC "
                + "FETCH FIRST ROW ONLY";

            Config.MyLog("SelectDocNo", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse CancelSalesOrder(CancelOrder Body, string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                Config.MyLog(
                    "CancelSalesOrder",
                    "Token",
                    Token,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(
                    RespDecrypt.Response.Result
                );

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError(
                        "CancelSalesOrder",
                        "The session has expired. Generate a new token",
                        null,
                        CorrelationId
                    );
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                FResponse RespBody = ValidCancelBody(Body, CorrelationId);
                if (!RespBody.Response.Status)
                    return RespBody;

                FResponse RespUpdate = CancelPayload(Body, CorrelationId);
                if (!RespUpdate.Response.Status)
                    return RespUpdate;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data processed successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Config.MyLog(
                    "CancelSalesOrder",
                    "Error",
                    $"{ex.Message}",
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
                FResponse.Response = HandleError(
                    "CancelSalesOrder",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CancelPayload(CancelOrder Body, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                FResponse RespStore = SearchStore(
                    Body.StoreNo,
                    ApiConfig.Subsidiary.No,
                    CorrelationId
                );
                if (!RespStore.Response.Status)
                    return RespStore;

                STORE Store = RespStore.Response.Result;

                FResponse RespSalesOrder = SearchSalesOrder(
                    Body.OrderTrackingNumber,
                    Body.FoNumber,
                    Body.RoNumber,
                    CorrelationId,
                    false
                );
                if (!RespSalesOrder.Response.Status)
                    return RespSalesOrder;

                Config.MyLog(
                    "CancelPayload",
                    "Sid",
                    JsonConvert.SerializeObject(RespSalesOrder.Response),
                    true,
                    $"{CorrelationId}_"
                );

                INP_ORDER_HEADER Document = RespSalesOrder.Response.Result;

                foreach (CancelItem Item in Body.DocItems)
                {
                    FResponse RespOrderItems = SearchOrderItem(
                        Document.DOC_SID,
                        Item.ScanUpc,
                        CorrelationId
                    );

                    if (!RespOrderItems.Response.Status)
                        return RespOrderItems;

                    OrderItem OrderItem = RespOrderItems.Response.Result;

                    FResponse RespCancel = CancelOrderItem(
                        OrderItem.SID,
                        (OrderItem.QTY - Item.Qty),
                        Body.OrderStatus,
                        CorrelationId
                    );
                    if (!RespCancel.Response.Status)
                        return RespCancel;
                }

                FResponse RespUpdateQty = UpdateOrderHeader(Document.DOC_SID, CorrelationId);

                if (!RespUpdateQty.Response.Status)
                    return RespUpdateQty;

                FResponse RespTotalItems = SearchTotalOrderItems(Document.DOC_SID, CorrelationId);

                long Total = RespTotalItems.Response?.Result ?? -1;

                if (Total == 0)
                {
                    FResponse RespCancelOrder = CancelOrder(
                        Document.DOC_SID,
                        Body.OrderStatus,
                        CorrelationId
                    );
                    if (!RespCancelOrder.Response.Status)
                        return RespCancelOrder;
                }

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data saved successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;

                Config.MyLog(
                    "CancelPayload",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("CancelPayload", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CancelOrder(long DocSid, string OrderType, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "UPDATE INP_ORDER_HEADER "
                        + $"SET ACTIVE = 0, STATUS = 2, STATUS_NEBULA = 13,STATUS_MESSAGE = '{OrderType.Trim().ToUpper()}' "
                        + $"WHERE DOC_SID = {DocSid}";

                    Config.MyLog("CancelOrder", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Record updated successfully: {DocSid}";
                    FResponse.Response.Result = DocSid;

                    Config.MyLog(
                        "CancelOrder",
                        "Log",
                        FResponse.Response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
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

                Config.MyLog(
                    "CancelOrder",
                    "Error",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }

            return FResponse;
        }

        public FResponse CancelOrderItem(
            long ItemSid,
            decimal Qty,
            string OrderType,
            string CorrelationId
        )
        {
            FResponse FResponse = new FResponse();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "UPDATE INP_ORDER_DETAIL "
                        + $"SET {(Qty <= 0 ? $"ORDER_TYPE = '{OrderType.Trim().ToUpper()}'," : "")} ACTIVE = {(Qty <= 0 ? 0 : 1)}, QTY = {Qty} "
                        + $"WHERE SID = {ItemSid}";

                    Config.MyLog("CancelOrderItem", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Record updated successfully: {ItemSid}";
                    FResponse.Response.Result = ItemSid;
                    FResponse.StatusCode = HttpStatusCode.OK;

                    Config.MyLog(
                        "CancelOrderItem",
                        "Log",
                        FResponse.Response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
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

                Config.MyLog(
                    "CancelOrderItem",
                    "Error",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );

                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse SearchOrderItem(long DocSid, string Upc, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            OrderItem Item = new OrderItem();

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Item = context
                        .Database.SqlQuery<OrderItem>(SelectOrderItems(DocSid, Upc, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Item != null && Item.SID > 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Information obtained successfully";
                    FResponse.Response.Result = Item;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"The item with UPC {Upc} was not found for Order Sid - {DocSid}";
                    FResponse.Response.Result = null;
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchOrderItem",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchOrderItem", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectOrderItems(long DocSid, string Upc, string CorrelationId)
        {
            string Sql =
                $"SELECT SID, QTY, TAX_PERC, PRICE, TAX_AMT, ORDER_TYPE FROM INP_ORDER_DETAIL WHERE ORDER_DOC_SID = {DocSid} AND UPC={Upc} AND ACTIVE = 1";

            Config.MyLog("SelectOrderItems", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchFulfillmentInfo(long DocSid, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            INP_FULFILLMENT_INFO Info = new INP_FULFILLMENT_INFO();
            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Info = context
                        .Database.SqlQuery<INP_FULFILLMENT_INFO>(
                            SelectFulfillmentInfo(DocSid, CorrelationId)
                        )
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Info != null && Info.SID > 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Information obtained successfully";
                    FResponse.Response.Result = Info;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"The fulfillment information was not found for Order Sid - {DocSid}";
                    FResponse.Response.Result = null;
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchFulfillmentInfo",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog(
                    "SearchFulfillmentInfo",
                    "Error",
                    ErrorMessage,
                    true,
                    $"{CorrelationId}_"
                );

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectFulfillmentInfo(long DocSid, string CorrelationId)
        {
            string Sql =
                $"SELECT * FROM INP_FULFILLMENT_INFO WHERE ORDER_DOC_SID = {DocSid} AND ACTIVE = 1";

            Config.MyLog("SelectFulfillmentInfo", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse SearchTotalOrderItems(long DocSid, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            long? Total = -1;

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    Total = context
                        .Database.SqlQuery<long>(SelectTotalOrderItems(DocSid, CorrelationId))
                        .FirstOrDefault();

                    context.Database.Connection.Close();
                }

                if (Total != null && Total >= 0)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Information obtained successfully";
                    FResponse.Response.Result = Total;
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message =
                        $"No items were found for the Order Sid - {DocSid}";
                    FResponse.Response.Result = Total;
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                }

                Config.MyLog(
                    "SearchOrderItem",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("SearchOrderItem", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = Total;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public string SelectTotalOrderItems(long DocSid, string CorrelationId)
        {
            string Sql =
                $"SELECT COUNT(*) AS TOTAL FROM INP_ORDER_DETAIL WHERE ORDER_DOC_SID = {DocSid} AND ACTIVE = 1";

            Config.MyLog("SelectTotalOrderItems", "Query", Sql, true, $"{CorrelationId}_");

            return Sql;
        }

        public FResponse ExchangeSalesOrder(ExchangeOrder Body, string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                Config.MyLog(
                    "ExchangeSalesOrder",
                    "Token",
                    Token,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(
                    RespDecrypt.Response.Result
                );

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError(
                        "ExchangeSalesOrder",
                        "The session has expired. Generate a new token",
                        null,
                        CorrelationId
                    );
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                FResponse RespBody = ValidExchangeBody(Body, CorrelationId);
                if (!RespBody.Response.Status)
                    return RespBody;

                FResponse RespUpdate = ExchangePayload(Body, CorrelationId);
                if (!RespUpdate.Response.Status)
                    return RespUpdate;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data processed successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Config.MyLog(
                    "ExchangeSalesOrder",
                    "Error",
                    $"{ex.Message}",
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
                FResponse.Response = HandleError(
                    "ExchangeSalesOrder",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse ValidExchangeBody(ExchangeOrder Body, string CorrelationId)
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
                if (Body?.OrderType < 0 || Body?.OrderType > 6)
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordertype' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.OrigStoreNo?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'origstoreno' is missing");
                }
                if (!DateTime.TryParse(Body.OrderedDate.ToString(), out _))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordereddate' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body.OrderTrackingNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'ordertrackingno' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.OrigOrderTrackingNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'orig_ordertrackingno' is invalid or missing");
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
                if (string.IsNullOrEmpty(Body?.OrigFoNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'origfono' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.OrigRoNumber?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'origrono' is invalid or missing");
                }
                if (string.IsNullOrEmpty(Body?.OrderStatus?.Trim()))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'orderstatus' is invalid or missing");
                }
                if (
                    Body.DocItems == null
                    || !(Body.DocItems is List<ExchangeItem>)
                    || Body.DocItems.Count == 0
                )
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add("The property 'docitem' is invalid or missing");
                }
                if ((bool)!(Body.DocItems?.All(di => di is ExchangeItem)))
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
                if ((bool)Body.DocItems?.Any(di => string.IsNullOrEmpty(di?.ScanUpc?.Trim())))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'scanupc' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => string.IsNullOrEmpty(di?.ExScanUpc?.Trim())))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'exscanupc' invalid or missing."
                    );
                }
                if ((bool)Body.DocItems?.Any(di => di?.ItemType != 4))
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "Data not valid";
                    Errors.Add(
                        "Some items in the list 'docitem' have the property 'itemtype' invalid or missing."
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
                    "ValidExchangeBody",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse ExchangePayload(ExchangeOrder Body, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                FResponse RespStore = SearchStore(
                    Body.StoreNo,
                    ApiConfig.Subsidiary.No,
                    CorrelationId
                );
                if (!RespStore.Response.Status)
                    return RespStore;

                STORE Store = RespStore.Response.Result;

                FResponse RespSalesOrder = SearchSalesOrder(
                    Body.OrigOrderTrackingNumber,
                    Body.OrigFoNumber,
                    Body.OrigRoNumber,
                    CorrelationId,
                    false
                );

                if (!RespSalesOrder.Response.Status)
                    return RespSalesOrder;

                INP_ORDER_HEADER Document = RespSalesOrder.Response.Result;

                FResponse RespDocNo = SearchDocNo(ApiConfig.Subsidiary.No, CorrelationId);
                if (!RespDocNo.Response.Status)
                    return RespDocNo;

                INP_ORDER_HEADER NewOrder = new INP_ORDER_HEADER
                {
                    SBS_SID = Store.SBS_SID,
                    SBS_NO = Store.SBS_NO,
                    STORE_SID = Store.SID,
                    STORE_NO = Store.STORE_NO,
                    DOC_NO = RespDocNo.Response.Result,
                    WKS_SID = ApiConfig.Workstation.Sid,
                    WKS_NAME = ApiConfig.Workstation.Name,
                    WKS_NO = ApiConfig.Workstation.No ?? 0,
                    ORDER_TYPE = Body.OrderType,
                    USE_VAT = (short)(ApiConfig.Order.UseVat ? 1 : 0),
                    ORDER_QTY = Body.DocItems.Sum(item => item.Qty),
                    TOTAL_TAX_AMT = 0,
                    TOTAL_AMT = 0,
                    ORDERED_DATETIME = Body.OrderedDate,
                    COMMENT2 = ApiConfig.Order.Comment2,
                    ORDER_TRACKING_NO = Body.OrderTrackingNumber.Trim(),
                    ARCHIVED = (short)(ApiConfig.Order.Archived ? 1 : 0),
                    IS_HELD = (short)(ApiConfig.Order.IsHeld ? 1 : 0),
                    STATUS = 3, // NORMAL
                    STATUS_NEBULA = 1, // PENDING
                    STATUS_MESSAGE = Body.OrderStatus.Trim(),
                    CUST_BILLING_SID = Document.CUST_BILLING_SID,
                    CUST_SHIPPING_SID = Document.CUST_SHIPPING_SID,
                    CUST_SID = Document.CUST_SID,
                    FO_NO = Body.FoNumber.Trim(),
                    RO_NO = Body.RoNumber.Trim(),
                    SHIP_METHOD = Body.ShipMethod.Trim() 
                };

                Response RespInsertOrder = InsertOrderHeader(NewOrder, CorrelationId);
                if (!RespInsertOrder.Status)
                {
                    FResponse.Response = RespInsertOrder;
                    FResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return FResponse;
                }

                foreach (ExchangeItem Item in Body.DocItems)
                {
                    FResponse RespExProduct = SearchProduct(
                        Item.ExScanUpc,
                        Store.STORE_NO,
                        CorrelationId
                    );

                    if (!RespExProduct.Response.Status)
                        return RespExProduct;

                    FResponse RespOrderItems = SearchOrderItem(
                        Document.DOC_SID,
                        Item.ExScanUpc,
                        CorrelationId
                    );

                    if (!RespOrderItems.Response.Status)
                        return RespOrderItems;

                    OrderItem OrderItem = RespOrderItems.Response.Result;

                    FResponse RespProduct = SearchProduct(
                        Item.ScanUpc,
                        Store.STORE_NO,
                        CorrelationId
                    );

                    if (!RespProduct.Response.Status)
                        return RespProduct;

                    PRODUCT Product = RespProduct.Response.Result;

                    FResponse RespOrderItem = SearchOrderItem(
                        RespInsertOrder.Result,
                        Item.ScanUpc,
                        CorrelationId
                    );

                    if (!RespOrderItem.Response.Status)
                    {
                        INP_ORDER_DETAIL NewItem = new INP_ORDER_DETAIL
                        {
                            ORDER_DOC_SID = RespInsertOrder.Result,
                            ITEM_POS = (short)Item.ItemPos,
                            ITEM_SID = Product.SID,
                            ITEM_UID = Product.INVN_ITEM_UID,
                            ITEM_TYPE = (short)Item.ItemType,
                            UPC = long.Parse(Item.ScanUpc),
                            ALU = Product.ALU,
                            TAX_CODE = Product.TAX_CODE,
                            TAX_CODE_SID = Product.TAX_CODE_SID,
                            TAX_PERC = Item.TaxPerc,
                            PRICE = Item.Price,
                            TAX_AMT = Item.TaxAmt,
                            COST = Product.COST,
                            QTY = Item.Qty,
                            DETAX_FLAG = (short)(ApiConfig.Order.Detax ? 1 : 0),
                            KIT_FLAG = (short)(ApiConfig.Order.Kit ? 1 : 0),
                            FULFILL_STORE_NO = Store.STORE_NO,
                            STORE_NO = Store.STORE_NO,
                            SBS_NO = Store.SBS_NO,
                            FULFILL_SBS_NO = Store.SBS_NO,
                            ITEM_STATUS = 0,
                            STATUS = 1, // PENDING
                            USE_QTY_DECIMALS = Product.USE_QTY_DECIMALS
                        };

                        Response RespInsertItem = InsertOrderDetail(
                            NewItem,
                            OrderItem.ORDER_TYPE,
                            CorrelationId
                        );

                        if (!RespInsertItem.Status)
                        {
                            FResponse.Response = RespInsertItem;
                            FResponse.StatusCode = HttpStatusCode.InternalServerError;
                            return FResponse;
                        }
                    }
                    else
                    {
                        OrderItem FoundItem = RespOrderItem.Response.Result;

                        FResponse RespItemQty = UpdateItemQty(
                            RespInsertOrder.Result,
                            (FoundItem.QTY + Item.Qty),
                            Item.ScanUpc,
                            CorrelationId
                        );

                        if (!RespItemQty.Response.Status)
                            return RespItemQty;
                    }

                    FResponse RespUpdateQty = UpdateItemQty(
                        Document.DOC_SID,
                        (OrderItem.QTY - Item.Qty),
                        Item.ExScanUpc,
                        CorrelationId
                    );

                    if (!RespUpdateQty.Response.Status)
                        return RespUpdateQty;
                }

                FResponse RespFulfillment = SearchFulfillmentInfo(Document.DOC_SID, CorrelationId);

                if (!RespFulfillment.Response.Status)
                    return RespFulfillment;

                INP_FULFILLMENT_INFO PreviousFulfillment = RespFulfillment.Response.Result;

                INP_FULFILLMENT_INFO NewFulFillment = new INP_FULFILLMENT_INFO
                {
                    DELIVERY_METHOD = PreviousFulfillment.DELIVERY_METHOD,
                    DELIVERY_SPEED = PreviousFulfillment.DELIVERY_SPEED,
                    CARRIER_NAME = PreviousFulfillment.CARRIER_NAME,
                    ESTIMATED_SHIPPING_DATE = PreviousFulfillment.ESTIMATED_SHIPPING_DATE,
                    ORDER_DOC_SID = RespInsertOrder.Result
                };

                Response RespInsertFulfillment = InsertFulfillmentInfo(
                    NewFulFillment,
                    CorrelationId
                );
                if (!RespInsertFulfillment.Status)
                {
                    FResponse.Response = RespInsertFulfillment;
                    FResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return FResponse;
                }

                FResponse RespUpdateAmount = UpdateOrderHeader(
                    RespInsertOrder.Result,
                    CorrelationId
                );
                if (!RespUpdateAmount.Response.Status)
                    return RespUpdateAmount;

                FResponse RespUpdateDoc = UpdateOrderHeader(Document.DOC_SID, CorrelationId);
                if (!RespUpdateDoc.Response.Status)
                    return RespUpdateDoc;

                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data saved successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;

                Config.MyLog(
                    "ExchangePayload",
                    "Log",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );
            }
            catch (Exception ex)
            {
                string ErrorMessage =
                    ex.InnerException == null
                        ? ex.Message
                        : ex.InnerException.InnerException == null
                            ? ex.InnerException.Message
                            : ex.InnerException.InnerException.Message;

                Config.MyLog("ExchangePayload", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse ExchangeItem(
            PRODUCT Item,
            long DocSid,
            string OrderType,
            short ItemType,
            long Qty,
            string ExUpc,
            string CorrelationId
        )
        {
            FResponse FResponse = new FResponse();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "UPDATE INP_ORDER_DETAIL "
                        + $"SET ORDER_TYPE = '{OrderType.Trim().ToUpper()}', ITEM_SID = {Item.SID}, "
                        + $"ITEM_UID = {Item.INVN_ITEM_UID}, ITEM_TYPE = {ItemType}, UPC = {Item.UPC}, "
                        + $"ALU = '{Item.ALU}', TAX_CODE = '{Item.TAX_CODE}', TAX_CODE_SID = {Item.TAX_CODE_SID}, "
                        + $"COST = {Item.COST}, QTY = {Qty} "
                        + $"WHERE ORDER_DOC_SID = {DocSid} AND UPC = {ExUpc}";

                    Config.MyLog("ExchangeItem", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Record updated successfully";
                    FResponse.StatusCode = HttpStatusCode.OK;

                    Config.MyLog(
                        "ExchangeItem",
                        "Log",
                        FResponse.Response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
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

                Config.MyLog(
                    "ExchangeItem",
                    "Error",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );

                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse UpdateItemQty(long DocSid, decimal Qty, string Upc, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "UPDATE INP_ORDER_DETAIL "
                        + $"SET QTY = {Qty} "
                        + $"WHERE ORDER_DOC_SID = {DocSid} AND UPC = {Upc}";

                    Config.MyLog("UpdateItemQty", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Record updated successfully";
                    FResponse.StatusCode = HttpStatusCode.OK;

                    Config.MyLog(
                        "UpdateItemQty",
                        "Log",
                        FResponse.Response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
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

                Config.MyLog(
                    "UpdateItemQty",
                    "Error",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );

                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse UpdateOrderHeader(long DocSid, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                using (ModelOracle context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();
                    string Query =
                        "UPDATE INP_ORDER_HEADER "
                        + $"SET (TOTAL_AMT, TOTAL_TAX_AMT, ORDER_QTY) = ("
                        + "SELECT COALESCE(SUM(PRICE * QTY), 0) AS TOTAL_AMT, COALESCE(SUM(TAX_AMT * QTY), 0) AS TOTAL_TAX_AMT, COALESCE(SUM(QTY), 0) AS ORDER_QTY "
                        + "FROM INP_ORDER_DETAIL "
                        + $"WHERE ORDER_DOC_SID = {DocSid} AND ACTIVE = 1) "
                        + $"WHERE DOC_SID = {DocSid}";

                    Config.MyLog("UpdateOrderHeader", "Query", Query, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(Query);

                    context.Database.Connection.Close();

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = $"Record updated successfully";
                    FResponse.StatusCode = HttpStatusCode.OK;

                    Config.MyLog(
                        "UpdateOrderHeader",
                        "Log",
                        FResponse.Response.Message,
                        true,
                        $"{CorrelationId}_"
                    );
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

                Config.MyLog(
                    "UpdateOrderHeader",
                    "Error",
                    FResponse.Response.Message,
                    true,
                    $"{CorrelationId}_"
                );

                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse GetInfoUsers(string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            try
            {
                Config.MyLog(
                    "ProcessSalesOrder",
                    "Token",
                    Token,
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(
                    RespDecrypt.Response.Result
                );

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError(
                        "ProcessSalesOrder",
                        "The session has expired. Generate a new token",
                        null,
                        CorrelationId
                    );
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }



                FResponse.Response = new Response
                {
                    Status = true,
                    Message = "Data processed successfully"
                };
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Config.MyLog(
                    "ProcessSalesOrder",
                    "Error",
                    $"{ex.Message}",
                    AConfig.EnableLog,
                    $"{CorrelationId}_"
                );
                FResponse.Response = HandleError(
                    "ProcessSalesOrder",
                    "An unexpected error occurred.",
                    null,
                    CorrelationId,
                    ex
                );
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse GetStoresAndEmployees(string Token, string CorrelationId)
        {
            FResponse FResponse = new FResponse();
            List<Store> stores = new List<Store>();
            List<Employee> employees = new List<Employee>();

            try
            {
                Config.MyLog("GetStoresAndEmployees", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("GetStoresAndEmployees", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("GetStoresAndEmployees", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("GetStoresAndEmployees", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Consultar todas las tiendas
                    using (var command = context.Database.Connection.CreateCommand())
                    {
                        command.CommandText = SelectAllStores();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var store = new Store
                                {
                                    SidStore = reader.IsDBNull(0) ? null : reader.GetString(0),
                                    StoreNo = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    StoreName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    SbsSid = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    SbsNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    SbsName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    FullName = reader.IsDBNull(6) ? null : reader.GetString(6)
                                };
                                stores.Add(store);
                            }
                        }
                    }

                    Config.MyLog("GetStoresAndEmployees", "Stores Query", $"Stores found: {stores.Count}", AConfig.EnableLog, $"{CorrelationId}_");

                    // Consultar todos los empleados
                    using (var command = context.Database.Connection.CreateCommand())
                    {
                        command.CommandText = SelectAllEmployees();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var employee = new Employee
                                {
                                    CustSid = reader.IsDBNull(0) ? null : reader.GetString(0),
                                    EmplName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    SidEmployee = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    SidCustomer = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    FullName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    StoreSid = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    SbsSid = reader.IsDBNull(6) ? null : reader.GetString(6)
                                };
                                employees.Add(employee);
                            }
                        }
                    }

                    Config.MyLog("GetStoresAndEmployees", "Employees Query", $"Employees found: {employees.Count}", AConfig.EnableLog, $"{CorrelationId}_");

                    context.Database.Connection.Close();
                }

                if (stores != null && employees != null)
                {
                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Information obtained successfully";
                    FResponse.Response.Result = new
                    {
                        Stores = stores,
                        Employees = employees,
                        CicloCoordenada = new Dictionary<int, string>
                {
                    { 0, "Manual" },
                    { 1, "Semanal" },
                    { 2, "Mensual" },
                    { 3, "Anual" }
                }
                    };
                    FResponse.StatusCode = HttpStatusCode.OK;
                    Config.MyLog("GetStoresAndEmployees", "Success", "Information obtained successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }
                else
                {
                    FResponse.Response.Status = false;
                    FResponse.Response.Message = "No stores or employees found";
                    FResponse.Response.Result = null;
                    FResponse.StatusCode = HttpStatusCode.NotFound;
                    Config.MyLog("GetStoresAndEmployees", "Failure", "No stores or employees found", AConfig.EnableLog, $"{CorrelationId}_");
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("GetStoresAndEmployees", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse GetUsers(string Token, string CorrelationId, string search = null)
        {
            FResponse FResponse = new FResponse();
            List<User> users = new List<User>();

            try
            {
                Config.MyLog("GetUsers", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("GetUsers", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("GetUsers", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("GetUsers", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();
                    string query = "SELECT ID, NOMBRE, EMAIL FROM USUARIO_CONTROL_DESCUENTO WHERE ISADMIN = 0";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND (NOMBRE LIKE :search OR EMAIL LIKE :search)";
                    }

                    using (var command = new OracleCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(search))
                        {
                            command.Parameters.Add(new OracleParameter("search", $"%{search}%"));
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Email = reader.GetString(2)
                                });
                            }
                        }
                    }
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Information obtained successfully";
                FResponse.Response.Result = users;
                FResponse.StatusCode = HttpStatusCode.OK;
                Config.MyLog("GetUsers", "Success", "Information obtained successfully", AConfig.EnableLog, $"{CorrelationId}_");

            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("GetUsers", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse UpdateUser(string Token, string CorrelationId, UserUpdateRequest request)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("UpdateUser", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("UpdateUser", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("UpdateUser", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("UpdateUser", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                string hashedPassword = Config.Encriptar_conKey(request.Password, "MD5", "ControlDescuentosEJJE");

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();
                    string query = "UPDATE USUARIO_CONTROL_DESCUENTO SET NOMBRE = :Name, EMAIL = :Email, PASSWORD = :Password WHERE ID = :Id";
                    using (var command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("Name", request.Name));
                        command.Parameters.Add(new OracleParameter("Email", request.Email));
                        command.Parameters.Add(new OracleParameter("Password", hashedPassword));
                        command.Parameters.Add(new OracleParameter("Id", request.Id));

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            FResponse.Response.Status = true;
                            FResponse.Response.Message = "User updated successfully";
                            FResponse.Response.Result = rowsAffected;
                            FResponse.StatusCode = HttpStatusCode.OK;
                            Config.MyLog("UpdateUser", "Success", "User updated successfully", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                        else
                        {
                            FResponse.Response.Status = false;
                            FResponse.Response.Message = "User not found";
                            FResponse.Response.Result = null;
                            FResponse.StatusCode = HttpStatusCode.NotFound;
                            Config.MyLog("UpdateUser", "Failure", "User not found", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("UpdateUser", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CreateUser(string Token, string CorrelationId, UserCreateRequest request)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("CreateUser", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("CreateUser", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("CreateUser", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("CreateUser", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();

                    // Verificar si ya existe un usuario con el mismo correo electrónico
                    string checkQuery = "SELECT COUNT(*) FROM USUARIO_CONTROL_DESCUENTO WHERE EMAIL = :Email";
                    using (var checkCommand = new OracleCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OracleParameter("Email", request.Email));
                        int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (userCount > 0)
                        {
                            FResponse.Response.Status = false;
                            FResponse.Response.Message = "A user with this email already exists";
                            FResponse.Response.Result = null;
                            FResponse.StatusCode = HttpStatusCode.Conflict;
                            Config.MyLog("CreateUser", "Failure", "A user with this email already exists", AConfig.EnableLog, $"{CorrelationId}_");
                            return FResponse;
                        }
                    }

                    string hashedPassword = Config.Encriptar_conKey(request.Password, "MD5", "ControlDescuentosEJJE");
                    string query = "INSERT INTO USUARIO_CONTROL_DESCUENTO (NOMBRE, EMAIL, PASSWORD, ISADMIN, FECHA_CREACION) VALUES (:Name, :Email, :Password, 0, CURRENT_TIMESTAMP)";
                    using (var command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("Name", request.Name));
                        command.Parameters.Add(new OracleParameter("Email", request.Email));
                        command.Parameters.Add(new OracleParameter("Password", hashedPassword));

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            FResponse.Response.Status = true;
                            FResponse.Response.Message = "User created successfully";
                            FResponse.Response.Result = rowsAffected;
                            FResponse.StatusCode = HttpStatusCode.Created;
                            Config.MyLog("CreateUser", "Success", "User created successfully", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                        else
                        {
                            FResponse.Response.Status = false;
                            FResponse.Response.Message = "User creation failed";
                            FResponse.Response.Result = null;
                            FResponse.StatusCode = HttpStatusCode.BadRequest;
                            Config.MyLog("CreateUser", "Failure", "User creation failed", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("CreateUser", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse DeleteUser(string Token, string CorrelationId, int userId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("DeleteUser", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("DeleteUser", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("DeleteUser", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("DeleteUser", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();
                    string query = "DELETE FROM USUARIO_CONTROL_DESCUENTO WHERE ID = :Id";
                    using (var command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("Id", userId));

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            FResponse.Response.Status = true;
                            FResponse.Response.Message = "User deleted successfully";
                            FResponse.Response.Result = rowsAffected;
                            FResponse.StatusCode = HttpStatusCode.OK;
                            Config.MyLog("DeleteUser", "Success", "User deleted successfully", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                        else
                        {
                            FResponse.Response.Status = false;
                            FResponse.Response.Message = "User not found";
                            FResponse.Response.Result = null;
                            FResponse.StatusCode = HttpStatusCode.NotFound;
                            Config.MyLog("DeleteUser", "Failure", "User not found", AConfig.EnableLog, $"{CorrelationId}_");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("DeleteUser", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
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


        public string SelectAllStores()
        {
            string Sql = @"
    SELECT 
        TO_CHAR(s.SID) AS SidStore,
        TO_CHAR(s.STORE_NO) AS StoreNo,
        TO_CHAR(s.STORE_NAME) AS StoreName,
        TO_CHAR(s.SBS_SID) AS SbsSid,
        TO_CHAR(sub.SBS_NO) AS SbsNo,
        TO_CHAR(sub.SBS_NAME) AS SbsName,
        TO_CHAR(s.STORE_NAME || ' - ' || sub.SBS_NAME) AS FullName
    FROM 
        RPS.STORE s
    LEFT JOIN 
        RPS.SUBSIDIARY sub
    ON 
        s.SBS_SID = sub.SID
    WHERE
        s.ACTIVE = 1";
            Config.MyLog("SelectAllStores", "Query", Sql, true, "");
            return Sql;
        }
        public string SelectAllEmployees()
        {
            string Sql = @"
    SELECT 
        TO_CHAR(e.CUST_SID) AS CustSid,
        TO_CHAR(e.EMPL_NAME) AS EmplName,
        TO_CHAR(e.SID) AS SidEmployee,
        TO_CHAR(c.SID) AS SidCustomer,
        TO_CHAR(c.FIRST_NAME || ' ' || c.LAST_NAME) AS FullName,
        TO_CHAR(c.STORE_SID) AS StoreSid,
        TO_CHAR(e.SBS_SID) AS SbsSid
    FROM 
        RPS.employee e
    LEFT JOIN 
        RPS.customer c
    ON 
        e.CUST_SID = c.SID
    WHERE
        e.USER_ACTIVE = 1";
            Config.MyLog("SelectAllEmployees", "Query", Sql, true, "");
            return Sql;
        }

        public FResponse SaveUsuarioCoordenada(UsuarioCoordenada usuarioCoordenada, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si ya existe un registro con el mismo SID y tienda
                    string checkQuery = @"
                SELECT COUNT(1) 
                FROM Usuario_Coordenada 
                WHERE sid = @Sid AND tienda = @Tienda";

                    Config.MyLog("SaveUsuarioCoordenada", "Check Query", checkQuery, true, $"{CorrelationId}_");

                    int existingRecordCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new SqlParameter("@Sid", usuarioCoordenada.Sid),
                        new SqlParameter("@Tienda", usuarioCoordenada.Tienda)
                    ).FirstOrDefault();

                    if (existingRecordCount > 0)
                    {
                        // Si el registro ya existe, lo actualizamos
                        string updateQuery = @"
                    UPDATE Usuario_Coordenada 
                    SET Email = @Email, 
                        max_descuento = @MaxDescuento, 
                        Ciclo_coordenada = @CicloCoordenada
                    WHERE sid = @Sid AND tienda = @Tienda";

                        Config.MyLog("SaveUsuarioCoordenada", "Update Query", updateQuery, true, $"{CorrelationId}_");

                        context.Database.ExecuteSqlCommand(
                            updateQuery,
                            new SqlParameter("@Email", usuarioCoordenada.Email),
                            new SqlParameter("@MaxDescuento", usuarioCoordenada.MaxDescuento),
                            new SqlParameter("@CicloCoordenada", usuarioCoordenada.CicloCoordenada),
                            new SqlParameter("@Sid", usuarioCoordenada.Sid),
                            new SqlParameter("@Tienda", usuarioCoordenada.Tienda)
                        );

                        Config.MyLog("SaveUsuarioCoordenada", "Success", "Record updated successfully", true, $"{CorrelationId}_");
                    }
                    else
                    {
                        // Si el registro no existe, lo insertamos
                        string insertQuery = @"
                    INSERT INTO Usuario_Coordenada (Email, sid, tienda, max_descuento, Ciclo_coordenada)
                    VALUES (@Email, @Sid, @Tienda, @MaxDescuento, @CicloCoordenada)";

                        Config.MyLog("SaveUsuarioCoordenada", "Insert Query", insertQuery, true, $"{CorrelationId}_");

                        context.Database.ExecuteSqlCommand(
                            insertQuery,
                            new SqlParameter("@Email", usuarioCoordenada.Email),
                            new SqlParameter("@Sid", usuarioCoordenada.Sid),
                            new SqlParameter("@Tienda", usuarioCoordenada.Tienda),
                            new SqlParameter("@MaxDescuento", usuarioCoordenada.MaxDescuento),
                            new SqlParameter("@CicloCoordenada", usuarioCoordenada.CicloCoordenada)
                        );

                        Config.MyLog("SaveUsuarioCoordenada", "Success", "Record inserted successfully", true, $"{CorrelationId}_");
                    }

                    context.Database.Connection.Close();
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Data saved or updated successfully";
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("SaveUsuarioCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CrearUsuario(string Token, UsuarioControlDescuento usuario, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("CrearUsuario", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("CrearUsuario", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("CrearUsuario", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("CrearUsuario", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el correo ya existe en la base de datos
                    string checkQuery = "SELECT COUNT(1) FROM Usuario WHERE email = @Email";
                    Config.MyLog("CrearUsuario", "Check Query", checkQuery, true, $"{CorrelationId}_");

                    int existingEmailCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new SqlParameter("@Email", usuario.Email)
                    ).FirstOrDefault();

                    if (existingEmailCount > 0)
                    {
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El correo electrónico ya está registrado";
                        FResponse.StatusCode = HttpStatusCode.BadRequest;
                        Config.MyLog("CrearUsuario", "Email Exists", "Email already registered", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Asignamos la fecha y hora actual en UTC con zona horaria
                    DateTimeOffset fechaCreacion = DateTimeOffset.UtcNow; // Zona horaria UTC

                    // Insertar el nuevo usuario
                    string insertQuery = @"
                INSERT INTO Usuario_Control_Descuento (nombre, email, fecha_creacion, password)
                VALUES (@Nombre, @Email, @FechaCreacion, @Password)";

                    Config.MyLog("CrearUsuario", "Insert Query", insertQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        insertQuery,
                        new SqlParameter("@Nombre", usuario.Nombre),
                        new SqlParameter("@Email", usuario.Email),
                        new SqlParameter("@FechaCreacion", OracleDbType.TimeStampTZ) { Value = fechaCreacion },  // Se asigna la fecha con zona horaria
                        new SqlParameter("@Password", usuario.Password)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("CrearUsuario", "Success", "Usuario creado exitosamente", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Usuario creado exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("CrearUsuario", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }


        public FResponse ActualizarUsuario(string Token, UsuarioControlDescuento usuario, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("ActualizarUsuario", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("ActualizarUsuario", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el correo ya está en uso por otro usuario
                    string checkQuery = @"
                SELECT COUNT(1) 
                FROM Usuario 
                WHERE email = @Email AND id != @Id";

                    Config.MyLog("ActualizarUsuario", "Check Email Query", checkQuery, true, $"{CorrelationId}_");

                    int existingEmailCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new SqlParameter("@Email", usuario.Email),
                        new SqlParameter("@Id", usuario.Id)
                    ).FirstOrDefault();

                    if (existingEmailCount > 0)
                    {
                        // Si el correo ya está en uso, devolver un error
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El correo electrónico ya está registrado por otro usuario";
                        FResponse.StatusCode = HttpStatusCode.BadRequest;
                        Config.MyLog("ActualizarUsuario", "Error", "Email is already registered by another user", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Actualizar el usuario
                    string updateQuery = @"
                UPDATE Usuario
                SET nombre = @Nombre, 
                    email = @Email, 
                    password = @Password
                WHERE id = @Id";

                    Config.MyLog("ActualizarUsuario", "Update Query", updateQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        updateQuery,
                        new SqlParameter("@Nombre", usuario.Nombre),
                        new SqlParameter("@Email", usuario.Email),
                        new SqlParameter("@Password", usuario.Password),
                        new SqlParameter("@Id", usuario.Id)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("ActualizarUsuario", "Success", "User updated successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Usuario actualizado exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("ActualizarUsuario", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse EliminarUsuario(string Token, int usuarioId, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("EliminarUsuario", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("EliminarUsuario", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el usuario existe
                    string checkQuery = "SELECT COUNT(1) FROM Usuario WHERE id = @Id";
                    Config.MyLog("EliminarUsuario", "Check User Query", checkQuery, true, $"{CorrelationId}_");

                    int existingUserCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new SqlParameter("@Id", usuarioId)
                    ).FirstOrDefault();

                    if (existingUserCount == 0)
                    {
                        // Si el usuario no existe, devolver un error
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El usuario no existe";
                        FResponse.StatusCode = HttpStatusCode.NotFound;
                        Config.MyLog("EliminarUsuario", "Error", "User does not exist", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Eliminar el usuario
                    string deleteQuery = "DELETE FROM Usuario WHERE id = @Id";
                    Config.MyLog("EliminarUsuario", "Delete Query", deleteQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        deleteQuery,
                        new SqlParameter("@Id", usuarioId)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("EliminarUsuario", "Success", "User deleted successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Usuario eliminado exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("EliminarUsuario", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
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

        public FResponse GetUsersFromCoordenada(string Token, string CorrelationId, string search = null)
        {
            FResponse FResponse = new FResponse();
            List<UserCoordenada> users = new List<UserCoordenada>();

            try
            {
                Config.MyLog("GetUsersFromCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("GetUsersFromCoordenada", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("GetUsersFromCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("GetUsersFromCoordenada", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var connection = new OracleConnection(Connection))
                {
                    connection.Open();
                    string query = "SELECT ID, EMAIL, TO_CHAR(SID), TIENDA, MAX_DESCUENTO, CICLO_COORDENADA FROM USUARIO_COORDENADA";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " WHERE EMAIL LIKE :search OR TIENDA LIKE :search";
                    }

                    using (var command = new OracleCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(search))
                        {
                            command.Parameters.Add(new OracleParameter("search", $"%{search}%"));
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new UserCoordenada
                                {
                                    Id = reader.GetInt32(0),
                                    Email = reader.GetString(1),
                                    Sid = reader.GetString(2), // Cambiado a GetString para TO_CHAR
                                    Tienda = reader.GetString(3),
                                    MaxDescuento = reader.GetInt32(4),
                                    CicloCoordenada = reader.GetInt32(5)
                                });
                            }
                        }
                    }
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Information obtained successfully";
                FResponse.Response.Result = users;
                FResponse.StatusCode = HttpStatusCode.OK;
                Config.MyLog("GetUsersFromCoordenada", "Success", "Information obtained successfully", AConfig.EnableLog, $"{CorrelationId}_");

            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("GetUsersFromCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.Response.Result = null;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CrearUsuarioCoordenada(string Token, UsuarioCoordenada usuario, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("CrearUsuarioCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                {
                    Config.MyLog("CrearUsuarioCoordenada", "Token Decrypt Failure", RespDecrypt.Response.Message, AConfig.EnableLog, $"{CorrelationId}_");
                    return RespDecrypt;
                }

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("CrearUsuarioCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    Config.MyLog("CrearUsuarioCoordenada", "Session Expired", "Session has expired. Generate a new token", AConfig.EnableLog, $"{CorrelationId}_");
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el SID y la Tienda ya existen en la base de datos
                    string checkQuery = "SELECT COUNT(1) FROM USUARIO_COORDENADA WHERE SID = :Sid AND TIENDA = :Tienda";
                    Config.MyLog("CrearUsuarioCoordenada", "Check Query", checkQuery, true, $"{CorrelationId}_");

                    int existingUserCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new OracleParameter("Sid", usuario.Sid),
                        new OracleParameter("Tienda", usuario.Tienda)
                    ).FirstOrDefault();

                    if (existingUserCount > 0)
                    {
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El usuario con el SID y la Tienda especificados ya está registrado";
                        FResponse.StatusCode = HttpStatusCode.BadRequest;
                        Config.MyLog("CrearUsuarioCoordenada", "User Exists", "User with specified SID and Tienda already registered", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Insertar el nuevo usuario y obtener el ID generado
                    string insertQuery = @"
        INSERT INTO USUARIO_COORDENADA (EMAIL, SID, TIENDA, MAX_DESCUENTO, CICLO_COORDENADA, SUBSIDIARIA)
        VALUES (:Email, :Sid, :Tienda, :MaxDescuento, :CicloCoordenada, :Subsidiaria)
        RETURNING ID INTO :Id";

                    Config.MyLog("CrearUsuarioCoordenada", "Insert Query", insertQuery, true, $"{CorrelationId}_");

                    var idParameter = new OracleParameter("Id", OracleDbType.Int32, ParameterDirection.Output);

                    context.Database.ExecuteSqlCommand(
                        insertQuery,
                        new OracleParameter("Email", usuario.Email),
                        new OracleParameter("Sid", usuario.Sid),
                        new OracleParameter("Tienda", usuario.Tienda),
                        new OracleParameter("MaxDescuento", usuario.MaxDescuento),
                        new OracleParameter("CicloCoordenada", usuario.CicloCoordenada),
                        new OracleParameter("Subsidiaria", usuario.Subsidiaria),
                        idParameter
                    );

                    int newUserId = ((OracleDecimal)idParameter.Value).ToInt32();

                    context.Database.Connection.Close();
                    Config.MyLog("CrearUsuarioCoordenada", "Success", "Usuario creado exitosamente", AConfig.EnableLog, $"{CorrelationId}_");

                    FResponse.Response.Status = true;
                    FResponse.Response.Message = "Usuario creado exitosamente";
                    FResponse.Response.Result = new { mensaje = "Usuario creado exitosamente", idUserCoordenada = newUserId };
                    FResponse.StatusCode = HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.InnerException == null ? ex.InnerException.Message : ex.InnerException.InnerException.Message;

                Config.MyLog("CrearUsuarioCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }
        public FResponse ActualizarUsuarioCoordenada(string Token, UsuarioCoordenada usuario, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("ActualizarUsuarioCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("ActualizarUsuarioCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Actualizar el usuario
                    string updateQuery = @"
        UPDATE USUARIO_COORDENADA
        SET EMAIL = :Email, 
            MAX_DESCUENTO = :MaxDescuento, 
            CICLO_COORDENADA = :CicloCoordenada,
            TIENDA = :Tienda,
            SUBSIDIARIA = :Subsidiaria
        WHERE ID = :Id";

                    Config.MyLog("ActualizarUsuarioCoordenada", "Update Query", updateQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        updateQuery,
                        new OracleParameter("Email", usuario.Email),
                        new OracleParameter("MaxDescuento", usuario.MaxDescuento),
                        new OracleParameter("CicloCoordenada", usuario.CicloCoordenada),
                        new OracleParameter("Tienda", usuario.Tienda),
                        new OracleParameter("Subsidiaria", usuario.Subsidiaria),
                        new OracleParameter("Id", usuario.Id)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("ActualizarUsuarioCoordenada", "Success", "User updated successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Usuario actualizado exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("ActualizarUsuarioCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse EliminarUsuarioCoordenada(string Token, int Id, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("EliminarUsuarioCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("EliminarUsuarioCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el usuario existe
                    string checkQuery = "SELECT COUNT(1) FROM USUARIO_COORDENADA WHERE ID = :Id";
                    Config.MyLog("EliminarUsuarioCoordenada", "Check User Query", checkQuery, true, $"{CorrelationId}_");

                    int existingUserCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new OracleParameter("Id", Id)
 
                    ).FirstOrDefault();

                    if (existingUserCount == 0)
                    {
                        // Si el usuario no existe, devolver un error
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El usuario no existe";
                        FResponse.StatusCode = HttpStatusCode.NotFound;
                        Config.MyLog("EliminarUsuarioCoordenada", "Error", "User does not exist", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Eliminar el usuario
                    string deleteQuery = "DELETE FROM USUARIO_COORDENADA WHERE ID = :Id";
                    Config.MyLog("EliminarUsuarioCoordenada", "Delete Query", deleteQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        deleteQuery,
                         new OracleParameter("Id", Id)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("EliminarUsuarioCoordenada", "Success", "User deleted successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Usuario eliminado exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("EliminarUsuarioCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse CrearCoordenada(string Token, Coordenada coordenada, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("CrearCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("CrearCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si el usuario ya existe
                    string checkQuery = @"
            SELECT COUNT(1) 
            FROM COORDENADAS 
            WHERE USUARIO_COORDENADA_ID = :UsuarioCoordenadaId";

                    Config.MyLog("CrearCoordenada", "Check Usuario Query", checkQuery, true, $"{CorrelationId}_");

                    int existingUserCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new OracleParameter("UsuarioCoordenadaId", coordenada.UsuarioCoordenadaId)
                    ).FirstOrDefault();

                    if (existingUserCount > 0)
                    {
                        // Si el usuario ya existe, devolver un error
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "El usuario ya existe";
                        FResponse.StatusCode = HttpStatusCode.BadRequest;
                        Config.MyLog("CrearCoordenada", "Error", "Usuario already exists", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Crear la coordenada
                    string insertQuery = @"
            INSERT INTO COORDENADAS (A1, B1, C1, D1, E1, F1, G1, H1, I1, J1, A2, B2, C2, D2, E2, F2, G2, H2, I2, J2, A3, B3, C3, D3, E3, F3, G3, H3, I3, J3, A4, B4, C4, D4, E4, F4, G4, H4, I4, J4, A5, B5, C5, D5, E5, F5, G5, H5, I5, J5, USUARIO_COORDENADA_ID)
            VALUES (:A1, :B1, :C1, :D1, :E1, :F1, :G1, :H1, :I1, :J1, :A2, :B2, :C2, :D2, :E2, :F2, :G2, :H2, :I2, :J2, :A3, :B3, :C3, :D3, :E3, :F3, :G3, :H3, :I3, :J3, :A4, :B4, :C4, :D4, :E4, :F4, :G4, :H4, :I4, :J4, :A5, :B5, :C5, :D5, :E5, :F5, :G5, :H5, :I5, :J5, :UsuarioCoordenadaId)";

                    Config.MyLog("CrearCoordenada", "Insert Query", insertQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        insertQuery,
                        new OracleParameter("A1", coordenada.A1),
                        new OracleParameter("B1", coordenada.B1),
                        new OracleParameter("C1", coordenada.C1),
                        new OracleParameter("D1", coordenada.D1),
                        new OracleParameter("E1", coordenada.E1),
                        new OracleParameter("F1", coordenada.F1),
                        new OracleParameter("G1", coordenada.G1),
                        new OracleParameter("H1", coordenada.H1),
                        new OracleParameter("I1", coordenada.I1),
                        new OracleParameter("J1", coordenada.J1),
                        new OracleParameter("A2", coordenada.A2),
                        new OracleParameter("B2", coordenada.B2),
                        new OracleParameter("C2", coordenada.C2),
                        new OracleParameter("D2", coordenada.D2),
                        new OracleParameter("E2", coordenada.E2),
                        new OracleParameter("F2", coordenada.F2),
                        new OracleParameter("G2", coordenada.G2),
                        new OracleParameter("H2", coordenada.H2),
                        new OracleParameter("I2", coordenada.I2),
                        new OracleParameter("J2", coordenada.J2),
                        new OracleParameter("A3", coordenada.A3),
                        new OracleParameter("B3", coordenada.B3),
                        new OracleParameter("C3", coordenada.C3),
                        new OracleParameter("D3", coordenada.D3),
                        new OracleParameter("E3", coordenada.E3),
                        new OracleParameter("F3", coordenada.F3),
                        new OracleParameter("G3", coordenada.G3),
                        new OracleParameter("H3", coordenada.H3),
                        new OracleParameter("I3", coordenada.I3),
                        new OracleParameter("J3", coordenada.J3),
                        new OracleParameter("A4", coordenada.A4),
                        new OracleParameter("B4", coordenada.B4),
                        new OracleParameter("C4", coordenada.C4),
                        new OracleParameter("D4", coordenada.D4),
                        new OracleParameter("E4", coordenada.E4),
                        new OracleParameter("F4", coordenada.F4),
                        new OracleParameter("G4", coordenada.G4),
                        new OracleParameter("H4", coordenada.H4),
                        new OracleParameter("I4", coordenada.I4),
                        new OracleParameter("J4", coordenada.J4),
                        new OracleParameter("A5", coordenada.A5),
                        new OracleParameter("B5", coordenada.B5),
                        new OracleParameter("C5", coordenada.C5),
                        new OracleParameter("D5", coordenada.D5),
                        new OracleParameter("E5", coordenada.E5),
                        new OracleParameter("F5", coordenada.F5),
                        new OracleParameter("G5", coordenada.G5),
                        new OracleParameter("H5", coordenada.H5),
                        new OracleParameter("I5", coordenada.I5),
                        new OracleParameter("J5", coordenada.J5),
                        new OracleParameter("UsuarioCoordenadaId", coordenada.UsuarioCoordenadaId)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("CrearCoordenada", "Success", "Coordenada created successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Coordenada creada exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("CrearCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse ActualizarCoordenada(string Token, Coordenada coordenada, string CorrelationId)
        {
            FResponse FResponse = new FResponse();

            try
            {
                Config.MyLog("ActualizarCoordenada", "Token", Token, AConfig.EnableLog, $"{CorrelationId}_");

                FResponse RespDecrypt = DecryptToken(Token, CorrelationId);
                if (!RespDecrypt.Response.Status)
                    return RespDecrypt;

                Session CSession = JsonConvert.DeserializeObject<Session>(RespDecrypt.Response.Result);

                if (DateTime.Parse(CSession.FinalDate) < DateTime.Now)
                {
                    FResponse.Response = HandleError("ActualizarCoordenada", "The session has expired. Generate a new token", null, CorrelationId);
                    FResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return FResponse;
                }

                using (var context = new ModelOracle(Connection, Schema))
                {
                    context.Database.Connection.Open();

                    // Verificar si la coordenada existe
                    string checkQuery = @"
            SELECT COUNT(1) 
            FROM COORDENADAS 
            WHERE ID = :Id";

                    Config.MyLog("ActualizarCoordenada", "Check Coordenada Query", checkQuery, true, $"{CorrelationId}_");

                    int existingCoordenadaCount = context.Database.SqlQuery<int>(
                        checkQuery,
                        new OracleParameter("Id", coordenada.Id)
                    ).FirstOrDefault();

                    if (existingCoordenadaCount == 0)
                    {
                        // Si la coordenada no existe, devolver un error
                        FResponse.Response.Status = false;
                        FResponse.Response.Message = "La coordenada no existe";
                        FResponse.StatusCode = HttpStatusCode.BadRequest;
                        Config.MyLog("ActualizarCoordenada", "Error", "Coordenada does not exist", AConfig.EnableLog, $"{CorrelationId}_");
                        return FResponse;
                    }

                    // Actualizar la coordenada
                    string updateQuery = @"
            UPDATE COORDENADAS 
            SET A1 = :A1, B1 = :B1, C1 = :C1, D1 = :D1, E1 = :E1, F1 = :F1, G1 = :G1, H1 = :H1, I1 = :I1, J1 = :J1, 
                A2 = :A2, B2 = :B2, C2 = :C2, D2 = :D2, E2 = :E2, F2 = :F2, G2 = :G2, H2 = :H2, I2 = :I2, J2 = :J2, 
                A3 = :A3, B3 = :B3, C3 = :C3, D3 = :D3, E3 = :E3, F3 = :F3, G3 = :G3, H3 = :H3, I3 = :I3, J3 = :J3, 
                A4 = :A4, B4 = :B4, C4 = :C4, D4 = :D4, E4 = :E4, F4 = :F4, G4 = :G4, H4 = :H4, I4 = :I4, J4 = :J4, 
                A5 = :A5, B5 = :B5, C5 = :C5, D5 = :D5, E5 = :E5, F5 = :F5, G5 = :G5, H5 = :H5, I5 = :I5, J5 = :J5, 
                USUARIO_COORDENADA_ID = :UsuarioCoordenadaId
            WHERE ID = :Id";

                    Config.MyLog("ActualizarCoordenada", "Update Query", updateQuery, true, $"{CorrelationId}_");

                    context.Database.ExecuteSqlCommand(
                        updateQuery,
                        new OracleParameter("A1", coordenada.A1),
                        new OracleParameter("B1", coordenada.B1),
                        new OracleParameter("C1", coordenada.C1),
                        new OracleParameter("D1", coordenada.D1),
                        new OracleParameter("E1", coordenada.E1),
                        new OracleParameter("F1", coordenada.F1),
                        new OracleParameter("G1", coordenada.G1),
                        new OracleParameter("H1", coordenada.H1),
                        new OracleParameter("I1", coordenada.I1),
                        new OracleParameter("J1", coordenada.J1),
                        new OracleParameter("A2", coordenada.A2),
                        new OracleParameter("B2", coordenada.B2),
                        new OracleParameter("C2", coordenada.C2),
                        new OracleParameter("D2", coordenada.D2),
                        new OracleParameter("E2", coordenada.E2),
                        new OracleParameter("F2", coordenada.F2),
                        new OracleParameter("G2", coordenada.G2),
                        new OracleParameter("H2", coordenada.H2),
                        new OracleParameter("I2", coordenada.I2),
                        new OracleParameter("J2", coordenada.J2),
                        new OracleParameter("A3", coordenada.A3),
                        new OracleParameter("B3", coordenada.B3),
                        new OracleParameter("C3", coordenada.C3),
                        new OracleParameter("D3", coordenada.D3),
                        new OracleParameter("E3", coordenada.E3),
                        new OracleParameter("F3", coordenada.F3),
                        new OracleParameter("G3", coordenada.G3),
                        new OracleParameter("H3", coordenada.H3),
                        new OracleParameter("I3", coordenada.I3),
                        new OracleParameter("J3", coordenada.J3),
                        new OracleParameter("A4", coordenada.A4),
                        new OracleParameter("B4", coordenada.B4),
                        new OracleParameter("C4", coordenada.C4),
                        new OracleParameter("D4", coordenada.D4),
                        new OracleParameter("E4", coordenada.E4),
                        new OracleParameter("F4", coordenada.F4),
                        new OracleParameter("G4", coordenada.G4),
                        new OracleParameter("H4", coordenada.H4),
                        new OracleParameter("I4", coordenada.I4),
                        new OracleParameter("J4", coordenada.J4),
                        new OracleParameter("A5", coordenada.A5),
                        new OracleParameter("B5", coordenada.B5),
                        new OracleParameter("C5", coordenada.C5),
                        new OracleParameter("D5", coordenada.D5),
                        new OracleParameter("E5", coordenada.E5),
                        new OracleParameter("F5", coordenada.F5),
                        new OracleParameter("G5", coordenada.G5),
                        new OracleParameter("H5", coordenada.H5),
                        new OracleParameter("I5", coordenada.I5),
                        new OracleParameter("J5", coordenada.J5),
                        new OracleParameter("UsuarioCoordenadaId", coordenada.UsuarioCoordenadaId),
                        new OracleParameter("Id", coordenada.Id)
                    );

                    context.Database.Connection.Close();
                    Config.MyLog("ActualizarCoordenada", "Success", "Coordenada updated successfully", AConfig.EnableLog, $"{CorrelationId}_");
                }

                FResponse.Response.Status = true;
                FResponse.Response.Message = "Coordenada actualizada exitosamente";
                FResponse.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                string ErrorMessage = ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.InnerException == null
                        ? ex.InnerException.Message
                        : ex.InnerException.InnerException.Message;

                Config.MyLog("ActualizarCoordenada", "Error", ErrorMessage, true, $"{CorrelationId}_");

                FResponse.Response.Status = false;
                FResponse.Response.Message = ErrorMessage;
                FResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return FResponse;
        }

        public FResponse GetUsuarioCoordenada(string Token, int id, string CorrelationId)
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

                    string query = "SELECT * FROM COORDENADAS WHERE USUARIO_COORDENADA_ID = :Id";
                    Config.MyLog("GetUsuarioCoordenada", "Query", query, true, $"{CorrelationId}_");

                    using (var command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("Id", id));

                        using (var reader = command.ExecuteReader())
                        {
                            var coordenadas = new List<Coordenada>();

                            while (reader.Read())
                            {
                                coordenadas.Add(new Coordenada
                                {
                                    Id = reader.GetInt32(0),
                                    A1 = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    B1 = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                    C1 = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    D1 = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                    E1 = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    F1 = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                    G1 = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                                    H1 = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                    I1 = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                                    J1 = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                                    A2 = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11),
                                    B2 = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                                    C2 = reader.IsDBNull(13) ? (int?)null : reader.GetInt32(13),
                                    D2 = reader.IsDBNull(14) ? (int?)null : reader.GetInt32(14),
                                    E2 = reader.IsDBNull(15) ? (int?)null : reader.GetInt32(15),
                                    F2 = reader.IsDBNull(16) ? (int?)null : reader.GetInt32(16),
                                    G2 = reader.IsDBNull(17) ? (int?)null : reader.GetInt32(17),
                                    H2 = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                                    I2 = reader.IsDBNull(19) ? (int?)null : reader.GetInt32(19),
                                    J2 = reader.IsDBNull(20) ? (int?)null : reader.GetInt32(20),
                                    A3 = reader.IsDBNull(21) ? (int?)null : reader.GetInt32(21),
                                    B3 = reader.IsDBNull(22) ? (int?)null : reader.GetInt32(22),
                                    C3 = reader.IsDBNull(23) ? (int?)null : reader.GetInt32(23),
                                    D3 = reader.IsDBNull(24) ? (int?)null : reader.GetInt32(24),
                                    E3 = reader.IsDBNull(25) ? (int?)null : reader.GetInt32(25),
                                    F3 = reader.IsDBNull(26) ? (int?)null : reader.GetInt32(26),
                                    G3 = reader.IsDBNull(27) ? (int?)null : reader.GetInt32(27),
                                    H3 = reader.IsDBNull(28) ? (int?)null : reader.GetInt32(28),
                                    I3 = reader.IsDBNull(29) ? (int?)null : reader.GetInt32(29),
                                    J3 = reader.IsDBNull(30) ? (int?)null : reader.GetInt32(30),
                                    A4 = reader.IsDBNull(31) ? (int?)null : reader.GetInt32(31),
                                    B4 = reader.IsDBNull(32) ? (int?)null : reader.GetInt32(32),
                                    C4 = reader.IsDBNull(33) ? (int?)null : reader.GetInt32(33),
                                    D4 = reader.IsDBNull(34) ? (int?)null : reader.GetInt32(34),
                                    E4 = reader.IsDBNull(35) ? (int?)null : reader.GetInt32(35),
                                    F4 = reader.IsDBNull(36) ? (int?)null : reader.GetInt32(36),
                                    G4 = reader.IsDBNull(37) ? (int?)null : reader.GetInt32(37),
                                    H4 = reader.IsDBNull(38) ? (int?)null : reader.GetInt32(38),
                                    I4 = reader.IsDBNull(39) ? (int?)null : reader.GetInt32(39),
                                    J4 = reader.IsDBNull(40) ? (int?)null : reader.GetInt32(40),
                                    A5 = reader.IsDBNull(41) ? (int?)null : reader.GetInt32(41),
                                    B5 = reader.IsDBNull(42) ? (int?)null : reader.GetInt32(42),
                                    C5 = reader.IsDBNull(43) ? (int?)null : reader.GetInt32(43),
                                    D5 = reader.IsDBNull(44) ? (int?)null : reader.GetInt32(44),
                                    E5 = reader.IsDBNull(45) ? (int?)null : reader.GetInt32(45),
                                    F5 = reader.IsDBNull(46) ? (int?)null : reader.GetInt32(46),
                                    G5 = reader.IsDBNull(47) ? (int?)null : reader.GetInt32(47),
                                    H5 = reader.IsDBNull(48) ? (int?)null : reader.GetInt32(48),
                                    I5 = reader.IsDBNull(49) ? (int?)null : reader.GetInt32(49),
                                    J5 = reader.IsDBNull(50) ? (int?)null : reader.GetInt32(50),
                                    UsuarioCoordenadaId = reader.GetString(51)
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

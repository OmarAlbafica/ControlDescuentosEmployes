using System;
using System.Collections.Generic;
using System.Net;

namespace ClassModel
{
    public class Response
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public dynamic Result { get; set; }
    }

    public class FResponse
    {
        public Response Response { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public FResponse()
        {
            Response = new Response();
        }
    }

    public class ApiConfig
    {
        public bool EnableLog { get; set; } = true;
        public string ConfigPath { get; set; }

        public string ConfigServicePath { get; set; }
    }

    public class ServiceConfig
    {
        public int TimeOutSession { get; set; }
        public List<AuthorizedUser> AuthorizedUsers { get; set; }
        public OracleConfig Oracle { get; set; }
        public TimeConfig Time { get; set; }
        public SbsConfig Subsidiary { get; set; }
        public WksConfig Workstation { get; set; }
        public PriceConfig PriceLevel { get; set; }
        public StatusConfig OrderStatus { get; set; }
        public NebulaConfig Api { get; set; }
        public OrderConfig Order { get; set; }
        public string JsonPath { get; set; }
        public ServiceConfig()
        {
            Oracle = new OracleConfig();
            Time = new TimeConfig();
            Subsidiary = new SbsConfig();
            Workstation = new WksConfig();
            PriceLevel = new PriceConfig();
            OrderStatus = new StatusConfig();
            Api = new NebulaConfig();
            Order = new OrderConfig();
            AuthorizedUsers = new List<AuthorizedUser>();
        }
    }

    public class OracleConfig
    {
        public string Server { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int TimeOut { get; set; } = 300;
    }

    public class TimeConfig
    {
        public int ExecutionInterval { get; set; } = 1;
    }

    public class SbsConfig
    {
        public long Sid { get; set; }
        public short No { get; set; }
        public string Name { get; set; }
    }

    public class WksConfig
    {
        public long Sid { get; set; }
        public string Name { get; set; }
        public short? No { get; set; }
    }

    public class PriceConfig
    {
        public long Sid { get; set; }
        public string Name { get; set; }
        public short Level { get; set; }
    }

    public class StatusConfig
    {
        public short IdSinc { get; set; }
        public string Name { get; set; }

    }

    public class NebulaConfig
    {
        public string BaseUrl { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string TraceId { get; set; }
        public string BusinessGroupId { get; set; }
    }

    public class OrderConfig
    {
        public bool UseVat { get; set; } = false;
        public bool Archived { get; set; } = false;
        public bool IsHeld { get; set; } = false;
        public bool Detax { get; set; } = false;
        public bool Kit { get; set; } = false;
        public string Comment2 { get; set; }
        public string Currency { get; set; } = "CLP";
    }

    public class AuthorizedUser
    {
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class Session
    {
        public string User { get; set; }
        public string InitialDate { get; set; }
        public string FinalDate { get; set; }
    }

    public class LoginBody
    {
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class OrderItem
    {
        public long SID { get; set; }
        public decimal QTY { get; set; }
        public decimal TAX_PERC { get; set; }
        public decimal PRICE { get; set; }
        public decimal TAX_AMT { get; set; }
        public string ORDER_TYPE { get; set; }
    }

    public class Store
    {
        public int StoreSid { get; set; }
        public string StoreName { get; set; }
    }

    public class Employee
    {
        public int EmployeeSid { get; set; }
        public string EmployeeName { get; set; }
        public int StoreSid { get; set; }
    }

    public class UsuarioCoordenada
    {
        public string Email { get; set; }
        public int Sid { get; set; }
        public string Tienda { get; set; }
        public decimal MaxDescuento { get; set; }
        public decimal CicloCoordenada { get; set; }
    }

    public class UsuarioControlDescuento
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Password { get; set; } // Contraseña encriptada
    }
}

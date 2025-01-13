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
        public string SidStore { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string SbsSid { get; set; }
        public string SbsNo { get; set; }
        public string SbsName { get; set; }
        public string FullName { get; set; }
    }

    public class Employee
    {
        public string CustSid { get; set; }
        public string EmplName { get; set; }
        public string SidEmployee { get; set; }
        public string SidCustomer { get; set; }
        public string FullName { get; set; }
        public string StoreSid { get; set; }
        public string SbsSid { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class UserCoordenada
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Sid { get; set; } // Cambiado a string para TO_CHAR
        public string Tienda { get; set; }
        public int MaxDescuento { get; set; }
        public int CicloCoordenada { get; set; }
    }

    public class UsuarioCoordenada
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Sid { get; set; }
        public string Tienda { get; set; }
        public int MaxDescuento { get; set; }
        public int CicloCoordenada { get; set; }
    }

    public class UsuarioControlDescuento
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Password { get; set; } // Contraseña encriptada
        public int isadmin { get; set; }
    }

    public class Coordenada
    {
        public int Id { get; set; }
        public int? A1 { get; set; }
        public int? B1 { get; set; }
        public int? C1 { get; set; }
        public int? D1 { get; set; }
        public int? E1 { get; set; }
        public int? F1 { get; set; }
        public int? G1 { get; set; }
        public int? H1 { get; set; }
        public int? I1 { get; set; }
        public int? J1 { get; set; }
        public int? A2 { get; set; }
        public int? B2 { get; set; }
        public int? C2 { get; set; }
        public int? D2 { get; set; }
        public int? E2 { get; set; }
        public int? F2 { get; set; }
        public int? G2 { get; set; }
        public int? H2 { get; set; }
        public int? I2 { get; set; }
        public int? J2 { get; set; }
        public int? A3 { get; set; }
        public int? B3 { get; set; }
        public int? C3 { get; set; }
        public int? D3 { get; set; }
        public int? E3 { get; set; }
        public int? F3 { get; set; }
        public int? G3 { get; set; }
        public int? H3 { get; set; }
        public int? I3 { get; set; }
        public int? J3 { get; set; }
        public int? A4 { get; set; }
        public int? B4 { get; set; }
        public int? C4 { get; set; }
        public int? D4 { get; set; }
        public int? E4 { get; set; }
        public int? F4 { get; set; }
        public int? G4 { get; set; }
        public int? H4 { get; set; }
        public int? I4 { get; set; }
        public int? J4 { get; set; }
        public int? A5 { get; set; }
        public int? B5 { get; set; }
        public int? C5 { get; set; }
        public int? D5 { get; set; }
        public int? E5 { get; set; }
        public int? F5 { get; set; }
        public int? G5 { get; set; }
        public int? H5 { get; set; }
        public int? I5 { get; set; }
        public int? J5 { get; set; }
        public int UsuarioCoordenadaId { get; set; }
        public string Email { get; set; }
        public string EmployeeSid { get; set; }
        public string Tienda { get; set; }
        public int MaxDescuento { get; set; }
        public int CicloCoordenada { get; set; }
        public string Subsidiaria { get; set; }
    }

    public class CoordenadaRequest
    {
        public int Id { get; set; }
        public int? A1 { get; set; }
        public int? B1 { get; set; }
        public int? C1 { get; set; }
        public int? D1 { get; set; }
        public int? E1 { get; set; }
        public int? F1 { get; set; }
        public int? G1 { get; set; }
        public int? H1 { get; set; }
        public int? I1 { get; set; }
        public int? J1 { get; set; }
        public int? A2 { get; set; }
        public int? B2 { get; set; }
        public int? C2 { get; set; }
        public int? D2 { get; set; }
        public int? E2 { get; set; }
        public int? F2 { get; set; }
        public int? G2 { get; set; }
        public int? H2 { get; set; }
        public int? I2 { get; set; }
        public int? J2 { get; set; }
        public int? A3 { get; set; }
        public int? B3 { get; set; }
        public int? C3 { get; set; }
        public int? D3 { get; set; }
        public int? E3 { get; set; }
        public int? F3 { get; set; }
        public int? G3 { get; set; }
        public int? H3 { get; set; }
        public int? I3 { get; set; }
        public int? J3 { get; set; }
        public int? A4 { get; set; }
        public int? B4 { get; set; }
        public int? C4 { get; set; }
        public int? D4 { get; set; }
        public int? E4 { get; set; }
        public int? F4 { get; set; }
        public int? G4 { get; set; }
        public int? H4 { get; set; }
        public int? I4 { get; set; }
        public int? J4 { get; set; }
        public int? A5 { get; set; }
        public int? B5 { get; set; }
        public int? C5 { get; set; }
        public int? D5 { get; set; }
        public int? E5 { get; set; }
        public int? F5 { get; set; }
        public int? G5 { get; set; }
        public int? H5 { get; set; }
        public int? I5 { get; set; }
        public int? J5 { get; set; }
        public string UsuarioCoordenadaId { get; set; }
    }

    public class SearchRequest
    {
        public string Search { get; set; }
    }
    public class UserDeleteRequest
    {
        public int Id { get; set; }
    }

    public class UserUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class UserCreateRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserCoordenadaRequest
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Sid { get; set; }
        public string Tienda { get; set; }
        public int MaxDescuento { get; set; }
        public int CicloCoordenada { get; set; }
    }
    public class UserCoordenadaDeleteRequest
    {
        public int Id { get; set; }
    }
}

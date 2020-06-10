using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class ClientController : ApiController
    {
        public static readonly HttpClient HttpClient;

        static ClientController()
        {
            HttpClient = new HttpClient();
        }
    }
}

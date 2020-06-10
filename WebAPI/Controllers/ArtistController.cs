using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace WebAPI.Controllers
{
    public class ArtistController : ApiController
    {
        // GET api/artist
        public string Get()
        {
            return  "Finish the link with a <q>/mbid</q><br>" +
                    "Some examples of mbid's are:<br>" +
                    "Nirvana : 5b11f4ce-a62d-471e-81fc-a69a8278c7da<br>" +
                    "Linking Park : f59c5520-5f46-4d2c-b2c4-822eabf53419<br>" +
                    "Eminem : b95ce3ff-3d05-4e87-9e01-c97b66af13d4<br>" +
                    "Östen med Resten : 2844b5b7-284b-4fc3-8bd4-0b3297938ee4";
        }

        // GET api/artist/mbid
        public async Task<JObject> Get(string id)
        {
            var jObject = await Program.ReturnJson(id);
            return jObject;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Web;
using System.Collections.Specialized;

using System.Globalization;
using System.Text.Json.Serialization;

namespace aspnetcoreapp
{
    public class Program
    {
        static readonly HttpClient client = new HttpClient();

        //Want to do this for the ASP .NET Core to work.
        //But currently unable
        public void OnPost(){
            //var mbidLink = Request.Form["mbidLink"];
            //do something with mbidLink
        }

        static async Task Main(string[] args)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try	
            {
                /*
                while(true){
                    //Could not get the Razor site to work, so this will have to do.
                    
                    string input = Console.ReadLine();
                    
                    //45f07934-675a-46d6-a577-6f8637a411b1
                    //5b11f4ce-a62d-471e-81fc-a69a8278c7da
                    


                }
                */

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Erik007");
                //HttpResponseMessage response = await client.GetAsync("https://musicbrainz.org/ws/2/area/" + input +"?inc=aliases&fmt=json");
                //HttpResponseMessage response = await client.GetAsync("http://musicbrainz.org/ws/2/artist/" + input + "?&fmt=json&inc=url-rels+release-groups");
                HttpResponseMessage response = await client.GetAsync("http://musicbrainz.org/ws/2/artist/5b11f4ce-a62d-471e-81fc-a69a8278c7da?&fmt=json&inc=url-rels+release-groups");
                
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JsonConvert.DeserializeObject(responseBody));
                var jsonObj = JsonConvert.DeserializeObject<MUSICBRAINZ_DATA>(responseBody);
                

                //Console.WriteLine(jsonObj.Isnis[0]);
                Console.WriteLine(jsonObj.Relations[0]);

                //Used for creating the ASP .NET Razor site.
                //CreateHostBuilder(args).Build().Run();
            }
            catch(HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");	
                Console.WriteLine("Message :{0} ",e.Message);
            }
        }

        public static string HelloWorld(string input){
            string str;
            str = "Hello World " + input; 
            return str;
        }

        //Creates the connection to localhost:5000
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class MUSICBRAINZ_DATA{
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        
        [JsonPropertyName("isnis")]
        public string[] Isnis { get; set; }

        [JsonPropertyName("relations")]
        public string[] Relations { get; set; }

        [JsonPropertyName("direction")]
        public List<string> Direction { get; set; }
        
    }


//Added to make the JSON prettier
//with appropriate indentations
//https://stackoverflow.com/questions/4580397/json-formatter-in-c
class JsonHelper
{
    private const string INDENT_STRING = "    ";
    public static string FormatJson(string str)
    {
        var indent = 0;
        var quoted = false;
        var sb = new StringBuilder();
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    sb.Append(ch);
                    break;
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    var index = i;
                    while (index > 0 && str[--index] == '\\')
                        escaped = !escaped;
                    if (!escaped)
                        quoted = !quoted;
                    break;
                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case ':':
                    sb.Append(ch);
                    if (!quoted)
                        sb.Append(" ");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}

static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
    {
        foreach (var i in ie)
        {
            action(i);
        }
    }
}
}

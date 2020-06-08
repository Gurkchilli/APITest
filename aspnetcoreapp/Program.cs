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
using System.Text.RegularExpressions;
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
                    string readLine = Console.ReadLine();
                    
                    //45f07934-675a-46d6-a577-6f8637a411b1
                    //5b11f4ce-a62d-471e-81fc-a69a8278c7da
                }
                */
                

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Erik007");
                //HttpResponseMessage response = await client.GetAsync("https://musicbrainz.org/ws/2/area/" + input +"?inc=aliases&fmt=json");
                string input = "5b11f4ce-a62d-471e-81fc-a69a8278c7da";
                HttpResponseMessage response = await client.GetAsync("http://musicbrainz.org/ws/2/artist/" + input + "?&fmt=json&inc=url-rels+release-groups");
                //HttpResponseMessage response = await client.GetAsync("http://musicbrainz.org/ws/2/artist/5b11f4ce-a62d-471e-81fc-a69a8278c7da?&fmt=json&inc=url-rels+release-groups");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                //Have to use dynamic Json since wikidata is several layers down.
                //and using a class for that did not work for me.
                dynamic jsonObject = JsonConvert.DeserializeObject(responseBody);

                //This identifier is the "Q11649" in the examples
                //used for connection to wikidata.
                string identifier = "";
                

                for(int i = 0; i < jsonObject.relations.Count; i++){
                    if(jsonObject.relations[i].type == "wikidata"){
                        identifier = jsonObject.relations[i].url.resource;
                        
                        int index = identifier.LastIndexOf('/');
                        //Just to make sure that the identifier exists
                        if(index != -1){
                            identifier = identifier.Substring(index+1);
                            
                            //creating Lists for saving Album Title, Picture Id, and Image Url
                            List<string> albumTitle = new List<string>();
                            List<string> pictureId = new List<string>();
                            List<string> imageUrl = new List<string>();

                            //Goes through all of the release-groups, sees if they are Albums,
                            //and then fetches album Title and Id for the cover picture
                            foreach(var album in jsonObject["release-groups"]){
                                if(album["primary-type"] == "Album"){
                                    albumTitle.Add(album.title.ToString());
                                    pictureId.Add(album.id.ToString());
                                }
                            }

                            //Gets a HttpRespinse to fetch the Album Cover Url
                            foreach(var id in pictureId){
                                HttpResponseMessage responseCoverArt = await client.GetAsync("http://coverartarchive.org/release-group/" + id);
                                //Since certain Album Cover's do not exist
                                //Check if the page is "404 : Not Found" 
                                if(responseCoverArt.StatusCode != HttpStatusCode.NotFound){
                                    string responseBodyCoverArt = await responseCoverArt.Content.ReadAsStringAsync();
                                    dynamic jsonObjectCoverArt = JsonConvert.DeserializeObject(responseBodyCoverArt);
                                    
                                    //Add the picture to the list
                                    imageUrl.Add(jsonObjectCoverArt.images[0].image.ToString());
                                }
                                //If the album cover does not exist, add a note.
                                else{
                                    imageUrl.Add("No Available Album Cover!");
                                }
                            }

                            for(int k = 0; k < albumTitle.Count; k++){
                                Console.WriteLine(albumTitle[k]);
                                Console.WriteLine(pictureId[k]);
                                Console.WriteLine(imageUrl[k]);
                                Console.WriteLine();
                            }                            
                        }
                        else{
                            Console.WriteLine("No WikiData Identifier");
                        }
                    }
                }

                //Wikidata
                //fetches the link to Wikipedia
                HttpResponseMessage responseWikiData = await client.GetAsync("https://www.wikidata.org/w/api.php?action=wbgetentities&ids="+ identifier +"&format=json&props=sitelinks");
                responseWikiData.EnsureSuccessStatusCode();
                string responseBodyWikiData = await responseWikiData.Content.ReadAsStringAsync();
                dynamic jsonObjectWikiData = JsonConvert.DeserializeObject(responseBodyWikiData);
                
                //Console.WriteLine(jsonObjectWikiData.entities[identifier].sitelinks.enwiki.title);
                //Get the siteUrl, and URL-encode the space with %20
                string siteUrl = jsonObjectWikiData.entities[identifier].sitelinks.enwiki.title;
                siteUrl = siteUrl.Replace(" ", "%20");


                //Wikipedia
                //Fetches the information regarding the band
                HttpResponseMessage responseWikipedia = await client.GetAsync("https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&exintro=true&redirects=true&titles=" + siteUrl);
                responseWikipedia.EnsureSuccessStatusCode();
                string responseBodyWikipedia = await responseWikipedia.Content.ReadAsStringAsync();
                //dynamic jsonObjectWikipedia = JsonConvert.DeserializeObject(responseBodyWikipedia);
                JObject jsonObjectWikipedia = JsonConvert.DeserializeObject<JObject>(responseBodyWikipedia);
                dynamic resultWikipedia = jsonObjectWikipedia["query"].First().First().First().First;
                string extract = resultWikipedia.extract;

                extract = StripHtml(extract);
                Console.WriteLine(extract);
                


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

        //First, remove new lines. Then remove html elements.
        public static string StripHtml(string input)
        {
            string replacement = Regex.Replace(input, @"\t|\n|\r", "");
            return Regex.Replace(replacement, "<.*?>", String.Empty);
        }
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
//Mainly used for debugging
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

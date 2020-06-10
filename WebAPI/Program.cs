using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net;

namespace WebAPI
{
    public class Program
    {
        //Creates new client
        //HttpClient is intended to be instantiated once and re-used throughout the life of an application.
        //Instantiate it through an ApiController, in order to avoid exeptions.
        static readonly HttpClient client = Controllers.ClientController.HttpClient;
        //Create the JObject that is going to be the result.
        static readonly JObject returnJObject = new JObject();



        //Used each time a resonce from an API is needed
        public static async Task<string> CreateClient(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }


        //Some HTML strings got unwanted text in them
        //First, remove new lines. Then remove html elements.
        public static string StripHtml(string input)
        {
            string replacement = Regex.Replace(input, @"\t|\n|\r", "");
            replacement = Regex.Replace(replacement, "<.*?>", "");
            return replacement;
        }


        //Creates objects for each album the artist has made
        //and puts these inside of a JArray.
        public static async Task CreateAlbumsArray(dynamic jsonObject)
        {
            //creating Lists for saving Album Title, Picture Id, and Image Url
            //All of these will be in the same object later
            List<string> albumTitle = new List<string>();
            List<string> pictureId = new List<string>();
            List<string> imageUrl = new List<string>();

            //Goes through all of the release-groups, sees if they are Albums,
            //and if they are then fetch album Title and Id for the cover picture
            foreach (var album in jsonObject["release-groups"])
            {
                if (album["primary-type"] == "Album")
                {
                    albumTitle.Add(album.title.ToString());
                    pictureId.Add(album.id.ToString());
                }
            }

            //Gets a HttpResponse to fetch the Album Cover Url
            foreach (var id in pictureId)
            {
                HttpResponseMessage responseCoverArt = await client.GetAsync("http://coverartarchive.org/release-group/" + id);
                //Since certain Album Cover's do not exist
                //Check if the page is "404 : Not Found" 
                if (responseCoverArt.StatusCode != HttpStatusCode.NotFound)
                {
                    string responseBodyCoverArt = await responseCoverArt.Content.ReadAsStringAsync();
                    dynamic jsonObjectCoverArt = JsonConvert.DeserializeObject(responseBodyCoverArt);

                    //Add the picture to the list
                    imageUrl.Add(jsonObjectCoverArt.images[0].image.ToString());

                    //An improvement could be:
                    //imageUrl.Add("http://coverartarchive.org/release-group/" + id + "/front[-(250)]");
                    //But seems to be slower during testing
                }
                //If the album cover does not exist, add a note.
                else
                {
                    imageUrl.Add("No Available Album Cover!");
                }
            }

            //Create the Json album objects and put them inside of an array
            JArray array = new JArray();
            for (int k = 0; k < albumTitle.Count; k++)
            {
                JObject obj = new JObject();

                obj.Add("title", albumTitle[k]);
                obj.Add("id", pictureId[k]);
                obj.Add("image", imageUrl[k]);

                array.Add(obj);
            }
            returnJObject["albums"] = array;
        }


        //Gets the description by first going to wikipages to get the title
        //and then using that title to fetch the Wikipedia extract.
        public static async Task GetArtistExtract(string identifier)
        {
            //Wikidata
            //fetches the link to Wikipedia
            string responseBodyWikiData = await CreateClient("https://www.wikidata.org/w/api.php?action=wbgetentities&ids=" + identifier + "&format=json&props=sitelinks");
            dynamic jsonObjectWikiData = JsonConvert.DeserializeObject(responseBodyWikiData);

            //Get the title, and URL-encode the spaces with %20
            string title = jsonObjectWikiData.entities[identifier].sitelinks.enwiki.title;
            title = title.Replace(" ", "%20");


            //Wikipedia
            //Fetches the information regarding the band
            string responseBodyWikipedia = await CreateClient("https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&exintro=true&redirects=true&titles=" + title); ;
            JObject jsonObjectWikipedia = JsonConvert.DeserializeObject<JObject>(responseBodyWikipedia);
            //One of the values in this jsonObject is unpredictable
            //But there is always only one path down to extract, so this trick does it.
            dynamic resultWikipedia = jsonObjectWikipedia["query"].First().First().First().First;
            string extract = resultWikipedia.extract;

            //Removes HTML tags and new lines
            extract = StripHtml(extract);

            //Add the extract to the Json answer.
            returnJObject["description"] = extract;
        }


        //The method that gets called from ArtistController.cs
        //Which returns the result as a Task<JObject>, containing all information needed.
        public static async Task<JObject> ReturnJson(string input)
        {
            try
            {
                //Immediately use the MBID-input as the first parameter of the JsonObject
                //Also add the parameter "Description" here, so it gets a nice placement in the end.
                returnJObject["mbid"] = input;
                returnJObject["description"] = "";

                //MusicBrainz require a User-Agent header
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Erik007");

                //Get the musicbrainz body
                string responseBody = await CreateClient("http://musicbrainz.org/ws/2/artist/" + input + "?&fmt=json&inc=url-rels+release-groups");

                //Have to use dynamic Json since wikidata is several layers down.
                //and using a class for that did not work for me.
                dynamic jsonObject = JsonConvert.DeserializeObject(responseBody);

                //This identifier is used for connecting to wikidata.
                //and (hopefully) gets a value inside of the 
                string identifier = "";

                //Go through all of the relations types, until wikidata is found.
                for (int i = 0; i < jsonObject.relations.Count; i++)
                {
                    if (jsonObject.relations[i].type == "wikidata")
                    {
                        //The identifier is always after the last '/'
                        identifier = jsonObject.relations[i].url.resource;
                        int index = identifier.LastIndexOf('/');
                        //Just to make sure that the identifier exists
                        if (index != -1)
                        {
                            identifier = identifier.Substring(index + 1);

                            //Creates the array which contains 
                            //album Title, Id for the cover picture, and the cover picture
                            await CreateAlbumsArray(jsonObject);
                        }

                        //Once the correct type (wikidata) has been found,
                        //break from the loop.
                        break;
                    }
                }

                //Make sure that the artist has a wikidata identifier
                if (identifier != "")
                {
                    await GetArtistExtract(identifier);
                }
                //If it does not exist, there is no Wikipedia page available for the artist
                else
                {
                    returnJObject["description"] = "No Wikipedia Page Available!";
                    returnJObject["albums"] = null;
                }

                //Return the jsonObject with (hopefully)
                //Mbid. Description. An array full of albums and corresponding cover pictures.
                return returnJObject;
            }
            catch (HttpRequestException e)
            {
                returnJObject["mbid"] = input;
                returnJObject["description"] = ("Exception Caught!<br>Message: ", e.Message).ToString();
                returnJObject["albums"] = null;
                return returnJObject;
            }
        }
    }
}
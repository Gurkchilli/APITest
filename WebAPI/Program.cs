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
        //Creates new clients for websites.
        static readonly HttpClient client = new HttpClient();
        //Create the JObject that is going to be the result.
        static JObject returnJObject = new JObject();

        public static async Task<JObject> ReturnJson(string input)
        {
            try
            {
                //Add the first parameter of the final JObject
                //Also add the parameter "Description" here, so it gets a nice placement in the end.
                returnJObject["mbid"] = input;
                returnJObject["description"] = "";

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Erik007");
                HttpResponseMessage response = await client.GetAsync("http://musicbrainz.org/ws/2/artist/" + input + "?&fmt=json&inc=url-rels+release-groups");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                //Have to use dynamic Json since wikidata is several layers down.
                //and using a class for that did not work for me.
                dynamic jsonObject = JsonConvert.DeserializeObject(responseBody);

                //This identifier is the "Q11649" in the examples
                //used for connection to wikidata.
                string identifier = "";


                for (int i = 0; i < jsonObject.relations.Count; i++)
                {
                    if (jsonObject.relations[i].type == "wikidata")
                    {
                        identifier = jsonObject.relations[i].url.resource;

                        int index = identifier.LastIndexOf('/');
                        //Just to make sure that the identifier exists
                        if (index != -1)
                        {
                            identifier = identifier.Substring(index + 1);

                            //creating Lists for saving Album Title, Picture Id, and Image Url
                            List<string> albumTitle = new List<string>();
                            List<string> pictureId = new List<string>();
                            List<string> imageUrl = new List<string>();

                            //Goes through all of the release-groups, sees if they are Albums,
                            //and then fetches album Title and Id for the cover picture
                            foreach (var album in jsonObject["release-groups"])
                            {
                                if (album["primary-type"] == "Album")
                                {
                                    albumTitle.Add(album.title.ToString());
                                    pictureId.Add(album.id.ToString());
                                }
                            }

                            //Gets a HttpRespinse to fetch the Album Cover Url
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
                        else
                        {
                            returnJObject["description"] = "No WikiData Identifier";
                            return returnJObject;
                        }

                        //Once the correct type (wikidata) has been found
                        //break from the loop.
                        break;
                    }
                }

                //Wikidata
                //fetches the link to Wikipedia
                HttpResponseMessage responseWikiData = await client.GetAsync("https://www.wikidata.org/w/api.php?action=wbgetentities&ids=" + identifier + "&format=json&props=sitelinks");
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

                //Removes HTML tags and new lines
                extract = StripHtml(extract);

                //Add the extract to the Json answer.
                returnJObject["description"] = extract;


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

        //First, remove new lines. Then remove html elements.
        public static string StripHtml(string input)
        {
            string replacement = Regex.Replace(input, @"\t|\n|\r", "");
            return Regex.Replace(replacement, "<.*?>", String.Empty);
        }
    }
}
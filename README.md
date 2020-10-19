# API Test

Welcome to this Mashup API by Erik Hildinge.
This is a ASP.NET Web Application, with the .NET Framework 4.7.1. Mainly tested in Google Chrome.

This .NET solution is developed in Visual Studio 2017, and also started from Visual Studio 2017.
Make sure that you have the ASP.NET workload installed in your Visual Studio Installer.

This API call makes use of:
* MusicBrainz, which contain detailed information about musical artists.
* Wikipedia, to get a description about the band.
* Cover Art Archive, which helps retrieve an url to a requested album.

You enter a MBID, which will return:
* The given MBID
* A description from Wikipedia
* All albums (with title, album id, and image url) in separate json objects.

In order to search using a MBID of your choise, use:\
localhost:65134/api/artist/"Your MBID"

Some examples of MBID's are:

BAND  | MBID
------------- | -------------
Nirvana | 5b11f4ce-a62d-471e-81fc-a69a8278c7da
Linking Park | f59c5520-5f46-4d2c-b2c4-822eabf53419
Eminem | b95ce3ff-3d05-4e87-9e01-c97b66af13d4
Östen med Resten | 2844b5b7-284b-4fc3-8bd4-0b3297938ee4
# funda-list
This is a small project to create a few listings based on the data in the Funda website (www.funda.nl), using 
a provided API.

# The assignment

Determine which makelaar's in Amsterdam have the most object listed for sale. Make a table of the top 10. 
Then do the same thing but only for objects with a tuin which are listed for sale. 

For the assignment you may write a program in any object oriented language of your choice and you may use any 
libraries that you find useful.

## Initial information

Useful translations:
* koop = purchase 
* tuin = garden
* makelaar = real estate agent

The funda API returns information about the objects that are listed on funda.nl which are for sale. An example 
of one of the URLs in the REST API is: 
http://partnerapi.funda.nl/feeds/Aanbod.svc/[key]/?type=koop&zo=/amsterdam/tuin/&page=1&pagesize=25

The [key] has been provided in the assignment.

Most of the parameters that you can pass are self explanatory. The parameter 'zo' (zoekopdracht or search query) 
is the same as the key used in funda.nl URLs. For example the URL shown above searches for houses in Amsterdam 
with a garden: http://www.funda.nl/koop/amsterdam/tuin/.

The API can return data about objects in XML format (the default) or in JSON (by inserting a 'json' slug between 
'Aanbod.svc' and '[key]').

Performing too many API requests in a short period of time (>100 per minute), the API will reject the 
requests. The implementation should mitigate (avoid) errors and handle any errors that occur, so take 
both into account.

## Initial investigation

To get a feel about the data (form and size), I've started with poking around on the Funda website. At the 
time of typing this, there are [4331 koopwoningen in Amsterdam]( https://www.funda.nl/koop/amsterdam/), and 
[899 with a tuin](https://www.funda.nl/koop/amsterdam/tuin/). Exploring a bit more, there are 
[428 makelaars in Amsterdam](https://www.funda.nl/makelaars/amsterdam/), though it's not clear if those have a 
100% correlation with the houses on offer (there might be makelaars who are registered in Amsterdam but who, 
say, only assist in renting out properties. Also, some of them are clearly not actually in Amsterdam). Back to 
the first two queries!

Trying out the API urls directly from the browser, substituting the key, gets me data in XML format. 
Browser = Chrome, with XV plugin to help unraveling the XML.

As the endpoint is a .svc file, one thing to try out is calling it with `?singleWsdl`. This does return a valid 
(and rather extensive) WSDL, but glancing through the definitions (and keeping in mind that the .svc in the 
information given isn't called by sending it a SOAP XML message, but with something which leans towards a REST 
API, but isn't quite that either), I instead turn to the JSON version (Chrome + JSON Formatter plugin).

For the requested functionality, only a few fields are relevant: Id (to uniquely identify an Object), MakelaarId and MakelaarNaam.

One thing I notice is that while you can set any page size in the request, and the `Paging` block at the end 
reacts accordingly, the call never returns more than 25 `Objects` at a time (it will return smaller pages just 
fine). Which means the "lazy" way of grabbing the data (do an initial call for pagesize 1, note the total amount 
indicated, do a second call for that amount of `Objects`) isn't going to work. Something to keep in mind.

Another thing to keep in mind is that this API does not seem to have a way to guarantee that the dataset is 
consistant over multiple invocations of the API like when retrieving multiple pages. This could lead to either 
losing elements or getting duplicate ones. There is the additional assumption that the elements will be returned 
in a consistent order. Avoiding duplicates is not to hard to avoid (merge incoming datasets based on a unique key
), missing elements I will ignore for now.

Reading over the information, the rate limiting and error handling on the remote API call need to be handled.
Unfortunately, the documentation does not indicate how exactly this will manifest (i.e. how hitting the limit will
affect subsequent requests).

## Choices

I will be using C# and .NET Core for this project. .NET has good support for both XML and JSON data and 
transformations. As (for this project) I'm only interested in a small subset of the dataset, I will use the 
JSON version of the API, to prevent building out the full object model, most of which I'm not interested in.
In my experience this is easier with JSON.

To get a better resilience to incidental issues with the availability of the Funda API webservice, I have
investigated options. As the Polly library is part of the .NET Core environment now, I have decided to use
that library to handle transient faults, using the Retry policy.

See: https://github.com/App-vNext/Polly 

Right now, the policy is injected close to the (single) call to the webservice. A cleaner way would be to 
inject the policy at the services level, but for now this should work. (I briefly tried to get the cleaner approach
to work, but ran out of time. Many samples explain how to do that in a web application, but it's less clear how to
implement this cleanly in a console app.)

About detecting "overflow" of the allowed rate:

According to https://cloud.google.com/solutions/rate-limiting-strategies-techniques, status code 429 is used
for "Too Many Requests" (and I've added that to the implementation), however while checking what was returned
this does not seem to be returned. Instead, the Funda API service gives back a statuscode 401 (Unauthorized) with 
the text "Request limit exceeded" in the reason. I've added that specific case as well.

To further prevent overflow, I've implemented a form of the "Leaky Bucket" or "Token Bucket" pattern.
This hands out access tokens at a predefined rate, flattening the amount of calls to the web service.
See this Wikipedia article for base information: https://en.wikipedia.org/wiki/Leaky_bucket

My previous experience with that was from the other side (preventing a web service from being overrun) and
implemented using a reverse proxy (nginx), so that wasn't directly applicable to this situation, though I
did recognise the pattern.

The actual implementation is done (with thanks to Google) with a tweaked implementation found at 
https://dotnetcoretutorials.com/2019/11/24/implementing-a-leaky-bucket-client-in-net-core/

# Implementation

This section hints at how to get the solution to work on your own workstation.

## Requirements

In order to run the project, the following is needed:

* .NET Core 3.1 SDK
* Git
* I used Visual Studio (Community edition) to build and debug the code, but it can be run with just .NET Core


## Technologies

The following technologies were used:
* .NET Core 3.1 SDK (3.1.401)
* C#

Dependencies:
* Microsoft.Extensions.Http.Polly (3.1.7)
* Serilog.AspNetCore (3.4.0)
* Serilog.Sinks.File (4.1.0)

## Installation and running the application

1. Clone the Repository: `git clone https://github.com/hduyker/funda-list.git funda-list`
2. `cd funda-list`
3. Check the contents of the file `FundaListApp\appsettings.json`. You will need to put a valid API key in there. See below for a sample file.
4. Depending on the system it might be necessary to put the `appsettings.json`  in `FundaListApp\bin\Debug\netcoreapp3.1`
5. Similar for running the tests, they also require a valid setting file in the right folder.
6. Start the console app by running `dotnet run --project FundaListApp\FundaListApp.csproj`

Format of the `appsettings.json` file. The code will do a sanity check for the key (currently the only check if it is 32 characters in length)
```
{
  "FundaAPIKey": "<API KEY HERE>",
  "FundaAPIBaseURL": "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/"
}
```

Running the code (succesfully) will display the output similar to what's shown below on screen, as well generate (append to) a log
file `FundaList-log-<date>.txt` in the current directory. If nothing gets displayed, check the log file for the possible reason.

```
Start retrieving information from Funda.

Top 10 makelaars with 'koop' objects in Amsterdam

  # Makelaar                                 Properties
  1 Eefje Voogd Makelaardij                         214
  2 Ramón Mossel Makelaardij o.g. B.V.               98
  3 Broersma Makelaardij                             95
  4 Hallie & Van Klooster Makelaardij                89
  5 Hoekstra en van Eck Amsterdam West               85
  6 Makelaardij Van der Linden Amsterdam             78
  7 Carla van den Brink B.V.                         72
  8 Smit & Heinen Makelaars en Taxateurs o/z         72
  9 Heeren Makelaars                                 69
 10 Makelaarsland                                    67

Total number of objects in selection: 3775.


Top 10 makelaars with 'koop' objects with tuin in Amsterdam

  # Makelaar                                 Properties
  1 Broersma Makelaardij                             35
  2 Hoekstra en van Eck Amsterdam Noord              28
  3 Makelaardij Van der Linden Amsterdam             27
  4 Hallie & Van Klooster Makelaardij                23
  5 Ramón Mossel Makelaardij o.g. B.V.               20
  6 Makelaarsland                                    19
  7 Heeren Makelaars                                 18
  8 Hoekstra en van Eck Amsterdam West               17
  9 RET Makelaars - Amsterdam-Oost & IJburg          17
 10 Eefje Voogd Makelaardij                          16

Total number of objects in selection: 862.

Press any key to exit.
```

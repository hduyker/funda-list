# funda-list
This is a small project to create a few listings based on the data in the Funda website (www.funda.nl), using 
a provided API.

# The assignment

Determine which makelaar's in Amsterdam have the most object listed for sale. Make a table of the top 10. 
Then do the same thing but only for objects with a tuin which are listed for sale. 

For the assignment you may write a program in any object oriented language of your choice and you may use any 
libraries that you find useful.

## Initial information and thoughts

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

The API can data about objects in XML format (the default) or in JSON (by inserting a 'json' slug between 
'Aanbod.svc' and '[key]'.

Performing too many API requests in a short period of time (>100 per minute), the API will reject the 
requests. The implementation should mitigate (avoid) errors and handle any errors that occur, so take 
both into account.

---

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

One thing I notice is that while you can set any page size in the request, and the `Paging` block at the end 
reacts accordingly, the call never returns more than 25 `Objects` at a time (it will return smaller pages just 
fine). Which means the "lazy" way of grabbing the data (do an initial call for pagesize 1, note the total amount 
indicated, do a second call for that amount of `Objects`) isn't going to work. Something to keep in mind.

Another thing to keep in mind is that this API does not seem to have a way to guarantee that the dataset is 
consistant over multiple invocations of the API like when retrieving multiple pages. This could lead to either 
losing elements or getting duplicate ones. There is the additional assumption that the elements will be returned 
in a consistent order. Avoiding duplicates is not to hard to avoid (merge incoming datasets based on a unique key
), missing elements I will ignore for now.

Reading over the information, another thing to keep in mind is the rate limiting and error handling on the 
remote API call. Unfortunately, the documentation does not indicate how exactly this will manifest. 

## Choices

I will be using C# and .NET Core for this project. As .NET has good support for both XML and JSON data and 
transformations. As (for this project) I'm only interested in a small subset of the dataset, I will use the 
JSON version of the API, to prevent building out the full object model, most of which I'm not interested in.

To get a better resilience to incidental issues with the availability of the Funda API webservice, I have
investigated options. As the Polly library is part of the .NET Core environment now, I have decided to use
that library to handle transient faults, using the Retry policy.

See: https://github.com/App-vNext/Polly 

Right now, the policy is injected close to the (single) call to the webservice. A cleaner way would be to 
inject the policy at the services level, but for now this should work. I briefly tried to get this to work,
but ran out of time. Many samples explain how to do that in a web application, and this is a console app.

About detecting "overflow" of the allowed rate:

According to https://cloud.google.com/solutions/rate-limiting-strategies-techniques, status code 429 is used
for "Too Many Requests" (and I've added that to the implementation), however while checking what was returned
this does not seem to be returned. Instead, the service gives back a statuscode 401 (Unauthorized) with the text
"Request limit exceeded" in the reason. I've added that specific case as well.

To further prevent overflow, I've implemented a form of the "Leaky Bucket" or "Token Bucket" pattern.
This hands out access tokens at a predefined rate, flattening the amount of calls to the web service.
See this Wikipedia article for base information: https://en.wikipedia.org/wiki/Leaky_bucket

My previous experience with that was from the other side (preventing a web service from being overrun) and
implemented using a reverse proxy (nginx), so that wasn't directly applicable to this situation, though I
did recognise the pattern.

The actual implementation is with thanks to Google, it's tweaked from an implementation found at 
https://dotnetcoretutorials.com/2019/11/24/implementing-a-leaky-bucket-client-in-net-core/

# Implementation

This section hints at how to get the solution to work on your own workstation.

## Requirements

In order to run the project, the following is needed:

* .NET Core 3.1
* Visual Studio (I used the Community edition)
* Git

## Technologies

The following technologies were used:

* .NET Core 3.1
* C#

## Installation

1. Clone the Repository: `git clone https://github.com/hduyker/funda-list.git funda-list`
2. Run `cd funda-list`
3. Run `start Funda.sln`
4. Right click on the project solution name and select `Set StartUp Projects...`
8. Select Start Without Debugging `Ctrl + F5`.

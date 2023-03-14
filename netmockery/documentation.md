# Running netmockery

Command line:
``` bash
    # Netmockery starts and listens on default port
    dotnet netmockery.dll --command web --endpoints p:\ath\to\endpoint\directory
    
    # Netmockery starts and listens on given port
    dotnet netmockery.dll --command web --endpoints p:\ath\to\endpoint\directory --urls http://*:9876
```


# Configuring netmockery

## Directory structure

To configure netmockery, create a endpoint collection directory. An endpoint collection directory contains one or more subdirectories with ``endpoint.json`` 
files that specify how netmockery should handle incoming requests.

Example directory structure:

    endpoint_collection_directory/
        endpoint1/
            endpoint.json
        endpoint2/
            endpoint.json
        endpoint3/
            endpoint.json

## The ``endpoint.json`` file

``endpoint.json`` contains:

* ``name``: The endpoint's name. The name is for display in the web UI only.
* ``pathregex``: A request path reqular expression, used in the first step of the incoming request handling.
* ``responses``: A list of request matching rules and response creation steps for the endpoint.
* TODO: Document ``record`` property

Example ``endpoint.json``:

    {
      "name": "Simple endpoint",
      "pathregex": "^/foobar",

      "responses": [
        {
          "match": {},

          "literal": "Hello world",
          "contenttype": "text/plain"
        }
      ]
    }

## Request matching

The first step in handling incoming request is to check the incoming request's request path. The request path is matched against each ``pathregex`` for all
endpoints in the endpoint collection directory. 

Exactly one endpoint must match the request. If zero endpoints matches the incoming request,
netmockery writes an error message to the console output, and returns nothing to the client. If more than one endpoint
matches the incoming request, netmockery writes an error message to the console output, and returns nothing to the client.

The second and final step in the request matching process is to check the incoming request against the list of rules in ``responses``. The first rule that matches
the request will be used for creating the response. If no rule matches the request, netmockery writes an error message to the console output and returns nothing to
the client.

The ``match`` parameter within the ``responses`` list can match requests using one of these methods:

### Match any request

    "match": {}

### Match a regular expression against the request path, query string and request body

    "match": {
        "regex": "..."
    }

### Match an XPath expression against the request body

    "match": {
        "xpath": "boolean XPath expression",

        // ... define any namespace prefixes used in the xpath expression
        "namespaces": [
            {
                "prefix": "prefix",
                "ns": "namespace"
            },

            {
                "prefix": "prefix2",
                "ns": "namespace2"
            }
        ]
    }

### Limiting HTTP methods

The ``match`` object can have an optional ``methods`` property to match requests by HTTP method.

Example, match PUT and POST requests:

    "match": {
        "methods": "PUT POST"
    }

Example, match regular expression, only GET requests:

    "match": {
        "regex": "...",
        "methods": "GET"
    }


## Response creation

Several parameters inside the ``responses`` list control how netmockery creates the response.

### Returning static responses

* ``"literal": "This is the response to send"``: Returns the specified string
* ``"file": "filename.ext"``: Returns content from the specified file. File names/paths are relative to the directory containing the ``endpoint.json`` file.

### Executing a script to create a Response

* ``"script": "scriptfilename.csscript"``: Execute the C# script specified. File names/paths are relative to the directory containing the ``endpoint.json`` file.

Inside a script, the following global variables and functions are available:

* ``RequestPath`` (string): The incoming request path
* ``QueryString`` (string): The incoming request query string
* ``RequestBody`` (string): The incoming request body
* ``GetNow()`` (returns System.DateTime): The current time. See below for why you might want to use ``GetNow()`` inside your scripts instead of using ``System.DateTime.Now``.
* ``GetParam(string paramname)`` (returns string): The value of the specified run-time parameter.

Source files can be included in scripts using the following syntax:

	#include "relativefilen.ame"

### Forwarding requests

You can configure a rule to forward the request to an external service:

* ``"strippath": "^/myservice"``: A reqular expression that is removed from the request path when calling the external url.
* ``"forward": "https://example.com/the/real/service"``: Forwards the request to the specified url
* ``"proxy": "http://proxy:port"``: (optional) Uses the specified proxy when doing the request

#### Example

``endpoint.json``:

    {
      "name": "MyEndpoint",
      "pathregex": "^/myservice",
      "responses": [
        {
          "match": { "regex": "foobar" },
          "file": "response.xml",
          "contenttype": "text/xml"
        },
    
        {
          "match": { },
          "strippath": "^/myservice"
          "forward": "https://example.com/the/real/service",
        }
      ]
    } 

Request ``http://netmockery:NNNN/myservice/resource/foobar``:

* The first rule matches
* ``response.xml`` is returned to the client

Request ``http://netmockery:NNNN/myservice/resource/another``:

* The last rule (``"match": {}`` == any request) matches
* The request path is ``/myservice/resource/another``
* Stripping ``^/myservice`` from the request path, we get ``/resource/another``
* ``/resource/another`` is appended to the ``forward`` URL ``https://example.com/the/real/service``
* Netmockery makes a HTTP request to ``https://example.com/the/real/service/resource/another`` and returns the response to the client


### Common parameters

* ``contenttype``: Sets the mediatype part of the content-type header. Not used for the forward request response creator.
* ``charset``: Sets the charset part of the content-type header. Not used for the forward request response creator. See the section "Encodings" below
for more information.
* ``statuscode``: Sets the HTTP status code for the response (default: 200). Not used for the forward request response creator.
* ``replacements``: TODO: Document. Not used for the forward request response creator.
* ``delay``: If set, netmockery waits for the specified number of seconds before returning the response to the client.

### Defaults

Default ``contenttype`` and ``charset`` can be configured by endpoint and for the entire endpoint collection.

To set defaults for an endpoint, create a ``defaults.json`` file inside the endpoint directory (i.e. in the same directory as ``endpoint.json``).

To set global defaults, create a ``defaults.json`` file in the endpoint collection directory (i.e. in the endpoint collection root directory).

Example ``defaults.json`` file:

    {
      "contenttype": "application/xml",
      "charset": "ascii"
    } 

If ``contenttype`` and/or ``charset`` is set on an individual request creator, it will override the defaults. Defaults defined on the endpoint level 
overrides defaults on the endpoint collection level.

If no defaults are used, the default for ``charset`` is utf-8. There is no default for ``contenttype``. See also the section 
"HTTP Response encoding and the Content-Type header".


## Encodings

### Netmockery input file encoding

* All netmockery *input* files should be in UTF-8 encoding:
 * Json configuration files
 * Static file responses (via ``"file"``)
 * C# script files
 * Test expectation response files

### HTTP Response encoding and the Content-Type header

* The ``charset`` parameter determines the response encoding for netmockery responses (expect for forwarded external requests).
* If no charset parameter is specified, netmockery uses UTF-8 encoding.
* The Content-Type header for the responses is set in this manner:
 * If ``contenttype`` is NOT set, no ``Content-Type`` header is set for the responses
 * If ``contenttype`` is set to ``foo/bar`` and ``charset`` is NOT set
  1. netmockery encodes the response using the UTF-8 encoding
  2. ``Content-Type`` = ``foo/bar; charset=utf-8``
 * If ``contenttype`` is set to ``foo/bar`` and ``charset`` is set to one of the supported encodings (see list below)
  1. netmockery encodes the response using the specified encoding (eg. ``iso-8859-1``)
  2. ``Content-Type`` = ``foo/bar; charset=iso-8859-1``
* For forwarded external requests, no encoding and content-type handling is done.

### Valid charset names (not case sensitive)

    .NET Core

    US-ASCII
    ISO_8859-1:1987
    UTF-8
    ISO-10646-UCS-2
    UTF-16BE
    UTF-16LE
    UTF-16
    UTF-32
    UTF-32BE
    UTF-32LE


# Run-time adjustable parameters

An endpoint can define run-time adjustable parameters. These values can be referenced in the ``endpoint.json`` file and can be used inside scripts.

Parameter values can be changed and reset to the default value using the web UI. Adjusted values are used until the configuration is reloaded. 

## The ``params.json`` file

To define run-time adjustable parameters for an endpoint, create a ``params.json`` file in the endpoint directory.

The following example defines three parameters. Each parameter must have name, a default value and a description (the description is only used for information purposes in the Web UI).

    [
        {
            "name": "greeting",
            "default": "Hello World",
            "description": "The greeting to display"
        },

        {
            "name": "responsestatuscode",
            "default": "200",
            "description": "Status code for response"
        },

        {
            "name": "responsedelay",
            "default": "0",
            "description": "Delay for response"
        }
    ]


## Using parameter values in endpoint.json config settings

Inside the ``endpoint.json`` file, ``$parameterName`` references to the value of a run time parameter.

``$parameterName`` can only be used in response creator configuration, and not for all properties. The following table lists run-time-parameter support.

<table class="table">
    <thead>
        <tr>
            <th>Request creator property</th>
            <th>Supports run-time parameter</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td><code>literal</code></td>
            <td>Yes</td>
        </tr>
        <tr>
            <td><code>file</code></td>
            <td>Yes</td>
        </tr>
        <tr>
            <td><code>script</code></td>
            <td>Yes</td>
        </tr>
        <tr>
            <td><code>contenttype</code></td>
            <td>Yes</td>
        </tr>
        <tr>
            <td><code>delay</code></td>
            <td>Yes</td>
        </tr>
        <tr>
            <td><code>statuscode</code></td>
            <td>Yes</td>
        </tr>
    </tbody>
</table>

Note that ``statuscode`` and ``delay`` are normally integer properties. Surround in quotes inside ``endpoint.json`` if the values should be supplied by run-time adjustable parameter value.

Example ``endpoint.json`` (using the ``params.json`` from above):

    {
        "name": "Endpoint",
        "pathregex": "^/endpoint/",

        "responses": [
            {
                "match": {},
                "literal": "$greeting",

                "delay": "$responsedelay",
                "statuscode": "$responsestatuscode"
            }
        ]
    }

## Using parameter values in scripts

Inside a script file, the function ``GetParam(parameterName)`` returns the current value of a run time parameter.

## Adjusting in the web UI

For endpoints that define parameters, the web ui displays the table of values on the endpoint details page. The user can change values and reset values to the default value.


# Endpoint tests

## Writing endpoint tests
Within a endpoint directory, a ``tests`` directory with a ``tests.json`` file defines test cases for the endpoint directory.

Example ``tests.json`` file:

    [
        {
            'name': 'My first test',
            'requestpath': '/somepath/',
            
            // optional request parameters:
            //      method
            //		querystring
            //		requestbody

            // one or more test expectations:
            //		expectedrequestmatcher
            //		expectedresponsecreator
            //		expectedresponsebody
            //		expectedcontenttype
            //		expectedcharset
            //      expectedstatuscode
        },
        // More test cases
    ]

Specifying the request:

* ``name``: display name for test (required)
* ``requestpath``: request path (required)
* ``method``: HTTP method, default GET
* ``querystring``: request query string (must include leading ?)
* ``requestbody``: request body

Specifying the expectations:

* ``expectedrequestmatcher``: Display name of request matcher
* ``expectedresponsecreator``: Display name of response creator
* ``expectedresponsebody``: Expected response body contents. If specified as ``file:filename``, the expected response body is read from the specified file.
* ``expectedcontenttype``: Expected response content type
* ``expectedcharset``: Expected response charset
* ``expectedstatuscode``: Expected response status code


## Running endpoint tests

TODO: Document test modes (network and internal).

Command line:

    # run all tests
    netmockery.exe --command test --endpoints p:\ath\to\endpoint\directory

    # run single test, numeric parameter N specifies which test (first test is test 0 (zero))
    netmockery.exe --command test --endpoints p:\ath\to\endpoint\directory --only N

    # execute request specified by test N, but display respons (do not check test expectations)
    netmockery.exe --command test --endpoints p:\ath\to\endpoint\directory --only N --showresponse

## Handling time when testing

* If you have scripts that need the current date/time, do not use ``System.DateTime.Now``. 
* Instead, use the ``GetNow()`` function inside your scripts.
* When netmockery is running serving requests in the normal case, ``GetNow()`` returns ``System.DateTime.Now``.
* But when running tests, ``GetNow()`` will return the timestamp specified in the special file ``tests\now.txt``. This file should contain a single line with the time stamp
  in ``yyyy-MM-dd HH:mm:ss`` format.
* Using ``GetNow()`` / ``now.txt`` you can create stable test cases, even if your scripted service simulators return dynamic data based on current time.

# Misc

TODO: delay parameter

TODO: index.md documentation

TODO: other commands, dump


# Netmockery Documentation

* [Running netmockery](#running)
* [Configuring netmockery](#configuring)
* [Writing tests](#tests)
* [Misc](#misc)

<a name="running"></a>
# Running netmockery

Command line:

	netmockery.exe p:\ath\to\endpoint\directory

Netmockery starts and listens on ``localhost`` port ``5000``.

To bind to another address/port, use the ``--url`` command line parameter. The command below binds netmockery to all network interfaces using port 9876.

    netmockery.exe p:\ath\to\endpoint\directory --url http://*:9876

## Installing as windows service

To install:

    sc create netmockery binPath= "p:\ath\to\netmockery.exe p:\ath\to\endpoint\directory service"

If ``p:\ath\to\netmockery.exe`` or ``p:\ath\to\endpoint\directory`` contains spaces, they must be escaped using ``\"`` . Example:

    sc create netmockery binPath= "p:\ath\to\netmockery.exe \"p:\ath\to\endpoint\directory\with space\" service"

Start/stop service:

    net start netmockery
    net stop netmockery

To uninstall:

    sc delete netmockery

<a name="configuring"></a>
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

TODO: More scripting documentation.

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


### Encodings

#### Netmockery input file encoding

* All netmockery *input* files should be in UTF-8 encoding:
 * Json configuration files
 * Static file responses (via ``"file"``)
 * C# script files
 * Test expectation response files

#### HTTP Response encoding and the Content-Type header

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

#### Valid charset names (not case sensitive)

    US-ASCII
    ISO_8859-1:1987
    ISO_8859-2:1987
    ISO_8859-3:1988
    ISO_8859-4:1988
    ISO_8859-5:1988
    ISO_8859-6:1987
    ISO_8859-7:1987
    ISO_8859-8:1988
    ISO_8859-9:1989
    Shift_JIS
    Extended_UNIX_Code_Packed_Format_for_Japanese
    DIN_66003
    NS_4551-1
    SEN_850200_B
    KS_C_5601-1987
    ISO-2022-KR
    EUC-KR
    ISO-2022-JP
    GB_2312-80
    UNICODE-1-1-UTF-7
    UTF-8
    ISO-8859-13
    ISO-8859-15
    GBK
    GB18030
    ISO-10646-UCS-2
    UTF-7
    UTF-16BE
    UTF-16LE
    UTF-16
    UTF-32
    UTF-32BE
    UTF-32LE
    IBM850
    IBM862
    IBM-Thai
    GB2312
    Big5
    macintosh
    IBM037
    IBM273
    IBM277
    IBM278
    IBM280
    IBM284
    IBM285
    IBM290
    IBM297
    IBM420
    IBM423
    IBM424
    IBM437
    IBM500
    IBM852
    IBM855
    IBM857
    IBM860
    IBM861
    IBM863
    IBM864
    IBM865
    IBM869
    IBM870
    IBM871
    IBM880
    IBM905
    IBM1026
    KOI8-R
    HZ-GB-2312
    IBM866
    IBM775
    KOI8-U
    IBM00858
    IBM00924
    IBM01140
    IBM01141
    IBM01142
    IBM01143
    IBM01144
    IBM01145
    IBM01146
    IBM01147
    IBM01148
    IBM01149
    Big5-HKSCS
    windows-874
    windows-1250
    windows-1251
    windows-1252
    windows-1253
    windows-1254
    windows-1255
    windows-1256
    windows-1257
    windows-1258
    TIS-620


<a name="tests"></a>
# Writing tests

Within a endpoint directory, a ``tests`` directory with a ``tests.json`` file defines test cases for the endpoint directory.

Example ``tests.json`` file:

	[
		{
			'name': 'My first test',
			'requestpath': '/somepath/',
			
			// optional request parameters:
			//		querystring
			//		requestbody

			// one or more test expectations:
			//		expectedrequestmatcher
			//		expectedresponsecreator
			//		expectedresponsebody
		},
		// More test cases
	]

Specifying the request:

* ``name``: display name for test (required)
* ``requestpath``: request path (required)
* ``querystring``: request query string
* ``requestbody``: request body

Specifying the expectations:

* ``expectedrequestmatcher``: Display name of request matcher
* ``expectedresponsecreator``: Display name of response creator
* ``expectedresponsebody``: Expected response body contents. If specified as ``file:filename``, the expected response body is read from the specified file.
* ``expectedcontenttype``: Expected response content type
* ``expectedcharset``: Expected response charset


## Running tests

TODO: Document test modes (network and internal).

Command line:

	# run all tests
	netmockery.exe p:\ath\to\endpoint\directory test

	# run single test, numeric parameter N specifies which test (first test is test 0 (zero))
	netmockery.exe p:\ath\to\endpoint\directory test --only N

	# execute request specified by test N, but display respons (do not check test expectations)
	netmockery.exe p:\ath\to\endpoint\directory test --only N --showResponse

## Handling time when testing

* If you have scripts that need the current date/time, do not use ``System.DateTime.Now``. 
* Instead, use the ``GetNow()`` function inside your scripts.
* When netmockery is running serving requests in the normal case, ``GetNow()`` returns ``System.DateTime.Now``.
* But when running tests, ``GetNow()`` will return the timestamp specified in the special file ``tests\now.txt``. This file should contain a single line with the time stamp
  in ``yyyy-MM-dd HH:mm:ss`` format.
* Using ``GetNow()`` / ``now.txt`` you can create stable test cases, even if your scripted service simulators return dynamic data based on current time.

<a name="misc"></a>
# Misc

TODO: delay parameter

TODO: index.md documentation

TODO: other commands, dump


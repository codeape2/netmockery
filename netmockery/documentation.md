# Netmockery Documentation

* [Running netmockery](#running)
* [Configuring netmockery](#configuring)
* [Writing tests](#tests)
* [Misc](#misc)

<a name="running"></a>
# Running netmockery

Command line:

	netmockery.exe p:\ath\to\endpoint\directory

Netmockery starts and listens on port ``5000``.

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

Exactly one endpoint must match the request. If zero or more than one endpoint matches the incoming request,
netmockery writes an error message to the console output, and returns nothing to the client.

The second and final step in the request matching process is to check the incoming request against the list of rules in ``responses``. The first rule that matches
the request will be used for creating the response. If no rule matches the request, netmockery writes an error message to the console output and returns nothing to
the client.

The ``match`` paramter within the ``responses`` list can match requests using one of these methods:

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

### Common parameters

* ``contenttype``: Sets the content-type header. Not used for the forward request response creator.
* ``replacements``: TODO: Document. Not used for the forward request response creator.
* ``delay``: If set, netmockery waits for the specified number of seconds before returning the response to the client.

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
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

TODO

## Request matching

TODO

## Response creation

TODO

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
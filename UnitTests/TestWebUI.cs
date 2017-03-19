using Microsoft.Extensions.DependencyInjection;
using netmockery;
using netmockery.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public static class HashSetExtensions
    {
        public static void AddRange(this HashSet<string> hashset, IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                hashset.Add(item);
            }
        }
    }

    public class TestWebUI : WebTestBase
    {
        private ITestOutputHelper output;
        private EndpointCollectionProvider endpointCollectionProvider;
        private EndpointCollection endpointCollection;
        private TestRunner testRunner;

        public TestWebUI(ITestOutputHelper output)
        {
            this.output = output;
            endpointCollectionProvider = new EndpointCollectionProvider("examples\\example1");

            endpointCollection = endpointCollectionProvider.EndpointCollection;
            testRunner = new WebTestRunner(endpointCollection);

            CreateServerAndClient();
        }

        public override EndpointCollectionProvider GetEndpointCollectionProvider() => endpointCollectionProvider;


        [Fact]
        public async Task RootRedirectsToHome()
        {
            var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/__netmockery/Home", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task HomeRedirectsToEndpoints()
        {
            var response = await client.GetAsync("/__netmockery/Home");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/__netmockery", response.Headers.Location.ToString());
        }

        const int EXPECTED_NUMBER_OF_URLS = 25;
        const int EXPECTED_EXTRA_URLS_PER_REQUEST = 2;

        private string[] GLOBAL_URLS = new[] {
            "",
            "/Home",
            "/Responses",
            "/Responses/ErrorsOnly",
            "/Tests",
            "/Tests/RunAll",
            "/Tests/RunAllStopOnFirstError",
            "/Documentation",            
            "/Endpoints/ReloadHistory",
        };

        private string[] RELOAD_URL = new[] { "/Endpoints/ReloadConfig" };

        [Fact]
        public async Task CheckAllUrlsNoRequestsMade()
        {
            AssertSetEquals(
                CreateExpectedUrls(
                    GLOBAL_URLS, 
                    RELOAD_URL,
                    UrlsForEndpoints(),
                    UrlsForTests()
                ),
                await visitAllUrlsAsHashsetAsync("/__netmockery")
            );
        }

        IEnumerable<string> UrlsForTests()
        {
            var i = 0;
            foreach (var test in testRunner.Tests)
            {
                yield return $"/Tests/ExpectedResponseBody?index={i}";
                yield return $"/Tests/Run?index={i}";
                yield return $"/Tests/ViewResponse?index={i}";
                i++;
            }
        }

        IEnumerable<string> UrlsForEndpoints()
        {
            return (from endpoint in endpointCollection.Endpoints select UrlsForEndpoint(endpoint.Name)).SelectMany(u => u);
        }

        IEnumerable<string> UrlsForEndpoint(string endpointname)
        {
            var encoded = Uri.EscapeUriString(endpointname);
            yield return $"/Endpoints/EndpointDetails?name={encoded}";
            yield return $"/Endpoints/EndpointJsonFile?name={encoded}";
            yield return $"/Responses/ForEndpoint?endpointName={encoded}";
        }


        HashSet<string> CreateExpectedUrls(params IEnumerable<string>[] urls)
        {
            var retval = new HashSet<string>();
            foreach (var urlList in urls)
            {
                retval.AddRange(urlList);
            }
            return retval;
        }


        void AssertSetEquals(HashSet<string> expected, HashSet<string> actual)
        {
            if (! expected.SetEquals(actual))
            {
                var inExpectedNotInActual = expected.Except(actual);
                if (inExpectedNotInActual.Count() > 0)
                {
                    Assert.True(
                        false, 
                        $"In expected, not actual: {string.Join("\n", inExpectedNotInActual.Take(5))}\n" +
                        $"Expected: {string.Join("\n", expected)}\n" +
                        $"Actual: {string.Join("\n", actual)}"
                    );
                }
                
                var inActualNotInExpected = actual.Except(expected);
                if (inActualNotInExpected.Count() > 0)
                {
                    Assert.True(false, $"In actual, not expected: {string.Join("\n", inActualNotInExpected)}");
                }
                Debug.Assert(false, "Should never go here");
            }
        }


        [Fact]
        public async Task CheckAllUrlsAfterTestRequestsMade()
        {
            Assert.Equal(0, GetResponseRegistry().Responses.Count());
            Assert.True(testRunner.Tests.Count() > 0);
            foreach (var test in testRunner.Tests)
            {
                var result = await test.ExecuteAgainstHttpClientAsync(client, test.RequestPath);
                Assert.True(result.OK);
            }

            Assert.Equal(2, GetResponseRegistry().Responses.Count());
            Assert.Equal(0, GetResponseRegistry().Responses.Count(r => r.Error != null));

            AssertSetEquals(
                CreateExpectedUrls(
                    GLOBAL_URLS,
                    UrlsForEndpoints(),
                    UrlsForTests(),
                    UrlsForResponses()
                ), 
                await visitAllUrlsAsHashsetAsync("/__netmockery", includeReloadConfig: false)
            );
            
            Assert.Equal(testRunner.Tests.Count(), GetResponseRegistry().Responses.Count());

            var response1 = GetResponseRegistry().Responses.Last();
            Assert.Equal(1, response1.Id);
            Assert.Equal(testRunner.Tests.ElementAt(0).Method, response1.Method);
            Assert.Equal("GET", response1.Method);

            var response2 = GetResponseRegistry().Responses.First();
            Assert.Equal(2, response2.Id);
            Assert.Equal(testRunner.Tests.ElementAt(1).Method, response2.Method);
            Assert.Equal("POST", response2.Method);

        }

        IEnumerable<string> UrlsForResponses()
        {
            foreach (var response in GetResponseRegistry().Responses)
            {
                yield return $"/Responses/RequestDetails?responseId={response.Id}";
                yield return $"/Responses/ResponseDetails?responseId={response.Id}";
                if (! string.IsNullOrEmpty(response.RequestBody))
                {
                    yield return $"/Responses/RequestBody?responseId={response.Id}";
                }
            }
        }

        ResponseRegistry GetResponseRegistry()
        {
            return server.Host.Services.GetService<ResponseRegistry>();
        }

        [Fact]
        public async Task CheckAllUrlsAfterFailingRequests()
        {
            Assert.Equal(0, GetResponseRegistry().Responses.Count());

            var response = await client.GetAsync("/lkj/");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("", content);
            Assert.Equal(1, GetResponseRegistry().Responses.Count());
            var registryItem = GetResponseRegistry().Responses.Single();
            Assert.Equal("No endpoint matches request path", registryItem.Error);

            var urls = await visitAllUrlsAsHashsetAsync("/__netmockery", includeReloadConfig: false);
            AssertSetEquals(
                CreateExpectedUrls(
                    GLOBAL_URLS,
                    UrlsForEndpoints(),
                    UrlsForResponses(),
                    UrlsForTests()
                ),
                urls
            );
        }

        [Fact]
        public async Task CheckAllUrlsAfterNoMatchInEndpoint()
        {
            Assert.Equal(0, GetResponseRegistry().Responses.Count());

            var response = await client.GetAsync("/endpoint2");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("", content);
            Assert.Equal(1, GetResponseRegistry().Responses.Count());

            var registryItem = GetResponseRegistry().Responses.Single();
            Assert.Equal("Endpoint has no match for request", registryItem.Error);

            var urls = await visitAllUrlsAsHashsetAsync("/__netmockery", includeReloadConfig: false);

            AssertSetEquals(
                CreateExpectedUrls(
                    GLOBAL_URLS,
                    UrlsForEndpoints(),
                    UrlsForTests(),
                    UrlsForResponses()
                ),
                urls
            );
        }

        [Fact]
        public async Task EndpointsListed()
        {
            var response = await client.GetAsync("/__netmockery");
            response.EnsureSuccessStatusCode();
        }
        private async Task<List<string>> visitAllUrlsAsync(string start, bool includeReloadConfig = true)
        {
            List<string> visited = new List<string>();

            if (! includeReloadConfig)
            {
                visited.Add("/__netmockery/Endpoints/ReloadConfig");
            }

            await visitUrls(visited, start);

            if (! includeReloadConfig)
            {
                visited.Remove("/__netmockery/Endpoints/ReloadConfig");
            }

            return visited;
        }

        private async Task<HashSet<string>> visitAllUrlsAsHashsetAsync(string start, bool includeReloadConfig = true)
        {
            var allUrls = await visitAllUrlsAsync(start, includeReloadConfig);
            Debug.Assert(allUrls.All(u => u.StartsWith("/__netmockery")));
            var retval = new HashSet<string>(from u in allUrls select u.Substring("/__netmockery".Length));
            Debug.Assert(allUrls.Count == retval.Count);
            return retval;
        }

        private async Task visitUrls(List<string> visited, string url)
        {
            if (visited.Contains(url))
            {
                return;
            }

            visited.Add(url);
            output.WriteLine(url);
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                await visitUrls(visited, response.Headers.Location.ToString());
            }
            else
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var hrefs = Regex.Matches(content, "href=\"(.*?)\"");
                foreach (var hrefMatch in hrefs.Cast<Match>())
                {
                    var href = hrefMatch.Groups[1].Value;
                    if (!href.StartsWith("/"))
                    {
                        continue;
                    }
                    await visitUrls(visited, href);
                }
            }
        }


    }
}

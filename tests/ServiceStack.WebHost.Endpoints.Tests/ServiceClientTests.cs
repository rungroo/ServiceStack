using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ServiceClientTests
        : ServiceClientTestBase
    {
        /// <summary>
        /// These tests require admin privillages
        /// </summary>
        /// <returns></returns>
        public override AppHostHttpListenerBase CreateListener()
        {
            return new TestAppHostHttpListener();
        }

        private JsonServiceClient client;

        [SetUp]
        public void SetUp()
        {
            client = new JsonServiceClient(BaseUrl);
        }

        [Test]
        public void Can_GetCustomers()
        {
            var request = new GetCustomer { CustomerId = 5 };

            Send<GetCustomerResponse>(request,
                response => Assert.That(response.Customer.Id, Is.EqualTo(request.CustomerId)));
        }

        [Test]
        public void Does_add_HttpHeaders_for_Get_Sync()
        {
            client.Headers.Add("Foo", "Bar");

            var response = client.Get(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public void Does_allow_sending_Cached_Response()
        {
            var cache = new Dictionary<string, object>();

            client.ResultsFilter = (type, method, uri, request) =>
            {
                var cacheKey = "{0} {1}".Fmt(method, uri);
                Assert.That(cacheKey, Is.EqualTo("GET {0}json/reply/GetCustomer?customerId=5".Fmt(client.BaseUri)));
                object entry;
                cache.TryGetValue(cacheKey, out entry);
                return entry;
            };
            client.ResultsFilterResponse = (webRes, res, method, uri, request) =>
            {
                Assert.That(webRes, Is.Not.Null);
                var cacheKey = "{0} {1}".Fmt(method, uri);
                cache[cacheKey] = res;
            };

            var response1 = client.Get(new GetCustomer { CustomerId = 5 });
            var response2 = client.Get(new GetCustomer { CustomerId = 5 });
            Assert.That(response1.Created, Is.EqualTo(response2.Created));
        }

        [Test]
        public async Task Does_allow_sending_Cached_Response_Async()
        {
            var cache = new Dictionary<string, object>();

            client.ResultsFilter = (type, method, uri, request) =>
            {
                var cacheKey = "{0} {1}".Fmt(method, uri);
                Assert.That(cacheKey, Is.EqualTo("GET {0}json/reply/GetCustomer?customerId=5".Fmt(client.BaseUri)));
                object entry;
                cache.TryGetValue(cacheKey, out entry);
                return entry;
            };
            client.ResultsFilterResponse = (webRes, res, method, uri, request) =>
            {
                Assert.That(webRes, Is.Not.Null);
                var cacheKey = "{0} {1}".Fmt(method, uri);
                cache[cacheKey] = res;
            };

            var response1 = await client.GetAsync(new GetCustomer { CustomerId = 5 });
            var response2 = await client.GetAsync(new GetCustomer { CustomerId = 5 });
            Assert.That(response1.Created, Is.EqualTo(response2.Created));
        }

        [Test]
        public async Task Does_add_HttpHeaders_for_Get_Async()
        {
            client.Headers.Add("Foo", "Bar");

            var response = await client.GetAsync(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Does_add_HttpHeaders_in_RequestFilter_for_Get_Async()
        {
            client.RequestFilter = req => req.Headers.Add("Foo", "Bar");

            var response = await client.GetAsync(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Can_call_return_void()
        {
            client.Post(new ReturnsVoid { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Foo"));

            await client.PostAsync(new ReturnsVoid { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Foo"));

            using (client.Post<HttpWebResponse>(new ReturnsVoid { Message = "Bar" })) { }
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Can_call_return_HttpWebResponse()
        {
            client.Post(new ReturnsWebResponse { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Foo"));

            await client.PostAsync(new ReturnsWebResponse { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Foo"));

            using (client.Post<HttpWebResponse>(new ReturnsWebResponse { Message = "Bar" })) { }
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Bar"));
        }

        [Test]
        public void Can_post_raw_response_as_raw_JSON()
        {
            var request = new GetCustomer { CustomerId = 5 };
            var response = client.Post(request);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            var requestPath = request.ToPostUrl();

            string json = request.ToJson();
            response = client.Post<GetCustomerResponse>(requestPath, json);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            byte[] bytes = json.ToUtf8Bytes();
            response = client.Put<GetCustomerResponse>(requestPath, bytes);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            Stream ms = new MemoryStream(bytes);
            response = client.Post<GetCustomerResponse>(requestPath, ms);
            Assert.That(response.Customer.Id, Is.EqualTo(5));
        }

        [Test]
        public async Task Can_post_raw_response_as_raw_JSON_async()
        {
            var request = new GetCustomer { CustomerId = 5 };
            var response = client.Post(request);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            var requestPath = request.ToPostUrl();

            string json = request.ToJson();
            response = await client.PostAsync<GetCustomerResponse>(requestPath, json);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            byte[] bytes = json.ToUtf8Bytes();
            response = await client.PutAsync<GetCustomerResponse>(requestPath, bytes);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            Stream ms = new MemoryStream(bytes);
            response = await client.PostAsync<GetCustomerResponse>(requestPath, ms);
            Assert.That(response.Customer.Id, Is.EqualTo(5));
        }

        [Test]
        public async Task Can_post_raw_response_as_raw_JSON_HttpClient()
        {
            var httpClient = new JsonHttpClient(BaseUrl);
            var request = new GetCustomer { CustomerId = 5 };
            var response = httpClient.Post(request);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            var requestPath = request.ToPostUrl();

            string json = request.ToJson();
            response = await httpClient.PostAsync<GetCustomerResponse>(requestPath, json);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            byte[] bytes = json.ToUtf8Bytes();
            response = await httpClient.PutAsync<GetCustomerResponse>(requestPath, bytes);
            Assert.That(response.Customer.Id, Is.EqualTo(5));

            Stream ms = new MemoryStream(bytes);
            response = await httpClient.PostAsync<GetCustomerResponse>(requestPath, ms);
            Assert.That(response.Customer.Id, Is.EqualTo(5));
        }

        [Test]
        public void Can_WaitAsync()
        {
            var called = 0;

            PclExportClient.Instance.WaitAsync(100)
                .ContinueWith(_ => {
                    called++;
                });

            Thread.Sleep(200);
            Assert.That(called, Is.EqualTo(1));
        }
    }

}
using Moq;
using Moq.Protected;
using Assessment.Services;
using Newtonsoft.Json.Linq;
using System.Net;
using Assessment.Business;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1
{
    public class Tests
    {
        private string ordersData;
        private OrdersAPIService ordersAPIService;
        private HttpClient mockHttpClient;
        private ILogger<OrdersAPIService> logger;

        [SetUp]
        public void Setup()
        {

            ordersData = @"[
                {
                    'OrderId':'1234',
                    'Items': [
                    {
                        'Description':'Test1',
                        'Status':'Delivered',
                        'deliveryNotification':0,
                    },
                    {
                        'Description':'Test2',
                        'Status':'Waiting',
                        'deliveryNotification':0,
                    },
                    {
                        'Description':'Test3',
                        'Status':'Delivered',
                        'deliveryNotification':0,
                    }
                    ]
                },
                {
                    'OrderId':'5678',
                    'Items': [
                    {
                        'Description':'Test4',
                        'Status':'Waiting',
                        'deliveryNotification':0,
                    },
                    {
                        'Description':'Test5',
                        'Status':'Waiting',
                        'deliveryNotification':0,
                    },
                    {
                        'Description':'Test6',
                        'Status':'Delivered',
                        'deliveryNotification':0,
                    },
                    {
                        'Description':'Test7',
                        'Status':'Delivered',
                        'deliveryNotification':0,
                    }
                    ]
                }
            ]";


            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            HttpResponseMessage httpResponseMessage = new()
            {
                Content = new StringContent(ordersData)
            };

            // Set up the SendAsync method behavior.
            httpMessageHandlerMock
                .Protected() // <= this is most important part that it need to setup.
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = httpResponseMessage.Content
                });

            // create the HttpClient
            mockHttpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new System.Uri("http://localhost") // It should be in valid uri format.
            };

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddDebug())
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();
            logger = factory.CreateLogger<OrdersAPIService>();

        }

        [TearDown]
        public void TearDown()
        {
            mockHttpClient.Dispose();
        }


        /// <summary>
        /// Test the full order processing
        /// </summary>
        [Test]
        public async Task FullTest()
        {

            ordersAPIService = new(mockHttpClient, logger);
            OrderProcessing orderProcessing = new(ordersAPIService);
            //Act
            var medicalEquipmentOrders = await ordersAPIService.FetchMedicalEquipmentOrders();
            //Assert
            Assert.That(medicalEquipmentOrders.Length, Is.EqualTo(2));
            foreach (var order in medicalEquipmentOrders)
            {
                var updatedOrder =orderProcessing.ProcessOrder(order);
                ordersAPIService.SendAlertAndUpdateOrder(updatedOrder).GetAwaiter().GetResult();
            }
            var firstOne = medicalEquipmentOrders[0]["Items"]?[0]?["deliveryNotification"]?.Value<int>();
            var notDelivered = medicalEquipmentOrders[0]["Items"]?[1]?["deliveryNotification"]?.Value<int>();
            Assert.That(firstOne, Is.EqualTo(1));
            Assert.That(notDelivered, Is.EqualTo(0));
        }


        /// <summary>
        /// Test the fetch medical equipment orders
        /// </summary>
        [Test]
        public async Task TestFetchMedicalEquipmentOrders()
        {
            OrdersAPIService ordersAPIService = new(mockHttpClient, logger);
            //Act
            var result = await ordersAPIService.FetchMedicalEquipmentOrders();
            //Assert
            Assert.That(result.Length, Is.EqualTo(2));
        }


        /// <summary>
        /// Test the send alert message
        /// </summary>
        [Test]
        public void TestSendAlertMessage()
        {
            OrdersAPIService ordersAPIService = new(mockHttpClient, logger);
            //Act
            ordersAPIService.SendAlertMessage(JToken.Parse("{'Description':'Test','Status':'Delivered','deliveryNotification':0}"), "1234");
            //Assert
            //No exception thrown
        }

        /// <summary>
        /// Test the send alert and update order
        /// </summary>
        [Test]
        public async Task TestSendAlertAndUpdateOrder()
        {

            OrdersAPIService ordersAPIService = new(mockHttpClient, logger);
            var order = JObject.Parse("{'OrderId':'1234','Items':[{'Description':'Test','Status':'Delivered','deliveryNotification':0}]}");
            //Act
            await ordersAPIService.SendAlertAndUpdateOrder(order);
            //Assert
            //No exception thrown
        }
    }
}
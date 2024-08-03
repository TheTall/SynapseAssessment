using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Assessment.Services;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Assessment.Business;
using Newtonsoft.Json.Linq;
using NLog.Extensions.Logging;

namespace Synapse.OrdersExample
{
    /// <summary>
    /// I Get a list of orders from the API
    /// I check if the order is in a delviered state, If yes then send a delivery alert and add one to deliveryNotification
    /// I then update the order.   
    /// </summary>
    class Program
    {
        private static  ILogger _logger;
        private static IServiceProvider _servicesProv;
        static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(System.IO.Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();


            _servicesProv= SetupDI(config, args);
            _logger = _servicesProv.GetRequiredService<ILogger<Program>>();


            _logger.LogInformation("Start of App");

            // get the API service
            IOrdersAPIService apiService = _servicesProv.GetRequiredService<IOrdersAPIService>();

            OrderProcessing orderProcessing = new OrderProcessing(apiService);

            // get orders from API
            JObject[] medicalEquipmentOrders;
            try
            {
                medicalEquipmentOrders = apiService.FetchMedicalEquipmentOrders().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders from API.");
                return 1;
            }

            // process orders
            try
            {
                foreach (var order in medicalEquipmentOrders)
                {
                    var updatedOrder = orderProcessing.ProcessOrder(order);
                    apiService.SendAlertAndUpdateOrder(updatedOrder).GetAwaiter().GetResult();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to process orders.");
                return 1;
            }

            _logger.LogInformation("Results sent to relevant APIs.");
			return 0;
        }



        private static IServiceProvider SetupDI(IConfiguration config, string[] args)
        {
            var services = new ServiceCollection();
            
            services.AddTransient<IOrdersAPIService, OrdersAPIService>();

            services.AddHttpClient();

            services.AddLogging(loggingBuilder =>
            {
                // configure Logging with NLog
                loggingBuilder.AddNLog(config);
            });
            return services.BuildServiceProvider();
        }



    }
}
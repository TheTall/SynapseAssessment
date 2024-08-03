using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Assessment.Services
{

    public class OrdersAPIService : IOrdersAPIService
    {
        private readonly HttpClient _httpClient;
        private ILogger<OrdersAPIService> _logger;

        private const string OrdersBaseApiUrl = "https://orders-api.com/";

        public OrdersAPIService(HttpClient httpClient, ILogger<OrdersAPIService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(OrdersBaseApiUrl);
            _logger = logger;
        }

        /// <summary>
        /// Fetch medical equipment orders
        /// </summary>
        /// <returns></returns>
        public async Task<JObject[]> FetchMedicalEquipmentOrders()
        {
            string ordersApiUrl = $"{OrdersBaseApiUrl}orders";
            var response = await _httpClient.GetAsync(ordersApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var ordersData = await response.Content.ReadAsStringAsync();
                //var orders = JsonSerializer.Deserialize<Order[]>(ordersData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var orders = JArray.Parse(ordersData).ToObject<JObject[]>();
                return orders;
            }
            else
            {
                _logger.LogError($"Failed to fetch orders from {ordersApiUrl}. Status code: {response.StatusCode}");
                return null;
            }
        }

        /// <summary>
        /// Delivery alert
        /// </summary>
        /// <param name="orderId">The order id for the alert</param>
        public void SendAlertMessage(JToken item, string orderId)
        {
            string alertApiUrl = "https://alert-api.com/alerts";
            var alertData = new
            {
                Message = $"Alert for delivered item: Order {orderId}, Item: {item["Description"]}, " +
                          $"Delivery Notifications: {item["deliveryNotification"]}"
            };
            var content = new StringContent(JObject.FromObject(alertData).ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = _httpClient.PostAsync(alertApiUrl, content).Result;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Alert sent for delivered item: {item["Description"]}");
            }
            else
            {
                Console.WriteLine($"Failed to send alert for delivered item: {item["Description"]}");
                _logger.LogError($"Failed to send alert for delivered item: {item["Description"]}");
            }
        }

        public async Task SendAlertAndUpdateOrder(JObject order)
        {
            string updateApiUrl = "https://update-api.com/update";
            var content = new StringContent(order.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(updateApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Updated order sent for processing: OrderId {order["OrderId"]}");
            }
            else
            {
                _logger.LogError($"Failed to send updated order for processing: OrderId {order["OrderId"]}");
            }
        }

    }
}

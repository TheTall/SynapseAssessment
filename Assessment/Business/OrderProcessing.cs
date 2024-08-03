using Assessment.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assessment.Business
{
    public class OrderProcessing
    {
        private IOrdersAPIService _apiService;

        public OrderProcessing(IOrdersAPIService apiService)
        {
            _apiService = apiService;
        }

        public JObject ProcessOrder(JObject order)
        {
            var items = order["Items"].ToObject<JArray>();
            foreach (var item in items)
            {
                if (IsItemDelivered(item))
                {
                    _apiService.SendAlertMessage(item, order["OrderId"].ToString());
                    IncrementDeliveryNotification(item);
                }
            }
            order["Items"] = items;
            return order;
        }

        public bool IsItemDelivered(JToken item)
        {
            return item["Status"].ToString().Equals("Delivered", StringComparison.OrdinalIgnoreCase);
        }


        public void IncrementDeliveryNotification(JToken item)
        {
            item["deliveryNotification"] = item["deliveryNotification"].Value<int>() + 1;
        }


    }
}

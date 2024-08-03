using Newtonsoft.Json.Linq;

namespace Assessment.Services
{
    public interface IOrdersAPIService
    {
        Task<JObject[]> FetchMedicalEquipmentOrders();
        void SendAlertMessage(JToken item, string orderId);
        Task SendAlertAndUpdateOrder(JObject order);
    }
}

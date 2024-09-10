using Microsoft.AspNetCore.Mvc;
using RestaurantBookingFrontend.Models;
using System.Text.Json;

namespace RestaurantBookingFrontend.Controllers
{
    public class MenuController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUri = "https://localhost:7251";

        public MenuController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index()
        {

            // Call api
            var response = await _httpClient.GetAsync($"{_baseUri}/api/Dish/GetAvailableDishes");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            // Make response into a readable string
            var jsonString = await response.Content.ReadAsStreamAsync();

            // Make it so the serializer ignore casing
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            // Deserialize into an object
            var dishes = await JsonSerializer.DeserializeAsync<IEnumerable<Dish>>(jsonString, jsonOptions);

            return View(dishes);
        }
    }
}

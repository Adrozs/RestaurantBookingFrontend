using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingFrontend.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantBookingFrontend.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUri = "https://localhost:7251";

        public AdminController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }



        public async Task<IActionResult> GetReservation(int reservationId)
        {
            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetReservationById?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var jsonString = await response.Content.ReadAsStreamAsync();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var reservation = await JsonSerializer.DeserializeAsync<ReservationById>(jsonString, jsonOptions);

            return View(reservation);
        }

        public async Task<IActionResult> AllReservations()
        {
            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetAllReservations");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var jsonString = await response.Content.ReadAsStreamAsync();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            var reservations = await JsonSerializer.DeserializeAsync<IEnumerable<Reservation>>(jsonString, jsonOptions);
            
            return View(reservations);
        }


        [HttpPost("Admin/EditReservation")]
        public async Task<IActionResult> EditReservation(ReservationById reservationById)
            {
            if(!ModelState.IsValid)
                return View(reservationById);


            // Create a new reservation object as we don't need ordered dishes or customer name to update the reservation
            var reservation = new Reservation
            {
                Id = reservationById.Id,
                ReservationTime = reservationById.ReservationTime,
                ReservationDurationMinutes = reservationById.ReservationDurationMinutes,
                Guests = reservationById.Guests,
                TotalBill = reservationById.TotalBill,
                TableId = reservationById.TableId,
                CustomerId = reservationById.CustomerId,
            };
            
            var json = JsonSerializer.Serialize(reservation);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PutAsync($"{_baseUri}/api/Reservation/UpdateReservation", content);

            return RedirectToAction("getReservation", new { reservationId = reservationById.Id });
        }

        // Made its own method as it required another model than the getbyid method for more data
        [HttpGet]
        public async Task<IActionResult> EditReservation(int reservationId)
        {
            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetReservationById?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var jsonString = await response.Content.ReadAsStreamAsync();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var reservation = await JsonSerializer.DeserializeAsync<ReservationById>(jsonString, jsonOptions);
                
            return View(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReservation(int reservationId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUri}/api/Reservation/DeleteReservation?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            return RedirectToAction("Index");
        }

        [HttpGet("Admin/EditMenu")]
        public async Task<IActionResult> EditMenu()
        {
            var response = await _httpClient.GetAsync($"{_baseUri}/api/Dish/GetAllDishes");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var jsonString = await response.Content.ReadAsStreamAsync();

            var dishes = await JsonSerializer.DeserializeAsync<IEnumerable<Dish>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(dishes);
        }

        [HttpGet]
        public async Task<IActionResult> EditMenuItem()
        {
            if (TempData["EditDish"] != null)
            {
                var dishJson = TempData["EditDish"]?.ToString();
                var dish = Newtonsoft.Json.JsonConvert.DeserializeObject<Dish>(dishJson);


                Console.WriteLine("IsAvailable after deserialization: " + dish.IsAvailable);


                if (dish != null)
                    return View(dish);
            }

            return RedirectToAction("EditMenu");
        }

        [HttpPost]
        public async Task<IActionResult> EditMenuItem(int dishId)
        {
            return RedirectToAction("EditMenu");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMenuItem(int dishId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUri}/api/Dish/DeleteDish?dishId={dishId}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            return RedirectToAction("EditMenu");
        }
    }
}

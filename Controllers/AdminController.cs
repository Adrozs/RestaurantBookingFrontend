using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingFrontend.Models;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace RestaurantBookingFrontend.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUri = "https://localhost:7251";
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public AdminController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        //[Authorize]
        public IActionResult Index()
        {
            // Return to login if not logged in
            if(HttpContext.Session.GetString("IsLoggedIn") == "false")
                return RedirectToAction("Login", "Admin");

            var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");
            ViewBag.IsLoggedIn = isLoggedIn;

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // Return to main admin page if logged in (no need to login twice)
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
                return RedirectToAction("Index", "Admin");

            return View();
        }

        public IActionResult Logout()
        {
            // "Remove" jwt by making the contents an empty string and setting the expiration date in the past = not valid
            Response.Cookies.Append("jwt", "", new CookieOptions
            {
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Set all logged in states to false
            HttpContext.Session.SetString("IsLoggedIn", "false");
            ViewBag.IsLoggedIn = "false";

            // Set jwt in a cookie
            var sessionCookieOptions = new CookieOptions
            {
                Path = "/",
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("JWT", "", sessionCookieOptions);

            return RedirectToAction("Login", "Admin");
        }


        [AllowAnonymous]
        [HttpPost("Admin/VerifyLogin")]
        public async Task<IActionResult> VerifyLogin(LoginModel loginModel)
        {
            // Validate login
            if (!ModelState.IsValid)
                return View("Login", loginModel);
                
            // Create user object object
            var user = new {username = loginModel.AdminUsername, password = loginModel.AdminPassword};

            // Serialize
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Verify login
            var response = await _httpClient.PostAsync($"{_baseUri}/api/User/AdminLogin", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", $"Failed to login: {response.StatusCode} - {response.ReasonPhrase}");
                return View("Login", loginModel);
            }


            // Deserialize json to jwt
            var jsonString = await response.Content.ReadAsStreamAsync();
            var jsonOptions = _jsonOptions;
            var jwt = await JsonSerializer.DeserializeAsync<Jwt>(jsonString, jsonOptions);

            if (jwt.Token == null)
            {
                ModelState.AddModelError("", $"Failed to login: An unexpected error occurred.");
                return View("Login", loginModel);
            }

            // Set jwt in a cookie
            var sessionCookieOptions = new CookieOptions
            {
                Path = "/",
                Expires = DateTime.Now.AddHours(1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("JWT", jwt.Token, sessionCookieOptions);


            // Set logged in status to true to keep user logged in across the admin pages 
            // Needed as frontend/backend is separated and we can't set [Authorize] on the pages - jwt only verifies access to endpoints
            HttpContext.Session.SetString("IsLoggedIn", "true");

            return RedirectToAction("Index", "Admin");
        }

        public async Task<IActionResult> GetReservation(int reservationId)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") == "false")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetReservationById?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                RedirectToAction("Login", "Admin");

            var jsonString = await response.Content.ReadAsStreamAsync();

            var jsonOptions = _jsonOptions;


            var reservation = await JsonSerializer.DeserializeAsync<ReservationById>(jsonString, jsonOptions);

            return View(reservation);
        }

        public async Task<IActionResult> AllReservations()
        {
            // Return to login if not logged in
            //if (HttpContext.Session.GetString("IsLoggedIn") == "false")
            //    return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetAllReservations");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Login", "Admin");

            var jsonString = await response.Content.ReadAsStreamAsync();
            
            var reservations = await JsonSerializer.DeserializeAsync<IEnumerable<Reservation>>(jsonString, _jsonOptions);
            
            return View(reservations);
        }


        [HttpPost("Admin/EditReservation")]
        public async Task<IActionResult> EditReservation(ReservationById reservationById)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") == "false")
                return RedirectToAction("Login", "Admin");


            if (!ModelState.IsValid)
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

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            await _httpClient.PutAsync($"{_baseUri}/api/Reservation/UpdateReservation", content);

            return RedirectToAction("getReservation", new { reservationId = reservationById.Id });
        }

        // Made its own method as it required another model than the getbyid method for more data
        [HttpGet]
        public async Task<IActionResult> EditReservation(int reservationId)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") == "false")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.GetAsync($"{_baseUri}/api/Reservation/GetReservationById?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                RedirectToAction("Login", "Admin");

            var jsonString = await response.Content.ReadAsStreamAsync();

            var reservation = await JsonSerializer.DeserializeAsync<ReservationById>(jsonString, _jsonOptions);
                
            return View(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReservation(int reservationId)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.DeleteAsync($"{_baseUri}/api/Reservation/DeleteReservation?resId={reservationId}");

            if (!response.IsSuccessStatusCode)
                RedirectToAction("Login", "Admin");

            return RedirectToAction("Index");
        }

        [HttpGet("Admin/EditMenu")]
        public async Task<IActionResult> EditMenu()
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.GetAsync($"{_baseUri}/api/Dish/GetAllDishes");

            if (!response.IsSuccessStatusCode)
                RedirectToAction("Login", "Admin");

            var jsonString = await response.Content.ReadAsStreamAsync();

            var dishes = await JsonSerializer.DeserializeAsync<IEnumerable<Dish>>(jsonString, _jsonOptions);

            return View(dishes);
        }

        [HttpGet]
        public async Task<IActionResult> EditMenuItem(int dishId)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.GetAsync($"{_baseUri}/api/Dish/GetDishById?dishId={dishId}");

            var jsonString = await response.Content.ReadAsStreamAsync();

            var dish = await JsonSerializer.DeserializeAsync<Dish>(jsonString, _jsonOptions);

            if(!response.IsSuccessStatusCode)
                return RedirectToAction("EditMenu");

            return View(dish);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMenuItem(Dish dish)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            var json = JsonSerializer.Serialize(dish);

            Console.WriteLine(json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            await _httpClient.PutAsync($"{_baseUri}/api/Dish/UpdateDish", content);

            return RedirectToAction("EditMenu");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMenuItem(int dishId)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.DeleteAsync($"{_baseUri}/api/Dish/DeleteDish?dishId={dishId}");

            if (!response.IsSuccessStatusCode)
                RedirectToAction("Login", "Admin");

            return RedirectToAction("EditMenu");
        }

        public async Task<IActionResult> AddMenuItem()
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") == "false")
                return RedirectToAction("Login", "Admin");

            var dish = new Dish();

            return View(dish);
        }

        [HttpPost]
        public async Task<IActionResult> AddMenuItem(Dish dish)
        {
            // Return to login if not logged in
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Admin");

            var json = JsonSerializer.Serialize(dish);

            var content = new StringContent (json, Encoding.UTF8, "application/json");

            // Get jwt from cookie and include it in the auth header for the api call
            string? jwt = Request.Cookies["JWT"];
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            await _httpClient.PostAsync($"{_baseUri}/api/Dish/CreateDish", content);

            return RedirectToAction("EditMenu");
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingFrontend.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string AdminUsername { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string AdminPassword { get; set; }
    }
}

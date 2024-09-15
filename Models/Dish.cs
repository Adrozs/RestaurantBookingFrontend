namespace RestaurantBookingFrontend.Models
{
    public class Dish
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        //public string ImgUrl { get; set; } need to setup in backend to be able to save
    }
}

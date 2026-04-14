using Plugin.CloudFirestore.Attributes;

namespace VinhKhanhstreet.Models
{
    public class UserModel
    {
        [Id]
        public string DocumentId { get; set; }

        [MapTo("username")]
        public string Username { get; set; }

        [MapTo("password")]
        public string Password { get; set; }

        [MapTo("restaurantId")]
        public string RestaurantId { get; set; } // Link to DocumentId of PoiModel
    }
}

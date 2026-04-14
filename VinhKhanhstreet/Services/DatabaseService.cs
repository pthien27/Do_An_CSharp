using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using VinhKhanhstreet.Models;
using Plugin.CloudFirestore;
using System;
using Microsoft.Maui.ApplicationModel;

namespace VinhKhanhstreet.Services
{
    public class DatabaseService
    {
        private readonly ICollectionReference _collection;
        private readonly ICollectionReference _userCollection;

        public DatabaseService()
        {
            // Kết nối tới collection "restaurants" trên Firebase
            _collection = CrossCloudFirestore.Current.Instance.GetCollection("restaurants");
            _userCollection = CrossCloudFirestore.Current.Instance.GetCollection("users");
        }

        public async Task InitAsync()
        {
            try {
                var query = await _collection.GetDocumentsAsync();
                
                if (query.IsEmpty)
                {
                    await SeedDataToCloudAsync();
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => App.Current.MainPage.DisplayAlert("Lỗi Init", ex.Message, "OK"));
            }
        }

        private async Task SeedDataToCloudAsync()
        {
            var defaultPois = new List<PoiModel>
            {
                new PoiModel {
                    DocumentId = "R01",
                    Name = "Ốc Oanh",
                    Latitude = 10.761204237032537, Longitude = 106.703307923906,
                    Radius = 30,
                    Rating = 4.4, ReviewCount = 649, ClosingTime = "23:00",
                    PhoneNumber = "0901111222", CategoryVi = "Quán ốc",
                    Description = "Chào mừng bạn đến với Ốc Oanh. Đây là địa điểm ẩm thực không thể bỏ qua tại Quận 4.",
                    DescriptionEn = "Welcome to Oc Oanh. This is a must-visit dining spot in District 4.",
                    DescriptionJa = "Oc Oanhへようこそ. ここは4区で必見のグルメスポットです。",
                    DescriptionZh = "欢迎来到Oc Oanh。这是位于第四区的必吃美食景点。",
                    ImageUrl1 = "ocoanh_1.jpg", ImageUrl2 = "ocoanh_2.jpg", ImageUrl3 = "ocoanh_3.jpg", ImageUrl4 = "ocoanh_4.jpg", ImageUrl5 = "ocoanh_5.jpg"
                },
                new PoiModel {
                    DocumentId = "R02",
                    Name = "Ốc Vũ",
                    Latitude = 10.761613292334369, Longitude = 106.702715423906,
                    Radius = 30,
                    Rating = 4.2, ReviewCount = 420, ClosingTime = "22:30",
                    PhoneNumber = "0903333444", CategoryVi = "Quán ốc",
                    Description = "Bạn đang đứng trước Ốc Vũ. Quán nổi tiếng với món sò dương nướng mỡ hành.",
                    DescriptionEn = "You are standing in front of Oc Vu. The restaurant is famous for its grilled cockles with onion fat.",
                    DescriptionJa = "あなたはOc Vuの前に立っています。このレストランはザル貝のネギ油焼きで有名です。",
                    DescriptionZh = "您正站在Oc Vu前。这家餐厅以葱油烤蚶闻名。",
                    ImageUrl1 = "ocvu_1.jpg", ImageUrl2 = "ocvu_2.jpg", ImageUrl3 = "ocvu_3.jpg", ImageUrl4 = "ocvu_4.jpg", ImageUrl5 = "ocvu_5.jpg"
                },
                new PoiModel {
                    DocumentId = "R03",
                    Name = "Bánh flan Ngọc Nga",
                    Latitude = 10.76120604173355, Longitude = 106.70273643655044,
                    Radius = 50,
                    Rating = 4.8, ReviewCount = 356, ClosingTime = "23:00",
                    PhoneNumber = "0909000111", CategoryVi = "Tráng miệng",
                    Description = "Quán bánh flan Ngọc Nga nổi tiếng với hương vị béo mịn, thơm ngon đặc trưng, là món tráng miệng lý tưởng sau khi thưởng thức hải sản.",
                    DescriptionEn = "Ngoc Nga Flan is famous for its smooth, creamy, and uniquely delicious flavor, making it a perfect dessert after seafood.",
                    DescriptionJa = "Ngoc Ngaのフラン（プリン）は、滑らかでクリーミーな味わいが特徴で、海鮮料理の後のデザートに最適です。",
                    DescriptionZh = "Ngoc Nga 焦糖布丁以其顺滑、细腻和独特美味而闻名，是享用海鲜后的理想甜点。",
                    ImageUrl1 = "ngocnga_1.jpg", ImageUrl2 = "ngocnga_2.jpg", ImageUrl3 = "ngocnga_3.jpg", ImageUrl4 = "ngocnga_4.jpg", ImageUrl5 = "ngocnga_5.jpg"
                },
                new PoiModel {
                    DocumentId = "R04",
                    Name = "Quán Ốc Thảo",
                    Latitude = 10.76173, Longitude = 106.70237,
                    Radius = 40,
                    Rating = 3.8, ReviewCount = 645, ClosingTime = "23:59",
                    PhoneNumber = "0899546789", CategoryVi = "Quán ốc",
                    Description = "Quán Ốc Thảo trên đường Vĩnh Khánh là địa điểm quen thuộc của tín đồ ẩm thực Sài Gòn với các món ốc tươi ngon, đậm đà hương vị.",
                    DescriptionEn = "Oc Thao Seafood on Vinh Khanh street is a familiar spot for Saigon foodies, offering fresh and flavorful snail dishes.",
                    DescriptionJa = "ビンカン通りにあるオク・タオは、新鮮で風味豊かな巻貝料理が楽しめる、サイゴンの美食家にはおなじみの場所です。",
                    DescriptionZh = "位于永庆街的Oc Thao海鲜是西贡美食爱好者的熟悉地点。",
                    ImageUrl1 = "octhao_1.jpg", ImageUrl2 = "octhao_2.jpg", ImageUrl3 = "octhao_3.jpg", ImageUrl4 = "octhao_4.jpg", ImageUrl5 = "octhao_5.jpg"
                }
            };

            foreach (var poi in defaultPois)
            {
                var poiDict = new Dictionary<string, object>
                {
                    { "name", poi.Name },
                    { "Latitude", poi.Latitude },
                    { "Longitude", poi.Longitude },
                    { "Radius", poi.Radius },
                    { "Rating", poi.Rating },
                    { "ReviewCount", poi.ReviewCount },
                    { "ClosingTime", poi.ClosingTime },
                    { "PhoneNumber", poi.PhoneNumber },
                    { "CategoryVi", poi.CategoryVi },
                    { "DescriptionVI", poi.Description },
                    { "DescriptionEN", poi.DescriptionEn },
                    { "DescriptionJA", poi.DescriptionJa },
                    { "DescriptionZH", poi.DescriptionZh },
                    { "ImageUrl1", poi.ImageUrl1 },
                    { "ImageUrl2", poi.ImageUrl2 },
                    { "ImageUrl3", poi.ImageUrl3 },
                    { "ImageUrl4", poi.ImageUrl4 },
                    { "ImageUrl5", poi.ImageUrl5 }
                };
                
                await _collection.GetDocument(poi.DocumentId).SetDataAsync(poiDict);
            }
        }

        public async Task<List<PoiModel>> GetPoisAsync()
        {
            try {
                // LUÔN LUÔN RE-SEED ĐỂ KIỂM SOÁT DỮ LIỆU CHUẨN!
                await SeedDataToCloudAsync();

                var query = await _collection.GetDocumentsAsync();
                return query.ToObjects<PoiModel>().ToList();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => App.Current.MainPage.DisplayAlert("Lỗi GetPois", ex.Message, "OK"));
                return new List<PoiModel>();
            }
        }

        public async Task UpdatePoiAsync(PoiModel poi)
        {
            try {
                await _collection.GetDocument(poi.DocumentId).UpdateDataAsync(poi);
            }
            catch (Exception ex) {
                MainThread.BeginInvokeOnMainThread(() => App.Current.MainPage.DisplayAlert("Lỗi Update", ex.Message, "OK"));
            }
        }

        // --- CÁC HÀM MỚI CHO ĐĂNG KÝ CHỦ QUÁN ---

        public async Task<bool> IsUsernameDuplicateAsync(string username)
        {
            var query = await _userCollection.WhereEqualsTo("username", username).GetDocumentsAsync();
            return !query.IsEmpty;
        }

        public async Task<bool> IsRestaurantDuplicateAsync(string name, double lat, double lon)
        {
            // Kiểm tra trùng tên
            var nameQuery = await _collection.WhereEqualsTo("name", name).GetDocumentsAsync();
            if (!nameQuery.IsEmpty) return true;

            // Kiểm tra trùng tọa độ (xấp xỉ 10-20m)
            var all = await GetPoisAsync();
            return all.Any(p => Math.Abs(p.Latitude - lat) < 0.0002 && Math.Abs(p.Longitude - lon) < 0.0002);
        }

        public async Task FinalizeOwnerRegistrationAsync(UserModel user, PoiModel poi)
        {
            // 1. Tạo ID duy nhất cho Quán và User
            string resId = "R" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string userId = "U" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // 2. Chuyển POI thành Dictionary để Firebase không tự ý quét các trường ngày tháng gây lỗi
            var poiData = new Dictionary<string, object>
            {
                { "name", poi.Name },
                { "Latitude", poi.Latitude },
                { "Longitude", poi.Longitude },
                { "Radius", poi.Radius },
                { "Rating", 4.5 },
                { "ReviewCount", 0 },
                { "ClosingTime", poi.ClosingTime },
                { "CategoryVi", poi.CategoryVi },
                { "DescriptionVI", poi.Description },
                { "DescriptionEN", poi.DescriptionEn },
                { "DescriptionJA", poi.DescriptionJa },
                { "DescriptionZH", poi.DescriptionZh },
                { "ImageUrl1", poi.ImageUrl1 },
                { "ImageUrl2", poi.ImageUrl2 },
                { "ImageUrl3", poi.ImageUrl3 },
                { "ImageUrl4", poi.ImageUrl4 },
                { "ImageUrl5", poi.ImageUrl5 },
                { "StrStatusOpen", poi.StrStatusOpen }
            };

            // 3. Chuyển User thành Dictionary
            var userData = new Dictionary<string, object>
            {
                { "username", user.Username },
                { "password", user.Password },
                { "restaurantId", resId }
            };

            // 4. Lưu trực tiếp dữ liệu dạng Map lên Firestore
            await _collection.GetDocument(resId).SetDataAsync(poiData);
            await _userCollection.GetDocument(userId).SetDataAsync(userData);
        }
    }
}

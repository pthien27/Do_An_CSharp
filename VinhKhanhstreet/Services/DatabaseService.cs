using SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using VinhKhanhstreet.Models;
using Microsoft.Maui.Storage;

namespace VinhKhanhstreet.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        public DatabaseService()
        {
        }

        public async Task InitAsync()
        {
            if (_db != null)
                return;

            // Đặt file DB vào vùng nhớ chuẩn của điện thoại (không lo mất quyền truy cập)
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "VinhKhanh.db3");
            _db = new SQLiteAsyncConnection(databasePath);

            // Tự động quét Model và tạo Bảng có các cột tương ứng
            await _db.CreateTableAsync<PoiModel>();

            // Kiểm tra: Nếu bảng trống thì rải dữ liệu mẫu vào (Chỉ chạy 1 lần đầu)
            var count = await _db.Table<PoiModel>().CountAsync();
            if (count == 0)
            {
                var defaultPois = new List<PoiModel>
                {
                    new PoiModel {
                        Name = "Ốc Oanh",
                        Latitude = 10.7588, Longitude = 106.7052,
                        Radius = 30,
                        Rating = 4.4, ReviewCount = 649, ClosingTime = "23:00",
                        PhoneNumber = "0901111222", CategoryVi = "Quán ốc",
                        Description = "Chào mừng bạn đến với Ốc Oanh. Đây là địa điểm ẩm thực không thể bỏ qua tại Quận 4.",
                        DescriptionEn = "Welcome to Oc Oanh. This is a must-visit dining spot in District 4.",
                        DescriptionJa = "Oc Oanhへようこそ。ここは4区で必見の食事スポットです。",
                        DescriptionZh = "欢迎来到Oc Oanh。这是位于第四区的必吃美食景点。",
                        
                        // 5 Slot hình ảnh (Điền đúng tên ảnh đã copy vào thư mục Images)
                        ImageUrl1 = "ocoanh_1.jpg",
                        ImageUrl2 = "ocoanh_2.jpg",
                        ImageUrl3 = "ocoanh_3.jpg",
                        ImageUrl4 = "ocoanh_4.jpg",
                        ImageUrl5 = "ocoanh_5.jpg"
                    },
                    new PoiModel {
                        Name = "Ốc Vũ",
                        Latitude = 10.7585, Longitude = 106.7055,
                        Radius = 30,
                        Rating = 4.2, ReviewCount = 420, ClosingTime = "22:30",
                        PhoneNumber = "0903333444", CategoryVi = "Quán ốc",
                        Description = "Bạn đang đứng trước Ốc Vũ. Quán nổi tiếng với món sò dương nướng mỡ hành.",
                        DescriptionEn = "You are standing in front of Oc Vu. The restaurant is famous for its grilled cockles with onion fat.",
                        DescriptionJa = "あなたはOc Vuの前に立っています。このレストランはザル貝のネギ油焼きで有名です。",
                        DescriptionZh = "您正站在Oc Vu前。这家餐厅以葱油烤蚶闻名。",
                        
                        // Nếu quán có 3 hình, khai báo 3 dòng rồi xóa 2 dòng kia cũng được
                        ImageUrl1 = "ocvu_1.jpg",
                        ImageUrl2 = "ocvu_2.jpg",
                        ImageUrl3 = "ocvu_3.jpg",
                        ImageUrl4 = "ocvu_4.jpg", // Để trống nghĩa là không có (Code tự ẩn)
                        ImageUrl5 = "ocvu_5.jpg"
                    },
                    new PoiModel {
                        Name = "Điểm Test",
                        Latitude = 10.75865, Longitude = 106.70535,
                        Radius = 50,
                        Rating = 5.0, ReviewCount = 10, ClosingTime = "23:59",
                        PhoneNumber = "18001008", CategoryVi = "Hệ thống",
                        Description = "Đây là Điểm Test tự động từ hệ thống.",
                        DescriptionEn = "This is an automatic Test Point from the system.",
                        DescriptionJa = "これはシステムからの自動テストポイントです。",
                        DescriptionZh = "这是来自系统的自动测试点。",
                        
                        ImageUrl1 = "diemtest_1.png", // Bạn có thể xài file .png
                        ImageUrl2 = "",
                        ImageUrl3 = "",
                        ImageUrl4 = "",
                        ImageUrl5 = ""
                    },
                    new PoiModel {
                        Name = "Quán Ốc Thảo",
                        Latitude = 10.76173, Longitude = 106.70237,
                        Radius = 40,
                        Rating = 3.8, ReviewCount = 645, ClosingTime = "23:59",
                        PhoneNumber = "0899546789", CategoryVi = "Quán ốc",
                        Description = "Quán Ốc Thảo trên đường Vĩnh Khánh là địa điểm quen thuộc của tín đồ ẩm thực Sài Gòn, nổi tiếng với các món ốc tươi ngon, đậm đà hương vị và giá cả bình dân. Không gian quán bình dân, nhộn nhịp, mang đậm nét văn hóa ăn uống đường phố đặc trưng của khu ẩm thực Vĩnh Khánh.",
                        DescriptionEn = "Oc Thao Seafood on Vinh Khanh street is a familiar spot for Saigon foodies, famous for its fresh, flavorful snail dishes at affordable prices. The casual, bustling atmosphere perfectly captures the local street food culture.",
                        DescriptionJa = "ビンカン通りにあるオク・タオは、新鮮で風味豊かな巻貝料理を手頃な価格で楽しめることで有名な、サイゴンの美食家にとっておなじみのスポットです。カジュアルで活気のある雰囲気は、地元の屋台文化を完璧に表現しています。",
                        DescriptionZh = "位于永庆街的Oc Thao海鲜是西贡美食家的熟悉地点，以其价格实惠、新鲜美味的贝类菜肴而闻名。热闹休闲的氛围完美地体现了独特的当地街头小吃文化。",
                        
                        ImageUrl1 = "octhao_1.jpg",
                        ImageUrl2 = "octhao_2.jpg",
                        ImageUrl3 = "octhao_3.jpg",
                        ImageUrl4 = "octhao_4.jpg",
                        ImageUrl5 = "octhao_5.jpg"
                    }
                };

                await _db.InsertAllAsync(defaultPois);
            }
        }

        // Kéo toàn bộ danh sách Quán từ file đĩa lên
        public async Task<List<PoiModel>> GetPoisAsync()
        {
            await InitAsync();
            return await _db.Table<PoiModel>().ToListAsync();
        }

        // Cập nhật Database khi có một dữ liệu thay đổi (ví dụ: bấm nút Thả Tym)
        public async Task<int> UpdatePoiAsync(PoiModel poi)
        {
            await InitAsync();
            return await _db.UpdateAsync(poi);
        }
    }
}

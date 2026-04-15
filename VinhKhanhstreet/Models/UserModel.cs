using System;

namespace VinhKhanhstreet.Models
{
    public class UserModel
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsOnline { get; set; } = false;
        public int LoginCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Constructor mặc định cho Firestore
        public UserModel() { }

        public UserModel(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
        }

        public double LastLatitude { get; set; }
        public double LastLongitude { get; set; }
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;

namespace HoneyBot.Models
{
    public class Student
    {
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; } // سنخزن كلمة المرور مشفرة دائمًا
    }
}

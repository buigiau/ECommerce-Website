using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class LoginVM
    {
        [Display(Name ="Tên Đăng Nhập")]
        [Required(ErrorMessage ="Chưa nhập tên đăng nhập")]
        [MaxLength(20, ErrorMessage ="Tối đa 20 kí tự")]
        public string UserName { get; set; }

        [Display(Name ="Mật Khẩu")]
        [Required(ErrorMessage ="Chưa nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

using System.Text;

namespace ECommerceMVC.Helpers
{
    public class MyUtil
    {

        public static string UploadHinh(IFormFile Hinh, string folder)
        {
            try
            {
                var fullpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", folder, Hinh.FileName);
                using (var myfile = new FileStream(fullpath, FileMode.CreateNew))
                {
                    Hinh.CopyTo(myfile);
                }
                return Hinh.FileName;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public static string GenerateRandomKey(int lenghth = 5)
        {
            var pattern = @"qwertyuiopasdfghjklZXCVBNMLKJHGFDSAPOIUY!";
            var rd = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < lenghth; i++)
            {
                sb.Append(pattern[rd.Next(0, pattern.Length)]);
            }
            return sb.ToString();
        }
    }
}

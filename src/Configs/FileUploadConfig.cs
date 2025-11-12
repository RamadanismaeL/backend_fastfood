using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Configs
{
    public class FileUploadConfig
    {
        public static async Task<string> UploadFile(IFormFile file)
        {
            if (file is null || file.Length == 0) throw new ArgumentException("Invalid file or empty");

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var filePath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }
    }
}
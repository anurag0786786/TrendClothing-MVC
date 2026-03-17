// Areas/Admin/Controllers/BulkUploadController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;
using System.Text;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BulkUploadController : Controller
    {
        private readonly IUnitofWork _unitOfWork;
        private readonly CloudinaryService _cloudinary;

        public BulkUploadController(IUnitofWork unitOfWork, CloudinaryService cloudinary)
        {
            _unitOfWork = unitOfWork;
            _cloudinary = cloudinary;
        }

        // ── INDEX ──
        public IActionResult Index() => View();

        // ── DOWNLOAD TEMPLATE ──
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var categories = _unitOfWork.category.GetAll().Select(c => c.Name).ToList();
            var brands = _unitOfWork.brand.GetAll().Select(b => b.Name).ToList();
            var productTypes = _unitOfWork.productType.GetAll().Select(t => t.Name).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Name *,Description *,Price *,Discount Price,Category *,Brand *,Product Type *,Image Filename,Is Active");

            var exCat = categories.FirstOrDefault() ?? "Men";
            var exBrand = brands.FirstOrDefault() ?? "YourBrand";
            var exType = productTypes.FirstOrDefault() ?? "T-Shirt";

            sb.AppendLine($"Example Product,Product description yahan likhna,999,799,{exCat},{exBrand},{exType},product1.jpg,TRUE");
            sb.AppendLine($"Another Product,Another description,1299,,{exCat},{exBrand},{exType},product2.jpg,TRUE");
            sb.AppendLine(",,,,,,,,");
            sb.AppendLine($"# Valid Categories: {string.Join(" | ", categories)},,,,,,,,");
            sb.AppendLine($"# Valid Brands: {string.Join(" | ", brands)},,,,,,,,");
            sb.AppendLine($"# Valid Product Types: {string.Join(" | ", productTypes)},,,,,,,,");
            sb.AppendLine("# Image Filename: CSV ke saath jo images upload karo unka naam likhna,,,,,,,,");
            sb.AppendLine("# Lines starting with # are ignored,,,,,,,,");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "TrendClothing_BulkUpload_Template.csv");
        }

        // ── UPLOAD CSV + IMAGES ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile csvFile, List<IFormFile> images)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["BulkError"] = "CSV file select karo pehle!";
                return RedirectToAction(nameof(Index));
            }

            if (!Path.GetExtension(csvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["BulkError"] = "Sirf CSV file allowed hai (.csv)";
                return RedirectToAction(nameof(Index));
            }

            // Images dictionary — filename -> IFormFile
            var imageDict = new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);
            if (images != null)
                foreach (var img in images)
                    if (img != null && img.Length > 0)
                        imageDict[img.FileName] = img;

            var categories = _unitOfWork.category.GetAll().ToList();
            var brands = _unitOfWork.brand.GetAll().ToList();
            var productTypes = _unitOfWork.productType.GetAll().ToList();

            var successRows = new List<string>();
            var errorRows = new List<string>();
            int rowNum = 1;

            using var reader = new StreamReader(csvFile.OpenReadStream());
            await reader.ReadLineAsync(); // header skip

            while (!reader.EndOfStream)
            {
                rowNum++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.TrimStart().StartsWith("#")) continue;

                var cols = ParseCsvLine(line);
                if (cols.Length < 7)
                {
                    errorRows.Add($"Row {rowNum}: Columns kam hain ({cols.Length} mili, 7+ chahiye)");
                    continue;
                }

                var name = cols[0].Trim();
                var description = cols[1].Trim();
                var priceStr = cols[2].Trim();
                var discStr = cols[3].Trim();
                var categoryStr = cols[4].Trim();
                var brandStr = cols[5].Trim();
                var typeStr = cols[6].Trim();
                var imageFilename = cols.Length > 7 ? cols[7].Trim() : "";
                var isActiveStr = cols.Length > 8 ? cols[8].Trim() : "TRUE";

                var rowErrors = new List<string>();

                if (string.IsNullOrEmpty(name)) rowErrors.Add("Name missing");
                if (string.IsNullOrEmpty(description)) rowErrors.Add("Description missing");

                if (!double.TryParse(priceStr, out double price) || price <= 0)
                    rowErrors.Add($"Price invalid: '{priceStr}'");

                double? discountPrice = null;
                if (!string.IsNullOrEmpty(discStr))
                {
                    if (!double.TryParse(discStr, out double dp))
                        rowErrors.Add($"Discount Price invalid: '{discStr}'");
                    else if (dp >= price)
                        rowErrors.Add($"Discount ({dp}) must be less than Price ({price})");
                    else
                        discountPrice = dp;
                }

                var category = categories.FirstOrDefault(c =>
                    c.Name.Equals(categoryStr, StringComparison.OrdinalIgnoreCase));
                if (category == null) rowErrors.Add($"Category '{categoryStr}' not found");

                var brand = brands.FirstOrDefault(b =>
                    b.Name.Equals(brandStr, StringComparison.OrdinalIgnoreCase));
                if (brand == null) rowErrors.Add($"Brand '{brandStr}' not found");

                var productType = productTypes.FirstOrDefault(t =>
                    t.Name.Equals(typeStr, StringComparison.OrdinalIgnoreCase));
                if (productType == null) rowErrors.Add($"Product Type '{typeStr}' not found");

                if (rowErrors.Any())
                {
                    errorRows.Add($"Row {rowNum} ({name}): {string.Join(" | ", rowErrors)}");
                    continue;
                }

                string? imageUrl = null;
                if (!string.IsNullOrEmpty(imageFilename) && imageDict.ContainsKey(imageFilename))
                {
                    try
                    {
                        imageUrl = await _cloudinary.UploadImageAsync(imageDict[imageFilename]);
                    }
                    catch (Exception ex)
                    {
                        errorRows.Add($"Row {rowNum} ({name}): Image upload fail — {ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(imageFilename))
                {
                    errorRows.Add($"Row {rowNum} ({name}): Image '{imageFilename}' select nahi ki (product bina image save hua)");
                }

                bool isActive = !isActiveStr.Equals("FALSE", StringComparison.OrdinalIgnoreCase);

                _unitOfWork.product.Add(new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    DiscountPrice = discountPrice,
                    CategoryId = category!.Id,
                    BrandId = brand!.Id,
                    ProductTypeId = productType!.Id,
                    ImageUrl = imageUrl,
                    IsActive = isActive
                });

                var imgStatus = imageUrl != null ? "✅ with image" : "⚠️ no image";
                successRows.Add($"Row {rowNum}: '{name}' added {imgStatus}");
            }

            if (successRows.Any()) _unitOfWork.Save();

            TempData["BulkSuccess"] = string.Join("||", successRows);
            TempData["BulkErrors"] = string.Join("||", errorRows);
            TempData["BulkTotal"] = rowNum - 1;

            return RedirectToAction(nameof(Result));
        }

        // ── RESULT ──
        public IActionResult Result() => View();

        // ── BULK IMAGE UPDATE PAGE ──
        public IActionResult UpdateImages() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateImages(List<IFormFile> images, bool overwriteExisting = false)
        {
            if (images == null || !images.Any())
            {
                TempData["ImgError"] = "Koi image select nahi ki!";
                return RedirectToAction(nameof(UpdateImages));
            }

            var allProducts = _unitOfWork.product.GetAll().ToList();
            int matched = 0, skipped = 0, failed = 0;

            foreach (var img in images)
            {
                if (img == null || img.Length == 0) continue;

                var fileNameNoExt = Path.GetFileNameWithoutExtension(img.FileName).Trim();

                // Flexible match — spaces, underscores, dashes
                var nameVariants = new[]
                {
                    fileNameNoExt,
                    fileNameNoExt.Replace("_", " "),
                    fileNameNoExt.Replace("-", " "),
                };

                var product = allProducts.FirstOrDefault(p =>
                    nameVariants.Any(v => p.Name.Equals(v, StringComparison.OrdinalIgnoreCase)));

                if (product == null) { skipped++; continue; }

                if (!string.IsNullOrEmpty(product.ImageUrl) && !overwriteExisting)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var url = await _cloudinary.UploadImageAsync(img);
                    if (url != null)
                    {
                        product.ImageUrl = url;
                        _unitOfWork.product.Update(product);
                        matched++;
                    }
                }
                catch { failed++; }
            }

            if (matched > 0) _unitOfWork.Save();

            TempData["ImgSuccess"] = $"✅ {matched} images updated | ⏭️ {skipped} skipped | ❌ {failed} failed";
            return RedirectToAction(nameof(UpdateImages));
        }

        // ── CSV PARSER ──
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
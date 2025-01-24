using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tesseract;

namespace ApiPdfImageReader.Controllers
{
    [Route("api/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        // Endpoint para subir y extraer texto de un archivo PDF
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // Verifica si se recibió un archivo
            if (file == null)
            {
                return BadRequest(new { message = "No se proporcionó ningún archivo o está vacío." });
            }

            if (IsPdf(file))
            {
                return await ExtraePDF(file);
            }

            if (IsImage(file))
            {
                return await ExtraeImagen(file);
            }

            return BadRequest(new { message = "El archivo no es de formato PDF o Imagen." });
        }

        private async Task<IActionResult> ExtraePDF(IFormFile file)
        {
            try
            {
                // Guarda el archivo en un Stream temporal
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Asegúrate de que el Stream esté al principio

                // Extrae el texto del PDF usando iText7
                var extractedText = ExtractTextFromPdf(memoryStream);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return BadRequest("El archivo no contiene texto legible.");
                }

                // Devuelve el texto extraído en formato JSON
                return Ok(new { text = extractedText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error al procesar el archivo.", error = ex.Message });
            }
        }

        private async Task<IActionResult> ExtraeImagen(IFormFile file)
        {
            try
            {
                // Guarda el archivo en un Stream temporal
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                string path = Path.Combine(Directory.GetCurrentDirectory(), "Tesseract", "tessdata");

                var engine = new TesseractEngine(path, "eng");
                var image = Pix.LoadFromMemory(memoryStream.ToArray());
                var page = engine.Process(image);

                var text = page.GetText();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return BadRequest("El archivo no contiene texto legible.");
                }

                // Devuelve el texto extraído en formato JSON
                return Ok(new { text = text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error al procesar el archivo.", error = ex.Message });
            }
        }


        // Método auxiliar para extraer texto de un PDF usando iText7
        private static string ExtractTextFromPdf(Stream pdfStream)
        {
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var textBuilder = new System.Text.StringBuilder();

            // Itera por todas las páginas del documento y extrae el texto
            foreach (var pageNumber in Enumerable.Range(1, pdfDocument.GetNumberOfPages()))
            {
                var page = pdfDocument.GetPage(pageNumber);
                var textFromPage = PdfTextExtractor.GetTextFromPage(page);
                textBuilder.Append(textFromPage);
            }

            return textBuilder.ToString();
        }

        private bool IsImage(IFormFile file)
        {
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedImageExtensions.Contains(extension);
        }

        // Método para verificar si es un PDF
        private bool IsPdf(IFormFile file)
        {
            var allowedPdfExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedPdfExtensions.Contains(extension);
        }
    }
}

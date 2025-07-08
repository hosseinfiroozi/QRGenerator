using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using QRCoder;

class Program
{
    static async Task<int> Main()
    {
        Console.Write("OperationCode: ");
        string? operationCode = Console.ReadLine()?.Trim();
        Console.Write("ProductionStationCode: ");
        string? productionStationCode = Console.ReadLine()?.Trim();
        Console.Write("Count: ");
        string? countInput = Console.ReadLine()?.Trim();
        Console.Write("LotNo: ");
        string? lotNo = Console.ReadLine()?.Trim();
        Console.Write("PreLot: ");
        string? preLot = Console.ReadLine()?.Trim();

        if (!int.TryParse(countInput, out int count) || count <= 0)
        {
            Console.WriteLine("Count must be a positive integer.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(operationCode) || string.IsNullOrWhiteSpace(productionStationCode))
        {
            Console.WriteLine("OperationCode and ProductionStationCode cannot be empty.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(lotNo) || string.IsNullOrWhiteSpace(preLot))
        {
            Console.WriteLine("LotNo and PreLot cannot be empty.");
            return 1;
        }

        string url = $"http://ts.icdgroup.org:84/api/Production-Cards/get-new?OperationCode={Uri.EscapeDataString(operationCode)}&ProductionStationCode={Uri.EscapeDataString(productionStationCode)}&Count={count}";
        using var http = new HttpClient();
        try
        {
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed with status code {response.StatusCode}");
                return 1;
            }
            var data = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(data))
            {
                Console.WriteLine("API returned no data.");
                return 1;
            }
            string[] barcodes = data.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (barcodes.Length == 0)
            {
                Console.WriteLine("No barcodes returned.");
                return 1;
            }

            var images = new List<Bitmap>();
            using var generator = new QRCodeGenerator();
            for (int i = 0; i < barcodes.Length; i++)
            {
                string barcode = barcodes[i];
                var qrData = generator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new BitmapByteQRCode(qrData);
                byte[] imageBytes = qrCode.GetGraphic(20);
                using var ms = new MemoryStream(imageBytes);
                var bitmap = new Bitmap(ms);
                images.Add(bitmap);

                int topHeight = 30;
                int bottomHeight = 20;
                int tot = bitmap.Height + topHeight + bottomHeight;
                var cell = new Bitmap(bitmap.Width, tot);
                
                using (var cg = Graphics.FromImage(cell))
                {
                    cg.Clear(Color.White);
                    using var font = new Font("Arial", 10);
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    cg.DrawString($"LotNo: {lotNo}, PreLot: {preLot}", font, Brushes.Black, new RectangleF(0, 0, cell.Width, topHeight), format);
                    cg.DrawImage(bitmap, 0, topHeight);
                    cg.DrawString((i + 1).ToString(), font, Brushes.Black, new RectangleF(0, topHeight + bitmap.Height, cell.Width, bottomHeight), format);
                }
                images.Add(cell);
            }

            int columns = 4;
            int rows = (int)Math.Ceiling(images.Count / (double)columns);
            int width = images[0].Width;
            int height = images[0].Height;
            int headerHeight = 40;
            using var result = new Bitmap(columns * width, rows * height + headerHeight);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                using var font = new Font("Arial", 16);
                string header = $"LotNo: {lotNo}, PreLot: {preLot}";
                var textSize = g.MeasureString(header, font);
                g.DrawString(header, font, Brushes.Black, (result.Width - textSize.Width) / 2, 5);
                for (int i = 0; i < images.Count; i++)
                {
                    int r = i / columns;
                    int c = i % columns;
                    g.DrawImage(images[i], c * width, r * height + headerHeight);
                }
            }

            string output = "output.png";
            result.Save(output, ImageFormat.Png);
            Console.WriteLine($"Image saved to {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return 1;
        }
    }
}

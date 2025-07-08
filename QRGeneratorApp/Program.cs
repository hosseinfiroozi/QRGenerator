using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
            foreach (var barcode in barcodes)
            {
                var qrData = generator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                images.Add(qrCode.GetGraphic(20));
            }

            int columns = 4;
            int rows = (int)Math.Ceiling(images.Count / (double)columns);
            int width = images[0].Width;
            int height = images[0].Height;
            using var result = new Bitmap(columns * width, rows * height);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                for (int i = 0; i < images.Count; i++)
                {
                    int r = i / columns;
                    int c = i % columns;
                    g.DrawImage(images[i], c * width, r * height);
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

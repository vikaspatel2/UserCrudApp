using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using UserCrudApp.Helpers;

public class QrCodeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(string qrdata)
    {
        if (string.IsNullOrEmpty(qrdata) || qrdata.Length > 17)
        {
            ViewBag.QR = null;
            ViewBag.Text = "Please enter up to 17 characters (Version 1 QR limit)";
            return View();
        }
        var bits = MyQrEncoder.EncodeByteMode(qrdata);
        var dataWords = MyQrEncoder.BitsToBytes(bits); // 19 bytes
        var ecWords = ReedSolomonEncoder.Encode(dataWords, 7); // 7 ECC bytes
        byte[] codewords = new byte[26];
        Array.Copy(dataWords, 0, codewords, 0, 19);
        Array.Copy(ecWords, 0, codewords, 19, 7);

        bool[,] matrix = MyQrEncoder.MakeMatrix(codewords);

        int size = matrix.GetLength(0), scale = 10, border = 4;
        using var bmp = new Bitmap((size + border * 2) * scale, (size + border * 2) * scale);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (matrix[x, y])
                        g.FillRectangle(Brushes.Black, (x + border) * scale, (y + border) * scale, scale, scale);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        string base64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        ViewBag.QR = base64;
        ViewBag.Text = qrdata;
        return View();
    }
}
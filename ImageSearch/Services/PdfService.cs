using ImageSearchRobot.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ImageSearch.Services;

public class PdfService
{
    public void CreatePdf(SearchResult result, string outputPath)
    {
        //определяем размер страницы под каждое изображение
        PageSize screenshotPageSize = GetPageSize(result.ScreenshotPath);
        PageSize imagePageSize = GetPageSize(result.ImagePath);

        Document.Create(container =>
        {
            //скриншот браузера
            container.Page(page =>
            {
                page.Size(screenshotPageSize);
                page.Margin(0);
                page.Content().Image(result.ScreenshotPath).FitArea();
            });

            //найденное изображение
            container.Page(page =>
            {
                page.Size(imagePageSize);
                page.Margin(0);
                page.Content().Image(result.ImagePath).FitArea();
            });
        })
        .GeneratePdf(outputPath);
    }

    //альбомная А4 для широких изображений, книжная для высоких
    private static PageSize GetPageSize(string imagePath)
    {
        (int width, int height) = ReadImageSize(imagePath);

        return width >= height
            ? PageSizes.A4.Landscape()
            : PageSizes.A4;
    }

    //считывает размеры png или jpeg из заголовка файла
    private static (int Width, int Height) ReadImageSize(string path)
    {
        using FileStream fs = File.OpenRead(path);
        using BinaryReader br = new(fs);

        byte[] header = br.ReadBytes(24);

        //png
        if (header.Length >= 24 &&
            header[0] == 0x89 && header[1] == 0x50 &&
            header[2] == 0x4E && header[3] == 0x47)
        {
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return (width, height);
        }

        //jpeg
        fs.Position = 0;
        if (br.ReadByte() == 0xFF && br.ReadByte() == 0xD8)
        {
            while (fs.Position < fs.Length)
            {
                if (br.ReadByte() != 0xFF) continue;

                byte marker = br.ReadByte();

                //маркер содержит размеры изображения
                if (marker == 0xC0 || marker == 0xC2)
                {
                    fs.Position += 3;
                    int height = (br.ReadByte() << 8) | br.ReadByte();
                    int width = (br.ReadByte() << 8) | br.ReadByte();
                    return (width, height);
                }

                int segmentLength = (br.ReadByte() << 8) | br.ReadByte();
                fs.Position += segmentLength - 2;
            }
        }

        //по умолчанию считаем альбомной
        return (1, 1);
    }
}
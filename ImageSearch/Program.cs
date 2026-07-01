using System.Text;
using ImageSearch.Services;
using ImageSearchRobot;
using ImageSearchRobot.Models;
using ImageSearchRobot.Services;
using QuestPDF.Infrastructure;

Console.OutputEncoding = Encoding.UTF8;

QuestPDF.Settings.License = LicenseType.Community;

Console.Write("Введите поисковый запрос: ");
string? input = Console.ReadLine();

//проверка на пустой запрос
if (string.IsNullOrWhiteSpace(input))
{
    Console.WriteLine("Ошибка: запрос не может быть пустым.");
    return;
}

string query = input.Trim();

//проверка максимальной длины запроса
if (query.Length > AppSettings.MaxQueryLength)
{
    Console.WriteLine($"Ошибка: запрос длиннее {AppSettings.MaxQueryLength} символов.");
    return;
}

try
{
    Console.WriteLine("Запускаю браузер...");

    SearchResult result;

    //поиск изображения
    using (BrowserService browser = new())
    {
        result = await browser.SearchAsync(query);
    }

    Console.WriteLine("Скриншот получен. Изображение скачано. Создаю PDF...");

    FileNameService fileNames = new();
    PdfService pdf = new();

    //формируем путь для сохранения PDF
    string desktop = fileNames.GetDesktopPath();
    string pdfPath = fileNames.GetUniquePath(
        desktop,
        AppSettings.PdfBaseName,
        AppSettings.PdfExtension);

    //создаем PDF-документ
    pdf.CreatePdf(result, pdfPath);

    Console.WriteLine($"Готово. Файл сохранён: {pdfPath}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Ошибка: нет прав на запись файла на рабочий стол.");
}
catch (IOException ex)
{
    Console.WriteLine($"Ошибка файловой системы: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Непредвиденная ошибка: {ex.Message}");
}
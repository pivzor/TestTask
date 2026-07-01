namespace ImageSearchRobot;

public static class AppSettings
{
    //Google Images
    public const string GoogleImagesUrl = "https://www.google.com/imghp?hl=ru";

    //время ожидания
    public const int BrowserWaitSeconds = 30;

    //пауза после поиска
    public const int SearchResultPause = 3;

    //pdf
    public const string PdfBaseName = "SearchResult";
    public const string PdfExtension = ".pdf";

    //временные файлы
    public const string ScreenshotFileName = "screenshot.png";
    public const string FoundImageFileName = "found_image.png";

    //ограничение длины запроса
    public const int MaxQueryLength = 200;
}
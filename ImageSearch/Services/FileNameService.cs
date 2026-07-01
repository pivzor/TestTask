namespace ImageSearchRobot.Services;

public class FileNameService
{
    //добавляет (1), (2) и тд, если файл уже существует
    public string GetUniquePath(string directory, string baseName, string extension)
    {
        string path = Path.Combine(directory, baseName + extension);

        if (!File.Exists(path))
            return path;

        int index = 1;

        while (true)
        {
            path = Path.Combine(directory, $"{baseName} ({index}){extension}");

            if (!File.Exists(path))
                return path;

            index++;
        }
    }

    //возвращает путь к рабочему столу пользователя
    public string GetDesktopPath()
        => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
}
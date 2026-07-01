using System;
using System.Collections.Generic;
using System.Text;

namespace ImageSearchRobot.Models;

public class SearchResult
{
    //запрос пользователя
    public string SearchText { get; set; }

    //путь к скриншоту браузера
    public string ScreenshotPath { get; set; }

    //путь к скачанной картинке
    public string ImagePath { get; set; }
}

using System;
using System.IO;
using System.Reflection;
using PdfSharp.Fonts;

namespace TrackSync.Functions
{
    public class CustomFontResolver : IFontResolver
    {
        // Имя шрифта по умолчанию
        public string DefaultFontName => "Verdana";

        // Метод для преобразования имени шрифта
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Приводим к нижнему регистру для сравнения
            string name = familyName.ToLower();

            // Определяем какой шрифт использовать
            string fontName;

            if (name.Contains("arial"))
                fontName = "Arial";
            else if (name.Contains("times") || name.Contains("new roman"))
                fontName = "Verdana";
            else if (name.Contains("verdana"))
                fontName = "Verdana";
            else if (name.Contains("tahoma"))
                fontName = "Tahoma";
            else
                fontName = DefaultFontName; // По умолчанию

            // Добавляем модификаторы для жирного/курсива
            if (isBold && isItalic)
                return new FontResolverInfo(fontName + "#bi");
            else if (isBold)
                return new FontResolverInfo(fontName + "#b");
            else if (isItalic)
                return new FontResolverInfo(fontName + "#i");
            else
                return new FontResolverInfo(fontName);
        }

        // Метод для получения данных шрифта
        public byte[] GetFont(string faceName)
        {
            try
            {
                // Определяем какой файл шрифта нужен
                string fontFileName;

                if (faceName.StartsWith("Arial"))
                {
                    if (faceName.EndsWith("#bi"))
                        fontFileName = "arialbi.ttf"; // Arial Bold Italic
                    else if (faceName.EndsWith("#b"))
                        fontFileName = "arialbd.ttf";  // Arial Bold
                    else if (faceName.EndsWith("#i"))
                        fontFileName = "ariali.ttf";   // Arial Italic
                    else
                        fontFileName = "arial.ttf";    // Arial Regular
                }
                else if (faceName.StartsWith("Verdana"))
                {
                    if (faceName.EndsWith("#bi"))
                        fontFileName = "timesbi.ttf";  // Times New Roman Bold Italic
                    else if (faceName.EndsWith("#b"))
                        fontFileName = "timesbd.ttf";  // Times New Roman Bold
                    else if (faceName.EndsWith("#i"))
                        fontFileName = "timesi.ttf";   // Times New Roman Italic
                    else
                        fontFileName = "times.ttf";    // Times New Roman Regular
                }
                else
                {
                    fontFileName = "verdana.ttf";
                }

                // Пробуем найти шрифт в системной папке Fonts
                string systemFontPath = GetSystemFontPath(fontFileName);
                if (File.Exists(systemFontPath))
                {
                    return File.ReadAllBytes(systemFontPath);
                }

                // Если нет в системе, пробуем найти во встроенных ресурсах
                return GetEmbeddedFont(fontFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки шрифта {faceName}: {ex.Message}");
                return null;
            }
        }

        // Метод для получения пути к системному шрифту
        private string GetSystemFontPath(string fontFileName)
        {
            // Стандартные пути к папке шрифтов в Windows
            string[] possiblePaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontFileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", fontFileName),
                Path.Combine("C:\\Windows\\Fonts", fontFileName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        // Метод для получения шрифта из встроенных ресурсов
        private byte[] GetEmbeddedFont(string fontFileName)
        {
            try
            {
                // Путь к ресурсу (предполагаем, что шрифты лежат в папке Fonts проекта)
                string resourceName = $"TrackSync.Fonts.{fontFileName}";

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        return buffer;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки встроенного шрифта {fontFileName}: {ex.Message}");
            }

            return null;
        }
    }
}
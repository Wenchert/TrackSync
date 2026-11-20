using System;
using System.IO;
using System.Reflection;
using PdfSharp.Fonts;

namespace TrackSync.Functions
{
    public class CustomFontResolver : IFontResolver
    {
        public string DefaultFontName => "Verdana";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string name = familyName.ToLower();

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
                fontName = DefaultFontName; 

            if (isBold && isItalic)
                return new FontResolverInfo(fontName + "#bi");
            else if (isBold)
                return new FontResolverInfo(fontName + "#b");
            else if (isItalic)
                return new FontResolverInfo(fontName + "#i");
            else
                return new FontResolverInfo(fontName);
        }
        public byte[] GetFont(string faceName)
        {
            try
            {
                string fontFileName;

                if (faceName.StartsWith("Arial"))
                {
                    if (faceName.EndsWith("#bi"))
                        fontFileName = "arialbi.ttf"; 
                    else if (faceName.EndsWith("#b"))
                        fontFileName = "arialbd.ttf";  
                    else if (faceName.EndsWith("#i"))
                        fontFileName = "ariali.ttf";   
                    else
                        fontFileName = "arial.ttf";    
                }
                else if (faceName.StartsWith("Verdana"))
                {
                    if (faceName.EndsWith("#bi"))
                        fontFileName = "timesbi.ttf";  
                    else if (faceName.EndsWith("#b"))
                        fontFileName = "timesbd.ttf";  
                    else if (faceName.EndsWith("#i"))
                        fontFileName = "timesi.ttf";   
                    else
                        fontFileName = "times.ttf";
                }
                else
                {
                    fontFileName = "verdana.ttf";
                }

                string systemFontPath = GetSystemFontPath(fontFileName);
                if (File.Exists(systemFontPath))
                {
                    return File.ReadAllBytes(systemFontPath);
                }

                return GetEmbeddedFont(fontFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки шрифта {faceName}: {ex.Message}");
                return null;
            }
        }

        private string GetSystemFontPath(string fontFileName)
        {
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

        private byte[] GetEmbeddedFont(string fontFileName)
        {
            try
            {
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using TrackSync.DataBase;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Fonts;

namespace TrackSync.Functions
{
    public class HikeRoomPdfGenerator
    {
        private readonly int _roomId;
        private readonly XImage _mapImage;
        private readonly TrackSyncContext _context;

        // Статический конструктор для инициализации FontResolver
        static HikeRoomPdfGenerator()
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new CustomFontResolver();
            }
        }

        public HikeRoomPdfGenerator(int roomId, RenderTargetBitmap mapImage)
        {
            _roomId = roomId;
            _context = new TrackSyncContext();

            // Конвертируем RenderTargetBitmap в XImage для PdfSharp
            if (mapImage != null)
            {
                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(mapImage));

                using (var stream = new System.IO.MemoryStream())
                {
                    bitmapEncoder.Save(stream);
                    _mapImage = XImage.FromStream(stream);
                }
            }
        }

        public void GeneratePdf(string filePath)
        {
            try
            {
                // Создаем PDF-документ
                var pdfDocument = new PdfDocument();
                pdfDocument.Info.Title = $"Отчет по походу - Комната {_roomId}";
                pdfDocument.Info.Author = "TrackSync";
                pdfDocument.Info.CreationDate = DateTime.Now;

                // Страница 1: Карта с легендой (альбомная ориентация)
                AddMapPage(pdfDocument);

                // Страница 2: Статистика маршрутов
                AddRoutesStatisticsPage(pdfDocument);

                // Страница 3: Снаряжение участников
                AddEquipmentPage(pdfDocument);

                // Страница 4: Продуктовая раскладка
                AddMealPlanPage(pdfDocument);

                // Страница 5: Статистика ингредиентов
                AddIngredientsStatisticsPage(pdfDocument);

                // Сохраняем PDF
                pdfDocument.Save(filePath);
                pdfDocument.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации PDF: {ex.Message}", ex);
            }
        }

        // Метод для отрисовки названия приложения на всех страницах
        private void DrawAppTitle(XGraphics gfx, PdfPage page)
        {
            var fontAppTitle = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
            gfx.DrawString("TrackSync", fontAppTitle, XBrushes.DarkBlue, 40, 20);
        }

        // Страница 1: Карта с легендой (альбомная ориентация)
        private void AddMapPage(PdfDocument document)
        {
            var page = document.AddPage();
            // Явно задаем альбомный A4
            page.Width = XUnit.FromPoint(842);
            page.Height = XUnit.FromPoint(595);
            var gfx = XGraphics.FromPdfPage(page);

            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 20, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            // Заголовок страницы
            gfx.DrawString("Карта маршрутов похода", fontTitle, XBrushes.Black,
                new XRect(0, 30, page.Width, 30), XStringFormats.Center);

            // Легенда (статистика маршрутов) - вверху страницы
            double legendY = 100;
            var routes = GetRoutesStatistics();

            gfx.DrawString("Легенда маршрутов", new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                XBrushes.Black, 40, legendY);
            legendY += 30;

            // Рисуем легенду как горизонтальную таблицу
            double legendX = 40;
            int routesPerRow = 4; // Увеличим количество маршрутов в строке для альбомной ориентации
            double routeWidth = (page.Width - 80) / routesPerRow;

            for (int i = 0; i < routes.Count; i++)
            {
                var route = routes[i];

                // Новая строка после каждых routesPerRow маршрутов
                if (i % routesPerRow == 0 && i > 0)
                {
                    legendY += 60; // Уменьшим высоту строки
                    legendX = 40;
                }

                // Прямоугольник с фоном для каждого маршрута в легенде
                gfx.DrawRectangle(XPens.LightGray, XBrushes.White,
                    legendX, legendY, routeWidth - 10, 55); // Уменьшим высоту

                // Цвет маршрута
                try
                {
                    var color = XColor.FromArgb(255,
                        Convert.ToByte(route.ColorHex.Substring(1, 2), 16),
                        Convert.ToByte(route.ColorHex.Substring(3, 2), 16),
                        Convert.ToByte(route.ColorHex.Substring(5, 2), 16));

                    gfx.DrawRectangle(new XSolidBrush(color), legendX + 5, legendY + 5, 15, 15); // Уменьшим размер
                }
                catch
                {
                    // Если ошибка с цветом, рисуем черный квадрат
                    gfx.DrawRectangle(XBrushes.Black, legendX + 5, legendY + 5, 15, 15);
                }

                // Информация о маршруте (уменьшим шрифт)
                gfx.DrawString(route.CreatorName, new XFont("Times New Roman", 8), XBrushes.Black,
                    legendX + 25, legendY + 5);
                gfx.DrawString($"Дистанция: {route.DistanceStr}", new XFont("Times New Roman", 7), XBrushes.Black,
                    legendX + 25, legendY + 20);
                gfx.DrawString($"Точек: {route.PointsCount}", new XFont("Times New Roman", 7), XBrushes.Black,
                    legendX + 25, legendY + 32);

                legendX += routeWidth;
            }

            // Карта под легендой
            double mapY = legendY + 70; // Уменьшим отступ
            double mapMargin = 20;
            double mapTop = 65;
            double legendHeight = 110; // Резервируем место под легенду внизу
            double mapAreaWidth = page.Width - (mapMargin * 2);
            double mapAreaHeight = page.Height - mapTop - legendHeight - 10;

            if (_mapImage != null)
            {
                try
                {
                    double scale = Math.Min(mapAreaWidth / _mapImage.PixelWidth, mapAreaHeight / _mapImage.PixelHeight);
                    double drawWidth = _mapImage.PixelWidth * scale;
                    double drawHeight = _mapImage.PixelHeight * scale;

                    // Центрируем
                    double drawX = mapMargin + (mapAreaWidth - drawWidth) / 2;
                    double drawY = mapTop + (mapAreaHeight - drawHeight) / 2;

                    gfx.DrawImage(_mapImage, drawX, drawY, drawWidth, drawHeight);

                    // Рамка вокруг карты (опционально)
                    gfx.DrawRectangle(XPens.LightGray, drawX, drawY, drawWidth, drawHeight);

                    // Добавим подпись под картой
                    gfx.DrawString("Карта маршрутов", new XFont("Times New Roman", 10, XFontStyleEx.Bold),
                        XBrushes.Black, new XRect(40, mapY + mapAreaHeight + 10, mapAreaWidth, 20),
                        XStringFormats.Center);
                }
                catch (Exception ex)
                {
                    gfx.DrawString($"Ошибка загрузки карты: {ex.Message}", fontNormal, XBrushes.Red,
                        new XRect(40, mapY, page.Width - 80, 100), XStringFormats.Center);
                }
            }
            else
            {
                gfx.DrawString("Карта не доступна", fontNormal, XBrushes.Gray,
                    new XRect(40, mapY, page.Width - 80, 100), XStringFormats.Center);
            }
        }

        // Страница 2: Подробная статистика маршрутов
        private void AddRoutesStatisticsPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Название приложения
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);
            var fontHeader = new XFont("Times New Roman", 12, XFontStyleEx.Bold);

            // Заголовок
            gfx.DrawString("Подробная статистика маршрутов", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            // Получаем статистику маршрутов
            var routes = GetRoutesStatistics();

            if (routes.Any())
            {
                // Заголовки таблицы
                gfx.DrawString("Цвет", fontHeader, XBrushes.Black, 40, yPos);
                gfx.DrawString("Создатель", fontHeader, XBrushes.Black, 100, yPos);
                gfx.DrawString("Дистанция", fontHeader, XBrushes.Black, 300, yPos);
                gfx.DrawString("Точек", fontHeader, XBrushes.Black, 400, yPos);
                gfx.DrawString("Участник ID", fontHeader, XBrushes.Black, 480, yPos);

                yPos += 25;
                gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
                yPos += 10;

                // Данные маршрутов
                foreach (var route in routes)
                {
                    if (yPos > page.Height - 50)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        DrawAppTitle(gfx, page);
                        yPos = 100;

                        // Повторяем заголовки на новой странице
                        gfx.DrawString("Цвет", fontHeader, XBrushes.Black, 40, yPos);
                        gfx.DrawString("Создатель", fontHeader, XBrushes.Black, 100, yPos);
                        gfx.DrawString("Дистанция", fontHeader, XBrushes.Black, 300, yPos);
                        gfx.DrawString("Точек", fontHeader, XBrushes.Black, 400, yPos);
                        gfx.DrawString("Участник ID", fontHeader, XBrushes.Black, 480, yPos);

                        yPos += 25;
                        gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
                        yPos += 10;
                    }

                    // Цвет маршрута
                    try
                    {
                        var color = XColor.FromArgb(255,
                            Convert.ToByte(route.ColorHex.Substring(1, 2), 16),
                            Convert.ToByte(route.ColorHex.Substring(3, 2), 16),
                            Convert.ToByte(route.ColorHex.Substring(5, 2), 16));

                        gfx.DrawRectangle(new XSolidBrush(color), 40, yPos - 5, 20, 20);
                    }
                    catch
                    {
                        gfx.DrawString(route.ColorHex, fontSmall, XBrushes.Black, 40, yPos);
                    }

                    // Остальные данные
                    gfx.DrawString(route.CreatorName, fontSmall, XBrushes.Black, 100, yPos);
                    gfx.DrawString(route.DistanceStr, fontSmall, XBrushes.Black, 300, yPos);
                    gfx.DrawString(route.PointsCount.ToString(), fontSmall, XBrushes.Black, 400, yPos);
                    gfx.DrawString(route.ParticipantId.ToString(), fontSmall, XBrushes.Black, 480, yPos);

                    yPos += 25;
                }
            }
            else
            {
                gfx.DrawString("Нет данных о маршрутах", fontNormal, XBrushes.Gray,
                    new XRect(40, 150, page.Width - 80, 100), XStringFormats.Center);
            }
        }

        // Страница 3: Снаряжение участников (остается как было, но с названием приложения)
        private void AddEquipmentPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Название приложения
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            // Заголовок
            gfx.DrawString("Снаряжение участников", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            // Получаем участников со снаряжением
            var participants = _context.Participants
                .Include(p => p.User)
                .Include(p => p.HikeRoomsEquipments)
                    .ThenInclude(hre => hre.Equipment)
                .Where(p => p.HikeRoomId == _roomId)
                .ToList();

            foreach (var participant in participants)
            {
                if (yPos > page.Height - 150)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    DrawAppTitle(gfx, page);
                    yPos = 100;
                }

                // Информация об участнике
                gfx.DrawString($"Участник: {participant.User?.Name ?? "Неизвестно"}",
                    new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                    XBrushes.Black, 40, yPos);

                if (participant.User != null)
                {
                    gfx.DrawString($"Рост: {participant.User.Height} см, Вес: {participant.User.Weight} кг",
                        fontSmall, XBrushes.Gray, 40, yPos + 25);
                }

                yPos += 50;

                // Заголовки таблицы снаряжения
                gfx.DrawString("Снаряжение", fontSmall, XBrushes.Black, 40, yPos);
                gfx.DrawString("Кол-во", fontSmall, XBrushes.Black, 300, yPos);
                gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 360, yPos);
                gfx.DrawString("Общ. вес", fontSmall, XBrushes.Black, 420, yPos);

                yPos += 20;
                gfx.DrawLine(XPens.Gray, 40, yPos, page.Width - 40, yPos);
                yPos += 10;

                // Список снаряжения
                var equipmentList = participant.HikeRoomsEquipments.ToList();
                int totalWeight = 0;
                int totalItems = 0;

                foreach (var hre in equipmentList)
                {
                    if (hre.Equipment != null)
                    {
                        int itemWeight = hre.Equipment.Weight * hre.Quantity;
                        totalWeight += itemWeight;
                        totalItems += hre.Quantity;

                        gfx.DrawString(hre.Equipment.Title, fontSmall, XBrushes.Black, 40, yPos);
                        gfx.DrawString(hre.Quantity.ToString(), fontSmall, XBrushes.Black, 300, yPos);
                        gfx.DrawString(hre.Equipment.Weight.ToString(), fontSmall, XBrushes.Black, 360, yPos);
                        gfx.DrawString(itemWeight.ToString(), fontSmall, XBrushes.Black, 420, yPos);

                        yPos += 20;
                    }
                }

                // Итоговая статистика для участника
                if (equipmentList.Any())
                {
                    yPos += 10;
                    gfx.DrawLine(XPens.Gray, 40, yPos, page.Width - 40, yPos);
                    yPos += 20;

                    gfx.DrawString($"Всего предметов: {totalItems} шт.",
                        fontSmall, XBrushes.Black, 40, yPos);
                    gfx.DrawString($"Общий вес: {totalWeight} г ({totalWeight / 1000.0:F2} кг)",
                        fontSmall, XBrushes.Black, 200, yPos);

                    yPos += 40;
                }
                else
                {
                    gfx.DrawString("Нет снаряжения", fontSmall, XBrushes.Gray, 40, yPos);
                    yPos += 40;
                }

                // Разделитель между участниками
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 30;
            }
        }

        // Страница 4: Продуктовая раскладка (остается как было, но с названием приложения)
        private void AddMealPlanPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Название приложения
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            // Заголовок
            gfx.DrawString("Продуктовая раскладка", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            // Получаем дни похода
            var room = _context.HikeRooms.FirstOrDefault(r => r.HikeRoomId == _roomId);
            if (room == null) return;

            int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                           room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

            // Получаем рецепты для комнаты
            var roomRecipes = _context.HikeRoomsRecipes
                .Include(hrr => hrr.Recipe)
                    .ThenInclude(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                .Where(hrr => hrr.HikeRoomId == _roomId)
                .ToList();

            for (int day = 1; day <= totalDays; day++)
            {
                if (yPos > page.Height - 200)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    DrawAppTitle(gfx, page);
                    yPos = 100;
                }

                // Заголовок дня
                var dayDate = room.StartDate.AddDays(day - 1);
                gfx.DrawString($"День {day}: {dayDate:dd.MM.yyyy}",
                    new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                    XBrushes.Black, 40, yPos);

                yPos += 35;

                // Получаем рецепты на этот день
                var dayRecipes = roomRecipes
                    .Where(r => r.MealDate == dayDate)
                    .GroupBy(r => r.TypeMealId)
                    .ToList();

                // Типы приемов пищи
                var mealTypes = new Dictionary<int, string>
                {
                    {1, "Завтрак"},
                    {2, "Обед"},
                    {3, "Ужин"},
                    {4, "Перекус"}
                };

                foreach (var mealGroup in dayRecipes)
                {
                    if (yPos > page.Height - 150)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        DrawAppTitle(gfx, page);
                        yPos = 100;
                    }

                    string mealName = mealTypes.ContainsKey(mealGroup.Key) ?
                        mealTypes[mealGroup.Key] : "Прием пищи";

                    gfx.DrawString(mealName, new XFont("Times New Roman", 12, XFontStyleEx.Bold),
                        XBrushes.DarkBlue, 50, yPos);
                    yPos += 25;

                    foreach (var roomRecipe in mealGroup)
                    {
                        var recipe = roomRecipe.Recipe;
                        if (recipe == null) continue;

                        // Название рецепта
                        gfx.DrawString(recipe.Title, fontSmall, XBrushes.Black, 70, yPos);
                        yPos += 20;

                        // Ингредиенты
                        if (recipe.IngredientsRecipes != null)
                        {
                            foreach (var ingredientRecipe in recipe.IngredientsRecipes)
                            {
                                if (ingredientRecipe.Ingredient != null)
                                {
                                    gfx.DrawString($"  • {ingredientRecipe.Ingredient.Title}: {ingredientRecipe.Weight}г",
                                        fontSmall, XBrushes.Gray, 90, yPos);
                                    yPos += 15;
                                }
                            }
                        }

                        yPos += 10;
                    }
                }

                // Если нет рецептов на день
                if (!dayRecipes.Any())
                {
                    gfx.DrawString("Нет рецептов", fontSmall, XBrushes.Gray, 50, yPos);
                    yPos += 30;
                }

                yPos += 30;

                // Разделитель между днями
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 20;
            }
        }

        // Страница 5: Статистика ингредиентов (остается как было, но с названием приложения)
        private void AddIngredientsStatisticsPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Название приложения
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            // Заголовок
            gfx.DrawString("Статистика ингредиентов", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            // Получаем количество участников
            int participantsCount = _context.Participants
                .Count(p => p.HikeRoomId == _roomId);

            // Общая финансовая сводка
            gfx.DrawString("Общая финансовая сводка",
                new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                XBrushes.Black, 40, yPos);
            yPos += 35;

            // Получаем статистику ингредиентов
            var stats = GetIngredientsStatistics();
            decimal totalCost = stats.Sum(s => s.TotalCost);
            decimal costPerPerson = participantsCount > 0 ? totalCost / participantsCount : 0;

            gfx.DrawString($"Общая стоимость: {totalCost:F2} руб.",
                fontNormal, XBrushes.Black, 50, yPos);
            yPos += 25;

            gfx.DrawString($"На одного участника: {costPerPerson:F2} руб.",
                fontNormal, XBrushes.Black, 50, yPos);
            yPos += 25;

            gfx.DrawString($"Количество участников: {participantsCount}",
                fontNormal, XBrushes.Black, 50, yPos);

            yPos += 40;

            // Детализация по ингредиентам
            gfx.DrawString("Детализация по ингредиентам",
                new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                XBrushes.Black, 40, yPos);
            yPos += 35;

            // Заголовки таблицы
            gfx.DrawString("Ингредиент", fontSmall, XBrushes.Black, 40, yPos);
            gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 250, yPos);
            gfx.DrawString("Стоимость", fontSmall, XBrushes.Black, 350, yPos);

            yPos += 20;
            gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
            yPos += 10;

            // Данные ингредиентов
            foreach (var stat in stats)
            {
                if (yPos > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    DrawAppTitle(gfx, page);
                    yPos = 100;

                    // Повторяем заголовки на новой странице
                    gfx.DrawString("Ингредиент", fontSmall, XBrushes.Black, 40, yPos);
                    gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 250, yPos);
                    gfx.DrawString("Стоимость", fontSmall, XBrushes.Black, 350, yPos);

                    yPos += 20;
                    gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
                    yPos += 10;
                }

                gfx.DrawString(stat.IngredientTitle, fontSmall, XBrushes.Black, 40, yPos);
                gfx.DrawString(stat.TotalWeight.ToString(), fontSmall, XBrushes.Black, 250, yPos);
                gfx.DrawString($"{stat.TotalCost:F2} руб.", fontSmall, XBrushes.Black, 350, yPos);

                // Стоимость на человека (меньшим шрифтом)
                gfx.DrawString($"{stat.CostPerPerson:F2} руб./чел",
                    new XFont("Times New Roman", 8), XBrushes.Gray, 450, yPos);

                yPos += 25;
            }
        }
        private List<RouteStatItem> GetRoutesStatistics()
        {
            var routes = new List<RouteStatItem>();

            try
            {
                var tracks = _context.Tracks
                    .Include(t => t.Participant)
                        .ThenInclude(p => p.User)
                    .Include(t => t.Segments)
                        .ThenInclude(s => s.StartWaypoint)
                    .Include(t => t.Segments)
                        .ThenInclude(s => s.EndWaypoint)
                    .Where(t => t.HikeRoomId == _roomId)
                    .ToList();

                foreach (var track in tracks)
                {
                    // Извлекаем точки маршрута
                    var points = new List<GMap.NET.PointLatLng>();
                    var segments = track.Segments.OrderBy(s => s.SegmentId).ToList();

                    if (segments.Any())
                    {
                        var first = segments.First();
                        if (first.StartWaypoint != null)
                        {
                            points.Add(new GMap.NET.PointLatLng(
                                (double)first.StartWaypoint.Latitude,
                                (double)first.StartWaypoint.Longitude));
                        }

                        foreach (var segment in segments)
                        {
                            if (segment.EndWaypoint != null)
                            {
                                points.Add(new GMap.NET.PointLatLng(
                                    (double)segment.EndWaypoint.Latitude,
                                    (double)segment.EndWaypoint.Longitude));
                            }
                        }
                    }

                    // Расчет дистанции
                    double distance = 0;
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        distance += CalculateDistance(points[i], points[i + 1]);
                    }

                    routes.Add(new RouteStatItem
                    {
                        CreatorName = track.Participant?.User?.Name ?? "Участник",
                        ColorHex = track.Color ?? "#FF0000",
                        Distance = distance,
                        PointsCount = points.Count,
                        ParticipantId = track.ParticipantId
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении статистики маршрутов: {ex.Message}");
            }

            return routes;
        }

        private List<IngredientStatItem> GetIngredientsStatistics()
        {
            var stats = new List<IngredientStatItem>();

            try
            {
                // Получаем количество участников
                int participantsCount = _context.Participants
                    .Count(p => p.HikeRoomId == _roomId);

                // Получаем все рецепты комнаты
                var roomRecipes = _context.HikeRoomsRecipes
                    .Include(hrr => hrr.Recipe)
                        .ThenInclude(r => r.IngredientsRecipes)
                            .ThenInclude(ir => ir.Ingredient)
                    .Where(hrr => hrr.HikeRoomId == _roomId)
                    .ToList();

                var ingredientStats = new Dictionary<int, IngredientStatItem>();
                var recipeUsageCount = new Dictionary<int, int>();

                foreach (var roomRecipe in roomRecipes)
                {
                    var recipe = roomRecipe.Recipe;
                    if (recipe?.IngredientsRecipes == null) continue;

                    foreach (var ingredientRecipe in recipe.IngredientsRecipes)
                    {
                        var ingredient = ingredientRecipe.Ingredient;
                        if (ingredient == null) continue;

                        // Рассчитываем общий вес для всех участников
                        int totalWeightForAllParticipants = ingredientRecipe.Weight * participantsCount;

                        // Рассчитываем стоимость (цена за 100 грамм)
                        decimal costPer100g = ingredient.Price;
                        decimal costForIngredient = (costPer100g * totalWeightForAllParticipants) / 100;

                        if (ingredientStats.ContainsKey(ingredient.IngredientId))
                        {
                            // Обновляем существующую запись
                            var existing = ingredientStats[ingredient.IngredientId];
                            existing.TotalWeight += totalWeightForAllParticipants;
                            existing.TotalCost += costForIngredient;
                            recipeUsageCount[ingredient.IngredientId]++;
                        }
                        else
                        {
                            // Создаем новую запись
                            ingredientStats[ingredient.IngredientId] = new IngredientStatItem
                            {
                                IngredientId = ingredient.IngredientId,
                                IngredientTitle = ingredient.Title,
                                TotalWeight = totalWeightForAllParticipants,
                                TotalCost = costForIngredient,
                                UsedInRecipesCount = 1
                            };
                            recipeUsageCount[ingredient.IngredientId] = 1;
                        }
                    }
                }

                // Обновляем счетчик использования и рассчитываем стоимость на человека
                foreach (var stat in ingredientStats.Values)
                {
                    stat.UsedInRecipesCount = recipeUsageCount[stat.IngredientId];
                    stat.CostPerPerson = participantsCount > 0 ? stat.TotalCost / participantsCount : 0;
                }

                // Сортируем по стоимости
                stats = ingredientStats.Values
                    .OrderByDescending(i => i.TotalCost)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении статистики ингредиентов: {ex.Message}");
            }

            return stats;
        }

        private double CalculateDistance(GMap.NET.PointLatLng point1, GMap.NET.PointLatLng point2)
        {
            // Формула гаверсинусов для расчета расстояния между двумя точками на сфере
            const double R = 6371; // Радиус Земли в км

            double dLat = (point2.Lat - point1.Lat) * Math.PI / 180;
            double dLon = (point2.Lng - point1.Lng) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(point1.Lat * Math.PI / 180) * Math.Cos(point2.Lat * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        // Вспомогательные классы
        public class RouteStatItem
        {
            public string CreatorName { get; set; }
            public string ColorHex { get; set; }
            public double Distance { get; set; }
            public string DistanceStr => $"{Distance:F2} км";
            public int PointsCount { get; set; }
            public int ParticipantId { get; set; }
        }
        public class IngredientStatItem
        {
            public string IngredientTitle { get; set; }
            public int IngredientId { get; set; }
            public int TotalWeight { get; set; }
            public decimal TotalCost { get; set; }
            public int UsedInRecipesCount { get; set; }
            public decimal CostPerPerson { get; set; }
        }
    }
}
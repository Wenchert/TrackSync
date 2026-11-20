using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using TrackSync.DataBase.WorkWithDB;

namespace TrackSync.Functions
{
    public class HikeRoomPdfGenerator
    {
        private readonly int _roomId;
        private readonly string _roomTitle;
        private readonly XImage _mapImage;
        private readonly TrackSyncContext _context;
        private readonly double _mapZoom;


        static HikeRoomPdfGenerator()
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new CustomFontResolver();
            }
        }

        public HikeRoomPdfGenerator(int roomId, RenderTargetBitmap mapImage, double mapZoom)
        {
            _roomId = roomId;
            _context = new TrackSyncContext();
            _roomTitle = _context.HikeRooms
                .Where(r => r.HikeRoomId == roomId)
                .Select(r => r.Title)
                .FirstOrDefault() ?? $"Комната_{roomId}";
            _mapZoom = mapZoom;

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

                // Страница 2: Снаряжение участников
                AddEquipmentPage(pdfDocument);

                // Страница 3: Продуктовая раскладка
                AddMealPlanPage(pdfDocument);

                // Страница 4: Статистика ингредиентов
                AddIngredientsStatisticsPage(pdfDocument);

                pdfDocument.Save(filePath);
                pdfDocument.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации PDF: {ex.Message}", ex);
            }
        }

        private void DrawAppTitle(XGraphics gfx, PdfPage page)
        {
            var fontAppTitle = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
            gfx.DrawString("TrackSync", fontAppTitle, XBrushes.DarkBlue, 40, 20);
        }

        // Страница 1: Карта с легендой (альбомная ориентация)
        private void AddMapPage(PdfDocument document)
        {
            var page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            var gfx = XGraphics.FromPdfPage(page);

            var routes = GetRoutesStatistics();

            if (_mapImage != null)
            {
                try
                {
                    double imageWidth = _mapImage.PixelWidth;
                    double imageHeight = _mapImage.PixelHeight;
                    double pageWidth = page.Width;
                    double pageHeight = page.Height;

                    double scaleX = pageWidth / imageWidth;
                    double scaleY = pageHeight / imageHeight;
                    double scale = Math.Min(scaleX, scaleY);

                    double drawWidth = imageWidth * scale;
                    double drawHeight = imageHeight * scale;
                    double x = (pageWidth - drawWidth) / 2;
                    double y = (pageHeight - drawHeight) / 2;

                    gfx.DrawImage(_mapImage, x, y, drawWidth, drawHeight);
                }
                catch (Exception ex)
                {
                    gfx.DrawString($"Ошибка загрузки карты: {ex.Message}",
                        new XFont("Times New Roman", 12), XBrushes.Red,
                        new XRect(40, 150, page.Width - 80, 100),
                        XStringFormats.Center);
                }
            }

            var fontAppTitle = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
            gfx.DrawString("TrackSync", fontAppTitle, XBrushes.DarkBlue, 20, 20);

            if (routes.Any())
            {
                double legendX = 20;
                double legendWidth = 200;
                double legendHeight = routes.Count * 40 + 60; 
                double legendY = page.Height - legendHeight - 20; 

                var legendBg = new XSolidBrush(XColor.FromArgb(230, 255, 255, 255));
                gfx.DrawRectangle(legendBg, legendX, legendY, legendWidth, legendHeight);
                gfx.DrawRectangle(XPens.LightGray, legendX, legendY, legendWidth, legendHeight);

                gfx.DrawString("Легенда маршрутов",
                    new XFont("Times New Roman", 12, XFontStyleEx.Bold),
                    XBrushes.Black, legendX + 10, legendY + 15);

                double currentY = legendY + 40;

                foreach (var route in routes)
                {
                    try
                    {
                        var color = XColor.FromArgb(255,
                            Convert.ToByte(route.ColorHex.Substring(1, 2), 16),
                            Convert.ToByte(route.ColorHex.Substring(3, 2), 16),
                            Convert.ToByte(route.ColorHex.Substring(5, 2), 16));

                        gfx.DrawRectangle(new XSolidBrush(color),
                            legendX + 10, currentY, 15, 15);
                        gfx.DrawRectangle(XPens.Black,
                            legendX + 10, currentY, 15, 15);
                    }
                    catch
                    {
                        gfx.DrawRectangle(XBrushes.Black,
                            legendX + 10, currentY, 15, 15);
                    }

                    string creatorText = route.CreatorName;
                    string statsText = $"{route.DistanceStr}, {route.PointsCount} точек";

                    gfx.DrawString(creatorText,
                        new XFont("Times New Roman", 9, XFontStyleEx.Bold),
                        XBrushes.Black,
                        legendX + 30, currentY + 2);

                    gfx.DrawString(statsText,
                        new XFont("Times New Roman", 8),
                        XBrushes.Black,
                        legendX + 30, currentY + 15);

                    currentY += 40;
                }

                if (_mapZoom > 0)
                {
                    currentY += 5;
                    gfx.DrawString($"Масштаб: 1:{_mapZoom:N0}",
                        new XFont("Times New Roman", 9),
                        XBrushes.Black,
                        legendX + 10, currentY);
                }
            }
        }

        // Страница 2: Страница снаряжения участников
        private void AddEquipmentPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            gfx.DrawString("Снаряжение участников", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

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

                gfx.DrawString($"Участник: {participant.User?.Name ?? "Неизвестно"}",
                    new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                    XBrushes.Black, 40, yPos);

                if (participant.User != null)
                {
                    gfx.DrawString($"Рост: {participant.User.Height} см, Вес: {participant.User.Weight} кг",
                        fontSmall, XBrushes.Gray, 40, yPos + 25);
                }

                yPos += 50;

                gfx.DrawString("Снаряжение", fontSmall, XBrushes.Black, 40, yPos);
                gfx.DrawString("Кол-во", fontSmall, XBrushes.Black, 300, yPos);
                gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 360, yPos);
                gfx.DrawString("Общ. вес", fontSmall, XBrushes.Black, 420, yPos);

                yPos += 20;
                gfx.DrawLine(XPens.Gray, 40, yPos, page.Width - 40, yPos);
                yPos += 10;

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

                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 30;
            }
        }

        // Страница 3: Продуктовая раскладка
        private void AddMealPlanPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontMedium = new XFont("Times New Roman", 11);
            var fontSmall = new XFont("Times New Roman", 10);
            var fontBold = new XFont("Times New Roman", 12, XFontStyleEx.Bold);

            gfx.DrawString("Продуктовая раскладка", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            var room = _context.HikeRooms.FirstOrDefault(r => r.HikeRoomId == _roomId);
            if (room == null) return;

            int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                           room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

            var roomRecipes = _context.HikeRoomsRecipes
                .Include(hrr => hrr.Recipe)
                    .ThenInclude(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                .Include(hrr => hrr.Recipe)
                    .ThenInclude(r => r.RecipesParticipants)
                        .ThenInclude(rp => rp.Participant)
                            .ThenInclude(p => p.User)
                .Where(hrr => hrr.HikeRoomId == _roomId)
                .ToList();

            int participantsCount = _context.Participants
                .Count(p => p.HikeRoomId == _roomId);

            for (int day = 1; day <= totalDays; day++)
            {
                if (yPos > page.Height - 200)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    DrawAppTitle(gfx, page);
                    yPos = 100;
                }

                var dayDate = room.StartDate.AddDays(day - 1);
                gfx.DrawString($"День {day}: {dayDate:dd.MM.yyyy}",
                    new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                    XBrushes.DarkBlue, 40, yPos);

                yPos += 25;

                var dayRecipes = roomRecipes
                    .Where(r => r.MealDate == dayDate)
                    .GroupBy(r => r.TypeMealId)
                    .ToList();

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
                    yPos += 20;

                    foreach (var roomRecipe in mealGroup)
                    {
                        var recipe = roomRecipe.Recipe;
                        if (recipe == null) continue;

                        gfx.DrawString(recipe.Title, fontBold, XBrushes.Black, 70, yPos);
                        yPos += 20;

                        if (!string.IsNullOrWhiteSpace(recipe.Description))
                        {
                            gfx.DrawString($"Описание: {recipe.Description}",
                                fontSmall, XBrushes.Gray, 90, yPos);
                            yPos += 15;
                        }

                        if (recipe.Time.HasValue)
                        {
                            gfx.DrawString($"Время готовки: {recipe.Time} минут",
                                fontSmall, XBrushes.Gray, 90, yPos);
                            yPos += 15;
                        }

                        if (recipe.IngredientsRecipes != null)
                        {
                            int totalCalories = 0;
                            int totalProteins = 0;
                            int totalFats = 0;
                            int totalCarbs = 0;
                            int totalWeight = 0;
                            int caloriesPerPerson = 0;
                            int proteinsPerPerson = 0;
                            int fatsPerPerson = 0;
                            int carbsPerPerson = 0;
                            int weightPerPerson = 0;

                            foreach (var ingredientRecipe in recipe.IngredientsRecipes)
                            {
                                if (ingredientRecipe.Ingredient != null)
                                {
                                    totalCalories += (ingredientRecipe.Ingredient.Calories * ingredientRecipe.Weight * participantsCount) / 100;
                                    totalProteins += (ingredientRecipe.Ingredient.Proteins * ingredientRecipe.Weight * participantsCount) / 100;
                                    totalFats += (ingredientRecipe.Ingredient.Fats * ingredientRecipe.Weight * participantsCount) / 100;
                                    totalCarbs += (ingredientRecipe.Ingredient.Carbs * ingredientRecipe.Weight * participantsCount) / 100;
                                    totalWeight += ingredientRecipe.Weight * participantsCount;

                                    caloriesPerPerson += (ingredientRecipe.Ingredient.Calories * ingredientRecipe.Weight) / 100;
                                    proteinsPerPerson += (ingredientRecipe.Ingredient.Proteins * ingredientRecipe.Weight) / 100;
                                    fatsPerPerson += (ingredientRecipe.Ingredient.Fats * ingredientRecipe.Weight) / 100;
                                    carbsPerPerson += (ingredientRecipe.Ingredient.Carbs * ingredientRecipe.Weight) / 100;
                                    weightPerPerson += ingredientRecipe.Weight;
                                }
                            }

                            gfx.DrawString($"Общий вес: {totalWeight}г",
                                fontMedium, XBrushes.Black, 90, yPos);
                            yPos += 15;

                            gfx.DrawString($"Калории: {caloriesPerPerson} ккал/чел",
                                fontMedium, XBrushes.Black, 90, yPos);
                            yPos += 15;

                            gfx.DrawString($"Белки: {proteinsPerPerson}г, Жиры: {fatsPerPerson}г, Углеводы: {carbsPerPerson}г",
                                fontMedium, XBrushes.Black, 90, yPos);
                            yPos += 15;
                        }

                        if (recipe.IngredientsRecipes != null)
                        {
                            gfx.DrawString("Ингредиенты:",
                                fontMedium, XBrushes.Black, 90, yPos);
                            yPos += 15;

                            foreach (var ingredientRecipe in recipe.IngredientsRecipes)
                            {
                                if (ingredientRecipe.Ingredient != null)
                                {
                                    int totalWeight = ingredientRecipe.Weight * participantsCount;
                                    gfx.DrawString($"  • {ingredientRecipe.Ingredient.Title}: {totalWeight}г",
                                        fontMedium, XBrushes.Gray, 110, yPos);
                                    yPos += 15;
                                }
                            }
                        }

                        if (recipe.RecipesParticipants != null && recipe.RecipesParticipants.Any())
                        {
                            gfx.DrawString("Ответственные:",
                                fontMedium, XBrushes.Black, 90, yPos);
                            yPos += 15;

                            foreach (var responsible in recipe.RecipesParticipants)
                            {
                                if (responsible.Participant?.User != null)
                                {
                                    gfx.DrawString($"  • {responsible.Participant.User.Name}",
                                        fontMedium, XBrushes.Gray, 110, yPos);
                                    yPos += 15;
                                }
                            }
                        }

                        yPos += 10;
                    }
                }

                if (!dayRecipes.Any())
                {
                    gfx.DrawString("Нет рецептов", fontMedium, XBrushes.Gray, 50, yPos);
                    yPos += 30;
                }

                yPos += 20;
            }
        }

        // Страница 4: Статистика ингредиентов
        private void AddIngredientsStatisticsPage(PdfDocument document)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            DrawAppTitle(gfx, page);

            var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            var fontNormal = new XFont("Times New Roman", 12);
            var fontSmall = new XFont("Times New Roman", 10);

            gfx.DrawString("Статистика ингредиентов", fontTitle, XBrushes.Black,
                new XRect(0, 50, page.Width, 30), XStringFormats.Center);

            double yPos = 100;

            int participantsCount = _context.Participants
                .Count(p => p.HikeRoomId == _roomId);

            gfx.DrawString("Общая финансовая сводка",
                new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                XBrushes.Black, 40, yPos);
            yPos += 35;

            var stats = GetIngredientsStatistics();
            decimal totalCost = stats.Sum(s => s.TotalCost);
            decimal costPerPerson = participantsCount > 0 ? totalCost / participantsCount : 0;

            gfx.DrawString($"Общая стоимость: {totalCost:F2} руб.",
                fontNormal, XBrushes.Black, 50, yPos);
            yPos += 25;

            gfx.DrawString($"Количество участников: {participantsCount}",
                fontNormal, XBrushes.Black, 50, yPos);
            yPos += 25;

            gfx.DrawString($"Общий вес продуктов: {stats.Sum(s => s.TotalWeight)} г",
                fontNormal, XBrushes.Black, 50, yPos);
            yPos += 40;

            gfx.DrawString("Детализация по ингредиентам",
                new XFont("Times New Roman", 14, XFontStyleEx.Bold),
                XBrushes.Black, 40, yPos);
            yPos += 35;

            gfx.DrawString("Ингредиент", fontSmall, XBrushes.Black, 40, yPos);
            gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 250, yPos);
            gfx.DrawString("Стоимость", fontSmall, XBrushes.Black, 350, yPos);
            gfx.DrawString("Используется", fontSmall, XBrushes.Black, 450, yPos);

            yPos += 20;
            gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
            yPos += 10;

            foreach (var stat in stats)
            {
                if (yPos > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    DrawAppTitle(gfx, page);
                    yPos = 100;

                    gfx.DrawString("Ингредиент", fontSmall, XBrushes.Black, 40, yPos);
                    gfx.DrawString("Вес (г)", fontSmall, XBrushes.Black, 250, yPos);
                    gfx.DrawString("Стоимость", fontSmall, XBrushes.Black, 350, yPos);
                    gfx.DrawString("Используется", fontSmall, XBrushes.Black, 450, yPos);

                    yPos += 20;
                    gfx.DrawLine(XPens.Black, 40, yPos, page.Width - 40, yPos);
                    yPos += 10;
                }

                gfx.DrawString(stat.IngredientTitle, fontSmall, XBrushes.Black, 40, yPos);
                gfx.DrawString(stat.TotalWeight.ToString(), fontSmall, XBrushes.Black, 250, yPos);
                gfx.DrawString($"{stat.TotalCost:F2} руб.", fontSmall, XBrushes.Black, 350, yPos);
                gfx.DrawString($"{stat.UsedInRecipesCount} рецепт(ов)", fontSmall, XBrushes.Black, 450, yPos);

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
                int participantsCount = _context.Participants
                    .Count(p => p.HikeRoomId == _roomId);

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

                        int totalWeightForAllParticipants = ingredientRecipe.Weight * participantsCount;

                        decimal costPer100g = ingredient.Price;
                        decimal costForIngredient = (costPer100g * totalWeightForAllParticipants) / 100;

                        if (ingredientStats.ContainsKey(ingredient.IngredientId))
                        {
                            var existing = ingredientStats[ingredient.IngredientId];
                            existing.TotalWeight += totalWeightForAllParticipants;
                            existing.TotalCost += costForIngredient;
                            recipeUsageCount[ingredient.IngredientId]++;
                        }
                        else
                        {
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

                foreach (var stat in ingredientStats.Values)
                {
                    stat.UsedInRecipesCount = recipeUsageCount[stat.IngredientId];
                    stat.CostPerPerson = participantsCount > 0 ? stat.TotalCost / participantsCount : 0;
                }

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
            const double R = 6371; // Радиус Земли в км

            double dLat = (point2.Lat - point1.Lat) * Math.PI / 180;
            double dLon = (point2.Lng - point1.Lng) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(point1.Lat * Math.PI / 180) * Math.Cos(point2.Lat * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

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
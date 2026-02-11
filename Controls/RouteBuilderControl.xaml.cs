using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TrackSync.Controls
{
    public partial class RouteBuilderControl : UserControl
    {
        public List<PointLatLng> RoutePoints => _routePoints;
        private List<GMapMarker> _myVisualMarkers = new List<GMapMarker>();
        private string _currentHexColor = "#FF0000";
        private List<PointLatLng> _routePoints = new List<PointLatLng>();
        public event EventHandler<string> ColorChanged;
        private bool _canEdit = true;
        public string SelectedColor => _currentHexColor;

        // Событие для обновления статистики в родительском окне
        public event EventHandler<(double distance, int points)> RouteUpdated;

        public RouteBuilderControl()
        {
            InitializeComponent();
            InitMap();
        }

        public void SetPermissions(bool canEdit)
        {
            _canEdit = canEdit;
            // Скрываем панель выбора цвета, если прав нет
            ColorPickerPanel.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ColorSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _currentHexColor = btn.Tag.ToString();

                SelectedColorDisplay.Background = btn.Background;
                ColorHexText.Text = _currentHexColor;

                ColorPickerToggleButton.IsChecked = false;

                DrawRoute();

                ColorChanged?.Invoke(this, _currentHexColor);
            }
        }

        public void DisableUsedColors(List<string> usedColors)
        {
            // Проходим по всем кнопкам внутри контейнера палитры
            foreach (var child in ColorsGrid.Children)
            {
                if (child is Button btn && btn.Tag != null)
                {
                    string btnColor = btn.Tag.ToString().ToUpper();

                    // Если цвет есть в списке занятых — выключаем кнопку и делаем её полупрозрачной
                    bool isUsed = usedColors.Any(c => c.ToUpper() == btnColor);

                    btn.IsEnabled = !isUsed;
                    btn.Opacity = isUsed ? 0.3 : 1.0;
                    btn.ToolTip = isUsed ? "Этот цвет уже занят другим участником" : null;
                }
            }
        }

        private Brush GetSelectedBrush()
        {
            return GetBrushFromString(_currentHexColor);
        }

        public void LoadRoute(List<PointLatLng> points, string colorHex, bool centerMap = true)
        {
            // 1. Очищаем только свои маркеры
            foreach (var marker in _myVisualMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            _myVisualMarkers.Clear();

            // 2. Обновляем список координат
            _routePoints = new List<PointLatLng>(points);

            // 3. Устанавливаем цвет в новый Color Picker
            if (!string.IsNullOrEmpty(colorHex))
            {
                _currentHexColor = colorHex;

                // Обновляем визуальные элементы
                SelectedColorDisplay.Background = GetBrushFromString(_currentHexColor);
                ColorHexText.Text = _currentHexColor;
            }

            // 4. Отрисовка и статистика
            DrawRoute();
            NotifyUpdate();

            if (centerMap && _routePoints.Count > 0)
            {
                CenterMapOnRoutes(_routePoints);
            }
        }

        public void CenterMapOnRoutes(List<PointLatLng> allPoints)
        {
            if (allPoints == null || allPoints.Count == 0)
            {
                // Если нет точек, центрируем на Москве
                MapControl.Position = new PointLatLng(55.7558, 37.6173);
                MapControl.Zoom = 10;
                return;
            }

            // Создаём прямоугольник, охватывающий все точки
            RectLatLng rect = RectLatLng.FromLTRB(
                allPoints.Min(p => p.Lng),
                allPoints.Max(p => p.Lat),
                allPoints.Max(p => p.Lng),
                allPoints.Min(p => p.Lat)
            );

            // Увеличиваем прямоугольник на 10% для отступов
            rect.Inflate(0.1, 0.1);

            // Автоматически подбираем зум и центр, чтобы всё поместилось
            MapControl.SetZoomToFitRect(rect);
        }

        public void CenterMapOnPoint(PointLatLng point, int zoom = 15)
        {
            MapControl.Position = point;
            MapControl.Zoom = zoom;
        }

        public void DisplayOtherTrack(List<PointLatLng> points, string colorString)
        {
            if (points == null || points.Count < 2) return;

            var routeMarker = new GMapRoute(points)
            {
                Shape = new System.Windows.Shapes.Path
                {
                    Stroke = GetBrushFromString(colorString),
                    StrokeThickness = 2.5,
                    Opacity = 1
                }
            };

            MapControl.Markers.Add(routeMarker);
        }

        private Brush GetBrushFromString(string colorString)
        {
            try
            {
                var brush = (Brush)new BrushConverter().ConvertFromString(colorString);
                if (brush != null)
                {
                    brush.Freeze();
                    return brush;
                }
            }
            catch
            {
            }
            return Brushes.Black;
        }

        private void InitMap()
        {
            MapControl.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Отключаем отображение позиции мыши
            MapControl.ShowCenter = false;

            // 1. Границы зума
            MapControl.MinZoom = 2;
            MapControl.MaxZoom = 20;

            // 2. Настройка зума
            MapControl.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            MapControl.IgnoreMarkerOnMouseWheel = true;

            // 3. Управление
            MapControl.CanDragMap = true;
            MapControl.DragButton = MouseButton.Left;

            // 4. Подписки
            MapControl.MouseRightButtonDown += MapControl_MouseRightButtonDown;

            // 5. Чтобы зум работал сразу, карте нужен фокус при наведении
            MapControl.MouseEnter += (s, e) => MapControl.Focus();
        }

        private void MapControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_canEdit)
            {
                return;
            }

            var pos = e.GetPosition(MapControl);
            var latLng = MapControl.FromLocalToLatLng((int)pos.X, (int)pos.Y);
            _routePoints.Add(latLng);
            DrawRoute();
            NotifyUpdate();
        }

        private void DrawRoute()
        {
            // Очищаем только свои визуальные элементы
            foreach (var marker in _myVisualMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            _myVisualMarkers.Clear();

            var currentBrush = GetSelectedBrush();

            // 1. Рисуем линию своего маршрута
            if (_routePoints.Count > 1)
            {
                var line = new GMapRoute(_routePoints)
                {
                    Shape = new System.Windows.Shapes.Path
                    {
                        Stroke = currentBrush,
                        StrokeThickness = 4,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    }
                };

                // Устанавливаем высокий ZIndex, чтобы линия была поверх чужих треков
                Panel.SetZIndex(line.Shape, 10);

                MapControl.Markers.Add(line);
                _myVisualMarkers.Add(line);
            }

            // 2. Рисуем точки своего маршрута
            foreach (var p in _routePoints)
            {
                var marker = new GMapMarker(p)
                {
                    Shape = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = currentBrush,
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    },
                    Offset = new Point(-5, -5)
                };

                // Точки должны быть выше линии
                Panel.SetZIndex(marker.Shape, 11);

                MapControl.Markers.Add(marker);
                _myVisualMarkers.Add(marker);
            }
        }

        public void Clear()
        {
            foreach (var marker in _myVisualMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            _myVisualMarkers.Clear();

            _routePoints.Clear();

            NotifyUpdate();
        }

        public void UndoLastPoint()
        {
            if (_routePoints.Count > 0)
            {
                _routePoints.RemoveAt(_routePoints.Count - 1);
                DrawRoute();
                NotifyUpdate();
            }
        }

        private void NotifyUpdate()
        {
            var stats = GetCurrentRouteStats();

            RouteUpdated?.Invoke(this, stats);
        }

        public (double distance, int points) GetCurrentRouteStats()
        {
            double distance = 0;

            if (_routePoints.Count >= 2)
            {
                for (int i = 0; i < _routePoints.Count - 1; i++)
                {
                    distance += GMap.NET.MapProviders.EmptyProvider.Instance.Projection
                        .GetDistance(_routePoints[i], _routePoints[i + 1]);
                }
            }

            return (distance, _routePoints.Count);
        }

        public string GetFirstAvailableColor(List<string> usedColors)
        {
            var defaultColors = new List<string>
            {
            "#3498DB", // Синий
            "#2ECC71", // Зеленый
            "#F1C40F", // Желтый
            "#9B59B6", // Фиолетовый
            "#E74C3C", // Красный
            "#1ABC9C", // Бирюзовый
            "#E67E22", // Оранжевый
            "#34495E"  // Темно-синий
            };

            // Ищем первый свободный цвет
            foreach (var color in defaultColors)
            {
                if (!usedColors.Contains(color))
                    return color;
            }

            // Если все цвета заняты, возвращаем null
            return null;
        }

        public void SetColor(string colorHex)
        {
            if (!string.IsNullOrEmpty(colorHex))
            {
                _currentHexColor = colorHex;

                // Обновляем визуальные элементы
                SelectedColorDisplay.Background = GetBrushFromString(_currentHexColor);
                ColorHexText.Text = _currentHexColor;
            }
        }

        public void DisableEditing()
        {
            _canEdit = false;
            ColorPickerPanel.Visibility = Visibility.Collapsed;

            // Отключаем обработчик кликов по карте
            MapControl.MouseRightButtonDown -= MapControl_MouseRightButtonDown;
        }

        public void HideAllMarkers()
        {
            try
            {
                // Скрываем только маркеры-точки, но оставляем линии
                foreach (var marker in MapControl.Markers)
                {
                    // Проверяем, является ли маркер точкой (эллипсом)
                    if (marker.Shape is Ellipse)
                    {
                        if (marker.Shape is UIElement shape)
                        {
                            shape.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                // Также скрываем точки из _myVisualMarkers
                foreach (var marker in _myVisualMarkers)
                {
                    if (marker.Shape is Ellipse)
                    {
                        if (marker.Shape is UIElement shape)
                        {
                            shape.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                // Принудительное обновление отрисовки
                MapControl.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при скрытии маркеров: {ex.Message}");
            }
        }

        public void ShowAllMarkers()
        {
            try
            {
                // Показываем все маркеры
                foreach (var marker in MapControl.Markers)
                {
                    if (marker.Shape is UIElement shape)
                    {
                        shape.Visibility = Visibility.Visible;
                    }
                }

                foreach (var marker in _myVisualMarkers)
                {
                    if (marker.Shape is UIElement shape)
                    {
                        shape.Visibility = Visibility.Visible;
                    }
                }

                // Принудительное обновление отрисовки
                MapControl.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при показе маркеров: {ex.Message}");
            }
        }
    }
}

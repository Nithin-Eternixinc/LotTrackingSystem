
using LTS.Common.Models;
using LTS.UI.ViewModels;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LTS.UI.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private readonly Dictionary<int, Border> _locationCards = new();
        public DashboardView(DashboardViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Loaded += DashboardView_Loaded;
            vm.AnimateMoveRequested += AnimateCarrierMovement;
        }

        //Before adding cards: CLEAR dictionary first when view reloads.


        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            _locationCards.Clear();
        }
        //When UI creates location cards: store their Border reference

        private void LocationCard_Loaded(object sender, RoutedEventArgs e)  
        {
            if (sender is not Border border)
                return;

            if (border.DataContext is not ProcessLocation location)
                return;

            _locationCards[location.ProcessLocationId] = border;

            

        }

        //get each card center
        private Point GetCardCenter(Border card)
        {
            if (!card.IsLoaded || !CarrierCanvas.IsLoaded)
                return new Point();

            var transform = card.TransformToVisual(CarrierCanvas); //get card positon

            Point topLeft = transform.Transform(new Point(0, 0));

            return new Point(
                topLeft.X + (card.ActualWidth / 2),
                topLeft.Y + (card.ActualHeight / 2)
            );
        }

        public async Task AnimateCarrierMovement(int fromLocationId, int toLocationId)
        {
            if (!_locationCards.ContainsKey(fromLocationId) ||
                !_locationCards.ContainsKey(toLocationId))
                return;

            var source = _locationCards[fromLocationId];
            var target = _locationCards[toLocationId];

            Point start = GetCardCenter(source);
            Point end = GetCardCenter(target);

            // create NEW line for this movement


            var path = new Path 
            { 
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 3, Opacity = 1, 
                StrokeDashArray = new DoubleCollection { 8, 4 }, 
                Data = new LineGeometry(start, end) 
            };


            // create NEW carrier dot
            var carrier = new Ellipse 
            { 
                Width = 24, Height = 24,
                Fill = Brushes.DeepSkyBlue,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2, Opacity = 1
            };

            // add to canvas
            CarrierCanvas.Children.Add(path); 
            CarrierCanvas.Children.Add(carrier);

            Canvas.SetLeft(
                carrier, start.X - (carrier.Width / 2));

            Canvas.SetTop(carrier, start.Y - (carrier.Height / 2));




            // animate X

            var xAnimation = new DoubleAnimation 
            { 
                From = start.X - (carrier.Width / 2),
                To = end.X - (carrier.Width / 2),
                Duration = TimeSpan.FromSeconds(2)
            };
            // animate Y

            var yAnimation = new DoubleAnimation
            {
                From = start.Y - (carrier.Height / 2),
                To = end.Y - (carrier.Height / 2),
                Duration = TimeSpan.FromSeconds(2)
            };

            carrier.BeginAnimation(Canvas.LeftProperty, xAnimation);
            carrier.BeginAnimation(Canvas.TopProperty, yAnimation);

            await Task.Delay(2000); //animation durations
            CarrierCanvas.Children.Remove(path);
            CarrierCanvas.Children.Remove(carrier);


        }

    }
}

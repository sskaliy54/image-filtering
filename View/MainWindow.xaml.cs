
using System.Windows;
using System.Windows.Input;


namespace STimg.View
{

    public partial class MainWindow : Window
    {
        private bool isMouseButtonDown;
        private Point startPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMouseButtonDown = true;
            startPoint = e.GetPosition(this);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseButtonDown)
            {
                Point endPoint = e.GetPosition(this);
                double offsetX = endPoint.X - startPoint.X;
                double offsetY = endPoint.Y - startPoint.Y;

                Left += offsetX;
                Top += offsetY;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseButtonDown = false;
        }
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}

using System.Windows;
using System.Windows.Input;
using Markdig;

namespace MDAsisst.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide(); // Tray minimize on close/minimize
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("アピアランス・自動非表示設定ダイアログ（実装中）", "設定", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MarkdownEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var markdown = MarkdownEditor.Text;
            var html = Markdown.ToHtml(markdown);
            string fullHtml = "<html><body style='color: white; background-color: #1e1e1e; font-family: sans-serif;'>" + html + "</body></html>";
            PreviewBrowser.NavigateToString(fullHtml);
        }

        private void SnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string snippet)
            {
                int caret = MarkdownEditor.CaretIndex;
                MarkdownEditor.Text = MarkdownEditor.Text.Insert(caret, snippet);
                MarkdownEditor.CaretIndex = caret + snippet.Length;
                MarkdownEditor.Focus();
            }
        }
    }
}

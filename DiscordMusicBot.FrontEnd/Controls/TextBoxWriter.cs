using Avalonia.Controls;
using Avalonia.Threading;
using System.IO;
using System.Text;

namespace DiscordMusicBot.FrontEnd.Controls
{
    public class TextBoxWriter : TextWriter
    {
        private readonly TextBox _textBox;
        private readonly ScrollViewer _scrollViewer;
        public TextBoxWriter(TextBox textBox, ScrollViewer scrollViewer)
        {
            _textBox = textBox;
            _scrollViewer = scrollViewer;
        }
        public override void Write(char value)
        {
            Dispatcher.UIThread.Invoke(() => 
            {
                _textBox.Text += value;
                _scrollViewer.ScrollToEnd();
            });
        }

        public override void Write(string? value)
        {
            Dispatcher.UIThread.Invoke(() => 
            { 
                _textBox.Text += value;
                _scrollViewer.ScrollToEnd();
            });
        }
        public override Encoding Encoding => Encoding.Unicode;
    }
}

using System.Windows.Controls;
using System.Windows.Media;

namespace Hermes_Executor.Controls
{
    public partial class StatusIndicator : UserControl
    {
        public StatusIndicator()
        {
            InitializeComponent();
        }

        public void SetStatus(string text, Brush color)
        {
            IndicatorText.Text = text;
            IndicatorDot.Fill = color;
        }
    }
}

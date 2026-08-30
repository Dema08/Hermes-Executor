using System.Windows.Controls;

namespace Hermes_Executor.Views
{
    public partial class ScriptEditor : UserControl
    {
        public ScriptEditor()
        {
            InitializeComponent();
            EditorControl.Text = "-- Hermes Script Editor\nprint(\"Ready!\")";
            EditorControl.TextArea.Caret.PositionChanged += (s, e) =>
            {
                TxtEditorStatus.Text = $"Ln: {EditorControl.TextArea.Caret.Line} | Col: {EditorControl.TextArea.Caret.Column}";
            };
        }

        public string GetScriptText() => EditorControl.Text;
        public void SetScriptText(string text) => EditorControl.Text = text;
        public void Clear() => EditorControl.Clear();
    }
}

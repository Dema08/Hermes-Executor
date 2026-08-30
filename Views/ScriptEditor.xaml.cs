using System.Reflection;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Hermes_Executor.Views
{
    public partial class ScriptEditor : UserControl
    {
        public ScriptEditor()
        {
            InitializeComponent();
            LoadLuaHighlighting();
            EditorControl.Text = "-- Hermes Script Editor\nprint(\"Ready!\")";
            EditorControl.TextArea.Caret.PositionChanged += (s, e) =>
            {
                TxtEditorStatus.Text = $"Ln: {EditorControl.TextArea.Caret.Line} | Col: {EditorControl.TextArea.Caret.Column}";
            };
        }

        private void LoadLuaHighlighting()
        {
            try
            {
                EditorControl.TextArea.TextView.LineTransformers.Add(new Hermes_Executor.Core.LuaColorizer());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lua Highlighting Error: {ex.Message}");
            }
        }

        public string GetScriptText() => EditorControl.Text;
        public void SetScriptText(string text) => EditorControl.Text = text;
        public void Clear() => EditorControl.Clear();
    }
}

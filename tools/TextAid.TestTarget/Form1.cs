namespace TextAid.TestTarget;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Text = "TextAid TestTarget";
        ClientSize = new Size(800, 450);
        var instructions = new Label
        {
            Dock = DockStyle.Top,
            Height = 54,
            Text = "Select some text below, hold Ctrl and press C twice. TextAid should show the captured selection."
        };
        var editor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = "This sample text is available for the TextAid capture walkthrough.\r\nSelect any words and try Ctrl+C+C."
        };
        Controls.Add(editor);
        Controls.Add(instructions);
    }
}

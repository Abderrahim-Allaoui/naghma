using System.Drawing;

namespace Naghma;

/// <summary>Single source of truth for colours. Apply() walks the control tree.</summary>
public static class Theme
{
    public static Color Text { get; private set; } = Color.FromArgb(31, 36, 48);
    public static Color Muted { get; private set; } = Color.FromArgb(107, 114, 128);
    public static Color Accent { get; private set; } = Color.FromArgb(109, 40, 217);
    public static Color Ok { get; private set; } = Color.FromArgb(21, 128, 61);
    public static Color Warn { get; private set; } = Color.FromArgb(180, 83, 9);
    public static Color Error { get; private set; } = Color.FromArgb(185, 28, 28);

    public static Color Bg { get; private set; } = Color.FromArgb(246, 246, 250);
    public static Color Card { get; private set; } = Color.White;
    public static Color Border { get; private set; } = Color.FromArgb(226, 227, 234);
    public static Color LogBg { get; private set; } = Color.FromArgb(22, 22, 27);

    public static bool IsDark { get; private set; }

    public static void Apply(Form form, bool dark)
    {
        IsDark = dark;

        if (dark)
        {
            Text = Color.FromArgb(232, 232, 240);
            Muted = Color.FromArgb(156, 163, 175);
            Accent = Color.FromArgb(167, 139, 250);
            Ok = Color.FromArgb(74, 222, 128);
            Warn = Color.FromArgb(251, 191, 36);
            Error = Color.FromArgb(248, 113, 113);
            Bg = Color.FromArgb(22, 22, 27);
            Card = Color.FromArgb(31, 31, 38);
            Border = Color.FromArgb(52, 52, 62);
            LogBg = Color.FromArgb(15, 15, 19);
        }
        else
        {
            Text = Color.FromArgb(31, 36, 48);
            Muted = Color.FromArgb(107, 114, 128);
            Accent = Color.FromArgb(109, 40, 217);
            Ok = Color.FromArgb(21, 128, 61);
            Warn = Color.FromArgb(180, 83, 9);
            Error = Color.FromArgb(185, 28, 28);
            Bg = Color.FromArgb(246, 246, 250);
            Card = Color.White;
            Border = Color.FromArgb(226, 227, 234);
            LogBg = Color.FromArgb(22, 22, 27);
        }

        form.BackColor = Bg;
        form.ForeColor = Text;

        foreach (Control c in form.Controls) Paint(c);
        form.Invalidate(true);
    }

    private static void Paint(Control c)
    {
        switch (c)
        {
            case RichTextBox rtb:
                rtb.BackColor = LogBg;
                rtb.ForeColor = Color.FromArgb(214, 214, 222);
                return;

            case TabPage page:
                page.BackColor = Bg;
                page.ForeColor = Text;
                break;

            case TabControl tabs:
                tabs.BackColor = Bg;
                tabs.ForeColor = Text;
                break;

            case TextBox tb:
                tb.BackColor = Card;
                tb.ForeColor = Text;
                tb.BorderStyle = BorderStyle.FixedSingle;
                return;

            case ComboBox cb:
                cb.BackColor = Card;
                cb.ForeColor = Text;
                cb.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                break;

            case NumericUpDown nud:
                nud.BackColor = Card;
                nud.ForeColor = Text;
                break;

            case ListBox lb:
                lb.BackColor = Card;
                lb.ForeColor = Text;
                lb.BorderStyle = BorderStyle.FixedSingle;
                return;

            case Button b:
                b.BackColor = IsDark ? Color.FromArgb(45, 45, 55) : Color.FromArgb(240, 240, 246);
                b.ForeColor = Text;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = Border;
                break;

            case CheckBox chk:
                chk.BackColor = Bg;
                chk.ForeColor = Text;
                return;

            case Label lbl:
                lbl.BackColor = Color.Transparent;
                if (lbl.ForeColor != Ok && lbl.ForeColor != Error && lbl.ForeColor != Warn)
                    lbl.ForeColor = lbl.Tag as string == "muted" ? Muted : Text;
                return;

            case ProgressBar _:
            case StatusStrip _:
                return;
        }

        c.BackColor = Bg;
        c.ForeColor = Text;

        foreach (Control child in c.Controls) Paint(child);
    }
}

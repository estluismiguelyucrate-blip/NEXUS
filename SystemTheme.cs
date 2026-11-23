using System.Drawing.Drawing2D;

namespace Nexus;

/// <summary>Shared visual language for NEXUS system windows and notifications.</summary>
public sealed partial class MainForm
{
    private static readonly Color SystemInk = Color.FromArgb(6, 8, 20);
    private static readonly Color SystemSurface = Color.FromArgb(13, 17, 36);
    private static readonly Color SystemLine = Color.FromArgb(93, 80, 159);
    private static readonly Color SystemViolet = Color.FromArgb(153, 111, 255);
    private static readonly Color SystemBlue = Color.FromArgb(87, 164, 255);

    private static Form CreateSystemDialog(string title, Size size)
    {
        var dialog = new Form
        {
            Text = $"NEXUS  //  {title.ToUpperInvariant()}",
            Size = size,
            MinimumSize = size,
            MaximumSize = size,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = SystemInk,
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 9.5f),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            KeyPreview = true
        };
        dialog.Paint += (_, e) =>
        {
            var bounds = new Rectangle(8, 8, dialog.ClientSize.Width - 17, dialog.ClientSize.Height - 17);
            if (bounds.Width < 1 || bounds.Height < 1) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new LinearGradientBrush(bounds, SystemSurface, SystemInk, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(fill, bounds);
            using var edge = new Pen(Color.FromArgb(190, SystemViolet), 1.2f);
            e.Graphics.DrawRectangle(edge, bounds);
            using var glow = new Pen(Color.FromArgb(60, SystemBlue), 3f);
            e.Graphics.DrawLine(glow, 18, 9, dialog.ClientSize.Width - 18, 9);
        };
        return dialog;
    }

    private static void StyleSystemInput(Control control)
    {
        control.Font = new Font("Bahnschrift", 10f);
        control.ForeColor = Color.FromArgb(238, 236, 255);
        switch (control)
        {
            case TextBox text:
                text.BackColor = Color.FromArgb(8, 11, 26);
                text.BorderStyle = BorderStyle.FixedSingle;
                break;
            case RichTextBox richText:
                richText.BackColor = Color.FromArgb(8, 11, 26);
                richText.BorderStyle = BorderStyle.None;
                richText.ReadOnly = true;
                break;
            case ComboBox combo:
                combo.BackColor = Color.FromArgb(8, 11, 26);
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown number:
                number.BackColor = Color.FromArgb(8, 11, 26);
                number.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox list:
                list.BackColor = Color.FromArgb(8, 11, 26);
                list.BorderStyle = BorderStyle.FixedSingle;
                break;
        }
    }

    private DialogResult ShowSystemMessage(
        string message,
        string title = "SISTEMA",
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.Information,
        IWin32Window? owner = null)
    {
        const int width = 500;
        using var textFont = new Font("Bahnschrift", 10f);
        var measured = TextRenderer.MeasureText(message, textFont, new Size(width - 104, 380), TextFormatFlags.WordBreak);
        int messageHeight = Math.Clamp(measured.Height + 8, 38, 340);
        int height = messageHeight + 145;
        using var dialog = CreateSystemDialog(title, new Size(width, height));

        Color accent = icon switch
        {
            MessageBoxIcon.Error or MessageBoxIcon.Stop or MessageBoxIcon.Hand => Color.FromArgb(255, 91, 121),
            MessageBoxIcon.Warning or MessageBoxIcon.Exclamation => Color.FromArgb(255, 197, 92),
            MessageBoxIcon.Question => SystemBlue,
            _ => SystemViolet
        };
        string symbol = icon switch
        {
            MessageBoxIcon.Error or MessageBoxIcon.Stop or MessageBoxIcon.Hand => "×",
            MessageBoxIcon.Warning or MessageBoxIcon.Exclamation => "!",
            MessageBoxIcon.Question => "?",
            _ => "✦"
        };
        var badge = MakeLabel(symbol, 24, accent, true, 26, 23, 46, 46);
        badge.TextAlign = ContentAlignment.MiddleCenter;
        badge.BackColor = Color.FromArgb(32, accent);
        dialog.Controls.Add(badge);
        dialog.Controls.Add(MakeLabel(title.ToUpperInvariant(), 12, Color.White, true, 84, 27, width - 120, 26));
        var body = new RichTextBox
        {
            Text = message,
            Left = 84,
            Top = 59,
            Width = width - 120,
            Height = messageHeight,
            Font = textFont,
            ScrollBars = measured.Height + 8 > messageHeight ? RichTextBoxScrollBars.Vertical : RichTextBoxScrollBars.None
        };
        StyleSystemInput(body);
        dialog.Controls.Add(body);

        var options = GetMessageOptions(buttons);
        int buttonWidth = options.Length == 1 ? 130 : 148;
        int gap = 12;
        int total = options.Length * buttonWidth + (options.Length - 1) * gap;
        int left = width - total - 28;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var button = MakeButton(option.Label, option.Result == DialogResult.Yes || option.Result == DialogResult.OK ? SystemViolet : Color.FromArgb(25, 34, 65), Color.White, left + i * (buttonWidth + gap), height - 66, buttonWidth, 38);
            button.DialogResult = option.Result;
            dialog.Controls.Add(button);
            if (option.IsDefault) dialog.AcceptButton = button;
            if (option.IsCancel) dialog.CancelButton = button;
        }

        return dialog.ShowDialog(owner ?? this);
    }

    private static (string Label, DialogResult Result, bool IsDefault, bool IsCancel)[] GetMessageOptions(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.YesNo => new[] { ("SÍ, CONTINUAR", DialogResult.Yes, true, false), ("CANCELAR", DialogResult.No, false, true) },
        MessageBoxButtons.YesNoCancel => new[] { ("SÍ, CONTINUAR", DialogResult.Yes, true, false), ("NO", DialogResult.No, false, false), ("CANCELAR", DialogResult.Cancel, false, true) },
        MessageBoxButtons.OKCancel => new[] { ("ACEPTAR", DialogResult.OK, true, false), ("CANCELAR", DialogResult.Cancel, false, true) },
        _ => new[] { ("ENTENDIDO", DialogResult.OK, true, true) }
    };
}


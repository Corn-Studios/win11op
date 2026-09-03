using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Win11Optimizer
{
    // Shared helpers for the scrollable "list of cards" tabs (Disk Cleanup, Driver Cleanup).
    // Both tabs build an identical toolbar-button-row / auto-scroll-list / badge-label pattern —
    // this keeps that logic in one place instead of two near-identical copies that can drift.
    public static class UiHelpers
    {
        // Small pill-style label used for size/status/risk badges on list rows.
        public static Label MakeBadge(string text, Color bg, Color fg) => new Label
        {
            Text      = text,
            Font      = new Font("Courier New", 6.5f),
            ForeColor = fg,
            BackColor = bg,
            AutoSize  = true,
            Padding   = new Padding(4, 2, 4, 2)
        };

        // Lays out toolbar buttons right-to-left from the container's right edge.
        // Pass buttons in visual right-to-left order with the gap (px) to leave before the
        // next button to its left — matches the 6px/14px group-separator spacing both
        // cleanup tabs use.
        public static void LayoutButtonsRightToLeft(Control container, int y,
            params (Control control, int gapAfter)[] buttons)
        {
            int r = container.Width - 12;
            foreach (var (control, gapAfter) in buttons)
            {
                control.Location = new Point(r - control.Width, y);
                r -= control.Width + gapAfter;
            }
        }

        // Stacks rows vertically inside innerPanel starting at startY, with `spacing` px
        // between rows, and resizes innerPanel.Height to fit. Returns the final Y — callers
        // with extra content below the list (banners, empty-state labels) can use it to
        // append further layout after the row stack.
        public static int ReflowRowsVertically(Panel innerPanel, IEnumerable<Control> rows, int startY, int spacing = 2)
        {
            int y = startY;
            foreach (var row in rows)
            {
                row.Width    = innerPanel.Width;
                row.Location = new Point(0, y);
                y += row.Height + spacing;
            }
            innerPanel.Height = y;
            return y;
        }
    }
}

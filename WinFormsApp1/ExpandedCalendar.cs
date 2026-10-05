using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.ComponentModel;



// Isang item sa calendar
public class CalEvent
{
    public int Id { get; set; }                  // activity_id o quiz_id
    public DateTime Date { get; set; }
    public string Kind { get; set; } = "";       // "Exam", "Quiz", "Activity", "Reminder"
    public string Text { get; set; } = "";
    public bool Done { get; set; }               // true kung na-submit na
}

public class ExpandedCalendar : UserControl
{
    // true = lahat ng Activity/Quiz/Exam ay clickable (gamit ng professor)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AllClickable { get; set; }

    private static readonly string[] KindOrder = { "Exam", "Quiz", "Activity", "Reminder" };

    private readonly Label lblTime = new Label();
    private readonly Label lblDate = new Label();
    private readonly Label lblMonth = new Label();
    private readonly Button btnPrev = new Button();
    private readonly Button btnNext = new Button();
    private readonly Button btnClose = new Button();
    private readonly TableLayoutPanel grid = new TableLayoutPanel();
    private readonly Label[] cells = new Label[42];
    private readonly Label lblToday = new Label();
    private readonly TextBox txtEvent = new TextBox();
    private readonly FlowLayoutPanel pnlEvents = new FlowLayoutPanel();
    private readonly Timer clock = new Timer { Interval = 1000 };

    private DateTime viewMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime selected = DateTime.Today;

    private List<CalEvent> external = new List<CalEvent>();            // galing sa database
    private readonly List<CalEvent> reminders = new List<CalEvent>();  // tinype sa textbox
    private Dictionary<DateTime, List<CalEvent>> byDate = new Dictionary<DateTime, List<CalEvent>>();

    private Bitmap? backdrop;

    public event EventHandler? CloseRequested;
    public event EventHandler<CalEvent>? EventOpened;

    public ExpandedCalendar()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        BackColor = Color.FromArgb(30, 36, 46);
        ForeColor = Color.White;
        Size = new Size(340, 720);
        Disposed += (s, e) => backdrop?.Dispose();

        // Oras
        lblTime.AutoSize = true;
        lblTime.Font = new Font("Segoe UI Light", 28f);
        lblTime.Location = new Point(18, 12);

        // Petsa
        lblDate.AutoSize = true;
        lblDate.Font = new Font("Segoe UI", 10f);
        lblDate.ForeColor = Color.FromArgb(120, 180, 240);
        lblDate.Location = new Point(22, 68);

        // =========================================================
        // HIDE AGENDA BUTTON — MOVED TO TOP-RIGHT
        // =========================================================

        btnClose.Text = "Hide agenda  ⌃";
        btnClose.FlatStyle = FlatStyle.Flat;
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 80, 95);
        btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 100, 115);
        btnClose.BackColor = Color.Transparent;
        btnClose.ForeColor = Color.FromArgb(120, 180, 240);
        btnClose.AutoSize = true;
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.Location = new Point(Width - 130, 14);
        btnClose.Cursor = Cursors.Hand;
        btnClose.Click += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        // Month + arrows
        lblMonth.AutoSize = true;
        lblMonth.Font = new Font("Segoe UI", 10.5f);
        lblMonth.Location = new Point(20, 112);

        StyleArrow(btnPrev, "▲", new Point(250, 104));
        StyleArrow(btnNext, "▼", new Point(285, 104));
        btnPrev.Click += (s, e) => { viewMonth = viewMonth.AddMonths(-1); RefreshGrid(); };
        btnNext.Click += (s, e) => { viewMonth = viewMonth.AddMonths(1); RefreshGrid(); };

        // Grid
        grid.Location = new Point(14, 145);
        grid.Size = new Size(312, 252);
        grid.ColumnCount = 7;
        grid.RowCount = 7;
        for (int c = 0; c < 7; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7));
        for (int r = 0; r < 7; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7));

        string[] days = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
        for (int c = 0; c < 7; c++)
            grid.Controls.Add(MakeCell(days[c], false), c, 0);

        for (int i = 0; i < 42; i++)
        {
            cells[i] = MakeCell("", true);
            cells[i].Click += Cell_Click;
            cells[i].Paint += Cell_Paint;
            grid.Controls.Add(cells[i], i % 7, i / 7 + 1);
        }

        // Today section
        lblToday.Text = "Today";
        lblToday.AutoSize = true;
        lblToday.Font = new Font("Segoe UI Semibold", 10.5f);
        lblToday.Location = new Point(20, 420);

        txtEvent.Location = new Point(20, 452);
        txtEvent.Width = 300;
        txtEvent.Font = new Font("Segoe UI", 10f);
        txtEvent.BackColor = Color.FromArgb(45, 52, 64);
        txtEvent.ForeColor = Color.White;
        txtEvent.BorderStyle = BorderStyle.FixedSingle;
        txtEvent.PlaceholderText = "Add an event or reminder";
        txtEvent.KeyDown += TxtEvent_KeyDown;

        // Listahan ng events (may scroll)
        pnlEvents.Location = new Point(20, 492);
        pnlEvents.Size = new Size(305, 178);
        pnlEvents.FlowDirection = FlowDirection.TopDown;
        pnlEvents.WrapContents = false;
        pnlEvents.AutoScroll = true;
        pnlEvents.BackColor = Color.Transparent;
        pnlEvents.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[] {
            lblTime, lblDate, btnClose, lblMonth, btnPrev, btnNext, grid,
            lblToday, txtEvent, pnlEvents });

        grid.BackColor = Color.Transparent;
        foreach (var l in new[] { lblTime, lblDate, lblMonth, lblToday })
            l.BackColor = Color.Transparent;

        clock.Tick += (s, e) => UpdateClock();
        VisibleChanged += (s, e) => { clock.Enabled = Visible; if (Visible) UpdateClock(); };

        Reindex();
        RefreshGrid();
        ShowEvents();
        UpdateClock();

        // Ensure the top-right position updates on first layout
        this.Resize += (s, e) =>
        {
            btnClose.Location = new Point(Width - btnClose.Width - 15, 14);
        };
    }

    public void SetEvents(IEnumerable<CalEvent> items)
    {
        external = (items ?? Enumerable.Empty<CalEvent>()).ToList();
        Reindex();
        RefreshGrid();
        ShowEvents();
    }

    public void ShowOn(Control parent)
    {
        Visible = false;
        if (Parent != parent) parent.Controls.Add(this);

        Size = new Size(340, parent.ClientSize.Height);
        Location = new Point(parent.ClientSize.Width - Width, 0);
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

        // Reposition Hide agenda button after resize
        btnClose.Location = new Point(Width - btnClose.Width - 15, 14);

        CaptureBackdrop(parent);

        Visible = true;
        BringToFront();
    }

    private void CaptureBackdrop(Control parent)
    {
        backdrop?.Dispose();
        backdrop = null;

        try
        {
            using (var full = new Bitmap(parent.ClientSize.Width, parent.ClientSize.Height))
            {
                parent.DrawToBitmap(full, new Rectangle(0, 0, full.Width, full.Height));

                var crop = new Rectangle(Left, 0, Width, Height);
                crop.Intersect(new Rectangle(0, 0, full.Width, full.Height));

                using (var piece = full.Clone(crop, full.PixelFormat))
                    backdrop = Blur(piece, 10);
            }
        }
        catch { backdrop = null; }
    }

    private static Bitmap Blur(Bitmap src, int factor)
    {
        int w = Math.Max(1, src.Width / factor);
        int h = Math.Max(1, src.Height / factor);

        using (var small = new Bitmap(w, h))
        {
            using (var g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(src, 0, 0, w, h);
            }

            var result = new Bitmap(src.Width, src.Height);
            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(small, 0, 0, src.Width, src.Height);
            }
            return result;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (backdrop != null)
            e.Graphics.DrawImage(backdrop, 0, 0, Width, Height);

        using (var tint = new SolidBrush(Color.FromArgb(backdrop != null ? 185 : 255, 24, 30, 42)))
            e.Graphics.FillRectangle(tint, ClientRectangle);

        using (var edge = new Pen(Color.FromArgb(90, 255, 255, 255)))
            e.Graphics.DrawLine(edge, 0, 0, 0, Height);
    }

    private static Color ColorFor(string kind)
    {
        switch (kind)
        {
            case "Exam": return Color.FromArgb(231, 76, 60);
            case "Quiz": return Color.FromArgb(175, 122, 197);
            case "Activity": return Color.FromArgb(243, 156, 18);
            default: return Color.FromArgb(46, 204, 113);
        }
    }

    private void Reindex()
    {
        byDate = external.Concat(reminders)
            .GroupBy(x => x.Date.Date)
            .ToDictionary(g => g.Key,
                          g => g.OrderBy(x => Array.IndexOf(KindOrder, x.Kind)).ToList());
    }

    private List<CalEvent> EventsOn(DateTime d)
    {
        return byDate.TryGetValue(d.Date, out var list) ? list : new List<CalEvent>();
    }

    private void Cell_Paint(object? sender, PaintEventArgs e)
    {
        var cell = (Label)sender!;
        if (!(cell.Tag is DateTime d)) return;

        var items = EventsOn(d);
        if (items.Count == 0) return;

        var kinds = KindOrder
            .Where(k => items.Any(x => x.Kind == k))
            .Select(k => new { Kind = k, Pending = items.Any(x => x.Kind == k && !x.Done) })
            .ToList();

        const int size = 5, gap = 3;
        int total = kinds.Count * size + (kinds.Count - 1) * gap;
        int x0 = (cell.Width - total) / 2;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var k in kinds)
        {
            Color c = ColorFor(k.Kind);
            var r = new Rectangle(x0, 3, size, size);

            if (k.Pending)
                using (var b = new SolidBrush(c)) e.Graphics.FillEllipse(b, r);
            else
                using (var p = new Pen(c, 1.3f)) e.Graphics.DrawEllipse(p, r);

            x0 += size + gap;
        }
    }

    private void ShowEvents()
    {
        lblToday.Text = selected.Date == DateTime.Today ? "Today" : selected.ToString("MMMM d", CultureInfo.InvariantCulture);

        pnlEvents.SuspendLayout();
        foreach (Control c in pnlEvents.Controls.Cast<Control>().ToList())
            c.Dispose();

        var items = EventsOn(selected);
        if (items.Count == 0)
        {
            pnlEvents.Controls.Add(new Label
            {
                Text = "No events",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            });
        }
        else
        {
            foreach (var ev in items)
                pnlEvents.Controls.Add(MakeEventRow(ev));
        }

        pnlEvents.ResumeLayout();
    }

    private Control MakeEventRow(CalEvent ev)
    {
        bool clickable = AllClickable
            ? ev.Kind != "Reminder"
            : (ev.Kind == "Activity" || ((ev.Kind == "Quiz" || ev.Kind == "Exam") && !ev.Done));

        var normalFont = new Font("Segoe UI", 9.5f);
        var hoverFont = new Font("Segoe UI", 9.5f, FontStyle.Underline);
        Color normalColor = ev.Done ? Color.FromArgb(150, 150, 150) : Color.White;

        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8),
            Cursor = clickable ? Cursors.Hand : Cursors.Default
        };

        var dot = new Label
        {
            Text = "●",
            AutoSize = true,
            ForeColor = ColorFor(ev.Kind),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0, 0, 4, 0),
            Cursor = row.Cursor
        };

        var text = new Label
        {
            Text = (ev.Done ? "✔ " : "") + ev.Kind + " – " + ev.Text,
            AutoSize = true,
            MaximumSize = new Size(255, 0),
            ForeColor = normalColor,
            BackColor = Color.Transparent,
            Font = normalFont,
            Margin = new Padding(0),
            Cursor = row.Cursor
        };

        if (clickable)
        {
            EventHandler open = (s, e) => EventOpened?.Invoke(this, ev);
            row.Click += open;
            dot.Click += open;
            text.Click += open;

            text.MouseEnter += (s, e) => { text.Font = hoverFont; text.ForeColor = Color.FromArgb(120, 180, 240); };
            text.MouseLeave += (s, e) => { text.Font = normalFont; text.ForeColor = normalColor; };
        }

        row.Controls.Add(dot);
        row.Controls.Add(text);
        return row;
    }

    private static void StyleArrow(Button b, string text, Point loc)
    {
        b.Text = text;
        b.Size = new Size(30, 30);
        b.Location = loc;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 80, 95);
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 100, 115);
        b.BackColor = Color.Transparent;
        b.ForeColor = Color.White;
        b.Cursor = Cursors.Hand;
    }

    private static Label MakeCell(string text, bool clickable)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(1),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Cursor = clickable ? Cursors.Hand : Cursors.Default
        };
    }

    private void UpdateClock()
    {
        lblTime.Text = DateTime.Now.ToString("h:mm:ss", CultureInfo.InvariantCulture)
                     + " " + DateTime.Now.ToString("tt", CultureInfo.InvariantCulture).ToLower();
    }

    private void RefreshGrid()
    {
        lblMonth.Text = viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        lblDate.Text = selected.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);

        DateTime start = viewMonth.AddDays(-(int)viewMonth.DayOfWeek);
        for (int i = 0; i < 42; i++)
        {
            DateTime d = start.AddDays(i);
            Label c = cells[i];
            c.Text = d.Day.ToString();
            c.Tag = d;

            bool inMonth = d.Month == viewMonth.Month;
            bool isToday = d.Date == DateTime.Today;
            bool isSel = d.Date == selected.Date;

            c.BackColor = isToday ? Color.FromArgb(0, 120, 215)
                        : isSel ? Color.FromArgb(75, 85, 100)
                        : Color.Transparent;
            c.ForeColor = inMonth || isToday ? Color.White : Color.FromArgb(120, 120, 120);
            c.Invalidate();
        }
    }

    private void Cell_Click(object? sender, EventArgs e)
    {
        selected = ((DateTime)((Label)sender!).Tag!).Date;
        viewMonth = new DateTime(selected.Year, selected.Month, 1);
        RefreshGrid();
        ShowEvents();
    }

    private void TxtEvent_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || string.IsNullOrWhiteSpace(txtEvent.Text)) return;
        e.SuppressKeyPress = true;

        reminders.Add(new CalEvent
        {
            Date = selected.Date,
            Kind = "Reminder",
            Text = txtEvent.Text.Trim()
        });
        txtEvent.Clear();

        Reindex();
        RefreshGrid();
        ShowEvents();
    }
}
using Gtk;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace CalendarApp;

public class MainWindow : Window
{
    private Calendar calendar;
    private TextView eventTextView;
    private List<CalendarEvent> events;
    private string eventsFile = "calendar_events.json";
    private Label dateLabel;
    private const int WINDOW_WIDTH = 800;
    private const int WINDOW_HEIGHT = 600;
    private const int PADDING = 10;
    private Gdk.Color eventColor = new Gdk.Color(0, 128, 255);
    private Gdk.Color selectedColor = new Gdk.Color(255, 200, 0);

    public MainWindow() : base("Calendar App")
    {
        SetDefaultSize(WINDOW_WIDTH, WINDOW_HEIGHT);
        DeleteEvent += OnDeleteEvent;

        events = new List<CalendarEvent>();
        LoadEvents();

        var mainBox = new VBox(false, PADDING);
        mainBox.BorderWidth = PADDING;
        Add(mainBox);

        var headerBox = new HBox(false, PADDING);
        var titleLabel = new Label("<span size='x-large' weight='bold'>Calendar App</span>");
        titleLabel.UseMarkup = true;
        headerBox.PackStart(titleLabel, false, false, 0);
        mainBox.PackStart(headerBox, false, false, 0);

        var contentBox = new VBox(false, PADDING);
        mainBox.PackStart(contentBox, true, true, 0);

        dateLabel = new Label();
        dateLabel.UseMarkup = true;
        UpdateDateLabel(DateTime.Now);
        contentBox.PackStart(dateLabel, false, false, 0);

        calendar = new Calendar();
        calendar.DaySelected += OnDaySelected;
        calendar.DaySelectedDoubleClick += OnDaySelectedDoubleClick;
        calendar.MonthChanged += OnMonthChanged;
        contentBox.PackStart(calendar, true, true, 0);

        var legendBox = new HBox(false, PADDING);
        contentBox.PackStart(legendBox, false, false, 0);

        var eventLegend = new HBox(false, 5);
        var eventColorBox = new DrawingArea();
        eventColorBox.SetSizeRequest(20, 20);
        eventColorBox.Drawn += (o, args) => {
            var cr = args.Cr;
            cr.SetSourceRGB(0, 0.5, 1);
            cr.Rectangle(0, 0, 20, 20);
            cr.Fill();
        };
        var eventLabel = new Label("Has Event");
        eventLegend.PackStart(eventColorBox, false, false, 0);
        eventLegend.PackStart(eventLabel, false, false, 0);
        legendBox.PackStart(eventLegend, false, false, 0);

        var selectedLegend = new HBox(false, 5);
        var selectedColorBox = new DrawingArea();
        selectedColorBox.SetSizeRequest(20, 20);
        selectedColorBox.Drawn += (o, args) => {
            var cr = args.Cr;
            cr.SetSourceRGB(1, 0.8, 0);
            cr.Rectangle(0, 0, 20, 20);
            cr.Fill();
        };
        var selectedLabel = new Label("Selected Date");
        selectedLegend.PackStart(selectedColorBox, false, false, 0);
        selectedLegend.PackStart(selectedLabel, false, false, 0);
        legendBox.PackStart(selectedLegend, false, false, 0);

        var eventBox = new VBox(false, PADDING);
        contentBox.PackStart(eventBox, false, false, 0);

        var eventLabel2 = new Label("<span weight='bold'>Event Details</span>");
        eventLabel2.UseMarkup = true;
        eventBox.PackStart(eventLabel2, false, false, 0);

        var scrolledWindow = new ScrolledWindow();
        scrolledWindow.ShadowType = ShadowType.In;
        eventTextView = new TextView();
        eventTextView.Editable = true;
        eventTextView.WrapMode = WrapMode.Word;
        scrolledWindow.Add(eventTextView);
        eventBox.PackStart(scrolledWindow, true, true, 0);

        var buttonBox = new HBox(false, PADDING);
        eventBox.PackStart(buttonBox, false, false, 0);

        var saveButton = new Button("Save Event");
        saveButton.Clicked += OnSaveClicked;
        var clearButton = new Button("Clear");
        clearButton.Clicked += OnClearClicked;
        var deleteButton = new Button("Delete Event");
        deleteButton.Clicked += OnDeleteClicked;

        buttonBox.PackStart(saveButton, true, true, 0);
        buttonBox.PackStart(clearButton, true, true, 0);
        buttonBox.PackStart(deleteButton, true, true, 0);

        var statusBar = new HBox(false, PADDING);
        mainBox.PackStart(statusBar, false, false, 0);

        var statusLabel = new Label("Ready");
        statusBar.PackStart(statusLabel, false, false, 0);

        UpdateCalendarColors();
        ShowAll();
    }

    private void UpdateDateLabel(DateTime date)
    {
        dateLabel.Markup = $"<span size='large'>{date.ToString("MMMM d, yyyy")}</span>";
    }

    private void UpdateCalendarColors()
    {
        var currentMonth = calendar.Date;
        var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        
        for (uint day = 1; day <= daysInMonth; day++)
        {
            calendar.UnmarkDay(day);
        }

        var monthEvents = events.Where(e => 
            e.Date.Year == currentMonth.Year && 
            e.Date.Month == currentMonth.Month).ToList();

        foreach (var evt in monthEvents)
        {
            calendar.MarkDay((uint)evt.Date.Day);
        }
    }

    private void LoadEvents()
    {
        try
        {
            if (File.Exists(eventsFile))
            {
                var json = File.ReadAllText(eventsFile);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var loadedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(json);
                    if (loadedEvents != null)
                    {
                        events = loadedEvents;
                    }
                }
            }
        }
        catch
        {
            events = new List<CalendarEvent>();
        }
    }

    private void SaveEvents()
    {
        try
        {
            var json = JsonSerializer.Serialize(events);
            File.WriteAllText(eventsFile, json);
        }
        catch
        {
        }
    }

    private void OnDaySelected(object? sender, EventArgs e)
    {
        var date = calendar.Date;
        UpdateDateLabel(date);
        var selectedEvent = events.Find(e => e.Date.Date == date.Date);
        eventTextView.Buffer.Text = selectedEvent?.Description ?? "";
    }

    private void OnMonthChanged(object? sender, EventArgs e)
    {
        UpdateCalendarColors();
    }

    private void OnDaySelectedDoubleClick(object? sender, EventArgs e)
    {
        OnClearClicked(sender, e);
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var date = calendar.Date;
        var description = eventTextView.Buffer.Text;

        var existingEvent = events.Find(e => e.Date.Date == date.Date);
        if (existingEvent != null)
        {
            existingEvent.Description = description;
        }
        else
        {
            events.Add(new CalendarEvent { Date = date, Description = description });
        }

        SaveEvents();
        UpdateCalendarColors();
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        eventTextView.Buffer.Text = "";
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        var date = calendar.Date;
        events.RemoveAll(e => e.Date.Date == date.Date);
        eventTextView.Buffer.Text = "";
        SaveEvents();
        UpdateCalendarColors();
    }

    private void OnDeleteEvent(object? sender, DeleteEventArgs args)
    {
        Application.Quit();
        args.RetVal = true;
    }
}

public class CalendarEvent
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
} 
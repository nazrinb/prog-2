using Gtk;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace CalendarApp;

public class EventDialog : Dialog
{
    private ComboBox categoryComboBox;
    private Entry eventNameEntry;
    private List<string> categories = new List<string> { "Personal", "Work", "Other" };
    private DateTime selectedDate;
    private List<CalendarEvent> events;
    private string eventsFile;
    private MainWindow mainWindow;

    public EventDialog(MainWindow parent, DateTime date, List<CalendarEvent> events, string eventsFile) 
        : base("Event Details", parent, DialogFlags.Modal)
    {
        this.selectedDate = date;
        this.events = events;
        this.eventsFile = eventsFile;
        this.mainWindow = parent;

        SetDefaultSize(500, 250);
        this.BorderWidth = 15;
        this.WindowPosition = WindowPosition.Center;

        var mainBox = new VBox(false, 15);
        mainBox.BorderWidth = 15;
        this.ContentArea.Add(mainBox);

        // Date label with larger font
        var dateLabel = new Label($"<span size='xx-large' weight='bold' color='#023047'>{date.ToString("MMMM d, yyyy")}</span>");
        dateLabel.UseMarkup = true;
        mainBox.PackStart(dateLabel, false, false, 0);

        // Event name entry
        var eventNameBox = new HBox(false, 10);
        var eventNameLabel = new Label("<span color='#023047'>Event Name:</span>");
        eventNameLabel.UseMarkup = true;
        eventNameEntry = new Entry();
        eventNameEntry.PlaceholderText = "Enter event name...";
        eventNameBox.PackStart(eventNameLabel, false, false, 0);
        eventNameBox.PackStart(eventNameEntry, true, true, 0);
        mainBox.PackStart(eventNameBox, false, false, 0);

        // Category selection
        var categoryBox = new HBox(false, 10);
        var categoryLabel = new Label("<span color='#023047'>Category:</span>");
        categoryLabel.UseMarkup = true;
        categoryComboBox = new ComboBox(categories.ToArray());
        categoryComboBox.Active = 0;
        categoryBox.PackStart(categoryLabel, false, false, 0);
        categoryBox.PackStart(categoryComboBox, true, true, 0);
        mainBox.PackStart(categoryBox, false, false, 0);

        // Load existing event for this date
        var existingEvent = events.FirstOrDefault(e => e.Date.Date == date.Date);
        if (existingEvent != null)
        {
            eventNameEntry.Text = existingEvent.EventName;
            var categoryIndex = categories.FindIndex(c => c.Equals(existingEvent.Category, StringComparison.OrdinalIgnoreCase));
            if (categoryIndex >= 0)
            {
                categoryComboBox.Active = categoryIndex;
            }
        }

        // Buttons
        var buttonBox = new HBox(false, 10);
        mainBox.PackStart(buttonBox, false, false, 0);

        var saveButton = new Button("Save Event");
        saveButton.StyleContext.AddClass("save-button");
        saveButton.Clicked += OnSaveClicked;

        var clearButton = new Button("Clear Event");
        clearButton.StyleContext.AddClass("clear-button");
        clearButton.Clicked += OnClearClicked;

        buttonBox.PackStart(saveButton, true, true, 0);
        buttonBox.PackStart(clearButton, true, true, 0);

        // Add CSS styling
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData(@"
            * {
                color: #023047;
            }
            .header { 
                background-color: #023047;
                border-radius: 0px;
            }
            .header label {
                color: white;
            }
            .calendar { 
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 4px rgba(2, 48, 71, 0.1);
            }
            .calendar button {
                padding: 8px;
                margin: 2px;
                border-radius: 4px;
                color: #023047;
            }
            .calendar button:active {
                background-color: rgba(33, 158, 188, 0.3);
                color: #023047;
            }
            .calendar button:selected {
                background-color: transparent;
                color: #023047;
            }
            .calendar button:checked {
                background-color: rgba(33, 158, 188, 0.3);
                color: #023047;
            }
            .calendar label {
                font-family: 'Segoe UI', Arial, sans-serif;
                font-size: 12px;
                color: #023047;
            }
            .calendar .header {
                font-family: 'Segoe UI', Arial, sans-serif;
                font-size: 14px;
                font-weight: bold;
                color: #023047;
            }
            .calendar button.day-with-event {
                font-weight: bold;
            }
            .calendar button.day-with-event.personal {
                color: #023047;
                background-color: rgba(255, 183, 3, 0.3);
            }
            .calendar button.day-with-event.work {
                color: #023047;
                background-color: rgba(33, 158, 188, 0.3);
            }
            .calendar button.day-with-event.other {
                color: #023047;
                background-color: rgba(251, 133, 0, 0.3);
            }
            entry {
                color: #023047;
                padding: 5px;
            }
            entry:focus {
                border-color: #219ebc;
            }
            .save-button { 
                background-color: #219ebc; 
                color: white; 
            }
            .clear-button { 
                background-color: #fb8500; 
                color: white; 
            }
        ");
        StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);

        ShowAll();
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var category = categories[categoryComboBox.Active];
        var eventName = eventNameEntry.Text.Trim();

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            // Remove any existing event for this date
            events.RemoveAll(e => e.Date.Date == selectedDate.Date);

            // Add new event with name and category
            events.Add(new CalendarEvent 
            { 
                Date = selectedDate,
                EventName = eventName,
                Category = category
            });

            SaveEvents();
            mainWindow.UpdateCalendarColors(); // Update calendar immediately
            Respond(ResponseType.Ok);
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        // Remove any existing event for this date
        events.RemoveAll(e => e.Date.Date == selectedDate.Date);
        SaveEvents();
        mainWindow.UpdateCalendarColors(); // Update calendar immediately
        Respond(ResponseType.Ok);
    }

    private void SaveEvents()
    {
        try
        {
            var json = JsonSerializer.Serialize(events);
            File.WriteAllText(eventsFile, json);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error serializing events: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error writing events file: {ex.Message}");
        }
    }
}

public class CalendarEvent
{
    public DateTime Date { get; set; }
    public string EventName { get; set; } = "";
    public string Category { get; set; } = "Personal";
}

public class MainWindow : Window
{
    private Calendar calendar;
    private List<CalendarEvent> events;
    private string eventsFile = "calendar_events.json";
    private const int WINDOW_WIDTH = 600;
    private const int WINDOW_HEIGHT = 600;
    private const int PADDING = 20;
    private Gdk.Color personalColor = new Gdk.Color(255, 165, 0);    // Orange
    private Gdk.Color workColor = new Gdk.Color(41, 128, 185);      // Dark Blue
    private Gdk.Color otherColor = new Gdk.Color(241, 196, 15);     // Yellow
    private Gdk.Color headerColor = new Gdk.Color(52, 73, 94);
    private Gdk.Color todayColor = new Gdk.Color(46, 204, 113);
    private DateTime lastClickTime = DateTime.MinValue;
    private const int DOUBLE_CLICK_INTERVAL = 300; // milliseconds

    public MainWindow() : base("Calendar App")
    {
        SetDefaultSize(WINDOW_WIDTH, WINDOW_HEIGHT);
        DeleteEvent += OnDeleteEvent;

        // Set window properties
        this.BorderWidth = 0;
        this.Resizable = false;
        this.WindowPosition = WindowPosition.Center;
        this.TypeHint = Gdk.WindowTypeHint.Dialog;

        events = new List<CalendarEvent>();
        LoadEvents();

        // Main container
        var mainBox = new VBox(false, 0);
        mainBox.BorderWidth = 0;
        Add(mainBox);

        // Header
        var headerBox = new HBox(false, PADDING);
        headerBox.BorderWidth = PADDING;
        headerBox.StyleContext.AddClass("header");
        
        var titleLabel = new Label("<span size='x-large' weight='bold' foreground='#ffffff'>Calendar</span>");
        titleLabel.UseMarkup = true;
        headerBox.PackStart(titleLabel, false, false, 0);
        mainBox.PackStart(headerBox, false, false, 0);

        // Calendar container with padding
        var calendarBox = new VBox(false, 0);
        calendarBox.BorderWidth = PADDING;
        mainBox.PackStart(calendarBox, true, true, 0);

        // Calendar
        calendar = new Calendar();
        calendar.MonthChanged += OnMonthChanged;
        calendar.StyleContext.AddClass("calendar");
        
        // Set calendar properties
        calendar.DisplayOptions = CalendarDisplayOptions.ShowHeading | 
                                CalendarDisplayOptions.ShowDayNames | 
                                CalendarDisplayOptions.ShowWeekNumbers;

        // Add button press event handler
        calendar.ButtonPressEvent += OnCalendarButtonPress;
        
        calendarBox.PackStart(calendar, true, true, 0);

        // Add CSS styling
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData(@"
            * {
                color: #023047;
            }
            .header { 
                background-color: #023047;
                border-radius: 0px;
                padding: 15px;
            }
            .header label {
                color: white;
                font-size: 24px;
            }
            .calendar { 
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 4px rgba(2, 48, 71, 0.1);
                padding: 10px;
            }
            calendar {
                color: #023047;
                font-size: 16px;
                padding: 10px;
            }
            calendar button {
                color: #023047;
                font-size: 16px;
                padding: 10px;
                border-radius: 6px;
            }
            calendar button.day {
                padding: 8px;
                margin: 2px;
            }
            calendar button.day:hover {
                background-color: rgba(2, 48, 71, 0.1);
            }
            calendar button.day.selected {
                background-color: rgba(33, 158, 188, 0.4);
                font-weight: bold;
            }
            calendar button.day.has-event {
                background-color: rgba(255, 183, 3, 0.4);
                border: 2px solid rgba(255, 183, 3, 0.8);
                font-weight: bold;
            }
            calendar.header {
                font-size: 18px;
                font-weight: bold;
                padding: 10px;
            }
            entry {
                color: #023047;
                padding: 8px;
                font-size: 16px;
            }
            entry:focus {
                border-color: #219ebc;
            }
            .save-button { 
                background-color: #219ebc; 
                color: white; 
                padding: 10px;
                font-size: 16px;
            }
            .clear-button { 
                background-color: #fb8500; 
                color: white; 
                padding: 10px;
                font-size: 16px;
            }
        ");
        StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);

        // Customize calendar appearance
        CustomizeCalendar();

        UpdateCalendarColors();
        ShowAll();
    }

    private void OnCalendarButtonPress(object? sender, ButtonPressEventArgs args)
    {
        if (args.Event.Button == 1) // Left mouse button
        {
            var now = DateTime.Now;
            var timeSinceLastClick = (now - lastClickTime).TotalMilliseconds;
            
            if (timeSinceLastClick < DOUBLE_CLICK_INTERVAL)
            {
                // Double click
                ShowEventDialog();
            }
            else
            {
                // Single click
                ShowEventDialog();
            }
            
            lastClickTime = now;
        }
    }

    private void ShowEventDialog()
    {
        var date = calendar.Date;
        var dialog = new EventDialog(this, date, events, eventsFile);
        dialog.Run();
        dialog.Destroy();
        UpdateCalendarColors();
    }

    private void CustomizeCalendar()
    {
        // Set header font
        var headerFont = Pango.FontDescription.FromString("Segoe UI 18");
        calendar.StyleContext.AddClass("calendar-header");
        
        // Set day names font
        var dayFont = Pango.FontDescription.FromString("Segoe UI 16");
        calendar.StyleContext.AddClass("calendar");
        
        // Set numbers font
        var numberFont = Pango.FontDescription.FromString("Segoe UI 16");
        calendar.StyleContext.AddClass("calendar");
    }

    public void UpdateCalendarColors()
    {
        var currentMonth = calendar.Date;
        var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        
        // Clear all marks first
        for (uint day = 1; day <= daysInMonth; day++)
        {
            calendar.UnmarkDay(day);
        }

        // Mark days with events
        var monthEvents = events.Where(e => 
            e.Date.Year == currentMonth.Year && 
            e.Date.Month == currentMonth.Month).ToList();

        foreach (var evt in monthEvents)
        {
            var day = (uint)evt.Date.Day;
            calendar.MarkDay(day);
            calendar.StyleContext.AddClass("has-event");
        }
    }

    private void OnMonthChanged(object? sender, EventArgs e)
    {
        UpdateCalendarColors();
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
        catch (JsonException ex)
        {
            Console.WriteLine($"Error loading events: {ex.Message}");
            events = new List<CalendarEvent>();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading events file: {ex.Message}");
            events = new List<CalendarEvent>();
        }
    }

    private void OnDeleteEvent(object? sender, DeleteEventArgs args)
    {
        Application.Quit();
        args.RetVal = true;
    }
} 
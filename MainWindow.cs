using Gtk;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace CalendarApp;

public class EventDialog : Dialog
{
    private TextView eventTextView;
    private ComboBox categoryComboBox;
    private List<string> categories = new List<string> { "Personal", "Work", "Other" };
    private DateTime selectedDate;
    private List<CalendarEvent> events;
    private string eventsFile;

    public EventDialog(Window parent, DateTime date, List<CalendarEvent> events, string eventsFile) 
        : base("Event Details", parent, DialogFlags.Modal)
    {
        this.selectedDate = date;
        this.events = events;
        this.eventsFile = eventsFile;

        SetDefaultSize(400, 300);
        this.BorderWidth = 10;
        this.WindowPosition = WindowPosition.Center;

        var mainBox = new VBox(false, 10);
        mainBox.BorderWidth = 10;
        this.ContentArea.Add(mainBox);

        // Date label
        var dateLabel = new Label($"<span size='large' weight='bold' color='#023047'>{date.ToString("MMMM d, yyyy")}</span>");
        dateLabel.UseMarkup = true;
        mainBox.PackStart(dateLabel, false, false, 0);

        // Category selection
        var categoryBox = new HBox(false, 10);
        var categoryLabel = new Label("<span color='#023047'>Category:</span>");
        categoryLabel.UseMarkup = true;
        categoryComboBox = new ComboBox(categories.ToArray());
        categoryComboBox.Active = 0;
        categoryBox.PackStart(categoryLabel, false, false, 0);
        categoryBox.PackStart(categoryComboBox, true, true, 0);
        mainBox.PackStart(categoryBox, false, false, 0);

        // Event text area
        var scrolledWindow = new ScrolledWindow();
        scrolledWindow.ShadowType = ShadowType.In;
        scrolledWindow.SetSizeRequest(-1, 150);
        eventTextView = new TextView();
        eventTextView.Editable = true;
        eventTextView.WrapMode = WrapMode.Word;
        eventTextView.StyleContext.AddClass("event-text");
        scrolledWindow.Add(eventTextView);
        mainBox.PackStart(scrolledWindow, true, true, 0);

        // Load existing events for this date
        var dayEvents = events.Where(e => e.Date.Date == date.Date).ToList();
        if (dayEvents.Any())
        {
            var eventText = string.Join("\n\n", dayEvents.Select(evt => 
                $"Category: {evt.Category}\n{evt.Description}"));
            eventTextView.Buffer.Text = eventText;
            
            // Set the category combo box to match the first event's category
            var firstEvent = dayEvents.First();
            var categoryIndex = categories.FindIndex(c => c.Equals(firstEvent.Category, StringComparison.OrdinalIgnoreCase));
            if (categoryIndex >= 0)
            {
                categoryComboBox.Active = categoryIndex;
            }
        }

        // Buttons
        var buttonBox = new HBox(false, 10);
        mainBox.PackStart(buttonBox, false, false, 0);

        var saveButton = new Button("Save");
        saveButton.StyleContext.AddClass("save-button");
        saveButton.Clicked += OnSaveClicked;

        var clearButton = new Button("Clear");
        clearButton.StyleContext.AddClass("clear-button");
        clearButton.Clicked += OnClearClicked;

        var deleteButton = new Button("Delete");
        deleteButton.StyleContext.AddClass("delete-button");
        deleteButton.Clicked += OnDeleteClicked;

        buttonBox.PackStart(saveButton, true, true, 0);
        buttonBox.PackStart(clearButton, true, true, 0);
        buttonBox.PackStart(deleteButton, true, true, 0);

        // Add CSS styling
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData(@"
            .event-text { 
                background-color: white; 
                color: #023047; 
            }
            .save-button { 
                background-color: #219ebc; 
                color: white; 
            }
            .clear-button { 
                background-color: #ffb703; 
                color: white; 
            }
            .delete-button { 
                background-color: #fb8500; 
                color: white; 
            }
        ");
        StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);

        ShowAll();
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var description = eventTextView.Buffer.Text;
        var category = categories[categoryComboBox.Active];

        if (!string.IsNullOrWhiteSpace(description))
        {
            // Remove existing events for this date
            events.RemoveAll(e => e.Date.Date == selectedDate.Date);

            // Add new event
            events.Add(new CalendarEvent 
            { 
                Date = selectedDate, 
                Description = description,
                Category = category
            });

            SaveEvents();
            Respond(ResponseType.Ok);
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        eventTextView.Buffer.Text = "";
        categoryComboBox.Active = 0;
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        events.RemoveAll(e => e.Date.Date == selectedDate.Date);
        SaveEvents();
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

public class MainWindow : Window
{
    private Calendar calendar;
    private List<CalendarEvent> events;
    private string eventsFile = "calendar_events.json";
    private const int WINDOW_WIDTH = 450;
    private const int WINDOW_HEIGHT = 450;
    private const int PADDING = 15;
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
                background-color: #219ebc;
                color: white;
            }
            .calendar button:selected {
                background-color: transparent;
                color: #023047;
            }
            .calendar button:checked {
                background-color: #219ebc;
                color: white;
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
                color: #ffb703;
            }
            .calendar button.day-with-event.work {
                color: #219ebc;
            }
            .calendar button.day-with-event.other {
                color: #fb8500;
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
        var headerFont = Pango.FontDescription.FromString("Segoe UI 14");
        calendar.StyleContext.AddClass("calendar-header");
        
        // Set day names font
        var dayFont = Pango.FontDescription.FromString("Segoe UI 12");
        calendar.StyleContext.AddClass("calendar-days");
        
        // Set numbers font
        var numberFont = Pango.FontDescription.FromString("Segoe UI 12");
        calendar.StyleContext.AddClass("calendar-numbers");
    }

    private void UpdateCalendarColors()
    {
        var currentMonth = calendar.Date;
        var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        
        // Clear all marks and styles first
        for (uint day = 1; day <= daysInMonth; day++)
        {
            calendar.UnmarkDay(day);
            var styleContext = calendar.StyleContext;
            styleContext.RemoveClass("day-with-event");
            styleContext.RemoveClass("personal");
            styleContext.RemoveClass("work");
            styleContext.RemoveClass("other");
        }

        // Mark today
        if (currentMonth.Year == DateTime.Now.Year && 
            currentMonth.Month == DateTime.Now.Month)
        {
            calendar.MarkDay((uint)DateTime.Now.Day);
        }

        // Mark days with events
        var monthEvents = events.Where(e => 
            e.Date.Year == currentMonth.Year && 
            e.Date.Month == currentMonth.Month).ToList();

        foreach (var evt in monthEvents)
        {
            var day = (uint)evt.Date.Day;
            calendar.MarkDay(day);

            // Apply category-specific styling
            var styleContext = calendar.StyleContext;
            styleContext.AddClass("day-with-event");
            switch (evt.Category.ToLower())
            {
                case "personal":
                    styleContext.AddClass("personal");
                    break;
                case "work":
                    styleContext.AddClass("work");
                    break;
                case "other":
                    styleContext.AddClass("other");
                    break;
            }
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

public class CalendarEvent
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public string Category { get; set; } = "Personal";
} 
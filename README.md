Programming 2: Semester Project (Calendar) in C#

This project is a Monthly Calendar with Event Management, created using C# and GTK#.

The main goal of this project is to display a monthly calendar where users can add, view, and remove events on specific dates. I used GTK# to build the graphical user interface because it supports modern design and is well-integrated with C#.

One of the important features of this project is that events are saved automatically and restored when the program is reopened. To achieve this, I used JSON file storage (calendar_events.json) to store all events. When the application starts, it loads this data so users never lose their saved events.

I also used different colors to represent event categories to make the interface clear and visually organized:

- Yellow for Personal events
- Blue for Work events
- Orange for Other events

This project helped me understand GTK layout management, event handling, file storage in JSON, and how to build user-friendly desktop applications in C#.

**User Documentation**
1) Open the application. You’ll see a calendar view for the current month.

2) To add an event:

3) Click on a date

4) Enter the event name

5) Choose a category (Personal, Work, or Other)

6) Click "Save Event". The selected date will appear with a colored background based on the event category.

_To view or remove an event:_

 - Click on a date with an event. The event details will show
 - To remove it, click "Clear Event"

**Navigation:**

Use the left/right arrows to switch months

Colored backgrounds help identify days with events

Data is saved automatically, even when you close the app

**Developer Documentation**

_MainWindow.cs_
Handles the GUI, calendar rendering, and user interaction.

_CalendarEvent.cs_
Defines the structure of an event: date, name, and category.

_calendar_events.json_
Stores event data persistently across sessions.

**Demo of the project:**

<img width="600" alt="image" src="https://github.com/user-attachments/assets/d7d63a81-a8d2-453d-b8c7-3e7ccfb2a1f1" />


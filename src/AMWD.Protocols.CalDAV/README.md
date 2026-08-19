# CalDAV for .NET

A .NET Standard 2.0 library to work with CalDAV servers.

## Features

- **Connection Management**: Connect to CalDAV by respecting redirects and `.well-known/caldav` information.
- **Principal Discovery**: Discover the principal URL for a user.
- **Calendar Management**: Create, list and delete calendars.
- **Event Management**: Create, list, update and delete events.
- **iCal Support**: Support for iCalendar format (RFC 5545) for event data provided by [ical.net](https://github.com/ical-org/ical.net).


## Usage

```csharp
using AMWD.Protocols.CalDAV;

using var client = new CalDavClient("https://your.caldav.host", "username", "P@ssw0rd!");
bool isInitialized = await client.InitializeAsync();
if (!isInitialized)
{
	Console.WriteLine("Failed to initialize CalDAV client.");
	return;
}

var calendars = await client.GetCalendarsAsync();
foreach (var calendar in calendars)
{
	Console.WriteLine($"Calendar: {calendar.DisplayName} ({calendar.Url})");
}

```

## Supported CalDAV Servers

The support might be limited as some providers do not allow creating/deleting new calendars.    
Also the authentication is limited to Basic Authentication.

#### Apple iCloud

- **Base URL**: `https://caldav.icloud.com/`
- **Username**: your-apple-id@icloud.com
- **Password**: your-app-specific-password

#### Google Calendar

- **Base URL**: `https://apidata.googleusercontent.com/caldav/v2/`
- **Username**: your-email-address@gmail.com
- **Password**: your-app-specific-password

#### Nextcloud / ownCloud

- **Base URL**: `https://nextcloud.your.server/remote.php/dav/`
- **Username**: nextcloud-username
- **Password**: nextcloud-password

#### Radicale

- **Base URL**: `http://radicale.local:5232/`
- **Username**: radicale-username
- **Password**: radicale-password

#### OpenCloud (extended with Radicale)

- **Base URL**: `https://opencloud.your.server/`
- **Username**: opencloud-username
- **Password**: your-app-specific-token


---

MIT License (see [choose a license](https://choosealicense.com/licenses/mit/)).

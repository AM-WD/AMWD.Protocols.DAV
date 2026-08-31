# WebDAV implementation for .NET

This repository contains .NET implementations for WebDAV associated protocols like [CalDAV] or [CardDAV].


## CalDAV

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


## CardDAV

```csharp
using AMWD.Protocols.CardDAV;

using var client = new CardDavClient("https://your.carddav.host", "username", "P@ssw0rd!");
bool isInitialized = await client.InitializeAsync();
if (!isInitialized)
{
	Console.WriteLine("Failed to initialize CardDAV client.");
	return;
}

var addressBooks = await client.GetAddressBooksAsync();
foreach (var addressBook in addressBooks)
{
	Console.WriteLine($"Address Book: {addressBook.DisplayName} ({addressBook.Url})");
}

```


---

[MIT License](LICENSE.txt) (see [choose a license](https://choosealicense.com/licenses/mit/)).

[CalDAV]: src/AMWD.Protocols.CalDAV
[CardDAV]: src/AMWD.Protocols.CardDAV

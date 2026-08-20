# CardDAV for .NET

A .NET Standard 2.0 library to work with CardDAV servers.

## Features

- **Connection Management**: Connect to CardDAV by respecting redirects and `.well-known/carddav` information.
- **Principal Discovery**: Discover the principal URL for a user.
- **Address Book Management**: Create, list and delete address books.
- **Contact Management**: Create, list, update and delete contacts.
- **vCard Support**: Support for vCard format (RFC 6350) for contact data provided by [vCard.Net](https://github.com/gachris/vCard.Net).


## Usage

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


## Supported CardDAV Servers

The support might be limited as some providers do not allow creating/deleting new address books.    
Also the authentication is limited to Basic Authentication.

#### Apple iCloud

- **Base URL**: `https://contacts.icloud.com/`
- **Username**: your-apple-id@icloud.com
- **Password**: your-app-specific-password

#### Google Calendar

- **Base URL**: `https://www.googleapis.com/.well-known/carddav`
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

_I used [sabre/dav] as orientation for the implementation._

---

MIT License (see [choose a license](https://choosealicense.com/licenses/mit/)).



[sabre/dav]: https://sabre.io/dav/building-a-carddav-client/

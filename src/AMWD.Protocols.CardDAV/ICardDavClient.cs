using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using vCard.Net.CardComponents;

namespace AMWD.Protocols.CardDAV
{
	/// <summary>
	/// Represents a client for interacting with a CardDAV server.
	/// </summary>
	public interface ICardDavClient
	{
		/// <summary>
		/// Gets the URI of the current principal (user) on the CardDAV server.
		/// </summary>
		Uri? PrincipalUri { get; }

		/// <summary>
		/// Initializes the <see cref="CardDavClient"/> by discovering the current principal (user) on the CardDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if initialization was successful; otherwise, <see langword="false"/>.</returns>
		Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the list of address books associated with the current principal (user) on the CardDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		Task<IReadOnlyCollection<DavAddressBook>> GetAddressBooksAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new address book on the CardDAV server for the current principal (user).
		/// </summary>
		/// <param name="request">The request containing the details of the address book to create.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the address book was created successfully; otherwise, <see langword="false"/>.</returns>
		Task<bool> CreateAddressBookAsync(CreateAddressBookRequest request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes an existing address book from the CardDAV server for the current principal (user).
		/// </summary>
		/// <param name="addressBook">The address book to delete.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the address book was deleted successfully; otherwise, <see langword="false"/>.</returns>
		Task<bool> DeleteAddressBookAsync(DavAddressBook addressBook, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the list of vCards (contacts) from a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book from which to retrieve vCards.</param>
		/// <param name="name">The name of the vCard to filter for (optional).</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		Task<IReadOnlyCollection<DavCard>> GetCardsAsync(DavAddressBook addressBook, string? name = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new vCard (contact) in a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book in which to create the vCard.</param>
		/// <param name="vCard">The vCard to create.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The <see cref="Uri"/> of the newly created vCard.</returns>
		Task<Uri> CreateCardAsync(DavAddressBook addressBook, VCard vCard, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates an existing vCard (contact) in a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book containing the vCard to update.</param>
		/// <param name="vCard">The vCard to update.</param>
		/// <param name="eTag">The entity tag (ETag) of the vCard for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the update was successful; otherwise, <see langword="false"/>.</returns>
		Task<bool> UpdateCardAsync(DavAddressBook addressBook, VCard vCard, string? eTag = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes an existing vCard (contact) from a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book containing the vCard to delete.</param>
		/// <param name="vCard">The vCard to delete.</param>
		/// <param name="eTag">The entity tag (ETag) of the vCard for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the deletion was successful; otherwise, <see langword="false"/>.</returns>
		Task<bool> DeleteCardAsync(DavAddressBook addressBook, VCard vCard, string? eTag = null, CancellationToken cancellationToken = default);
	}
}

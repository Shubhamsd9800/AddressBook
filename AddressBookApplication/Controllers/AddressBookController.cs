using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.DTO;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;

namespace AddressBookApplication.Controllers
{
    [ApiController]
    [Route("api/addressbook")]
    [Authorize]
    public class AddressBookController : ControllerBase
    {
        private readonly IAddressBookBL _addressBookBL;

        public AddressBookController(IAddressBookBL addressBookService)
        {
            _addressBookBL = addressBookService;
        }

        private (int? UserId, string Role) GetAuthenticatedUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Token is missing or invalid.");

            return (int.Parse(userIdClaim), roleClaim);
        }


        [HttpGet]
        public async Task<IActionResult> GetAllContacts()
        {
            try
            {
                var (userId, role) = GetAuthenticatedUser();

                // Admins can view all contacts, users can only see their own
                var contacts = await _addressBookBL.GetAllAsync(role == "Admin" ? null : userId);
                return Ok(new ApiResponse<IEnumerable<AddressBookEntry>>(true, "Contacts retrieved successfully", contacts));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message, null));
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactById(int id)
        {
            try
            {
                var (userId, role) = GetAuthenticatedUser();
                var contact = await _addressBookBL.GetByIdAsync(role == "Admin" ? null : userId, id);

                if (contact == null)
                    return NotFound(new ApiResponse<string>(false, "Contact not found", null));

                return Ok(new ApiResponse<AddressBookEntry>(true, "Contact retrieved successfully", contact));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message, null));
            }
        }

        // ✅ Users can only add their own contacts
        [HttpPost]
        public async Task<IActionResult> AddContact([FromBody] AddressBookDto dto)
        {
            try
            {
                var (userId, role) = GetAuthenticatedUser();
                if (role == "Admin")
                    return BadRequest(new ApiResponse<string>(false, "Admins cannot add personal contacts", null));

                await _addressBookBL.AddAsync(userId.Value, dto);
                return CreatedAtAction(nameof(GetAllContacts), new ApiResponse<string>(true, "Contact added successfully", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message, null));
            }
        }

        // ✅ Admins can update any contact, users can only update their own
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] AddressBookDto dto)
        {
            try
            {
                var (userId, role) = GetAuthenticatedUser();
                await _addressBookBL.UpdateAsync(role == "Admin" ? null : userId, id, dto);
                return Ok(new ApiResponse<string>(true, "Contact updated successfully", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message, null));
            }
        }

        // ✅ Admins can delete any contact, users can only delete their own
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            try
            {
                var (userId, role) = GetAuthenticatedUser();
                await _addressBookBL.DeleteAsync(role == "Admin" ? null : userId, id);
                return Ok(new ApiResponse<string>(true, "Contact deleted successfully", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message, null));
            }
        }
    }
}

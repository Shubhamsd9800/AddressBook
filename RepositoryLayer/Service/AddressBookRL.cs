using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Context;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;

namespace RepositoryLayer.Service
{
    public class AddressBookRL : IAddressBookRL

    {
        private readonly AppDbContext _context;

        public AddressBookRL(AppDbContext context)
        {
            _context = context;
        }

        // Fetch only contacts belonging to the logged-in user
        public async Task<IEnumerable<AddressBookEntry>> GetAllAsync(int? userId) =>
    userId == null ? await _context.AddressBookEntries.ToListAsync()
                   : await _context.AddressBookEntries.Where(c => c.UserId == userId)
            .Include(a => a.User)
            .ToListAsync();

        // Fetch a specific contact only if it belongs to the user
        public async Task<AddressBookEntry?> GetByIdAsync(int? userId, int id)
        {
            return await _context.AddressBookEntries
        .Where(c => c.Id == id && (userId == null || c.UserId == userId))
        .Include(a => a.User) 
        .FirstOrDefaultAsync();
        //await _context.AddressBookEntries.FirstOrDefaultAsync(c => c.Id == id && (userId == null || c.UserId == userId));
        }

        // Add a new contact and assign it to the logged-in user
        public async Task AddAsync(AddressBookEntry entry)
        {
            await _context.AddressBookEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        // Update only if the contact belongs to the user
        public async Task UpdateAsync(AddressBookEntry entry)
        {
            var existingEntry = await _context.AddressBookEntries
                .FirstOrDefaultAsync(contact => contact.Id == entry.Id && contact.UserId == entry.UserId);

            if (existingEntry != null)
            {
                _context.Entry(existingEntry).CurrentValues.SetValues(entry);
                await _context.SaveChangesAsync();
            }
        }

        // Delete only if the contact belongs to the user
        public async Task DeleteAsync(int? userId, int id)
        {
            var entry = await _context.AddressBookEntries
                .FirstOrDefaultAsync(c => c.Id == id && (userId == null || c.UserId == userId));

            if (entry != null)
            {
                _context.AddressBookEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }
        }


    }
}

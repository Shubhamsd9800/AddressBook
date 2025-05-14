using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.Helper;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RepositoryLayer.DTO;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;

namespace RepositoryLayer.Service
{
    public class AddressBookBL : IAddressBookBL

    {
        private readonly IAddressBookRL _repository;
        private readonly IDistributedCache _cache;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public AddressBookBL(IAddressBookRL repository, IDistributedCache cache, RabbitMQPublisher rabbitMQPublisher)
        {
            _repository = repository;
            _cache = cache;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<IEnumerable<AddressBookEntry>> GetAllAsync(int? userId)
        {
            string cacheKey = userId == null ? "AllContacts" : $"Contacts_{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<List<AddressBookEntry>>(cachedData);
            }

            var contacts = await _repository.GetAllAsync(userId);

            if (contacts.Any())
            {
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Cache expires in 5 minutes
                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(contacts), cacheOptions);
            }

            return contacts;
        }

        public async Task<AddressBookEntry?> GetByIdAsync(int? userId, int id)
        {
            string cacheKey = $"Contact_{id}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<AddressBookEntry>(cachedData);
            }

            var contact = await _repository.GetByIdAsync(userId, id);

            if (contact != null)
            {
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(contact), cacheOptions);
            }

            return contact;
        }

        public async Task AddAsync(int userId, AddressBookDto dto)
        {
            await _repository.AddAsync(new AddressBookEntry
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                UserId = userId
            });

            // Publish Contact Added Event to RabbitMQ
            _rabbitMQPublisher.PublishMessage("ContactAddedQueue", new
            {
                UserId = userId,
                Name = dto.Name,
                Email = dto.Email
            });

            // Clear cache after adding a new contact
            await _cache.RemoveAsync($"Contacts_{userId}");
            await _cache.RemoveAsync("AllContacts");
        }

        public async Task UpdateAsync(int? userId, int id, AddressBookDto dto)
        {
            var entry = await _repository.GetByIdAsync(userId, id);
            if (entry == null || (userId.HasValue && entry.UserId != userId))
                throw new UnauthorizedAccessException("You are not authorized to modify this contact.");

            entry.Name = dto.Name;
            entry.Email = dto.Email;
            entry.Phone = dto.Phone;
            entry.Address = dto.Address;
            await _repository.UpdateAsync(entry);

            // Clear cache after updating
            await _cache.RemoveAsync($"Contacts_{userId}");
            await _cache.RemoveAsync($"Contact_{id}");
            await _cache.RemoveAsync("AllContacts");
        }

        public async Task DeleteAsync(int? userId, int id)
        {
            await _repository.DeleteAsync(userId, id);

            // ✅ Clear cache after deleting a contact
            await _cache.RemoveAsync($"Contacts_{userId}");
            await _cache.RemoveAsync($"Contact_{id}");
            await _cache.RemoveAsync("AllContacts");
        }



    }


}


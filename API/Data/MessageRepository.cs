using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            this.mapper = mapper;
            this.context = context;

        }

        public void AddMessage(Message message)
        {
            this.context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            this.context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await this.context.Messages
            .Include(u => u.Sender)
            .Include(u => u.Recipient)
            .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = this.context.Messages.OrderByDescending(m => m.MessageSent).AsQueryable();

            query = messageParams.Container switch{
                "Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username 
                && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u .Sender.UserName == messageParams.Username
                && u.SenderDeleted == false),
                _ => query.Where(u => u.Recipient.UserName == messageParams.Username
                 && u.RecipientDeleted == false && u.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(this.mapper.ConfigurationProvider);
            return await PagedList<MessageDto>.CreateAsync(messages,messageParams.PageNumber,messageParams.PageSize);
        }



        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await this.context.Messages
            .Include(u => u .Sender).ThenInclude(p => p.Photos)
            .Include(u => u .Recipient).ThenInclude(p => p.Photos)
            .Where(m => m.RecipientUsername == currentUsername && m.RecipientDeleted == false
            && m.Sender.UserName == recipientUsername || m.Recipient.UserName == recipientUsername
            &&m.Sender.UserName == currentUsername && m.SenderDeleted == false
            )
            .OrderBy( m => m.MessageSent)
            .ToListAsync();

            var unreadMessages = messages.Where( m => m.DateRead == null 
            && m.Recipient.UserName == currentUsername).ToList();

            if(unreadMessages.Any()){
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.Now;
                }
                await this.context.SaveChangesAsync();
            }

            return this.mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await this.context.SaveChangesAsync()> 0;
        }
    }
}
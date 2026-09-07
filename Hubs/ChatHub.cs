using CloudDesk.Data;
using CloudDesk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CloudDesk.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        private static readonly ConcurrentDictionary<string, int> OnlineUsers = new();
        private static readonly object OnlineLock = new();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================
        // CONNECTED
        // =========================

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                bool firstConnection = false;

                lock (OnlineLock)
                {
                    if (OnlineUsers.ContainsKey(userId))
                    {
                        OnlineUsers[userId]++;
                    }
                    else
                    {
                        OnlineUsers[userId] = 1;
                        firstConnection = true;
                    }
                }

                // User ki first connection par hi Online bhejo
                if (firstConnection)
                {
                    await Clients.All.SendAsync(
                        "UserStatus",
                        userId,
                        true
                    );
                }
            }

            await base.OnConnectedAsync();
        }


        // =========================
        // CHECK ONLINE
        // =========================

        public Task<bool> IsUserOnline(string userId)
        {
            lock (OnlineLock)
            {
                return Task.FromResult(
                    OnlineUsers.ContainsKey(userId)
                );
            }
        }


        // =========================
        // DISCONNECTED
        // =========================

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                bool isNowOffline = false;

                lock (OnlineLock)
                {
                    if (OnlineUsers.TryGetValue(
                        userId,
                        out var count))
                    {
                        if (count <= 1)
                        {
                            OnlineUsers.TryRemove(
                                userId,
                                out _
                            );

                            isNowOffline = true;
                        }
                        else
                        {
                            OnlineUsers[userId] =
                                count - 1;
                        }
                    }
                }

                // Sirf last connection close hone par Offline
                if (isNowOffline)
                {
                    await Clients.All.SendAsync(
                        "UserStatus",
                        userId,
                        false
                    );
                }
            }

            await base.OnDisconnectedAsync(exception);
        }


        // =========================
        // SEND MESSAGE
        // =========================

        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var sender = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserName ==
                    Context.User!.Identity!.Name
                );

            if (sender == null)
                return;

            // Viewer message send nahi kar sakta
            if (await IsViewer(sender))
                return;

            // Fixed chat: UserA <-> UserB
            var receiver =
                await GetOtherChatUser(sender);

            if (receiver == null)
                return;

            var newMessage = new Message
            {
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Text = message.Trim(),
                SentAt = DateTime.Now,
                IsReadByReceiver = false
            };

            _context.Messages.Add(newMessage);

            await _context.SaveChangesAsync();

            await Clients.All.SendAsync(
                "ReceiveMessage",
                sender.DisplayName,
                newMessage.Text,
                newMessage.SentAt,
                sender.Id
            );
        }

        // =========================
        // DELETE MY MESSAGES
        // =========================

        public async Task DeleteMyMessages()
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserName ==
                    Context.User!.Identity!.Name
                );

            if (currentUser == null)
                return;

            // Delete time save karo
            // Iske baad current user ko purane messages
            // dobara load nahi honge.
            currentUser.ChatClearedAt = DateTime.Now;

            // Current user ke apne messages database se delete karo.
            // Isse doosre user ki screen se bhi ye messages hat jayenge.
            var myMessages = await _context.Messages
                .Where(m =>
                    m.SenderId == currentUser.Id
                )
                .ToListAsync();

            if (myMessages.Count > 0)
            {
                _context.Messages.RemoveRange(myMessages);
            }

            await _context.SaveChangesAsync();

            // Dono users ko notification
            await Clients.All.SendAsync(
                "MyMessagesDeleted",
                currentUser.Id
            );
        }

        // =========================
        // GET SAVED MESSAGES
        // =========================

        public async Task<List<object>> GetMessages()
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == Context.User!.Identity!.Name);

            if (currentUser == null)
                return new List<object>();

            var otherUser = await GetOtherChatUser(currentUser);

            if (otherUser == null)
                return new List<object>();

            var query = _context.Messages
                .Where(m =>
                    (m.SenderId == currentUser.Id &&
                     m.ReceiverId == otherUser.Id)
                    ||
                    (m.SenderId == otherUser.Id &&
                     m.ReceiverId == currentUser.Id)
                );

            // User ne Delete karne ke baad ke messages hi
            // uski screen par dikhne chahiye.
            if (currentUser.ChatClearedAt.HasValue)
            {
                query = query.Where(m =>
                    m.SentAt > currentUser.ChatClearedAt.Value);
            }

            var messages = await query
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    senderName = m.Sender!.DisplayName,
                    senderId = m.SenderId,
                    text = m.Text,
                    sentAt = m.SentAt,
                    isMine = m.SenderId == currentUser.Id
                })
                .ToListAsync();

            return messages
                .Cast<object>()
                .ToList();
        }


        // =========================
        // CHECK VIEWER
        // =========================

        private async Task<bool> IsViewer(
            ApplicationUser user)
        {
            var roles = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Join(
                    _context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name
                )
                .ToListAsync();

            return roles.Contains("Viewer");
        }


        // =========================
        // GET OTHER CHAT USER
        // =========================

        private async Task<ApplicationUser?>
            GetOtherChatUser(
                ApplicationUser sender)
        {
            var users = await _context.Users
                .Where(
                    u => u.UserName != sender.UserName
                )
                .ToListAsync();

            foreach (var user in users)
            {
                var roles = await _context.UserRoles
                    .Where(
                        x => x.UserId == user.Id
                    )
                    .Join(
                        _context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name
                    )
                    .ToListAsync();

                if (roles.Contains("ChatUser"))
                    return user;
            }

            return null;
        }


        // =========================
        // TYPING
        // =========================

        public async Task Typing()
        {
            var sender = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserName ==
                    Context.User!.Identity!.Name
                );

            if (sender == null)
                return;

            await Clients.All.SendAsync(
                "UserTyping",
                sender.DisplayName
            );
        }
    }
}
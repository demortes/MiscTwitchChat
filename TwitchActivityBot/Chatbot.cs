using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Classlib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix.Models.Common;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace TwitchActivityBot
{
    public class Chatbot
    {
        private ILogger _logger;
        private ActivityBotDbContext _db;
        private IConfiguration _config;
        private readonly TwitchAPI _api;
        private TwitchClient _client;
        private FollowerService _followerMonitor;
        private List<string> _cahannelsToMonitor = new List<string>();

        public Chatbot(IConfiguration config, ActivityBotDbContext db, ILogger logger, TwitchAPI api)
        {
            _logger = logger;
            _db = db;
            _config = config;
            _api = api;
            _followerMonitor = new FollowerService(_api);
            _followerMonitor.OnNewFollowersDetected += newFollowerCheck;

            ConnectionCredentials creds = new ConnectionCredentials(_config.GetValue<string>("Twitch:Username"), _config.GetValue<string>("Twitch:OAuth"));
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.OnLog += onLog;
            _client.Initialize(creds);
            _client.OnMessageReceived += messageReceived;
            _client.OnMessageReceived += checkBanHossAsync;
            _client.OnJoinedChannel += joinedChannel;
            _client.OnConnected += onConnected;
            _client.OnConnectionError += onConnectionError;
            _client.OnReconnected += onReconnected;
            _client.AutoReListenOnException = true;
            _client.OnDisconnected += onDisconnected;
            _client.OnFailureToReceiveJoinConfirmation += onFailuredToReceiveJoinConfirmation;
            _client.Connect();


        }

        private async void checkBanHossAsync(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Username == e.ChatMessage.Channel && e.ChatMessage.Message == "!banhoss")
            {
                _logger.LogInformation("Banning Hoss accounts that are following for {channel} authorized by {userName}.", e.ChatMessage.Channel, e.ChatMessage.Username);
                var channelInfo = await _api.Helix.Channels.GetChannelInformationAsync(e.ChatMessage.UserId);
                long totalFollowers = 1000;
                Pagination currentIndex = new();
                var followers = new List<Follow>();
                _client.SendMessage(e.ChatMessage.Channel, "Retrieving followers.");
                do
                {
                    var curPageFollow = await _api.Helix.Users.GetUsersFollowsAsync(currentIndex.Cursor, null, 100, null, e.ChatMessage.UserId);
                    followers.AddRange(curPageFollow.Follows);
                    if (totalFollowers != curPageFollow.TotalFollows)
                        totalFollowers = curPageFollow.TotalFollows;
                } while (followers.Count < totalFollowers);

                var hossAccounts = followers.Where(x => x.FromUserId.ToLower().Contains("hoss00312_"));
                if (!hossAccounts.Any())
                {
                    _client.SendMessage(e.ChatMessage.Channel, "404: Hoss not found.");
                    return;
                }
                _client.SendMessage(e.ChatMessage.Channel, $"Found {hossAccounts.Count()} Hoss00312 followers. Eliminating already banned accounts.");
                var hossIds = hossAccounts.Select(x => x.FromUserId).ToList();
                var bannedAccounts = await _api.Helix.Moderation.GetBannedUsersAsync(e.ChatMessage.UserId, hossIds);
                _client.SendMessage(e.ChatMessage.Channel, $"Found {bannedAccounts.Data.Count()} banned already.");
                if(hossIds.Count == bannedAccounts.Data.Length)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "No new accounts to ban.");
                    return;
                }
                var bannedIds = bannedAccounts.Data.Select(x => x.UserId).ToList();
                hossAccounts = hossAccounts.Where(x => !bannedIds.Contains(x.FromUserId)).ToList();
                _client.SendMessage(e.ChatMessage.Channel, $"Attempting to ban {hossAccounts.Count()} more.");
                (hossAccounts as List<Follow>).ForEach(x =>
                {
                    _client.SendMessage(e.ChatMessage.Channel, $"/ban {x.FromUserName} Automatically banning on behalf of {e.ChatMessage.Channel}, suspected hate raid bot. - {_client.TwitchUsername}");
                });
                _client.SendMessage(e.ChatMessage.Channel, "Completed.");
            }
        }

        private async void newFollowerCheck(object sender, OnNewFollowersDetectedArgs e)
        {
            e.NewFollowers.ForEach(x =>
            {
                if (x.FromUserName.ToLower().Contains("hoss00312_"))
                {
                    var channel = _client.JoinedChannels.FirstOrDefault(y => y.Channel.Normalize().ToLower() == x.ToUserName.ToLower());
                    _client.SendMessage(channel, $"/ban {x.FromUserName} Likely Hateraid bot... Automatically banned by {_client.TwitchUsername}");
                }
            });
        }

        private void onDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            _logger.LogWarning($"Server disconnected.");
            Thread.Sleep(5000);
            if (!_client.IsConnected)
            {
                _client.Connect();
            }
        }

        private void onReconnected(object sender, OnReconnectedEventArgs e)
        {
            _logger.LogWarning($"Reconnected to server.");
        }

        private void onFailuredToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            _logger.LogError($"Failed to receive join confirmation on channel {e.Exception.Channel}: {e.Exception.Details}");
            _client.JoinChannel(e.Exception.Channel);
        }

        private void onConnectionError(object sender, OnConnectionErrorArgs e)
        {
            _logger.LogError($"Connection error: {e.Error.Message}");
        }

        private void onConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation($"Connected to Twitch chat.");
            foreach (var channel in _config.GetSection("Channels").Get<string[]>())
            {
                _client.JoinChannel(channel.ToLower());
                _cahannelsToMonitor.Add(channel);
            }
            _followerMonitor.SetChannelsByName(_cahannelsToMonitor);
        }

        private void onLog(object sender, OnLogArgs e)
        {
            _logger.LogDebug(e.Data);
        }

        private void joinedChannel(object sender, OnJoinedChannelArgs e)
        {
            //_client.SendMessage(e.Channel, "I will help take over the world.");
            _logger.LogInformation($"Joined channel {e.Channel} as {e.BotUsername}.");
        }

        public bool isConnected()
        {
            return _client.IsConnected;
        }

        private void messageReceived(object sender, OnMessageReceivedArgs e)
        {
            _logger.LogInformation($"Chat message detected from {e.ChatMessage.Username} in channel {e.ChatMessage.Channel}.");
            var record = _db.ActiveChatters.FirstOrDefault(p => p.Channel == e.ChatMessage.Channel && p.Username == e.ChatMessage.Username);
            if (record != null)
                record.LastSeen = DateTimeOffset.UtcNow;
            else
            {
                var newRecord = new ActiveChatter();
                newRecord.Channel = e.ChatMessage.Channel;
                newRecord.Username = e.ChatMessage.Username;
                newRecord.LastSeen = DateTimeOffset.UtcNow;
                _db.Add(newRecord);
            }
            _db.SaveChanges();
        }
    }
}

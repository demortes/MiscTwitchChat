using Microsoft.Extensions.Configuration;
using MiscTwitchChat.Classlib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchActivityBot
{
    public class Chatbot
    {
        private ActivityBotDbContext _db;
        private IConfiguration _config;
        private TwitchClient _client;

        public Chatbot(IConfiguration config, ActivityBotDbContext db)
        {
            _db = db;
            _config = config;
            ConnectionCredentials creds = new ConnectionCredentials(_config.GetValue<string>("Twitch:Username"), _config.GetValue<string>("Twitch:OAuth"));
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(creds);
            _client.OnMessageReceived += messageReceived;
            _client.OnJoinedChannel += joinedChannel;
            _client.Connect();

            foreach (var channel in _config.GetSection("Channels").Get<string[]>())
                _client.JoinChannel(channel.ToLower());
            
        }

        private void joinedChannel(object sender, OnJoinedChannelArgs e)
        {
            //_client.SendMessage(e.Channel, "I will help take over the world.");
        }

        public bool isConnected()
        {
            return _client.IsConnected;
        }

        private void messageReceived(object sender, OnMessageReceivedArgs e)
        {
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

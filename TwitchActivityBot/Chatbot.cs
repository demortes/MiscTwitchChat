﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Classlib.Entities;
using System;
using System.Linq;
using System.Threading;
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
        private ILogger<Chatbot> _logger;
        private ActivityBotDbContext _db;
        private IConfiguration _config;
        private TwitchClient _client;

        public Chatbot(IConfiguration config, ActivityBotDbContext db, ILogger<Chatbot> logger)
        {
            _logger = logger;
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
            _client.OnLog += onLog;
            _client.Initialize(creds);
            _client.OnMessageReceived += messageReceived;
            _client.OnJoinedChannel += joinedChannel;
            _client.OnConnected += onConnected;
            _client.OnConnectionError += onConnectionError;
            _client.OnReconnected += onReconnected;
            _client.AutoReListenOnException = true;
            _client.OnDisconnected += onDisconnected;
            _client.OnFailureToReceiveJoinConfirmation += onFailuredToReceiveJoinConfirmation;

            if (!_client.Connect())
            {
                _logger.LogError("Failed to connect. Exiting.");
            }

            foreach (var channel in _config.GetSection("Channels").Get<string[]>())
                _client.JoinChannel(channel.ToLower());

        }

        private void onDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            _logger.LogWarning($"Servier disconnected.");
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
            _logger.LogInformation($"Connected to Twitch.");
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

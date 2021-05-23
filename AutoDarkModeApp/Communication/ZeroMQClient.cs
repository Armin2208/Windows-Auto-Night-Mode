﻿using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;
using System.IO.MemoryMappedFiles;

namespace AutoDarkModeApp.Communication
{
    class ZeroMQClient : ICommandClient
    {
        private string Port { get;  }
        public ZeroMQClient(string port)
        {
            Port = port;
        }

        private string GetBackendPort()
        {
            try
            {
                using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("adm-backend-port");
                using MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor();
                byte[] bytes = new byte[sizeof(int)];
                viewAccessor.ReadArray(0, bytes, 0, bytes.Length);
                int backendPort = BitConverter.ToInt32(bytes, 0);
                return backendPort.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Port;
            }
        }

        public bool SendMessage(string message)
        {

            using (var client = new RequestSocket())
            {
                client.Connect("tcp://127.0.0.1:" + GetBackendPort());
                client.SendFrame(message);
                var response = GetResponse(client, message);
                if (response.Contains(Response.Err))
                {
                    return false;
                }
                else if (response.Contains(Response.Ok))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetResponse(RequestSocket client, string message)
        {
            bool hasResponse = false;
            string response;
            lock (this)
            {
                hasResponse = client.TryReceiveFrameString(new TimeSpan(0, 0, 3), out response);
            }

            if (hasResponse)
            {
                return response;   
            }
            return Response.Err;
        }

        public string SendMessageAndGetReply(string message)
        {
            using var client = new RequestSocket();
            client.Connect("tcp://127.0.0.1:" + GetBackendPort());
            client.SendFrame(message);
            return GetResponse(client, message);
        }

        public Task<bool> SendMessageAsync(string message)
        {
            return Task.Run(() => SendMessage(message));
        }

        public Task<string> SendMessageAndGetReplyAsync(string message)
        {
            return Task.Run(() => SendMessageAndGetReply(message));
        }
    }
}

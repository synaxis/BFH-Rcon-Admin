﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kernel.BFHAdmin.Client.BFHRconProtocol.Models;

namespace Kernel.BFHAdmin.Client.BFHRconProtocol.Commands
{
    public class ClientChatBufferCommand
    {
          public RconClient RconClient { get; private set; }
        public int ChatHistorySize = 10;
        public List<ChatHistoryItem> ChatHistory { get; private set; }
        // -1	Admin	None	ServerMessage	[03:12:17]	(auto-all): I see last kill!
        private Regex reg_ClientBuffer = new Regex(@"^(?<number>\-?\d+)\s*(?<from>\S+)\s+(?<what>\S+)\s+(?<type>\S+)\s+(?<message>.*)$");

        public delegate void ChatLineReceivedDelegate(object sender, ChatHistoryItem chatHistoryItem);

        public event ChatLineReceivedDelegate ChatLineReceived;

        protected virtual void OnChatLineReceived(ChatHistoryItem chathistoryitem)
        {
            ChatLineReceivedDelegate handler = ChatLineReceived;
            if (handler != null) handler(this, chathistoryitem);
        }


        public ClientChatBufferCommand(RconClient rconClient)
        {
            RconClient = rconClient;
            ChatHistory = new List<ChatHistoryItem>();
        }

        public async void RefreshClientChatBufferCommand()
        {
            var qi = new RconQueueItem("bf2cc clientchatbuffer", RconClient.RconState.AsyncCommand);
            RconClient.EnqueueCommand(qi);
            var lines = await qi.TaskCompletionSource.Task;

            foreach (var line in lines)
            {
                    var clientBuffer = reg_ClientBuffer.Match(line);
                    if (clientBuffer.Success)
                    {
                        var number = clientBuffer.Groups["number"].Value;
                        var from = clientBuffer.Groups["from"].Value;
                        var what = clientBuffer.Groups["what"].Value;
                        var type = clientBuffer.Groups["type"].Value;
                        var message = clientBuffer.Groups["message"].Value;
                        Debug.WriteLine("<" + from + "> " + message);
                        var item = new ChatHistoryItem()
                                       {
                                           Number = int.Parse(number),
                                           From = from,
                                           What = what,
                                           Type = type,
                                           Message = message
                                       };
                        lock (ChatHistory)
                        {
                            ChatHistory.Add(item);
                            while (ChatHistory.Count > ChatHistorySize)
                            {
                                ChatHistory.RemoveAt(0);
                            }
                        }
                        OnChatLineReceived(item);
                    }
            }
        }
    }
}

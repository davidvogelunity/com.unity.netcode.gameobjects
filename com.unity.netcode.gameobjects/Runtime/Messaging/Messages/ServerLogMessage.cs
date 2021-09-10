﻿using System;

namespace Unity.Netcode.Messages
{
    internal struct ServerLogMessage : INetworkMessage
    {
        public NetworkLog.LogType LogType;
        // It'd be lovely to be able to replace this with FixedUnmanagedArray<char>...
        // But it's not really practical. On the sending side, the user is likely to want
        // to work with strings and would need to convert, and on the receiving side,
        // we'd have to convert it to a string to be able to pass it to the log system.
        // So an allocation is unavoidable here on both sides.
        public string Message;


        public void Serialize(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(LogType);
            BytePacker.WriteValuePacked(ref writer, Message);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var networkManager = (NetworkManager) context.SystemOwner;
            if (networkManager.IsServer && networkManager.NetworkConfig.EnableNetworkLogs)
            {
                var message = new ServerLogMessage();
                reader.ReadValueSafe(out message.LogType);
                ByteUnpacker.ReadValuePacked(ref reader, out message.Message);
                message.Handle(context.SenderId, networkManager, reader.Length);
            }
        }

        public void Handle(ulong senderId, NetworkManager networkManager, int messageSize)
        {
            
            networkManager.NetworkMetrics.TrackServerLogReceived(senderId, (uint)LogType, messageSize);

            switch (LogType)
            {
                case NetworkLog.LogType.Info:
                    NetworkLog.LogInfoServerLocal(Message, senderId);
                    break;
                case NetworkLog.LogType.Warning:
                    NetworkLog.LogWarningServerLocal(Message, senderId);
                    break;
                case NetworkLog.LogType.Error:
                    NetworkLog.LogErrorServerLocal(Message, senderId);
                    break;
            }
        }
    }
}
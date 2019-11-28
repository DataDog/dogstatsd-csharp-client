using System;
using System.Text;

namespace StatsdClient
{
    ///----------------------------------------------------------------------------
    /// SocketSender splits a message before sending them.
    ///----------------------------------------------------------------------------
    static class SocketSender
    {
        public static void Send(int maxPacketSize, string command, Action<byte[]> sender)
        {
            Send(maxPacketSize, Encoding.UTF8.GetBytes(command), sender);
        }

        private static void Send(int maxPacketSize, byte[] encodedCommand, Action<byte[]> sender)
        {
            if (maxPacketSize > 0 && encodedCommand.Length > maxPacketSize)
            {
                // If the command is too big to send, linear search backwards from the maximum
                // packet size to see if we can find a newline delimiting two stats. If we can,
                // split the message across the newline and try sending both componenets individually
                byte newline = Encoding.UTF8.GetBytes("\n")[0];
                for (int i = maxPacketSize; i > 0; i--)
                {
                    if (encodedCommand[i] == newline)
                    {
                        byte[] encodedCommandFirst = new byte[i];
                        Array.Copy(encodedCommand, encodedCommandFirst, encodedCommandFirst.Length); // encodedCommand[0..i-1]
                        Send(maxPacketSize, encodedCommandFirst, sender);

                        int remainingCharacters = encodedCommand.Length - i - 1;
                        if (remainingCharacters > 0)
                        {
                            byte[] encodedCommandSecond = new byte[remainingCharacters];
                            Array.Copy(encodedCommand, i + 1, encodedCommandSecond, 0, encodedCommandSecond.Length); // encodedCommand[i+1..end]
                            Send(maxPacketSize, encodedCommandSecond, sender);
                        }

                        return; // We're done here if we were able to split the message.
                    }
                    // At this point we found an oversized message but we weren't able to find a
                    // newline to split upon. We'll still send it to the UDP socket, which upon sending an oversized message
                    // will fail silently if the user is running in release mode or report a SocketException if the user is
                    // running in debug mode.
                    // Since we're conservative with our maxPacketSize, the oversized message might even
                    // be sent without issue.
                }
            }
            sender(encodedCommand);
        }

        //private async Task SendAsync(ArraySegment<byte> encodedCommand)
        //{
        //    if (MaxUDPPacketSize > 0 && encodedCommand.Count > MaxUDPPacketSize)
        //    {
        //        // If the command is too big to send, linear search backwards from the maximum
        //        // packet size (taking into account the offset in the array)
        //        // to see if we can find a newline delimiting two stats. If we can,
        //        // split the message across the newline and try sending both componenets individually
        //        byte newline = Encoding.UTF8.GetBytes("\n")[0];
        //        for (int i = MaxUDPPacketSize + encodedCommand.Offset; i > encodedCommand.Offset; i--)
        //        {
        //            if (encodedCommand.Array[i] == newline)
        //            {
        //                var encodedCommandFirst = new ArraySegment<byte>(encodedCommand.Array, encodedCommand.Offset, i);

        //                await SendAsync(encodedCommandFirst).ConfigureAwait(false);

        //                int remainingCharacters = encodedCommand.Count - i - 1;
        //                if (remainingCharacters > 0)
        //                {
        //                    await SendAsync(new ArraySegment<byte>(encodedCommand.Array, i + 1, remainingCharacters)).ConfigureAwait(false);
        //                }

        //                return; // We're done here if we were able to split the message.
        //            }
        //            // At this point we found an oversized message but we weren't able to find a
        //            // newline to split upon. We'll still send it to the UDP socket, which upon sending an oversized message
        //            // will fail silently if the user is running in release mode or report a SocketException if the user is
        //            // running in debug mode.
        //            // Since we're conservative with our MAX_UDP_PACKET_SIZE, the oversized message might even
        //            // be sent without issue.
        //        }
        //    }
        //    var tcs = new TaskCompletionSource<object>();

        //    var args = new SocketAsyncEventArgs
        //    {
        //        RemoteEndPoint = IPEndpoint,
        //        SocketFlags = SocketFlags.None,
        //    };
        //    args.SetBuffer(encodedCommand.Array, encodedCommand.Offset, encodedCommand.Count);
        //    args.Completed += new EventHandler<SocketAsyncEventArgs>((object sender, SocketAsyncEventArgs eventArgs) => {
        //        if (eventArgs.SocketError == SocketError.Success)
        //        {
        //            tcs.SetResult(null);
        //        }
        //        else
        //        {
        //            tcs.SetException(new SocketException((int)eventArgs.SocketError));
        //        }
        //    });
        //    var completedAsync = UDPSocket.SendToAsync(args);
        //    if (!completedAsync)
        //        tcs.SetResult(null);

        //    await tcs.Task;
        //}
    }
}
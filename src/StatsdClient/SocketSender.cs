using System;
using System.Text;

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
}
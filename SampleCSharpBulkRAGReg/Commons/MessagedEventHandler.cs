using System;

namespace SampleCSharpBulkRAGReg.Commons
{
    public delegate void MessagedEventHandler(object sender, MessageEventArgs e);
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}

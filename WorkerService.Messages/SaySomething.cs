using System;
using NServiceBus;

namespace WorkerService.Messages
{
    public class SaySomething : ICommand
    {
        public string Message { get; set; }
    }

    public class SaySomethingResponse : IMessage
    {
        public string Message { get; set; }
    }
}

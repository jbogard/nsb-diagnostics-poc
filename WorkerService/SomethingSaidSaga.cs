using System;
using System.Threading.Tasks;
using ChildWorkerService.Messages;
using NServiceBus;
using WorkerService.Messages;

namespace WorkerService
{
    public class SomethingSaidSagaData : ContainSagaData
    {
        public Guid OriginatorId { get; set; }
        public bool SomethingYelled { get; set; }
        public bool TemperatureRead { get; set; }
        public int Temp { get; set; }
        public string Message { get; set; }
    }

    public class WaitTimeout { }

    public class SomethingSaidSaga : Saga<SomethingSaidSagaData>,
        IAmStartedByMessages<TemperatureRead>,
        IAmStartedByMessages<SomethingYelled>,
        IHandleTimeouts<WaitTimeout>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SomethingSaidSagaData> mapper)
        {
            mapper.ConfigureMapping<TemperatureRead>(m => m.Id).ToSaga(s => s.OriginatorId);
            mapper.ConfigureMapping<SomethingYelled>(m => m.Id).ToSaga(s => s.OriginatorId);
        }

        public Task Handle(TemperatureRead message, IMessageHandlerContext context)
        {
            Data.OriginatorId = message.Id;
            Data.TemperatureRead = true;
            Data.Temp = message.Value;

            return CheckComplete(context);
        }

        public Task Handle(SomethingYelled message, IMessageHandlerContext context)
        {
            Data.OriginatorId = message.Id;
            Data.SomethingYelled = true;
            Data.Message = message.Message;

            return CheckComplete(context);
        }

        private Task CheckComplete(IMessageHandlerContext context)
        {
            if (Data.TemperatureRead && Data.SomethingYelled)
            {
                return RequestTimeout<WaitTimeout>(context, TimeSpan.FromSeconds(2));
            }

            return Task.CompletedTask;
        }

        public Task Timeout(WaitTimeout state, IMessageHandlerContext context)
        {
            MarkAsComplete();

            var message = new SomethingSaidCompleted { Message = $"{Data.Message} and it's {Data.Temp}F outside." };

            return context.Publish(message);
        }
    }
}
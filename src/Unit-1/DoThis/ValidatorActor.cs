using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace WinTail
{
    class ValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public ValidatorActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as String;
            if (String.IsNullOrEmpty(msg))
            {
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                if(IsValid(msg))
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you, valid message"));
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.InputError("Invalid: odd number of chars"));
                }
            }
            Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsValid(string msg)
        {
            return msg.Length % 2 == 0;
        }
    }
}

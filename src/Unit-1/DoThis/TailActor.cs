using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.IO;

namespace WinTail
{
    class TailActor : UntypedActor
    {
        #region Message Types
        public class FileWrite
        {
            public FileWrite(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; private set; }
        }

        public class FileError
        {
            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }

            public string FileName { get; private set; }
            public string Reason { get; private set; }
        }

        public class InitialRead
        {
            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }

            public string FileName { get; private set; }
            public string Text { get; private set; }
        }
#endregion

        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private readonly FileObserver _observer;
        private readonly Stream _fileStream;
        private readonly StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;

            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            _fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(_filePath, text));
        }


        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case FileWrite _:
                {
                    var text = _fileStreamReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _reporterActor.Tell(text);
                    }

                    break;
                }
                case FileError _:
                {
                    var fe = message as FileError;
                    _reporterActor.Tell($"Tail reason: {fe.Reason}");
                    break;
                }
                case InitialRead _:
                    var ir = message as InitialRead;
                    _reporterActor.Tell(ir.Text);
                    break;
            }
        }
    }
}

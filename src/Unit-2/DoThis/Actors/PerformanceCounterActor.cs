using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _counter;

        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;
            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
        }

        protected override void PreStart()
        {
            _counter = _performanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                Self,
                _cancelPublishing
            );
        }

        protected override void PostStop()
        {
            try
            {
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                base.PostStop();
            }

        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case GatherMetrics _:
                    var metric = new Metric(_seriesName, _counter.NextValue());
                    foreach (var sub in _subscriptions)
                    {
                        sub.Tell(metric);
                    }
                    break;
                case SubscribeCounter _:
                    var sc = message as SubscribeCounter;
                    _subscriptions.Add(sc.Subscriber);
                    break;
                case UnsubscribeCounter _:
                    var uc = message as UnsubscribeCounter;
                    _subscriptions.Remove(uc.Subscriber);
                    break;
            }
        }
    }
}

﻿namespace WorkoutWotch.Models.Actions
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using Kent.Boogaart.HelperTrinity.Extensions;
    using WorkoutWotch.Services.Contracts.Delay;

    public sealed class WaitAction : IAction
    {
        private static readonly TimeSpan maximumDelayTime = TimeSpan.FromSeconds(1);
        private readonly IDelayService delayService;
        private readonly TimeSpan delay;

        public WaitAction(IDelayService delayService, TimeSpan delay)
        {
            delayService.AssertNotNull(nameof(delayService));

            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentException("delay must be greater than or equal to zero.", "delay");
            }

            this.delayService = delayService;
            this.delay = delay;
        }

        public TimeSpan Duration => this.delay;

        public IObservable<Unit> ExecuteAsync(ExecutionContext context)
        {
            context.AssertNotNull(nameof(context));

            var remainingDelay = this.delay;
            var skipAhead = MathExt.Min(remainingDelay, context.SkipAhead);

            if (skipAhead > TimeSpan.Zero)
            {
                remainingDelay = remainingDelay - skipAhead;
                context.AddProgress(skipAhead);
            }

            var remaining = Observable
                .Generate(
                    remainingDelay,
                    r => r > TimeSpan.Zero,
                    r =>
                    {
                        var delayFor = MathExt.Min(remainingDelay, maximumDelayTime);
                        return r - delayFor;
                    },
                    r => r)
                .Publish();
            var nextRemaining = remaining
                .Skip(1)
                .Concat(Observable.Return(TimeSpan.Zero));
            var delays = remaining
                .Zip(
                    nextRemaining,
                    (current, next) => current - next);
            var result = Observable
                .Concat(
                    delays
                        .SelectMany(delay => context.WaitWhilePausedAsync().Select(_ => delay))
                        .Select(
                            delay =>
                                Observable
                                    .Defer(
                                        () =>
                                            this
                                                .delayService
                                                .DelayAsync(delay, context.CancellationToken)
                                                .Select(_ => delay))))
                .Do(delay => context.AddProgress(delay))
                .Select(_ => Unit.Default)
                .RunAsync(context.CancellationToken);

            remaining.Connect();

            return result;
        }
    }
}
﻿namespace WorkoutWotch.Models.Actions
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Kent.Boogaart.HelperTrinity.Extensions;
    using WorkoutWotch.Services.Contracts.Logger;

    public sealed class DoNotAwaitAction : IAction
    {
        private readonly ILogger logger;
        private readonly IAction innerAction;

        public DoNotAwaitAction(ILoggerService loggerService, IAction innerAction)
        {
            loggerService.AssertNotNull(nameof(loggerService));
            innerAction.AssertNotNull(nameof(innerAction));

            this.logger = loggerService.GetLogger(this.GetType());
            this.innerAction = innerAction;
        }

        public TimeSpan Duration => TimeSpan.Zero;

        public IAction InnerAction => this.innerAction;

        public IObservable<Unit> ExecuteAsync(ExecutionContext context)
        {
            context.AssertNotNull(nameof(context));

            this
                .innerAction
                .ExecuteAsync(context)
                .Subscribe(
                    _ => { },
                    ex => this.logger.Error("Failed to execute inner action: " + ex));

            return Observable.Return(Unit.Default);
        }
    }
}
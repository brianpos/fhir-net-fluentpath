/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hl7.Fhir.FluentPath.Binding
{
    public delegate IEnumerable<IValueProvider> Invokee(IEvaluationContext context, IEnumerable<Invokee> arguments);

    public static class InvokeeFactory
    {
        public static IEnumerable<IValueProvider> Select(this Invokee evaluator, IEvaluationContext context)
        {
            return evaluator(context, InvokeeFactory.EmptyArgs);
        }

        public static IEnumerable<IValueProvider> Select(this Invokee evaluator, IEnumerable<IValueProvider> input)
        {
            return evaluator.Select(BaseEvaluationContext.Root(input));
        }

        public static object Scalar(this Invokee evaluator, IEvaluationContext context)
        {
            var result = evaluator.Select(context);
            if (result.Any())
                return evaluator.Select(context).Single().Value;
            else
                return null;
        }

        public static object Scalar(this Invokee evaluator, IEnumerable<IValueProvider> input)
        {
            return evaluator.Scalar(BaseEvaluationContext.Root(input));
        }

        // For predicates, Empty is considered true
        // https://hl7-fhir.github.io/fluentpath.html#2.1.9.3.1
        public static bool Predicate(this Invokee evaluator, IEvaluationContext context)
        {
            var result = evaluator.Select(context).BooleanEval();

            if (result == null)
                return true;
            else
                return result.Value;
        }

        public static bool Predicate(this Invokee evaluator, IEnumerable<IValueProvider> input)
        {
            return evaluator.Predicate(BaseEvaluationContext.Root(input));
        }

        public static bool IsBoolean(this Invokee evaluator, bool value, IEvaluationContext context)
        {
            var result = evaluator.Select(context).BooleanEval();

            if (result == null)
                return false;
            else
                return result.Value == value;
        }

        public static bool IsBoolean(this Invokee evaluator, bool value, IEnumerable<IValueProvider> input)
        {
            return evaluator.IsBoolean(value, BaseEvaluationContext.Root(input));
        }


        public static readonly IEnumerable<Invokee> EmptyArgs = Enumerable.Empty<Invokee>();

        public static Invokee NullProp(this Invokee source)
        {
            return (ctx, args) =>
            {
                foreach (var arg in args)
                {
                    var argValue = arg(ctx, InvokeeFactory.EmptyArgs);
                    if (!argValue.Any()) return FhirValueList.Empty;
                }

                return source(ctx, args);
            };
        }


        public static IEnumerable<IValueProvider> GetThis(IEvaluationContext context, IEnumerable<Invokee> args)
        {
            return context.GetThis();
        }

        public static IEnumerable<IValueProvider> GetContext(IEvaluationContext context, IEnumerable<Invokee> arguments)
        {
            return context.GetOriginalContext();
        }

        public static IEnumerable<IValueProvider> GetResource(IEvaluationContext context, IEnumerable<Invokee> arguments)
        {
            return context.GetResource();
        }


        public static Invokee Trace()
        {
            return (ctx, args) =>
            {
                var focus = args.First()(ctx, InvokeeFactory.EmptyArgs);
                var argValue = Typecasts.CastTo<string>(args.Skip(1).First()(ctx, InvokeeFactory.EmptyArgs));
                ctx.Trace(argValue, focus);
                return focus;
            };
        }

        public static Invokee Wrap<R>(Func<R> func)
        {
            return (ctx, args) =>
            {
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func());
            };
        }

        public static Invokee Wrap<A, R>(Func<A, R> func)
        {
            return (ctx, args) =>
            {
                var focus = Typecasts.CastTo<A>(args.First()(ctx, InvokeeFactory.EmptyArgs));
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func(focus));
            };
        }

        public static Invokee Wrap<A, B, R>(Func<A, B, R> func)
        {
            return (ctx, args) =>
            {
                var focus = Typecasts.CastTo<A>(args.First()(ctx, InvokeeFactory.EmptyArgs));
                var argA = Typecasts.CastTo<B>(args.Skip(1).First()(ctx, InvokeeFactory.EmptyArgs));
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func(focus, argA));
            };
        }

        public static Invokee Wrap<A, B, C, R>(Func<A, B, C, R> func)
        {
            return (ctx, args) =>
            {
                var focus = Typecasts.CastTo<A>(args.First()(ctx, InvokeeFactory.EmptyArgs));
                var argA = Typecasts.CastTo<B>(args.Skip(1).First()(ctx, InvokeeFactory.EmptyArgs));
                var argB = Typecasts.CastTo<C>(args.Skip(2).First()(ctx, InvokeeFactory.EmptyArgs));
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func(focus, argA, argB));
            };
        }

        public static Invokee Wrap<A, B, C, D, R>(Func<A, B, C, D, R> func)
        {
            return (ctx, args) =>
            {
                var focus = Typecasts.CastTo<A>(args.First()(ctx, InvokeeFactory.EmptyArgs));
                var argA = Typecasts.CastTo<B>(args.Skip(1).First()(ctx, InvokeeFactory.EmptyArgs));
                var argB = Typecasts.CastTo<C>(args.Skip(2).First()(ctx, InvokeeFactory.EmptyArgs));
                var argC = Typecasts.CastTo<D>(args.Skip(3).First()(ctx, InvokeeFactory.EmptyArgs));
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func(focus, argA, argB, argC));
            };
        }

        public static Invokee WrapLogic(Func<Func<bool?>, Func<bool?>, bool?> func)
        {
            return (ctx, args) =>
            {
                // Ignore focus
                var left = args.Skip(1).First();
                var right = args.Skip(2).First();

                // Return function that actually executes the Invokee at the last moment
                return Typecasts.CastTo<IEnumerable<IValueProvider>>(func(() => left(ctx, InvokeeFactory.EmptyArgs).BooleanEval(), () => right(ctx, InvokeeFactory.EmptyArgs).BooleanEval()));
            };
        }

        public static Invokee Return(Hl7.Fhir.FluentPath.IValueProvider value)
        {
            return (_, __) => (new[] { (Hl7.Fhir.FluentPath.IValueProvider)value });
        }

        public static Invokee Return(IEnumerable<Hl7.Fhir.FluentPath.IValueProvider> value)
        {
            return (_, __) => value;
        }

        public static Invokee Invoke(string functionName, IEnumerable<Invokee> arguments, Invokee invokee)
        {
            Func<IEvaluationContext, IEnumerable<IValueProvider>> boundFunc = (ctx) => invokee(ctx, arguments);

            return (ctx, _) =>
            {
                try
                {
                    return boundFunc(ctx);
                    //return invokee(ctx, arguments);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Invocation of '{0}' failed: {1}".FormatWith(functionName, e.Message));
                }
            };
        }

    }
}
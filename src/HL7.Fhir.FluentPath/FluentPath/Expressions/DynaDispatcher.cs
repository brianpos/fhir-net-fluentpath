﻿/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */
using Hl7.FluentPath.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hl7.FluentPath.Expressions
{
    internal class DynaDispatcher
    {
        public DynaDispatcher(string name, SymbolTable scope)
        {
            _scope = scope;
            _name = name;
        }


        private string _name;
        private SymbolTable _scope;

        public IEnumerable<IValueProvider> Dispatcher(Closure context, IEnumerable<Invokee> args)
        {
            var actualArgs = new List<IEnumerable<IValueProvider>>();

            var focus = args.First()(context, InvokeeFactory.EmptyArgs);
            if (!focus.Any()) return FluentValueList.Empty;

            actualArgs.Add(focus);
            var newCtx = context.Nest(focus);

            actualArgs.AddRange(args.Skip(1).Select(a => a(newCtx, InvokeeFactory.EmptyArgs)));
            if (actualArgs.Any(aa=>!aa.Any())) return FluentValueList.Empty;

            var entry = _scope.DynamicGet(_name, actualArgs);

            if (entry != null)
            {
                try
                {
                    // The Get() here should never fail, since we already know there's a (dynamic) matching candidate
                    // Need to clean up this duplicate logic later
                    return entry(context, args);
                }
                catch (TargetInvocationException tie)
                {
                    // Unwrap the very non-informative T.I.E, and throw the nested exception instead
                    throw tie.InnerException;
                }
            }
            else
            {
                //TODO: Make error reporting better
                throw Error.Argument(noMatchError(actualArgs));
            }
        }


        private string noMatchError(IEnumerable<object> arguments)
        {
            string result;

            if (!arguments.Any())
                return "(no signature)";

            result = "on focus of type '{0}'".FormatWith(Typecasts.ReadableFluentPathName(arguments.First().GetType()));
            
            if(arguments.Skip(1).Any())
            {
                result = "with parameters of type '{0}' "
                        .FormatWith(String.Join(",", arguments.Skip(1).Select(a => Typecasts.ReadableFluentPathName(a.GetType()))), result);
            }

            return "Function cannot be called " + result;
        }     
    }
}

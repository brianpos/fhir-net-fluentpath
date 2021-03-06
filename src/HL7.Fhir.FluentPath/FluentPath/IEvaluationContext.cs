﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7.Fhir.FluentPath
{
    public interface IEvaluationContext
    {
        /// <summary>
        /// Invoked whenever a log() function is encountered in an expression.
        /// </summary>
        /// <param name="argument">The parameter passed to the FhirPath log() function</param>
        /// <param name="focus">The focus at the moment the log() function was called</param>
        void Log(string argument, IEnumerable<IFluentPathValue> focus);

        /// <summary>
        /// Whenever the engine encountes an unknown function, it will call this method to try to invoke it externally
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters">An ordered set parameters, each a collection of IFhirPathValues representing the value of that parameter</param>
        /// <remarks>Should throw NotSupportedException if the external function is not supported.</remarks>
        void InvokeExternalFunction(string name, IList<IEnumerable<IFluentPathValue>> parameters);

        /// <summary>
        /// Provide a value when a constant expression (%name) is encountered in an expression
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Return null when the constant is not known</returns>
        IFluentPathValue ResolveConstant(string name);

        IEnumerable<IFluentPathValue> OriginalContext { get; set; }

        IFluentPathElement OriginalResource { get; }
    }
}
